using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

public record AccountDto(
    Guid PlayerId, IReadOnlyList<string> Providers, int? BirthYear, string AgeBand,
    string? Country, string? Language, DateTimeOffset CreatedAt, DateTimeOffset? DeletionRequestedAt);

public record AuthResponse(Guid PlayerId, string AccessToken, DateTimeOffset AccessExpiresAt, string RefreshToken, AccountDto Account);

public record SaveSummaryDto(int? Level, int? Chapter);

/// <summary>A save's version and where it came from, shown when two phones disagree.</summary>
public record SaveInfo(long Version, SaveSummaryDto Summary, DateTimeOffset UpdatedAt, string? Device)
{
    public static SaveInfo From(PlayerSave s) => new(s.Version, new SaveSummaryDto(s.Level, s.Chapter), s.UpdatedAt, s.Device);
}

public static class AgeBands
{
    public const string Under13 = "under13", From13To15 = "13to15", From16 = "16plus", Unknown = "unknown";

    /// <summary>
    /// The game asks for the birth year only, so the birthday may not have come yet this year.
    /// Count the younger possibility (year - birthYear - 1), so a child is never placed in an older band.
    /// </summary>
    public static string For(int? birthYear, int currentYear)
    {
        if (birthYear is not { } year) return Unknown;
        int age = currentYear - year - 1;
        return age < 13 ? Under13 : age < 16 ? From13To15 : From16;
    }
}

/// <summary>Creates players, starts sessions and builds the account the game sees.</summary>
public sealed class AccountService(PlatformDb db, TokenService tokens, TimeProvider clock)
{
    public Player NewPlayer(Game game, string? device)
    {
        var now = clock.GetUtcNow();
        var player = new Player { GameId = game.Id, CreatedDevice = Trim(device, 200), CreatedAt = now, LastSeenAt = now };
        db.Players.Add(player);
        return player;
    }

    /// <summary>Starts a session: a 1-hour access token and a 180-day refresh token. Saves changes.</summary>
    public async Task<AuthResponse> StartSessionAsync(Game game, Player player, string? device, Guid? familyId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var refresh = TokenService.NewRefreshToken();
        db.Sessions.Add(new PlayerSession
        {
            GameId = game.Id,
            PlayerId = player.Id,
            FamilyId = familyId ?? Guid.NewGuid(),
            TokenHash = TokenService.Hash(refresh),
            Device = Trim(device, 200),
            CreatedAt = now,
            ExpiresAt = now + TokenService.RefreshLifetime,
        });
        player.LastSeenAt = now;
        await db.SaveChangesAsync(ct);

        var (access, expires) = tokens.PlayerAccess(player.Id, game.Slug);
        return new AuthResponse(player.Id, access, expires, refresh, await AccountAsync(player, ct));
    }

    public async Task<AccountDto> AccountAsync(Player player, CancellationToken ct)
    {
        var providers = await db.Identities.Where(i => i.PlayerId == player.Id)
            .OrderBy(i => i.Provider).Select(i => i.Provider).ToListAsync(ct);
        return new AccountDto(player.Id, providers, player.BirthYear,
            AgeBands.For(player.BirthYear, clock.GetUtcNow().Year),
            player.Country, player.Language, player.CreatedAt, player.DeletionRequestedAt);
    }

    public async Task<SaveInfo?> SaveInfoAsync(Guid playerId, CancellationToken ct)
    {
        var save = await db.Saves.AsNoTracking().FirstOrDefaultAsync(s => s.PlayerId == playerId, ct);
        return save is null ? null : SaveInfo.From(save);
    }

    public static string? Trim(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= max ? value.Trim() : value[..max].Trim();
}
