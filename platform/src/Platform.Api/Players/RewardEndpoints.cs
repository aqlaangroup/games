using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Api.Games;
using Platform.Domain;

namespace Platform.Api.Players;

public record RewardRequest(string? Id);
public record SpendRequest(string? Item, string? RequestId);

/// <summary>The player's wallet, gems the game earned (each reward once), and gems spent (each request once).</summary>
public static class RewardEndpoints
{
    public static void Map(RouteGroupBuilder me)
    {
        me.MapGet("/wallet", Wallet);
        me.MapPost("/rewards", Claim);
        me.MapPost("/spend", Spend);
    }

    static async Task<IResult> Wallet(HttpContext http, WalletService wallets, CancellationToken ct) =>
        Results.Ok(new { wallet = await wallets.BalancesAsync(http.Player().Id, ct) });

    // 409 not_earned_yet when the latest cloud save doesn't show it yet: the game uploads its save, then asks again.
    static async Task<IResult> Claim(RewardRequest request, HttpContext http, PlatformDb db, WalletService wallets,
        TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var player = http.Player();
        var id = request.Id?.Trim() ?? "";
        using var balance = await GameBalance.ActiveAsync(db, game.Id, ct);
        if (balance is null) return RequestContext.Error("no_config", StatusCodes.Status503ServiceUnavailable);
        var save = await db.Saves.AsNoTracking().FirstOrDefaultAsync(s => s.PlayerId == player.Id, ct);
        var (gems, error) = LiveRewards.Handles(id)
            ? await LiveRewards.RewardAsync(id, balance.RootElement, db, game.Id, player.Id, clock.GetUtcNow(), ct)
            : RewardRules.Reward(id, balance.RootElement, save);
        if (error == "not_earned_yet") return RequestContext.Error(error, StatusCodes.Status409Conflict);
        if (error is not null) return RequestContext.Error(error);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.RewardClaims.Add(new RewardClaim { GameId = game.Id, PlayerId = player.Id, RewardId = id, Gems = gems, ClaimedAt = clock.GetUtcNow() });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (PlayerEndpoints.IsUniqueViolation(e))
        {
            await tx.RollbackAsync(ct);
            return Results.Ok(new { alreadyClaimed = true, wallet = await wallets.BalancesAsync(player.Id, ct) });
        }
        if (gems > 0) await wallets.AddAsync(game.Id, player.Id, "gems", gems, "reward", id, null, null, ct);
        await tx.CommitAsync(ct);
        return Results.Ok(new { granted = new { gems }, wallet = await wallets.BalancesAsync(player.Id, ct) });
    }

    // The game applies "grant" (more energy, a storage slot...) only after this reply, never before.
    static async Task<IResult> Spend(SpendRequest request, HttpContext http, PlatformDb db, WalletService wallets,
        TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var player = http.Player();
        var item = request.Item?.Trim() ?? "";
        var requestId = request.RequestId?.Trim();
        if (string.IsNullOrEmpty(requestId) || requestId.Length > 64) return RequestContext.Error("invalid_request_id");

        var earlier = await db.GemSpends.AsNoTracking().FirstOrDefaultAsync(s => s.PlayerId == player.Id && s.RequestId == requestId, ct);
        if (earlier is not null) return await Repeat(earlier, wallets, player.Id, ct);

        using var balance = await GameBalance.ActiveAsync(db, game.Id, ct);
        if (balance is null) return RequestContext.Error("no_config", StatusCodes.Status503ServiceUnavailable);
        var now = clock.GetUtcNow();
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        int refillsToday = await db.GemSpends.CountAsync(s => s.PlayerId == player.Id && s.Item == "energyRefill" && s.CreatedAt >= today, ct);
        var (gems, grant, error) = RewardRules.Spend(item, balance.RootElement, refillsToday);
        if (error is not null) return RequestContext.Error(error);
        if (item.StartsWith("storageSlot:") && await db.GemSpends.AnyAsync(s => s.PlayerId == player.Id && s.Item == item, ct))
            return RequestContext.Error("already_owned", StatusCodes.Status409Conflict);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var spend = new GemSpend
        {
            GameId = game.Id, PlayerId = player.Id, RequestId = requestId, Item = item, Gems = gems,
            GrantJson = JsonSerializer.Serialize(grant), CreatedAt = now,
        };
        db.GemSpends.Add(spend);
        try
        {
            await db.SaveChangesAsync(ct);
            await wallets.AddAsync(game.Id, player.Id, "gems", -gems, "spend", item, null, null, ct);
        }
        catch (DbUpdateException e) when (PlayerEndpoints.IsUniqueViolation(e))
        {
            // The same request arrived twice at once; the other one spent.
            await tx.RollbackAsync(ct);
            return Results.Ok(new { alreadySpent = true, wallet = await wallets.BalancesAsync(player.Id, ct) });
        }
        catch (WalletOverdrawnException)
        {
            await tx.RollbackAsync(ct);
            return RequestContext.Error("not_enough_gems", StatusCodes.Status409Conflict);
        }
        await tx.CommitAsync(ct);
        return Results.Ok(new { item, gems, grant, wallet = await wallets.BalancesAsync(player.Id, ct) });
    }

    static async Task<IResult> Repeat(GemSpend earlier, WalletService wallets, Guid playerId, CancellationToken ct)
    {
        using var grant = JsonDocument.Parse(earlier.GrantJson);
        return Results.Ok(new
        {
            alreadySpent = true, item = earlier.Item, gems = earlier.Gems, grant = grant.RootElement.Clone(),
            wallet = await wallets.BalancesAsync(playerId, ct),
        });
    }
}
