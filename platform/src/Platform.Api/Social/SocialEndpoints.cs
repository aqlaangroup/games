using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Api.Games;
using Platform.Api.Players;
using Platform.Domain;

namespace Platform.Api.Social;

public record FriendRequest(string? Code);
public record GiftRequest(Guid To);

/// <summary>
/// Friends, energy gifts and café visits (SYSTEMS_B.md section 7; games/gahwit-sitti/missing/20). Friends add each
/// other by code; there is no chat. Players under 13 or of unknown age get 403 parent_needed until a parent agrees.
/// </summary>
public static class SocialEndpoints
{
    public static void Map(RouteGroupBuilder me)
    {
        var social = me.MapGroup("").AddEndpointFilter(new SocialFilter());
        social.MapGet("/social", Summary);
        social.MapPost("/friends", AddFriend);
        social.MapDelete("/friends/{friendId:guid}", RemoveFriend);
        social.MapPost("/gifts", SendGift);
        social.MapPost("/gifts/claim", ClaimGifts);
        social.MapGet("/friends/{friendId:guid}/cafe", Visit);
    }

    sealed class SocialFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var clock = http.RequestServices.GetRequiredService<TimeProvider>();
            if (!SocialRules.Allowed(http.Player(), clock.GetUtcNow())) return RequestContext.Error("parent_needed", StatusCodes.Status403Forbidden);
            return await next(context);
        }
    }

    // The player's own code, friends, gifts waiting and gifts left to send today.
    static async Task<IResult> Summary(HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var me = await db.Players.FirstAsync(p => p.Id == http.Player().Id, ct);
        if (me.FriendCode is null) await GiveCodeAsync(db, me, ct);
        using var balance = await GameBalance.ActiveAsync(db, game.Id, ct);
        int perDay = (int)SocialRules.Number(balance?.RootElement ?? default, 5, "social", "giftsPerDay");
        string today = SocialRules.Day(clock.GetUtcNow());
        var sentToday = await db.Gifts.Where(g => g.FromPlayerId == me.Id && g.Day == today).Select(g => g.ToPlayerId).ToListAsync(ct);
        var friends = await db.Friendships.AsNoTracking().Where(f => f.PlayerId == me.Id)
            .Join(db.Players.Where(p => p.DeletionRequestedAt == null), f => f.FriendId, p => p.Id, (f, p) => new { f.FriendId, f.Points, f.CreatedAt, p.FriendCode })
            .OrderBy(f => f.CreatedAt).ToListAsync(ct);
        int waiting = await db.Gifts.CountAsync(g => g.ToPlayerId == me.Id && g.ClaimedAt == null, ct);
        return Results.Ok(new
        {
            friendCode = me.FriendCode,
            friends = friends.Select(f => new { playerId = f.FriendId, code = f.FriendCode, points = f.Points, since = f.CreatedAt, giftSentToday = sentToday.Contains(f.FriendId) }),
            giftsWaiting = waiting,
            giftsLeftToday = Math.Max(0, perDay - sentToday.Count),
        });
    }

    static async Task GiveCodeAsync(PlatformDb db, Player me, CancellationToken ct)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            me.FriendCode = SocialRules.NewFriendCode();
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException e) when (PlayerEndpoints.IsUniqueViolation(e)) { }
        }
        throw new InvalidOperationException("No free friend code.");
    }

    // Adding by code makes both players friends at once; there is nothing to accept and no chat.
    static async Task<IResult> AddFriend(FriendRequest request, HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player();
        var code = request.Code?.Trim().ToUpperInvariant();
        var friend = string.IsNullOrEmpty(code) ? null
            : await db.Players.FirstOrDefaultAsync(p => p.GameId == game.Id && p.FriendCode == code && p.DeletionRequestedAt == null, ct);
        if (friend is null || !SocialRules.Allowed(friend, clock.GetUtcNow())) return RequestContext.Error("unknown_code", StatusCodes.Status404NotFound);
        if (friend.Id == me.Id) return RequestContext.Error("own_code");
        if (await db.Friendships.AnyAsync(f => f.PlayerId == me.Id && f.FriendId == friend.Id, ct)) return RequestContext.Error("already_friends");
        using var balance = await GameBalance.ActiveAsync(db, game.Id, ct);
        int max = (int)SocialRules.Number(balance?.RootElement ?? default, 50, "social", "maxFriends");
        if (await db.Friendships.CountAsync(f => f.PlayerId == me.Id, ct) >= max ||
            await db.Friendships.CountAsync(f => f.PlayerId == friend.Id, ct) >= max) return RequestContext.Error("too_many_friends");
        db.Friendships.Add(new Friendship { GameId = game.Id, PlayerId = me.Id, FriendId = friend.Id, CreatedAt = clock.GetUtcNow() });
        db.Friendships.Add(new Friendship { GameId = game.Id, PlayerId = friend.Id, FriendId = me.Id, CreatedAt = clock.GetUtcNow() });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (PlayerEndpoints.IsUniqueViolation(e))
        {
            return RequestContext.Error("already_friends");
        }
        return Results.Ok(new { playerId = friend.Id, code = friend.FriendCode });
    }

    static async Task<IResult> RemoveFriend(Guid friendId, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var me = http.Player().Id;
        await db.Friendships.Where(f => f.PlayerId == me && f.FriendId == friendId || f.PlayerId == friendId && f.FriendId == me).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    // One gift per friend a day, up to social.giftsPerDay a day, each social.giftEnergy energy.
    static async Task<IResult> SendGift(GiftRequest request, HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        if (!await db.Friendships.AnyAsync(f => f.PlayerId == me && f.FriendId == request.To, ct)) return RequestContext.Error("not_friends");
        using var balance = await GameBalance.ActiveAsync(db, game.Id, ct);
        var root = balance?.RootElement ?? default;
        string today = SocialRules.Day(clock.GetUtcNow());
        if (await db.Gifts.CountAsync(g => g.FromPlayerId == me && g.Day == today, ct) >= (int)SocialRules.Number(root, 5, "social", "giftsPerDay"))
            return RequestContext.Error("daily_limit");
        db.Gifts.Add(new Gift
        {
            GameId = game.Id, FromPlayerId = me, ToPlayerId = request.To, Day = today,
            Energy = (int)SocialRules.Number(root, 5, "social", "giftEnergy"), SentAt = clock.GetUtcNow(),
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (PlayerEndpoints.IsUniqueViolation(e))
        {
            return RequestContext.Error("already_sent_today");
        }
        return Results.NoContent();
    }

    // Takes every gift waiting; the game adds the energy to the save (energy isn't kept on the platform).
    static async Task<IResult> ClaimGifts(HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var me = http.Player().Id;
        var waiting = await db.Gifts.Where(g => g.ToPlayerId == me && g.ClaimedAt == null).ToListAsync(ct);
        foreach (var gift in waiting) gift.ClaimedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { gifts = waiting.Count, energy = waiting.Sum(g => g.Energy) });
    }

    // A friend's café to walk around, read-only, from their latest cloud save.
    static async Task<IResult> Visit(Guid friendId, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var me = http.Player().Id;
        if (!await db.Friendships.AnyAsync(f => f.PlayerId == me && f.FriendId == friendId, ct)) return RequestContext.Error("not_friends", StatusCodes.Status404NotFound);
        var save = await db.Saves.AsNoTracking().FirstOrDefaultAsync(s => s.PlayerId == friendId, ct);
        if (save is null) return RequestContext.Error("no_cafe_yet", StatusCodes.Status404NotFound);
        // Only what the café shows: no story choices, letters or anything else in the save.
        using var doc = JsonDocument.Parse(save.Json);
        var root = doc.RootElement;
        JsonElement? Field(string section, string name) =>
            root.TryGetProperty(section, out var s) && s.ValueKind == JsonValueKind.Object && s.TryGetProperty(name, out var value) ? value.Clone() : null;
        return Results.Ok(new
        {
            level = save.Level,
            cafe = new { placed = Field("cafe", "placed"), taskStyles = Field("cafe", "taskStyles"), cosiness = Field("cafe", "cosiness") },
            story = new { chapter = Field("story", "chapter"), task = Field("story", "task") },
            wardrobe = new { worn = Field("wardrobe", "worn") },
        });
    }
}
