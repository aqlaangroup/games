using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Api.Games;
using Platform.Api.Players;
using Platform.Domain;

namespace Platform.Api.Social;

public record InviteRequest(Guid FriendId);
public record GiveRequest(int Count);
public record MessageRequest(int Preset);

/// <summary>
/// Hosting (SYSTEMS_B.md section 7; games/gahwit-sitti/missing/20): a big order friends fill together in its time.
/// The host invites up to levelB.hosting.maxHelpers friends; every item anyone gives counts. A host with no helper
/// giving is helped by neighbour bots, which fill botShareOfOrder of the order over its time, for a smaller reward.
/// Rewards are granted here, once, when the order is finished.
/// </summary>
public static class HostingEndpoints
{
    public static void Map(RouteGroupBuilder me)
    {
        var hosting = me.MapGroup("/hosting").AddEndpointFilter(new HostingFilter());
        hosting.MapGet("", List);
        hosting.MapPost("", Start);
        hosting.MapPost("/{id:guid}/invite", Invite);
        hosting.MapPost("/{id:guid}/give", Give);
        hosting.MapPost("/{id:guid}/message", Message);
    }

    sealed class HostingFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var clock = http.RequestServices.GetRequiredService<TimeProvider>();
            if (!SocialRules.Allowed(http.Player(), clock.GetUtcNow())) return RequestContext.Error("parent_needed", StatusCodes.Status403Forbidden);
            return await next(context);
        }
    }

    sealed record Numbers(int PerWeek, int Hours, int Items, int MaxHelpers, int Friendship, int Presets, int HostGems, int HelperGems,
        double SoloShare, double BotShare)
    {
        public static Numbers From(JsonElement b) => new(
            (int)SocialRules.Number(b, 2, "levelB", "hosting", "perWeek"),
            (int)SocialRules.Number(b, 48, "levelB", "hosting", "durationHours"),
            (int)SocialRules.Number(b, 30, "levelB", "hosting", "itemsNeeded"),
            (int)SocialRules.Number(b, 3, "levelB", "hosting", "maxHelpers"),
            (int)SocialRules.Number(b, 10, "levelB", "hosting", "helperFriendship"),
            (int)SocialRules.Number(b, 12, "levelB", "hosting", "presetMessages"),
            (int)SocialRules.Number(b, 15, "levelB", "hosting", "rewards", "hostGems"),
            (int)SocialRules.Number(b, 5, "levelB", "hosting", "rewards", "helperGems"),
            SocialRules.Number(b, 0.5, "levelB", "hosting", "rewards", "soloBotRewardShare"),
            SocialRules.Number(b, 0.5, "levelB", "hosting", "botShareOfOrder"));
    }

    static async Task<Numbers> NumbersAsync(PlatformDb db, Guid gameId, CancellationToken ct)
    {
        using var balance = await GameBalance.ActiveAsync(db, gameId, ct);
        return Numbers.From(balance?.RootElement ?? default);
    }

    // Bots work only while no helper has given anything: they fill botShareOfOrder of the order, evenly over its time.
    static int Bots(HostingOrder order, IReadOnlyList<HostingHelper> helpers, Numbers n, DateTimeOffset now)
    {
        if (helpers.Any(h => h.Given > 0)) return 0;
        var until = now < order.EndsAt ? now : order.EndsAt;
        double share = Math.Clamp((until - order.StartedAt) / (order.EndsAt - order.StartedAt), 0, 1);
        return (int)Math.Floor(order.ItemsNeeded * n.BotShare * share);
    }

    static object Dto(HostingOrder order, IReadOnlyList<HostingHelper> helpers, int bots) => new
    {
        id = order.Id,
        hostId = order.HostId,
        startedAt = order.StartedAt,
        endsAt = order.EndsAt,
        needed = order.ItemsNeeded,
        progress = Math.Min(order.ItemsNeeded, order.ItemsGiven + bots),
        bots,
        status = order.Status,
        helpers = helpers.Select(h => new { playerId = h.PlayerId, given = h.Given }),
        messages = JsonNode.Parse(order.MessagesJson),
    };

    // Moves an order on: finished when full (rewards once), expired when its time is up.
    static async Task<(HostingOrder Order, List<HostingHelper> Helpers, int Bots)> RefreshAsync(PlatformDb db, WalletService wallets,
        Guid gameId, HostingOrder order, Numbers n, DateTimeOffset now, CancellationToken ct)
    {
        var helpers = await db.HostingHelpers.Where(h => h.OrderId == order.Id).OrderBy(h => h.InvitedAt).ToListAsync(ct);
        int bots = Bots(order, helpers, n, now);
        if (order.Status != HostingStatus.Open) return (order, helpers, bots);
        bool full = order.ItemsGiven + bots >= order.ItemsNeeded;
        if (!full && now < order.EndsAt) return (order, helpers, bots);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var next = full ? HostingStatus.Done : HostingStatus.Expired;
        // Only the first request to see it finished moves it on and pays.
        int moved = await db.HostingOrders.Where(o => o.Id == order.Id && o.Status == HostingStatus.Open)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, next).SetProperty(o => o.FinishedAt, now), ct);
        if (moved == 1 && full)
        {
            bool solo = helpers.All(h => h.Given == 0);
            long hostGems = (long)Math.Round(n.HostGems * (solo ? n.SoloShare : 1));
            if (hostGems > 0) await wallets.AddAsync(gameId, order.HostId, "gems", hostGems, "hosting", "host", null, null, ct);
            foreach (var helper in helpers.Where(h => h.Given > 0))
            {
                if (n.HelperGems > 0) await wallets.AddAsync(gameId, helper.PlayerId, "gems", n.HelperGems, "hosting", "helper", null, null, ct);
                await db.Friendships.Where(f => f.PlayerId == helper.PlayerId && f.FriendId == order.HostId || f.PlayerId == order.HostId && f.FriendId == helper.PlayerId)
                    .ExecuteUpdateAsync(s => s.SetProperty(f => f.Points, f => f.Points + n.Friendship), ct);
            }
        }
        await tx.CommitAsync(ct);
        db.Entry(order).State = EntityState.Detached;
        order = await db.HostingOrders.FirstAsync(o => o.Id == order.Id, ct);
        return (order, helpers, bots);
    }

    // The player's own order (the newest), and open orders they were invited to help.
    static async Task<IResult> List(HttpContext http, PlatformDb db, WalletService wallets, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var n = await NumbersAsync(db, game.Id, ct);
        var now = clock.GetUtcNow();
        object? mine = null;
        var own = await db.HostingOrders.Where(o => o.HostId == me).OrderByDescending(o => o.StartedAt).FirstOrDefaultAsync(ct);
        if (own is not null)
        {
            var (order, helpers, bots) = await RefreshAsync(db, wallets, game.Id, own, n, now, ct);
            mine = Dto(order, helpers, bots);
        }
        var invited = await db.HostingHelpers.Where(h => h.PlayerId == me).Select(h => h.OrderId).ToListAsync(ct);
        var helping = new List<object>();
        foreach (var order in await db.HostingOrders.Where(o => invited.Contains(o.Id) && o.Status == HostingStatus.Open).ToListAsync(ct))
        {
            var (o, helpers, bots) = await RefreshAsync(db, wallets, game.Id, order, n, now, ct);
            if (o.Status == HostingStatus.Open) helping.Add(Dto(o, helpers, bots));
        }
        int startedThisWeek = await db.HostingOrders.CountAsync(o => o.HostId == me && o.StartedAt > now.AddDays(-7), ct);
        return Results.Ok(new { mine, helping, canStart = startedThisWeek < n.PerWeek && (own is null || own.Status != HostingStatus.Open) });
    }

    static async Task<IResult> Start(HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var n = await NumbersAsync(db, game.Id, ct);
        var now = clock.GetUtcNow();
        if (await db.HostingOrders.AnyAsync(o => o.HostId == me && o.Status == HostingStatus.Open && o.EndsAt > now, ct)) return RequestContext.Error("already_hosting");
        if (await db.HostingOrders.CountAsync(o => o.HostId == me && o.StartedAt > now.AddDays(-7), ct) >= n.PerWeek) return RequestContext.Error("weekly_limit");
        var order = new HostingOrder { GameId = game.Id, HostId = me, StartedAt = now, EndsAt = now.AddHours(n.Hours), ItemsNeeded = n.Items, Status = HostingStatus.Open };
        db.HostingOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return Results.Ok(Dto(order, [], 0));
    }

    static async Task<IResult> Invite(Guid id, InviteRequest request, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var order = await db.HostingOrders.FirstOrDefaultAsync(o => o.Id == id && o.HostId == me && o.Status == HostingStatus.Open, ct);
        if (order is null) return RequestContext.Error("unknown_order", StatusCodes.Status404NotFound);
        if (!await db.Friendships.AnyAsync(f => f.PlayerId == me && f.FriendId == request.FriendId, ct)) return RequestContext.Error("not_friends");
        var n = await NumbersAsync(db, game.Id, ct);
        if (await db.HostingHelpers.CountAsync(h => h.OrderId == id, ct) >= n.MaxHelpers) return RequestContext.Error("too_many_helpers");
        db.HostingHelpers.Add(new HostingHelper { OrderId = id, PlayerId = request.FriendId });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (PlayerEndpoints.IsUniqueViolation(e))
        {
            return RequestContext.Error("already_invited");
        }
        return Results.NoContent();
    }

    // Items given from the giver's own board; the game takes them off the board after this reply.
    static async Task<IResult> Give(Guid id, GiveRequest request, HttpContext http, PlatformDb db, WalletService wallets, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var order = await db.HostingOrders.FirstOrDefaultAsync(o => o.Id == id && o.GameId == game.Id, ct);
        if (order is null) return RequestContext.Error("unknown_order", StatusCodes.Status404NotFound);
        var helper = order.HostId == me ? null : await db.HostingHelpers.FirstOrDefaultAsync(h => h.OrderId == id && h.PlayerId == me, ct);
        if (order.HostId != me && helper is null) return RequestContext.Error("not_invited", StatusCodes.Status403Forbidden);
        var n = await NumbersAsync(db, game.Id, ct);
        var now = clock.GetUtcNow();
        var (current, helpers, bots) = await RefreshAsync(db, wallets, game.Id, order, n, now, ct);
        if (current.Status != HostingStatus.Open) return RequestContext.Error("order_closed");
        int room = current.ItemsNeeded - current.ItemsGiven - bots;
        if (request.Count < 1 || request.Count > room) return RequestContext.Error("invalid_count");

        await db.HostingOrders.Where(o => o.Id == id).ExecuteUpdateAsync(s => s.SetProperty(o => o.ItemsGiven, o => o.ItemsGiven + request.Count), ct);
        if (helper is not null)
            await db.HostingHelpers.Where(h => h.Id == helper.Id).ExecuteUpdateAsync(s => s.SetProperty(h => h.Given, h => h.Given + request.Count), ct);
        db.ChangeTracker.Clear();
        var updated = await db.HostingOrders.FirstAsync(o => o.Id == id, ct);
        var (final, finalHelpers, finalBots) = await RefreshAsync(db, wallets, game.Id, updated, n, now, ct);
        return Results.Ok(Dto(final, finalHelpers, finalBots));
    }

    // One of the preset messages (no chat); the game shows its text.
    static async Task<IResult> Message(Guid id, MessageRequest request, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var order = await db.HostingOrders.FirstOrDefaultAsync(o => o.Id == id && o.GameId == game.Id, ct);
        if (order is null) return RequestContext.Error("unknown_order", StatusCodes.Status404NotFound);
        if (order.HostId != me && !await db.HostingHelpers.AnyAsync(h => h.OrderId == id && h.PlayerId == me, ct))
            return RequestContext.Error("not_invited", StatusCodes.Status403Forbidden);
        var n = await NumbersAsync(db, game.Id, ct);
        if (request.Preset < 1 || request.Preset > n.Presets) return RequestContext.Error("unknown_preset");
        var messages = JsonNode.Parse(order.MessagesJson)!.AsArray();
        messages.Add(new JsonObject { ["playerId"] = me.ToString(), ["preset"] = request.Preset, ["at"] = DateTimeOffset.UtcNow.ToString("O") });
        while (messages.Count > 50) messages.RemoveAt(0);
        order.MessagesJson = messages.ToJsonString();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
