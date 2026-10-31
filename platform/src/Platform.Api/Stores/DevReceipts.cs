using Platform.Domain;

namespace Platform.Api.Stores;

/// <summary>
/// For a desk only: accepts receipts written "dev:transactionId", which the Editor's test store sends, so the whole
/// purchase flow runs without a store. It wraps the real verifier and is used only when the server runs in the
/// Development environment with Stores:AllowDevReceipts on; anywhere else such receipts are refused like any other.
/// </summary>
public sealed class DevReceiptVerifier(IStoreVerifier store, TimeProvider clock) : IStoreVerifier
{
    public Store Store => store.Store;

    public Task<VerifiedReceipt?> VerifyAsync(Game game, VerifyRequest request, CancellationToken ct) =>
        request.Receipt.StartsWith("dev:") && request.Receipt.Length is > 4 and <= 64
            ? Task.FromResult<VerifiedReceipt?>(new VerifiedReceipt(request.Receipt, request.Sku, 0m, null, clock.GetUtcNow()))
            : store.VerifyAsync(game, request, ct);

    public Task<StoreTransaction?> LookUpAsync(Game game, string transactionId, CancellationToken ct) =>
        store.LookUpAsync(game, transactionId, ct);
}
