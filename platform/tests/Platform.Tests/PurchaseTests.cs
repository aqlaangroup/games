using System.Net;
using System.Net.Http.Json;
using Platform.Api.Auth;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class PurchaseTests(PlatformFactory api)
{
    [Fact]
    public async Task APurchaseGrantsToTheSignedInPlayerOnlyOnce()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        var receipt = $"ok:tx-{Guid.NewGuid():N}";

        var first = await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "gems.s", receipt });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(100, (await first.Json()).GetProperty("wallet").GetProperty("gems").GetInt64());

        var again = await (await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "gems.s", receipt })).Json();
        Assert.True(again.GetProperty("alreadyGranted").GetBoolean());
        Assert.Equal(100, again.GetProperty("wallet").GetProperty("gems").GetInt64());
    }

    [Fact]
    public async Task PurchasesNeedASignedInPlayerAndAConfirmedReceipt()
    {
        var anonymous = await api.Client().PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "gems.s", receipt = "ok:x" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var client = api.Client((await api.Guest()).Str("accessToken"));
        var refused = await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "google", sku = "gems.s", receipt = "forged" });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        var unknown = await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "nope", receipt = "ok:y" });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    static string Jws(object payload) =>
        $"e30.{Platform.Api.Stores.Jws.Base64Url(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(payload)))}.sig";

    [Fact]
    public async Task OnlyGemsGoToTheWalletAndOtherGrantsComeBackAsItems()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        var bought = await (await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "decor", receipt = $"ok:d-{Guid.NewGuid():N}" })).Json();
        Assert.Equal("eid", bought.GetProperty("items").GetProperty("decorSet").GetString());
        Assert.False(bought.GetProperty("items").TryGetProperty("gems", out _));
        Assert.Equal(60, bought.GetProperty("wallet").GetProperty("gems").GetInt64());
        Assert.Single(bought.GetProperty("wallet").EnumerateObject());

        var list = await (await client.GetAsync($"/games/{GameA}/players/me/purchases")).Json();
        Assert.Equal("decor", list[0].Str("sku"));
        Assert.Equal(bought.Str("purchaseId"), list[0].Str("purchaseId"));
    }

    [Fact]
    public async Task ANonConsumableRestoresItsItemsToANewAccountButNotItsGems()
    {
        string receipt = $"ok:r-{Guid.NewGuid():N}";
        var first = api.Client((await api.Guest()).Str("accessToken"));
        (await first.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "decor", receipt })).EnsureSuccessStatusCode();

        var second = api.Client((await api.Guest()).Str("accessToken"));
        var restored = await (await second.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "decor", receipt })).Json();
        Assert.True(restored.GetProperty("alreadyGranted").GetBoolean());
        Assert.Equal("eid", restored.GetProperty("items").GetProperty("decorSet").GetString());
        Assert.False(restored.GetProperty("wallet").TryGetProperty("gems", out _));
    }

    [Fact]
    public async Task AnAppleRefundNotificationTakesTheGemsBackOnce()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        string id = $"refunded-{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "gems.s", receipt = $"ok:{id}" })).EnsureSuccessStatusCode();
        // Spend some first: the refund takes what's left.
        (await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "energyRefill", requestId = "r-" + id })).EnsureSuccessStatusCode();

        var notification = new { signedPayload = Jws(new { notificationType = "REFUND", data = new { signedTransactionInfo = Jws(new { transactionId = id }) } }) };
        for (int i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.OK, (await api.Client().PostAsJsonAsync($"/games/{GameA}/stores/apple/notifications", notification)).StatusCode);

        var wallet = (await (await client.GetAsync($"/games/{GameA}/players/me/wallet")).Json()).GetProperty("wallet");
        Assert.Equal(0, wallet.TryGetProperty("gems", out var gems) ? gems.GetInt64() : 0);
        var again = await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "gems.s", receipt = $"ok:{id}" });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal("refunded", (await again.Json()).Str("error"));
    }

    [Fact]
    public async Task GoogleNotificationsNeedTheTokenAndVoidedPurchasesAreRefunded()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        string order = $"GPA.{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "google", sku = "gems.s", receipt = $"ok:{order}" })).EnsureSuccessStatusCode();
        string data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(new { voidedPurchaseNotification = new { orderId = order, purchaseToken = "t" } })));
        var push = new { message = new { data } };

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client().PostAsJsonAsync($"/games/{GameA}/stores/google/notifications?token=wrong", push)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.Client().PostAsJsonAsync($"/games/{GameA}/stores/google/notifications?token={GooglePushToken}", push)).StatusCode);
        var wallet = (await (await client.GetAsync($"/games/{GameA}/players/me/wallet")).Json()).GetProperty("wallet");
        Assert.Equal(0, wallet.TryGetProperty("gems", out var gems) ? gems.GetInt64() : 0);
    }

    [Fact]
    public async Task ServerTimeIsGivenInMilliseconds()
    {
        var time = await (await api.Client().GetAsync($"/games/{GameA}/time")).Json();
        var nowMs = time.GetProperty("nowMs").GetInt64();
        Assert.InRange(nowMs, DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeMilliseconds(), DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds());
    }
}

public sealed class RateLimitedFactory : PlatformFactory
{
    protected override int AuthPerMinute => 3;
}

public class RateLimitTests(RateLimitedFactory api) : IClassFixture<RateLimitedFactory>
{
    [Fact]
    public async Task GuestCreationIsLimitedPerAddress()
    {
        var codes = new List<HttpStatusCode>();
        for (int i = 0; i < 4; i++)
            codes.Add((await api.Client().PostAsJsonAsync($"/games/{GameA}/players/guest", new { device = "x" })).StatusCode);
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests], codes);
    }

    // Production traffic comes through Cloudflare and Traefik: the limit is per CF-Connecting-IP (Auth/ClientIp.cs).
    [Fact]
    public async Task TheLimitFollowsTheCloudflareClientAddress()
    {
        async Task<HttpStatusCode> GuestFrom(string ip)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/games/{GameA}/players/guest")
            {
                Content = JsonContent.Create(new { device = "x" }),
            };
            request.Headers.Add(ClientIp.Header, ip);
            return (await api.Client().SendAsync(request)).StatusCode;
        }

        var codes = new List<HttpStatusCode>();
        for (int i = 0; i < 4; i++) codes.Add(await GuestFrom("203.0.113.7"));
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests], codes);

        // Another player: every request reaches the app from the same proxy, yet they keep their own limit.
        Assert.Equal(HttpStatusCode.OK, await GuestFrom("198.51.100.20"));
    }
}
