using Platform.Domain;

namespace Platform.Api;

public record VerifyRequest(Store Store, string Sku, string Receipt);

public record VerifiedReceipt(string StoreTransactionId, string Sku, decimal Price, string? Currency, DateTimeOffset PurchasedAt);

/// <summary>Checks a receipt directly with Apple or Google for one game's app ids.</summary>
public interface IStoreVerifier
{
    Store Store { get; }
    Task<VerifiedReceipt?> VerifyAsync(Game game, VerifyRequest request, CancellationToken ct);
}

/// <summary>
/// Placeholder until the App Store Server API (Apple) and the Play Developer API (Google) are wired in.
/// It refuses every receipt, so nothing is granted by mistake.
/// </summary>
public class NotConfiguredVerifier(Store store) : IStoreVerifier
{
    public Store Store { get; } = store;

    public Task<VerifiedReceipt?> VerifyAsync(Game game, VerifyRequest request, CancellationToken ct) =>
        Task.FromResult<VerifiedReceipt?>(null);
}
