using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

/// <summary>Receipt checks. The phone never decides it has been paid; the platform asks the store.</summary>
public static class PurchaseEndpoints
{
    public static void Map(RouteGroupBuilder signedIn) => signedIn.MapPost("/purchases/verify", Verify);

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
            // The same store transaction can only grant once.
            await tx.RollbackAsync(ct);
            return Results.Ok(new { alreadyGranted = true, wallet = await wallets.BalancesAsync(player.Id, ct) });
        }

        foreach (var (item, amount) in product.Grants)
            if (long.TryParse(amount, out var n))
                await wallets.AddAsync(game.Id, player.Id, item, n, "purchase", product.Sku, purchase.Id, null, ct);
        await tx.CommitAsync(ct);
        return Results.Ok(new { granted = product.Grants, wallet = await wallets.BalancesAsync(player.Id, ct) });
    }
}
