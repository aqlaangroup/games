using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Platform.Domain;

namespace Platform.Api.Stores;

/// <summary>
/// A Google Cloud service account with access to the app in the Play Console. The private key comes from a secret
/// store or the environment (Stores__Google__PrivateKey), never the repo. PushToken guards the real-time developer
/// notifications endpoint: the Pub/Sub push subscription's URL carries it as ?token=.
/// </summary>
public sealed class GooglePlayOptions
{
    public string? ServiceAccountEmail { get; set; }
    public string? PrivateKey { get; set; }
    public string? PushToken { get; set; }

    public bool Configured => !string.IsNullOrWhiteSpace(ServiceAccountEmail) && !string.IsNullOrWhiteSpace(PrivateKey);
}

/// <summary>
/// Checks purchases with the Google Play Developer API. The game sends the purchase token; the platform asks Google
/// for it, accepts only a completed purchase of the right product, and acknowledges it (Google refunds purchases
/// that aren't acknowledged within 3 days). Google's purchase record has no price, so revenue comes from the Play
/// Console's reports (missing/22).
/// </summary>
public sealed class GooglePlayVerifier(IHttpClientFactory http, GooglePlayOptions options, TimeProvider clock) : IStoreVerifier
{
    public const string Api = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications";
    public const string TokenUrl = "https://oauth2.googleapis.com/token";
    const string Scope = "https://www.googleapis.com/auth/androidpublisher";

    readonly SemaphoreSlim tokenLock = new(1, 1);
    string? accessToken;
    DateTimeOffset accessTokenExpires;

    public Store Store => Store.Google;

    public async Task<VerifiedReceipt?> VerifyAsync(Game game, VerifyRequest request, CancellationToken ct)
    {
        string token = request.Receipt.Trim();
        if (!options.Configured || game.GooglePackageName is null || token.Length is 0 or > 2048) return null;
        string url = $"{Api}/{game.GooglePackageName}/purchases/products/{Uri.EscapeDataString(request.Sku)}/tokens/{Uri.EscapeDataString(token)}";
        var client = http.CreateClient("google-play");
        using var reply = await client.SendAsync(await Authorized(HttpMethod.Get, url, ct), ct);
        if (!reply.IsSuccessStatusCode) return null;
        var purchase = await reply.Content.ReadFromJsonAsync<JsonElement>(ct);
        // 0 purchased, 1 cancelled, 2 pending.
        if (!purchase.TryGetProperty("purchaseState", out var state) || state.GetInt32() != 0) return null;
        if (purchase.TryGetProperty("acknowledgementState", out var acked) && acked.GetInt32() == 0)
            using (await client.SendAsync(await Authorized(HttpMethod.Post, url + ":acknowledge", ct), ct)) { }
        string id = purchase.TryGetProperty("orderId", out var order) && order.GetString() is { Length: > 0 } o ? o : "token:" + Hash(token);
        var at = purchase.TryGetProperty("purchaseTimeMillis", out var ms) && long.TryParse(ms.GetString(), out var m)
            ? DateTimeOffset.FromUnixTimeMilliseconds(m) : clock.GetUtcNow();
        return new VerifiedReceipt(id, request.Sku, 0m, null, at);
    }

    // Google's voided-purchase notifications are authoritative once the push is authenticated, so nothing to look up.
    public Task<StoreTransaction?> LookUpAsync(Game game, string transactionId, CancellationToken ct) =>
        Task.FromResult<StoreTransaction?>(null);

    async Task<HttpRequestMessage> Authorized(HttpMethod method, string url, CancellationToken ct)
    {
        var message = new HttpRequestMessage(method, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await AccessTokenAsync(ct));
        return message;
    }

    // OAuth 2.0 for service accounts: a signed assertion traded for an access token, kept until shortly before it ends.
    async Task<string> AccessTokenAsync(CancellationToken ct)
    {
        await tokenLock.WaitAsync(ct);
        try
        {
            var now = clock.GetUtcNow();
            if (accessToken != null && now < accessTokenExpires) return accessToken;
            var key = RSA.Create();
            key.ImportFromPem(options.PrivateKey);
            string assertion = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = options.ServiceAccountEmail,
                Audience = TokenUrl,
                IssuedAt = now.UtcDateTime,
                NotBefore = now.UtcDateTime,
                Expires = now.UtcDateTime.AddMinutes(30),
                Claims = new Dictionary<string, object> { ["scope"] = Scope },
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(key), SecurityAlgorithms.RsaSha256),
            });
            var client = http.CreateClient("google-play");
            using var reply = await client.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion,
            }), ct);
            reply.EnsureSuccessStatusCode();
            var token = await reply.Content.ReadFromJsonAsync<JsonElement>(ct);
            accessToken = token.GetProperty("access_token").GetString()!;
            accessTokenExpires = now.AddSeconds(token.GetProperty("expires_in").GetInt32() - 60);
            return accessToken;
        }
        finally
        {
            tokenLock.Release();
        }
    }

    static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))[..32];
}
