using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

public record GuestRequest(string? Device, string? GameVersion);
public record SignInRequest(string? Provider, string? IdToken, string? Nonce, string? Device);
public record RefreshRequest(string? RefreshToken);
public record PatchMeRequest(int? BirthYear, string? Language, string? Country);

/// <summary>Guest accounts, Apple and Google sign-in, linking, sessions and account deletion.</summary>
public static partial class PlayerEndpoints
{
    public static void Map(RouteGroupBuilder game, RouteGroupBuilder me)
    {
        game.MapPost("/players/guest", Guest).RequireRateLimiting(RateLimits.Auth);
        game.MapPost("/players/sign-in", SignIn).RequireRateLimiting(RateLimits.Auth);
        game.MapPost("/players/refresh", Refresh).RequireRateLimiting(RateLimits.Auth);

        me.MapGet("", (HttpContext http, AccountService accounts, CancellationToken ct) =>
            accounts.AccountAsync(http.Player(), ct));
        me.MapPatch("", Patch);
        me.MapDelete("", Delete);
        me.MapPost("/link", Link);
    }

    // POST /players/sign-out sits outside /players/me but needs the same signed-in player.
    public static void MapSignOut(RouteGroupBuilder signedIn) => signedIn.MapPost("/players/sign-out", SignOut);

    static async Task<IResult> Guest(GuestRequest? request, HttpContext http, AccountService accounts, CancellationToken ct)
    {
        var game = http.Game();
        var player = accounts.NewPlayer(game, request?.Device);
        return Results.Ok(await accounts.StartSessionAsync(game, player, request?.Device, null, ct));
    }

    static async Task<IResult> SignIn(SignInRequest request, HttpContext http, PlatformDb db,
        IdentityVerifier verifier, AccountService accounts, CancellationToken ct)
    {
        var game = http.Game();
        var check = await verifier.VerifyAsync(game, request.Provider, request.IdToken, request.Nonce, ct);
        if (check.Error is not null) return IdentityError(check.Error);
        string provider = request.Provider!, subject = check.Subject!;

        for (int attempt = 0; ; attempt++)
        {
            var identity = await db.Identities.FirstOrDefaultAsync(
                i => i.GameId == game.Id && i.Provider == provider && i.Subject == subject, ct);
            if (identity is not null)
            {
                var existing = await db.Players.FirstAsync(p => p.Id == identity.PlayerId, ct);
                return Results.Ok(await accounts.StartSessionAsync(game, existing, request.Device, null, ct));
            }

            // A new identity: a new player. Two phones signing in at the same moment can race; the unique index decides.
            var player = accounts.NewPlayer(game, request.Device);
            db.Identities.Add(new PlayerIdentity { GameId = game.Id, PlayerId = player.Id, Provider = provider, Subject = subject });
            try
            {
                return Results.Ok(await accounts.StartSessionAsync(game, player, request.Device, null, ct));
            }
            catch (DbUpdateException e) when (IsUniqueViolation(e) && attempt == 0)
            {
                db.ChangeTracker.Clear();
            }
        }
    }

    static async Task<IResult> Link(SignInRequest request, HttpContext http, PlatformDb db,
        IdentityVerifier verifier, AccountService accounts, CancellationToken ct)
    {
        var game = http.Game();
        var player = http.Player();
        var check = await verifier.VerifyAsync(game, request.Provider, request.IdToken, request.Nonce, ct);
        if (check.Error is not null) return IdentityError(check.Error);
        string provider = request.Provider!, subject = check.Subject!;

        var owner = await db.Identities.FirstOrDefaultAsync(
            i => i.GameId == game.Id && i.Provider == provider && i.Subject == subject, ct);
        if (owner?.PlayerId == player.Id)
            return Results.Ok(new { account = await accounts.AccountAsync(player, ct) });
        if (owner is not null)
        {
            // The identity already has its own progress. The game asks which one to keep.
            return Results.Json(new
            {
                error = "identity_taken",
                other = new { playerId = owner.PlayerId, save = await accounts.SaveInfoAsync(owner.PlayerId, ct) },
            }, statusCode: StatusCodes.Status409Conflict);
        }
        if (await db.Identities.AnyAsync(i => i.PlayerId == player.Id && i.Provider == provider, ct))
            return RequestContext.Error("provider_already_linked", StatusCodes.Status409Conflict);

        db.Identities.Add(new PlayerIdentity { GameId = game.Id, PlayerId = player.Id, Provider = provider, Subject = subject });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (IsUniqueViolation(e))
        {
            return RequestContext.Error("identity_taken", StatusCodes.Status409Conflict);
        }
        return Results.Ok(new { account = await accounts.AccountAsync(player, ct) });
    }

    static async Task<IResult> Refresh(RefreshRequest request, HttpContext http, PlatformDb db,
        AccountService accounts, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        if (string.IsNullOrEmpty(request.RefreshToken)) return RequestContext.Error("invalid_refresh", 401);
        var now = clock.GetUtcNow();
        var hash = TokenService.Hash(request.RefreshToken);
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.TokenHash == hash && s.GameId == game.Id, ct);
        if (session is null) return RequestContext.Error("invalid_refresh", 401);

        // Each refresh token works once. If a used one comes back, it was copied: end every session it led to.
        int claimed = session.ReplacedAt is null && session.RevokedAt is null && session.ExpiresAt > now
            ? await db.Sessions.Where(s => s.Id == session.Id && s.ReplacedAt == null && s.RevokedAt == null)
                .ExecuteUpdateAsync(u => u.SetProperty(s => s.ReplacedAt, now).SetProperty(s => s.RevokedAt, now), ct)
            : 0;
        if (claimed == 0)
        {
            // Already swapped, or swapped by another request a moment ago.
            if (session.ReplacedAt is not null || (session.RevokedAt is null && session.ExpiresAt > now))
                await db.Sessions.Where(s => s.FamilyId == session.FamilyId && s.RevokedAt == null)
                    .ExecuteUpdateAsync(u => u.SetProperty(s => s.RevokedAt, now), ct);
            return RequestContext.Error("invalid_refresh", 401);
        }

        var player = await db.Players.FirstOrDefaultAsync(p => p.Id == session.PlayerId && p.DeletionRequestedAt == null, ct);
        if (player is null) return RequestContext.Error("invalid_refresh", 401);
        return Results.Ok(await accounts.StartSessionAsync(game, player, session.Device, session.FamilyId, ct));
    }

    static async Task<IResult> SignOut(RefreshRequest? request, HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var player = http.Player();
        if (!string.IsNullOrEmpty(request?.RefreshToken))
        {
            var hash = TokenService.Hash(request.RefreshToken);
            var now = clock.GetUtcNow();
            await db.Sessions.Where(s => s.TokenHash == hash && s.PlayerId == player.Id && s.RevokedAt == null)
                .ExecuteUpdateAsync(u => u.SetProperty(s => s.RevokedAt, now), ct);
        }
        return Results.NoContent();
    }

    static async Task<IResult> Patch(PatchMeRequest request, HttpContext http, PlatformDb db,
        AccountService accounts, TimeProvider clock, CancellationToken ct)
    {
        var player = http.Player();
        if (request.BirthYear is { } year)
        {
            if (year < 1900 || year > clock.GetUtcNow().Year) return RequestContext.Error("invalid_birth_year");
            player.BirthYear = year;
        }
        if (request.Language is { } language)
        {
            if (!LanguagePattern().IsMatch(language)) return RequestContext.Error("invalid_language");
            player.Language = language;
        }
        if (request.Country is { } country)
        {
            if (!CountryPattern().IsMatch(country)) return RequestContext.Error("invalid_country");
            player.Country = country;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(await accounts.AccountAsync(player, ct));
    }

    /// <summary>
    /// Deleting is immediate for the player: every session ends and the Apple or Google id is released, so it
    /// can start fresh. The data itself is purged by <see cref="AccountPurger"/> within the grace period (at most 30 days).
    /// </summary>
    static async Task<IResult> Delete(HttpContext http, PlatformDb db, IConfiguration config,
        IAppleTokenRevoker apple, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var player = http.Player();
        var now = clock.GetUtcNow();
        player.DeletionRequestedAt = now;
        player.DeleteBy = now + AccountPurger.GracePeriod(config);

        await db.Sessions.Where(s => s.PlayerId == player.Id && s.RevokedAt == null)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.RevokedAt, now), ct);
        var identities = await db.Identities.Where(i => i.PlayerId == player.Id).ToListAsync(ct);
        foreach (var identity in identities.Where(i => i.Provider == Providers.Apple))
            await apple.RevokeAsync(game, identity.Subject, ct);
        db.Identities.RemoveRange(identities);
        await db.SaveChangesAsync(ct);
        return Results.Accepted(value: new { deleteBy = player.DeleteBy });
    }

    static IResult IdentityError(string error) =>
        RequestContext.Error(error, error == "invalid_token" ? StatusCodes.Status401Unauthorized : StatusCodes.Status400BadRequest);

    internal static bool IsUniqueViolation(DbUpdateException e) =>
        e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z]{2,4})?$")]
    private static partial Regex LanguagePattern();

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryPattern();
}
