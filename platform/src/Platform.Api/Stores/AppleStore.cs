using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Platform.Domain;

namespace Platform.Api.Stores;

/// <summary>
/// App Store Connect's in-app purchase key (Users and Access > Integrations > In-App Purchase). The private key is
/// the .p8 file's text, from a secret store or the environment (Stores__Apple__PrivateKey), never the repo.
/// </summary>
public sealed class AppleStoreOptions
{
    public string? IssuerId { get; set; }
    public string? KeyId { get; set; }
    public string? PrivateKey { get; set; }

    public bool Configured => !string.IsNullOrWhiteSpace(IssuerId) && !string.IsNullOrWhiteSpace(KeyId) && !string.IsNullOrWhiteSpace(PrivateKey);
}

/// <summary>
/// Checks purchases with the App Store Server API. The game sends the transaction id; the platform asks Apple for
/// that transaction (production first, then the sandbox for TestFlight and test accounts) and accepts it only when
/// the bundle id and product match and Apple hasn't revoked it. The reply comes from Apple over TLS with our own
/// key, so its signedTransactionInfo is read without checking Apple's certificate chain.
/// </summary>
public sealed class AppleStoreVerifier(IHttpClientFactory http, AppleStoreOptions options, TimeProvider clock) : IStoreVerifier
{
    public const string Production = "https://api.storekit.itunes.apple.com";
    public const string Sandbox = "https://api.storekit-sandbox.itunes.apple.com";

    public Store Store => Store.Apple;

    public async Task<VerifiedReceipt?> VerifyAsync(Game game, VerifyRequest request, CancellationToken ct)
    {
        var transaction = await FetchAsync(game, request.Receipt.Trim(), ct);
        if (transaction is not { } t) return null;
        if (Text(t, "productId") != request.Sku || t.TryGetProperty("revocationDate", out _)) return null;
        long? price = t.TryGetProperty("price", out var p) ? p.GetInt64() : null; // in thousandths
        return new VerifiedReceipt(Text(t, "transactionId")!, request.Sku, price is { } milli ? milli / 1000m : 0m,
            Text(t, "currency"), DateTimeOffset.FromUnixTimeMilliseconds(t.GetProperty("purchaseDate").GetInt64()));
    }

    public async Task<StoreTransaction?> LookUpAsync(Game game, string transactionId, CancellationToken ct)
    {
        var transaction = await FetchAsync(game, transactionId, ct);
        if (transaction is not { } t) return null;
        return new StoreTransaction(Text(t, "transactionId")!, Text(t, "productId") ?? "", t.TryGetProperty("revocationDate", out _));
    }

    async Task<JsonElement?> FetchAsync(Game game, string transactionId, CancellationToken ct)
    {
        if (!options.Configured || game.AppleBundleId is null || transactionId.Length is 0 or > 64) return null;
        var client = http.CreateClient("apple-store");
        foreach (var host in new[] { Production, Sandbox })
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, $"{host}/inApps/v1/transactions/{Uri.EscapeDataString(transactionId)}");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(game.AppleBundleId));
            using var reply = await client.SendAsync(message, ct);
            if (reply.StatusCode == HttpStatusCode.NotFound) continue; // not in this environment
            if (!reply.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await reply.Content.ReadAsStringAsync(ct));
            var info = Jws.Payload(Text(doc.RootElement, "signedTransactionInfo"));
            if (info is not { } t || Text(t, "bundleId") != game.AppleBundleId) return null;
            return t;
        }
        return null;
    }

    // A short-lived token signed with the in-app purchase key (App Store Server API, "Generating JSON Web Tokens").
    string Token(string bundleId)
    {
        var key = ECDsa.Create();
        key.ImportFromPem(options.PrivateKey);
        var now = clock.GetUtcNow().UtcDateTime;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.IssuerId,
            Audience = "appstoreconnect-v1",
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(20),
            Claims = new Dictionary<string, object> { ["bid"] = bundleId },
            SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(key) { KeyId = options.KeyId }, SecurityAlgorithms.EcdsaSha256),
        });
    }

    static string? Text(JsonElement json, string property) =>
        json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
