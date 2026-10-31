using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Platform.Api;
using Platform.Api.Stores;
using Platform.Domain;

namespace Platform.Tests;

/// <summary>The Apple and Google verifiers against fake store servers: what they ask and what they accept.</summary>
public class StoreVerifierTests
{
    sealed class FakeServer(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public readonly List<HttpRequestMessage> Requests = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(answer(request));
        }
    }

    sealed class Clients(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

    static string Jws(object payload) => $"e30.{Jws_(payload)}.sig";
    static string Jws_(object payload) => Platform.Api.Stores.Jws.Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));

    static readonly Game Game = new() { Slug = "g", NameAr = "g", NameEn = "g", AppleBundleId = "com.aqlaan.g", GooglePackageName = "com.aqlaan.g" };

    [Fact]
    public async Task AppleChecksProductionThenTheSandboxAndAcceptsOnlyTheRightProduct()
    {
        var server = new FakeServer(request => request.RequestUri!.Host.Contains("sandbox")
            ? Json(new { signedTransactionInfo = Jws(new { transactionId = "2000", bundleId = "com.aqlaan.g", productId = "gs.gems.s", purchaseDate = 1793372280000, price = 1990, currency = "USD" }) })
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var verifier = new AppleStoreVerifier(new Clients(server),
            new AppleStoreOptions { IssuerId = "issuer", KeyId = "KEY123", PrivateKey = key.ExportPkcs8PrivateKeyPem() }, TimeProvider.System);

        var receipt = await verifier.VerifyAsync(Game, new VerifyRequest(Store.Apple, "gs.gems.s", "2000"), default);
        Assert.NotNull(receipt);
        Assert.Equal("2000", receipt.StoreTransactionId);
        Assert.Equal(1.99m, receipt.Price);
        Assert.Equal(2, server.Requests.Count);
        Assert.Equal(AppleStoreVerifier.Production + "/inApps/v1/transactions/2000", server.Requests[0].RequestUri!.ToString());

        // The token is ours: our key id, our issuer, and the app's bundle id.
        var token = new JsonWebToken(server.Requests[1].Headers.Authorization!.Parameter);
        Assert.Equal("KEY123", token.Kid);
        Assert.Equal("issuer", token.Issuer);
        Assert.Equal("com.aqlaan.g", token.GetClaim("bid").Value);
        Assert.Equal("ES256", token.Alg);

        Assert.Null(await verifier.VerifyAsync(Game, new VerifyRequest(Store.Apple, "gs.gems.l", "2000"), default));
    }

    [Fact]
    public async Task AppleRefusesARevokedTransactionAndReportsItForRefunds()
    {
        var server = new FakeServer(_ => Json(new { signedTransactionInfo = Jws(new
        {
            transactionId = "3000", bundleId = "com.aqlaan.g", productId = "gs.gems.s", purchaseDate = 1793372280000, revocationDate = 1793372380000,
        }) }));
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var verifier = new AppleStoreVerifier(new Clients(server),
            new AppleStoreOptions { IssuerId = "i", KeyId = "k", PrivateKey = key.ExportPkcs8PrivateKeyPem() }, TimeProvider.System);
        Assert.Null(await verifier.VerifyAsync(Game, new VerifyRequest(Store.Apple, "gs.gems.s", "3000"), default));
        Assert.True((await verifier.LookUpAsync(Game, "3000", default))!.Revoked);
    }

    [Fact]
    public async Task GoogleAcceptsAPurchasedTokenAndAcknowledgesIt()
    {
        var server = new FakeServer(request =>
        {
            string url = request.RequestUri!.ToString();
            if (url == GooglePlayVerifier.TokenUrl) return Json(new { access_token = "access", expires_in = 3600 });
            if (url.EndsWith(":acknowledge")) return new HttpResponseMessage(HttpStatusCode.NoContent);
            return url.Contains("/tokens/pending")
                ? Json(new { purchaseState = 2, acknowledgementState = 0 })
                : Json(new { purchaseState = 0, acknowledgementState = 0, orderId = "GPA.1", purchaseTimeMillis = "1793372280000" });
        });
        var key = RSA.Create(2048);
        var verifier = new GooglePlayVerifier(new Clients(server),
            new GooglePlayOptions { ServiceAccountEmail = "svc@example.iam.gserviceaccount.com", PrivateKey = key.ExportPkcs8PrivateKeyPem() }, TimeProvider.System);

        var receipt = await verifier.VerifyAsync(Game, new VerifyRequest(Store.Google, "gs.gems.s", "good"), default);
        Assert.Equal("GPA.1", receipt!.StoreTransactionId);
        Assert.Contains(server.Requests, r => r.Method == HttpMethod.Post && r.RequestUri!.ToString().EndsWith("/purchases/products/gs.gems.s/tokens/good:acknowledge"));
        Assert.All(server.Requests.Where(r => r.RequestUri!.Host.StartsWith("androidpublisher")), r => Assert.Equal("access", r.Headers.Authorization!.Parameter));

        Assert.Null(await verifier.VerifyAsync(Game, new VerifyRequest(Store.Google, "gs.gems.s", "pending"), default));
        Assert.Single(server.Requests, r => r.RequestUri!.ToString() == GooglePlayVerifier.TokenUrl); // the access token is kept
    }

    [Fact]
    public async Task DevReceiptsPassOnlyThroughTheDeskWrapperAndOthersGoToTheStore()
    {
        var dev = new DevReceiptVerifier(new NotConfiguredVerifier(Store.Apple), TimeProvider.System);
        Assert.Equal("dev:abc", (await dev.VerifyAsync(Game, new VerifyRequest(Store.Apple, "gs.gems.s", "dev:abc"), default))!.StoreTransactionId);
        Assert.Null(await dev.VerifyAsync(Game, new VerifyRequest(Store.Apple, "gs.gems.s", "2000"), default));
        Assert.Null(await new NotConfiguredVerifier(Store.Apple).VerifyAsync(Game, new VerifyRequest(Store.Apple, "gs.gems.s", "dev:abc"), default));
    }
}
