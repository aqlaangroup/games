using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Admin;

public record AdminDto(Guid Id, string Email, bool MustChangePassword, bool TwoFactorEnabled, Dictionary<string, string> Roles);

/// <summary>Staff accounts: passwords, temporary passwords, roles and the audit log.</summary>
public sealed class StaffAccounts(PlatformDb db, TimeProvider clock)
{
    public const int MinPasswordLength = 12;
    readonly PasswordHasher<AdminUser> hasher = new();

    public static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();

    public static string RoleName(StaffRole role) => role.ToString().ToLowerInvariant();

    public static StaffRole? ParseRole(string? role) =>
        Enum.TryParse<StaffRole>(role, ignoreCase: true, out var r) && Enum.IsDefined(r) ? r : null;

    public async Task<AdminUser?> CheckPasswordAsync(string email, string password, CancellationToken ct)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Email == Normalize(email) && !a.Disabled, ct);
        if (admin is null) return null;
        var result = hasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            admin.PasswordHash = hasher.HashPassword(admin, password);
            await db.SaveChangesAsync(ct);
        }
        return admin;
    }

    public bool PasswordMatches(AdminUser admin, string password) =>
        hasher.VerifyHashedPassword(admin, admin.PasswordHash, password) != PasswordVerificationResult.Failed;

    public void SetPassword(AdminUser admin, string password, bool temporary)
    {
        admin.PasswordHash = hasher.HashPassword(admin, password);
        admin.MustChangePassword = temporary;
    }

    /// <summary>Creates a staff account with a temporary password, shown once.</summary>
    public (AdminUser Admin, string Password) Create(string email)
    {
        var admin = new AdminUser { Email = Normalize(email), PasswordHash = "", CreatedAt = clock.GetUtcNow() };
        var password = TemporaryPassword();
        SetPassword(admin, password, temporary: true);
        db.AdminUsers.Add(admin);
        return (admin, password);
    }

    public static string TemporaryPassword() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(15));

    public async Task<AdminDto> DtoAsync(AdminUser admin, CancellationToken ct)
    {
        var roles = await db.AdminRoles.Where(r => r.AdminUserId == admin.Id)
            .Join(db.Games, r => r.GameId, g => g.Id, (r, g) => new { g.Slug, r.Role })
            .ToListAsync(ct);
        return new AdminDto(admin.Id, admin.Email, admin.MustChangePassword, admin.TwoFactorEnabled,
            roles.ToDictionary(r => r.Slug, r => RoleName(r.Role)));
    }

    /// <summary>Records a staff write on a game. Saved with the caller's next SaveChanges.</summary>
    public void Audit(Guid gameId, AdminUser admin, string action, string target, object? details = null) =>
        db.Audit.Add(new AuditEntry
        {
            GameId = gameId, AdminUserId = admin.Id, Action = action, Target = target,
            Details = details is null ? null : JsonSerializer.Serialize(details, JsonSerializerOptions.Web),
            CreatedAt = clock.GetUtcNow(),
        });

    /// <summary>
    /// `dotnet run -- create-owner email`: makes the account an owner of every registered game and gives it a new
    /// temporary password. Run it once to set up the first owner; owners add everyone else from the admin API.
    /// </summary>
    public async Task<string> CreateOwnerAsync(string email, CancellationToken ct)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Email == Normalize(email), ct);
        string password;
        if (admin is null) (admin, password) = Create(email);
        else
        {
            password = TemporaryPassword();
            SetPassword(admin, password, temporary: true);
            admin.Disabled = false;
        }

        foreach (var gameId in await db.Games.Select(g => g.Id).ToListAsync(ct))
        {
            var role = await db.AdminRoles.FirstOrDefaultAsync(r => r.AdminUserId == admin.Id && r.GameId == gameId, ct);
            if (role is null) db.AdminRoles.Add(new AdminRole { AdminUserId = admin.Id, GameId = gameId, Role = StaffRole.Owner });
            else role.Role = StaffRole.Owner;
        }
        await db.SaveChangesAsync(ct);
        return password;
    }
}
