using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

/// <summary>Changes paid currencies. The only place gems move, always with a ledger row saying why.</summary>
public sealed class WalletService(PlatformDb db, TimeProvider clock)
{
    /// <summary>
    /// Adds <paramref name="amount"/> (negative to take away) in one SQL statement, so two changes at once can't
    /// lose each other. Throws <see cref="WalletOverdrawnException"/> if the balance would go below zero.
    /// Call inside a transaction together with whatever caused the change.
    /// </summary>
    public async Task AddAsync(Guid gameId, Guid playerId, string currency, long amount, string source,
        string? reason, Guid? purchaseId, Guid? adminUserId, CancellationToken ct)
    {
        int rows;
        try
        {
            // PostgreSQL checks the not-negative rule on the would-be inserted row before ON CONFLICT,
            // so taking away is a plain UPDATE: no row means nothing to take.
            rows = amount >= 0
                ? await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "WalletBalances" ("PlayerId", "Currency", "GameId", "Amount")
                    VALUES ({playerId}, {currency}, {gameId}, {amount})
                    ON CONFLICT ("PlayerId", "Currency")
                    DO UPDATE SET "Amount" = "WalletBalances"."Amount" + EXCLUDED."Amount"
                    """, ct)
                : await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE "WalletBalances" SET "Amount" = "Amount" + {amount}
                    WHERE "PlayerId" = {playerId} AND "Currency" = {currency}
                    """, ct);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.CheckViolation)
        {
            throw new WalletOverdrawnException();
        }
        if (rows == 0) throw new WalletOverdrawnException();
        db.WalletEntries.Add(new WalletEntry
        {
            GameId = gameId, PlayerId = playerId, Currency = currency, Amount = amount, Source = source,
            Reason = reason, PurchaseId = purchaseId, AdminUserId = adminUserId, CreatedAt = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);
    }

    public Task<Dictionary<string, long>> BalancesAsync(Guid playerId, CancellationToken ct) =>
        db.WalletBalances.AsNoTracking().Where(b => b.PlayerId == playerId)
            .ToDictionaryAsync(b => b.Currency, b => b.Amount, ct);
}

public sealed class WalletOverdrawnException : Exception;
