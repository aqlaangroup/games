using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Admin;

/// <summary>
/// Read-only staff pages beyond players and support (missing/22): what the game registered, purchases and refunds,
/// accounts waiting for deletion, a published config's text, and store revenue.
/// </summary>
public static class AdminInsights
{
    public static void Map(RouteGroupBuilder game)
    {
        var admin = game.MapGroup("/admin").RequireAuthorization(AuthSchemes.Admin);
        admin.MapGet("/game", Game).AddEndpointFilter(new StaffFilter(StaffRole.Viewer));
        admin.MapGet("/purchases", Purchases).AddEndpointFilter(new StaffFilter(StaffRole.Viewer));
        admin.MapGet("/deletions", Deletions).AddEndpointFilter(new StaffFilter(StaffRole.Support));
        admin.MapGet("/config/{version:int}", ConfigText).AddEndpointFilter(new StaffFilter(StaffRole.Viewer));
        admin.MapGet("/revenue", Revenue).AddEndpointFilter(new StaffFilter(StaffRole.Viewer));
    }

    static async Task<IResult> Game(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var products = await db.Products.AsNoTracking().Where(p => p.GameId == game.Id).OrderBy(p => p.Sku)
            .Select(p => new { p.Sku, p.Type, p.OncePerPlayer, p.Grants }).ToListAsync(ct);
        return Results.Ok(new
        {
            game.Slug, game.NameAr, game.NameEn, game.AppleBundleId, game.GooglePackageName, game.MinGameVersion, products,
        });
    }

    // query: a store transaction id, a player id, or a sku. status: verified, refunded or revoked.
    static async Task<IResult> Purchases(string? query, string? status, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var gameId = http.Game().Id;
        var purchases = db.Purchases.AsNoTracking().Where(p => p.GameId == gameId);
        if (Enum.TryParse<PurchaseStatus>(status, true, out var wanted)) purchases = purchases.Where(p => p.Status == wanted);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            purchases = Guid.TryParse(q, out var playerId)
                ? purchases.Where(p => p.PlayerId == playerId || p.Id == playerId)
                : purchases.Where(p => p.StoreTransactionId == q || p.Sku == q);
        }
        return Results.Ok(await purchases.OrderByDescending(p => p.PurchasedAt).Take(100)
            .Select(p => new { p.Id, p.PlayerId, p.Store, p.StoreTransactionId, p.Sku, p.Price, p.Currency, p.Status, p.PurchasedAt, p.VerifiedAt })
            .ToListAsync(ct));
    }

    // Accounts their players deleted, waiting for the purge (Accounts:DeletionGraceDays).
    static async Task<IResult> Deletions(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var gameId = http.Game().Id;
        return Results.Ok(await db.Players.AsNoTracking().Where(p => p.GameId == gameId && p.DeletionRequestedAt != null)
            .OrderBy(p => p.DeleteBy).Take(200)
            .Select(p => new { playerId = p.Id, requestedAt = p.DeletionRequestedAt, deleteBy = p.DeleteBy }).ToListAsync(ct));
    }

    static async Task<IResult> ConfigText(int version, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var gameId = http.Game().Id;
        var config = await db.Configs.AsNoTracking().FirstOrDefaultAsync(c => c.GameId == gameId && c.Version == version, ct);
        return config is null ? RequestContext.Error("unknown_version", StatusCodes.Status404NotFound) : Results.Text(config.Json, "application/json");
    }

    // Store revenue by day, currency and product, from verified purchases. Apple's records carry the price; Google's
    // don't, so Google sales count but add no money here (its reports come from the Play Console).
    static async Task<IResult> Revenue(int? days, HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var gameId = http.Game().Id;
        var since = clock.GetUtcNow().AddDays(-Math.Clamp(days ?? 30, 1, 365));
        var rows = await db.Purchases.AsNoTracking().Where(p => p.GameId == gameId && p.PurchasedAt >= since)
            .Select(p => new { p.PurchasedAt, p.Store, p.Sku, p.Price, p.Currency, p.Status }).ToListAsync(ct);
        return Results.Ok(new
        {
            since,
            byDay = rows.Where(r => r.Status == PurchaseStatus.Verified)
                .GroupBy(r => new { day = r.PurchasedAt.UtcDateTime.ToString("yyyy-MM-dd"), currency = r.Currency ?? "?" })
                .OrderBy(g => g.Key.day)
                .Select(g => new { g.Key.day, g.Key.currency, sales = g.Count(), amount = g.Sum(r => r.Price) }),
            bySku = rows.GroupBy(r => r.Sku).OrderBy(g => g.Key)
                .Select(g => new
                {
                    sku = g.Key,
                    sales = g.Count(r => r.Status == PurchaseStatus.Verified),
                    refunded = g.Count(r => r.Status != PurchaseStatus.Verified),
                    apple = g.Count(r => r.Store == Store.Apple),
                    google = g.Count(r => r.Store == Store.Google),
                }),
        });
    }
}
