using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

/// <summary>Receipt checks. The phone never decides it has been paid; the platform asks the store.</summary>
public static class PurchaseEndpoints
{
    /// <summary>The paid currency kept in the wallet. Every other grant (energy, a generator copy, a decor set, an
    /// outfit, the pass) is an item the game applies to its save, once per purchase id (decided 2026-10-31).</summary>
    public const string Gems = "gems";

    public static void Map(RouteGroupBuilder signedIn)
    {
        signedIn.MapPost("/purchases/verify", Verify);
        signedIn.MapGet("/players/me/purchases", List);
    }

    static Dictionary<string, string> Items(Product product) =>
        product.Grants.Where(g => g.Key != Gems).ToDictionary(g => g.Key, g => g.Value);

    static async Task<IResult> Verify(VerifyRequest request, HttpContext http, PlatformDb db,
        IEnumerable<IStoreVerifier> verifiers, WalletService wallets, CancellationToken ct)
    {
        var game = http.Game();
        var player = http.Player();
        var product = await db.Products.FirstOrDefaultAsync(p => p.GameId == game.Id && p.Sku == request.Sku, ct);
        if (product is null) return RequestContext.Error("unknown_product");

        var verifier = verifiers.First(v => v.Store == request.Store);
        var receipt = await verifier.VerifyAsync(game, request, ct);
        if (receipt is null) return RequestContext.Error("not_confirmed");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var purchase = new Purchase
        {
            GameId = game.Id, PlayerId = player.Id, Store = request.Store,
            StoreTransactionId = receipt.StoreTransactionId, Sku = receipt.Sku,
            Price = receipt.Price, Currency = receipt.Currency, PurchasedAt = receipt.PurchasedAt,
        };
        db.Purchases.Add(purchase);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (PlayerEndpoints.IsUniqueViolation(e))
        {
            // The same store transaction can only grant once. Sent again (a retry, or restore purchases), it returns
            // the items so the game can apply what it's missing; gems are never granted twice. A non-consumable
            // restored to another account brings its items, not its gems.
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            var earlier = await db.Purchases.AsNoTracking()
                .FirstAsync(p => p.Store == request.Store && p.StoreTransactionId == receipt.StoreTransactionId, ct);
            if (earlier.Status != PurchaseStatus.Verified) return RequestContext.Error("refunded");
            bool restorable = earlier.PlayerId == player.Id || product.Type is ProductType.NonConsumable or ProductType.NonRenewing;
            return Results.Ok(new
            {
                alreadyGranted = true,
                purchaseId = earlier.Id,
                items = restorable ? Items(product) : new Dictionary<string, string>(),
                wallet = await wallets.BalancesAsync(player.Id, ct),
            });
        }

        if (product.Grants.TryGetValue(Gems, out var gems) && long.TryParse(gems, out var n))
            await wallets.AddAsync(game.Id, player.Id, Gems, n, "purchase", product.Sku, purchase.Id, null, ct);
        await tx.CommitAsync(ct);
        return Results.Ok(new { granted = product.Grants, purchaseId = purchase.Id, items = Items(product), wallet = await wallets.BalancesAsync(player.Id, ct) });
    }

    // This player's purchases, so the game can restore items after a reinstall and hide once-only offers.
    static async Task<IResult> List(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var player = http.Player();
        var products = await db.Products.AsNoTracking().Where(p => p.GameId == game.Id).ToDictionaryAsync(p => p.Sku, ct);
        var purchases = await db.Purchases.AsNoTracking().Where(p => p.GameId == game.Id && p.PlayerId == player.Id)
            .OrderBy(p => p.PurchasedAt).ToListAsync(ct);
        return Results.Ok(purchases.Select(p => new
        {
            purchaseId = p.Id,
            sku = p.Sku,
            status = p.Status,
            purchasedAt = p.PurchasedAt,
            items = p.Status == PurchaseStatus.Verified && products.TryGetValue(p.Sku, out var product) ? Items(product) : new Dictionary<string, string>(),
        }));
    }
}
