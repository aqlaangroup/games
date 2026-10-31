using Platform.Domain;

namespace Platform.Api;

public record VerifyRequest(Store Store, string Sku, string Receipt);

public record VerifiedReceipt(string StoreTransactionId, string Sku, decimal Price, string? Currency, DateTimeOffset PurchasedAt);

/// <summary>What a store says about one transaction now, for refund notifications.</summary>
public record StoreTransaction(string TransactionId, string Sku, bool Revoked);

/// <summary>Checks a receipt directly with Apple or Google for one game's app ids.</summary>
public interface IStoreVerifier
{
    Store Store { get; }
    Task<VerifiedReceipt?> VerifyAsync(Game game, VerifyRequest request, CancellationToken ct);

    /// <summary>The store's current word on a transaction, or null when it doesn't know it (or isn't set up).</summary>
    Task<StoreTransaction?> LookUpAsync(Game game, string transactionId, CancellationToken ct);
}

/// <summary>
/// Used when a store's credentials aren't configured (Stores:Apple or Stores:Google). It refuses every receipt, so
/// nothing is granted by mistake.
/// </summary>
public class NotConfiguredVerifier(Store store) : IStoreVerifier
{
    public Store Store { get; } = store;

    public Task<VerifiedReceipt?> VerifyAsync(Game game, VerifyRequest request, CancellationToken ct) =>
        Task.FromResult<VerifiedReceipt?>(null);

    public Task<StoreTransaction?> LookUpAsync(Game game, string transactionId, CancellationToken ct) =>
        Task.FromResult<StoreTransaction?>(null);
}
