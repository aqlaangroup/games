using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;
using Platform.Api.Players;
using Platform.Domain;

namespace Platform.Api.Stores;

/// <summary>
/// A refund or revocation the store told us about. The purchase is marked refunded, so the game stops treating its
/// items as owned, and its gems are taken back. A balance can't go below zero: when some gems are already spent,
/// what's left is taken and the ledger notes the shortfall, for staff to see (decided 2026-10-31, missing/17).
/// </summary>
public sealed class Refunds(PlatformDb db, WalletService wallets, ILogger<Refunds> log)
{
    public async Task<bool> RefundAsync(Game game, Store store, string transactionId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var purchase = await db.Purchases.FirstOrDefaultAsync(p => p.GameId == game.Id && p.Store == store && p.StoreTransactionId == transactionId, ct);
        if (purchase is null || purchase.Status != PurchaseStatus.Verified) return false;
        purchase.Status = PurchaseStatus.Refunded;
        await db.SaveChangesAsync(ct);

        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.GameId == game.Id && p.Sku == purchase.Sku, ct);
        if (purchase.PlayerId is Guid player && product is not null)
            foreach (var (currency, value) in product.Grants)
            {
                if (!long.TryParse(value, out long granted) || granted <= 0) continue;
                long balance = (await wallets.BalancesAsync(player, ct)).GetValueOrDefault(currency);
                long take = Math.Min(balance, granted);
                string reason = take < granted ? $"{purchase.Sku} (short {granted - take})" : purchase.Sku;
                if (take > 0) await wallets.AddAsync(game.Id, player, currency, -take, "refund", reason, purchase.Id, null, ct);
                if (take < granted) log.LogWarning("Refund of {Sku} ({Transaction}) was short by {Short} {Currency}.", purchase.Sku, transactionId, granted - take, currency);
            }
        await tx.CommitAsync(ct);
        return true;
    }
}
