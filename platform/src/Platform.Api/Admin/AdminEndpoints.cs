using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Api.Players;
using Platform.Domain;

namespace Platform.Api.Admin;

public record AdminSignInRequest(string? Email, string? Password);
public record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
public record GrantRequest(string? Currency, long Amount, string? Reason);
public record CloseTicketRequest(string? Note);
public record StaffRequest(string? Email, string? Role);

/// <summary>
/// The API behind the admin panel (COMPLETE_SPEC.md section 16): staff sign in, look up players, grant
/// compensation, answer support and manage who can do what. Every write on a game goes into its audit log.
/// </summary>
public static partial class AdminEndpoints
{
    public const long MaxGrant = 1_000_000;

    public static void MapStaff(WebApplication app)
    {
        app.MapPost("/admin/sign-in", SignIn).RequireRateLimiting(RateLimits.Auth);
        var me = app.MapGroup("/admin/me").RequireAuthorization(AuthSchemes.Admin)
            .AddEndpointFilter(new StaffFilter(null, allowPasswordChangeDue: true));
        me.MapGet("", (HttpContext http, StaffAccounts staff, CancellationToken ct) => staff.DtoAsync(http.Admin(), ct));
        me.MapPost("/password", ChangePassword);
    }

    public static void MapGame(RouteGroupBuilder game)
    {
        var admin = game.MapGroup("/admin").RequireAuthorization(AuthSchemes.Admin);
        admin.MapGet("/players", FindPlayers).AddEndpointFilter(new StaffFilter(StaffRole.Viewer));
        admin.MapGet("/players/{id:guid}", PlayerDetail).AddEndpointFilter(new StaffFilter(StaffRole.Viewer));
        admin.MapPost("/players/{id:guid}/grants", Grant).AddEndpointFilter(new StaffFilter(StaffRole.Support));
        admin.MapGet("/support", Tickets).AddEndpointFilter(new StaffFilter(StaffRole.Support));
        admin.MapPost("/support/{id:guid}/close", CloseTicket).AddEndpointFilter(new StaffFilter(StaffRole.Support));
        admin.MapGet("/staff", Staff).AddEndpointFilter(new StaffFilter(StaffRole.Owner));
        admin.MapPut("/staff", SetStaff).AddEndpointFilter(new StaffFilter(StaffRole.Owner));
        admin.MapDelete("/staff/{adminId:guid}", RemoveStaff).AddEndpointFilter(new StaffFilter(StaffRole.Owner));
        admin.MapGet("/audit", Audit).AddEndpointFilter(new StaffFilter(StaffRole.Owner));
    }

    static async Task<IResult> SignIn(AdminSignInRequest request, StaffAccounts staff, TokenService tokens, CancellationToken ct)
    {
        var admin = await staff.CheckPasswordAsync(request.Email ?? "", request.Password ?? "", ct);
        if (admin is null) return RequestContext.Error("invalid_credentials", StatusCodes.Status401Unauthorized);
        var (token, expires) = tokens.AdminAccess(admin.Id);
        return Results.Ok(new { accessToken = token, expiresAt = expires, admin = await staff.DtoAsync(admin, ct) });
    }

    static async Task<IResult> ChangePassword(ChangePasswordRequest request, HttpContext http, PlatformDb db,
        StaffAccounts staff, CancellationToken ct)
    {
        var admin = http.Admin();
        if (!staff.PasswordMatches(admin, request.CurrentPassword ?? ""))
            return RequestContext.Error("invalid_credentials", StatusCodes.Status401Unauthorized);
        if ((request.NewPassword ?? "").Length < StaffAccounts.MinPasswordLength || request.NewPassword == request.CurrentPassword)
            return RequestContext.Error("weak_password");
        staff.SetPassword(admin, request.NewPassword!, temporary: false);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // By player id, or by an Apple or Google subject. With no query, the newest players.
    static async Task<IResult> FindPlayers(string? query, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var players = db.Players.Where(p => p.GameId == game.Id);
        query = query?.Trim();
        if (Guid.TryParse(query, out var id)) players = players.Where(p => p.Id == id);
        else if (!string.IsNullOrEmpty(query))
            players = players.Where(p => db.Identities.Any(i => i.PlayerId == p.Id && i.Subject == query));

        var rows = await players.OrderByDescending(p => p.CreatedAt).Take(50)
            .Select(p => new
            {
                playerId = p.Id,
                providers = db.Identities.Where(i => i.PlayerId == p.Id).Select(i => i.Provider).ToList(),
                createdAt = p.CreatedAt,
                lastSeenAt = p.LastSeenAt,
                level = db.Saves.Where(s => s.PlayerId == p.Id).Select(s => s.Level).FirstOrDefault(),
                chapter = db.Saves.Where(s => s.PlayerId == p.Id).Select(s => s.Chapter).FirstOrDefault(),
                deletionRequestedAt = p.DeletionRequestedAt,
            })
            .ToListAsync(ct);
        return Results.Ok(rows);
    }

    static async Task<IResult> PlayerDetail(Guid id, HttpContext http, PlatformDb db, AccountService accounts,
        WalletService wallets, CancellationToken ct)
    {
        var game = http.Game();
        var player = await db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.GameId == game.Id, ct);
        if (player is null) return RequestContext.Error("unknown_player", StatusCodes.Status404NotFound);

        var identities = await db.Identities.Where(i => i.PlayerId == id).OrderBy(i => i.Provider).ToListAsync(ct);
        return Results.Ok(new
        {
            account = await accounts.AccountAsync(player, ct),
            identities = identities.Select(i => new { provider = i.Provider, subject = Mask(i.Subject), linkedAt = i.LinkedAt }),
            save = await accounts.SaveInfoAsync(id, ct),
            flags = await db.SaveFlags.Where(f => f.PlayerId == id).OrderByDescending(f => f.CreatedAt).Take(100)
                .Select(f => new { f.Reason, f.Details, f.FromVersion, f.ToVersion, f.CreatedAt }).ToListAsync(ct),
            wallet = await wallets.BalancesAsync(id, ct),
            ledger = await db.WalletEntries.Where(w => w.PlayerId == id).OrderByDescending(w => w.CreatedAt).Take(100)
                .Select(w => new { w.Currency, w.Amount, w.Source, w.Reason, w.PurchaseId, w.AdminUserId, w.CreatedAt })
                .ToListAsync(ct),
            purchases = await db.Purchases.Where(p => p.PlayerId == id).OrderByDescending(p => p.PurchasedAt)
                .Select(p => new { p.Id, p.Store, p.StoreTransactionId, p.Sku, p.Price, p.Currency, p.Status, p.PurchasedAt })
                .ToListAsync(ct),
            tickets = await db.SupportTickets.Where(t => t.PlayerId == id).OrderByDescending(t => t.CreatedAt)
                .Select(t => new { t.Id, t.Message, t.Email, t.Status, t.CreatedAt, t.ClosedAt, t.CloseNote })
                .ToListAsync(ct),
            deletion = player.DeletionRequestedAt is null ? null
                : new { requestedAt = player.DeletionRequestedAt, deleteBy = player.DeleteBy },
        });
    }

    // Compensation, or fixing a wallet by taking some back. Never below zero.
    static async Task<IResult> Grant(Guid id, GrantRequest request, HttpContext http, PlatformDb db,
        WalletService wallets, StaffAccounts staff, CancellationToken ct)
    {
        var game = http.Game();
        var admin = http.Admin();
        var reason = request.Reason?.Trim();
        if (request.Currency is null || !CurrencyPattern().IsMatch(request.Currency)) return RequestContext.Error("invalid_currency");
        if (request.Amount == 0 || Math.Abs(request.Amount) > MaxGrant) return RequestContext.Error("invalid_amount");
        if (string.IsNullOrEmpty(reason) || reason.Length > 500) return RequestContext.Error("reason_required");
        if (!await db.Players.AnyAsync(p => p.Id == id && p.GameId == game.Id && p.DeletionRequestedAt == null, ct))
            return RequestContext.Error("unknown_player", StatusCodes.Status404NotFound);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            staff.Audit(game.Id, admin, "wallet.grant", $"player:{id}", new { request.Currency, request.Amount, reason });
            await wallets.AddAsync(game.Id, id, request.Currency, request.Amount, "admin", reason, null, admin.Id, ct);
        }
        catch (WalletOverdrawnException)
        {
            return RequestContext.Error("insufficient_balance");
        }
        await tx.CommitAsync(ct);
        return Results.Ok(new { wallet = await wallets.BalancesAsync(id, ct) });
    }

    static async Task<IResult> Tickets(string? status, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var wanted = string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase) ? TicketStatus.Closed : TicketStatus.Open;
        var game = http.Game();
        return Results.Ok(await db.SupportTickets.Where(t => t.GameId == game.Id && t.Status == wanted)
            .OrderByDescending(t => t.CreatedAt).Take(200)
            .Select(t => new
            {
                t.Id, t.PlayerId, t.Message, t.Email, t.GameVersion, t.Device, t.Language,
                t.Status, t.CreatedAt, t.ClosedAt, t.CloseNote,
            })
            .ToListAsync(ct));
    }

    static async Task<IResult> CloseTicket(Guid id, CloseTicketRequest? request, HttpContext http, PlatformDb db,
        StaffAccounts staff, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.GameId == game.Id, ct);
        if (ticket is null) return RequestContext.Error("unknown_ticket", StatusCodes.Status404NotFound);
        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = clock.GetUtcNow();
        ticket.ClosedBy = http.Admin().Id;
        ticket.CloseNote = AccountService.Trim(request?.Note, 2000);
        staff.Audit(game.Id, http.Admin(), "support.close", $"ticket:{id}", new { note = ticket.CloseNote });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { ticket.Id, ticket.Status, ticket.ClosedAt });
    }

    static async Task<IResult> Staff(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var rows = await db.AdminRoles.Where(r => r.GameId == game.Id)
            .Join(db.AdminUsers, r => r.AdminUserId, a => a.Id, (r, a) => new { a.Id, a.Email, r.Role, a.Disabled })
            .OrderBy(r => r.Email).ToListAsync(ct);
        return Results.Ok(rows.Select(r => new { r.Id, r.Email, role = StaffAccounts.RoleName(r.Role), r.Disabled }));
    }

    // Gives someone a role on this game. A new email gets an account and a temporary password, shown only here.
    static async Task<IResult> SetStaff(StaffRequest request, HttpContext http, PlatformDb db, StaffAccounts staff, CancellationToken ct)
    {
        var game = http.Game();
        var email = StaffAccounts.Normalize(request.Email);
        if (!email.Contains('@') || email.Length > 320) return RequestContext.Error("invalid_email");
        if (StaffAccounts.ParseRole(request.Role) is not { } role) return RequestContext.Error("invalid_role");

        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Email == email, ct);
        string? temporaryPassword = null;
        if (admin is null) (admin, temporaryPassword) = staff.Create(email);

        var current = await db.AdminRoles.FirstOrDefaultAsync(r => r.AdminUserId == admin.Id && r.GameId == game.Id, ct);
        if (current?.Role == StaffRole.Owner && role != StaffRole.Owner && await IsLastOwnerAsync(db, game.Id, ct))
            return RequestContext.Error("last_owner");
        if (current is null) db.AdminRoles.Add(new AdminRole { AdminUserId = admin.Id, GameId = game.Id, Role = role });
        else current.Role = role;

        staff.Audit(game.Id, http.Admin(), "staff.set", $"admin:{admin.Id}", new { email, role = StaffAccounts.RoleName(role) });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = admin.Id, email, role = StaffAccounts.RoleName(role), temporaryPassword });
    }

    static async Task<IResult> RemoveStaff(Guid adminId, HttpContext http, PlatformDb db, StaffAccounts staff, CancellationToken ct)
    {
        var game = http.Game();
        var role = await db.AdminRoles.FirstOrDefaultAsync(r => r.AdminUserId == adminId && r.GameId == game.Id, ct);
        if (role is null) return RequestContext.Error("unknown_staff", StatusCodes.Status404NotFound);
        if (role.Role == StaffRole.Owner && await IsLastOwnerAsync(db, game.Id, ct)) return RequestContext.Error("last_owner");
        db.AdminRoles.Remove(role);
        staff.Audit(game.Id, http.Admin(), "staff.remove", $"admin:{adminId}");
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    static async Task<IResult> Audit(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        return Results.Ok(await db.Audit.Where(a => a.GameId == game.Id).OrderByDescending(a => a.CreatedAt).Take(200)
            .Join(db.AdminUsers, a => a.AdminUserId, u => u.Id,
                (a, u) => new { a.Id, admin = u.Email, a.Action, a.Target, a.Details, a.CreatedAt })
            .ToListAsync(ct));
    }

    static async Task<bool> IsLastOwnerAsync(PlatformDb db, Guid gameId, CancellationToken ct) =>
        await db.AdminRoles.CountAsync(r => r.GameId == gameId && r.Role == StaffRole.Owner, ct) <= 1;

    // Support sees enough to match a player's report, not the full Apple or Google id.
    static string Mask(string subject) => subject.Length <= 10 ? "…" : $"{subject[..4]}…{subject[^4..]}";

    [GeneratedRegex("^[a-z][a-z0-9_]{0,31}$")]
    private static partial Regex CurrencyPattern();
}
