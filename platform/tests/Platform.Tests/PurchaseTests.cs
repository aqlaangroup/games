using System.Net;
using System.Net.Http.Json;
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
}
