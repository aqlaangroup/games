using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Platform.Api.Players;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class AccountTests(PlatformFactory api)
{
    static string NewSubject() => $"001234.{Guid.NewGuid():N}.0001";

    [Fact]
    public async Task GuestLinksAppleThenANewPhoneSignsInToTheSamePlayer()
    {
        var guest = await api.Guest();
        Assert.Equal("unknown", guest.GetProperty("account").Str("ageBand"));
        Assert.Equal(0, guest.GetProperty("account").GetProperty("providers").GetArrayLength());

        var subject = NewSubject();
        var link = await api.Client(guest.Str("accessToken"))
            .PostAsJsonAsync($"/games/{GameA}/players/me/link", new { provider = "apple", idToken = api.Jwks.Apple(subject, nonce: "n1"), nonce = "n1" });
        Assert.Equal(HttpStatusCode.OK, link.StatusCode);
        Assert.Equal("apple", (await link.Json()).GetProperty("account").GetProperty("providers")[0].GetString());

        var newPhone = await api.Client().PostAsJsonAsync($"/games/{GameA}/players/sign-in",
            new { provider = "apple", idToken = api.Jwks.Apple(subject) });
        Assert.Equal(HttpStatusCode.OK, newPhone.StatusCode);
        Assert.Equal(guest.Str("playerId"), (await newPhone.Json()).Str("playerId"));
    }

    [Fact]
    public async Task SignInWithANewIdentityCreatesAPlayerOnce()
    {
        var token = api.Jwks.Google(NewSubject());
        var first = await (await api.Client().PostAsJsonAsync($"/games/{GameA}/players/sign-in", new { provider = "google", idToken = token })).Json();
        var again = await (await api.Client().PostAsJsonAsync($"/games/{GameA}/players/sign-in", new { provider = "google", idToken = token })).Json();
        Assert.Equal(first.Str("playerId"), again.Str("playerId"));
        Assert.Equal("google", first.GetProperty("account").GetProperty("providers")[0].GetString());
    }

    [Fact]
    public async Task LinkingAnIdentityThatHasItsOwnPlayerAnswersIdentityTaken()
    {
        var subject = NewSubject();
        var owner = await (await api.Client().PostAsJsonAsync($"/games/{GameA}/players/sign-in",
            new { provider = "apple", idToken = api.Jwks.Apple(subject) })).Json();
        await api.Client(owner.Str("accessToken")).PutAsJsonAsync($"/games/{GameA}/players/me/save",
            new { baseVersion = 0, save = new { player = new { level = 1 }, story = new { chapter = 1 } }, device = "old phone" });

        var guest = await api.Guest();
        var link = await api.Client(guest.Str("accessToken"))
            .PostAsJsonAsync($"/games/{GameA}/players/me/link", new { provider = "apple", idToken = api.Jwks.Apple(subject) });
        Assert.Equal(HttpStatusCode.Conflict, link.StatusCode);
        var body = await link.Json();
        Assert.Equal("identity_taken", body.Str("error"));
        Assert.Equal(owner.Str("playerId"), body.GetProperty("other").Str("playerId"));
        var save = body.GetProperty("other").GetProperty("save");
        Assert.Equal(1, save.GetProperty("version").GetInt64());
        Assert.Equal(1, save.GetProperty("summary").GetProperty("chapter").GetInt32());
        Assert.Equal("old phone", save.Str("device"));
    }

    [Fact]
    public async Task APlayerLinksOneIdentityPerProvider()
    {
        var guest = await api.Guest();
        var client = api.Client(guest.Str("accessToken"));
        await client.PostAsJsonAsync($"/games/{GameA}/players/me/link", new { provider = "apple", idToken = api.Jwks.Apple(NewSubject()) });
        var second = await client.PostAsJsonAsync($"/games/{GameA}/players/me/link", new { provider = "apple", idToken = api.Jwks.Apple(NewSubject()) });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("provider_already_linked", (await second.Json()).Str("error"));
    }

    [Fact]
    public async Task IdentityTokensMustBeForThisGameUnexpiredSignedAndMatchTheNonce()
    {
        async Task<HttpStatusCode> SignIn(string token, string? nonce = null, string provider = "apple", string game = GameA) =>
            (await api.Client().PostAsJsonAsync($"/games/{game}/players/sign-in", new { provider, idToken = token, nonce })).StatusCode;

        var subject = NewSubject();
        Assert.Equal(HttpStatusCode.Unauthorized, await SignIn(api.Jwks.Apple(subject, audience: "com.someone.else")));
        Assert.Equal(HttpStatusCode.Unauthorized, await SignIn(api.Jwks.Apple(subject, expires: DateTime.UtcNow.AddMinutes(-10))));
        Assert.Equal(HttpStatusCode.Unauthorized, await SignIn(api.Jwks.Apple(subject, signWith: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" })));
        Assert.Equal(HttpStatusCode.Unauthorized, await SignIn(api.Jwks.Apple(subject, nonce: "a"), nonce: "b"));
        Assert.Equal(HttpStatusCode.Unauthorized, await SignIn(api.Jwks.Google(subject, audience: "other-client"), provider: "google"));
        // Test B has no Google client ids yet, like gahwit-sitti before the Google Cloud setup.
        Assert.Equal(HttpStatusCode.BadRequest, await SignIn(api.Jwks.Google(subject), provider: "google", game: GameB));
        Assert.Equal(HttpStatusCode.BadRequest, await SignIn(api.Jwks.Apple(subject), provider: "facebook"));
        Assert.Equal(HttpStatusCode.OK, await SignIn(api.Jwks.Apple(subject, nonce: "a"), nonce: "a"));
    }

    [Fact]
    public async Task RefreshTokensWorkOnceAndAReusedOneEndsTheSession()
    {
        var guest = await api.Guest();
        var first = guest.Str("refreshToken");

        var refreshed = await api.Client().PostAsJsonAsync($"/games/{GameA}/players/refresh", new { refreshToken = first });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var second = (await refreshed.Json()).Str("refreshToken");
        Assert.NotEqual(first, second);

        // The old token comes back: someone copied it. Both it and the one that replaced it stop working.
        var reused = await api.Client().PostAsJsonAsync($"/games/{GameA}/players/refresh", new { refreshToken = first });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        var afterReuse = await api.Client().PostAsJsonAsync($"/games/{GameA}/players/refresh", new { refreshToken = second });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);

        var garbage = await api.Client().PostAsJsonAsync($"/games/{GameA}/players/refresh", new { refreshToken = "nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, garbage.StatusCode);
    }

    [Fact]
    public async Task OnlyTheHashOfARefreshTokenIsStored()
    {
        var guest = await api.Guest();
        var token = guest.Str("refreshToken");
        var stored = await api.WithDb(db => Task.FromResult(db.Sessions.Any(s => s.TokenHash == token)));
        var hashed = await api.WithDb(db => Task.FromResult(db.Sessions.Any(s => s.TokenHash == Platform.Api.Auth.TokenService.Hash(token))));
        Assert.False(stored);
        Assert.True(hashed);
    }

    [Fact]
    public async Task SignOutEndsThatSession()
    {
        var guest = await api.Guest();
        var signOut = await api.Client(guest.Str("accessToken"))
            .PostAsJsonAsync($"/games/{GameA}/players/sign-out", new { refreshToken = guest.Str("refreshToken") });
        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);
        var refresh = await api.Client().PostAsJsonAsync($"/games/{GameA}/players/refresh", new { refreshToken = guest.Str("refreshToken") });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task AGamesTokenDoesNotWorkOnAnotherGame()
    {
        var guest = await api.Guest(GameA);
        Assert.Equal(HttpStatusCode.OK, (await api.Client(guest.Str("accessToken")).GetAsync($"/games/{GameA}/players/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client(guest.Str("accessToken")).GetAsync($"/games/{GameB}/players/me")).StatusCode);
        var refresh = await api.Client().PostAsJsonAsync($"/games/{GameB}/players/refresh", new { refreshToken = guest.Str("refreshToken") });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task UnknownGamesAndMissingTokensAreRefused()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client().PostAsJsonAsync("/games/nope/players/guest", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client().GetAsync($"/games/{GameA}/players/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client("not-a-token").GetAsync($"/games/{GameA}/players/me")).StatusCode);
    }

    [Fact]
    public async Task BirthYearSetsTheAgeBandCountingTheYoungerPossibleAge()
    {
        int year = DateTime.UtcNow.Year;
        var guest = await api.Guest();
        var client = api.Client(guest.Str("accessToken"));

        async Task<string> Band(int birthYear) =>
            (await (await client.PatchAsJsonAsync($"/games/{GameA}/players/me", new { birthYear })).Json()).Str("ageBand");

        Assert.Equal("under13", await Band(year - 13));   // 12 or 13: counted as 12
        Assert.Equal("13to15", await Band(year - 14));
        Assert.Equal("13to15", await Band(year - 16));    // 15 or 16: counted as 15
        Assert.Equal("16plus", await Band(year - 17));

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/games/{GameA}/players/me", new { birthYear = 1899 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/games/{GameA}/players/me", new { birthYear = year + 1 })).StatusCode);

        var me = await (await client.PatchAsJsonAsync($"/games/{GameA}/players/me", new { language = "en", country = "JO" })).Json();
        Assert.Equal("en", me.Str("language"));
        Assert.Equal("JO", me.Str("country"));
        Assert.Equal(year - 17, me.GetProperty("birthYear").GetInt32());
    }

    [Theory]
    [InlineData(null, 2026, "unknown")]
    [InlineData(2014, 2026, "under13")]
    [InlineData(2013, 2026, "under13")]
    [InlineData(2012, 2026, "13to15")]
    [InlineData(2010, 2026, "13to15")]
    [InlineData(2009, 2026, "16plus")]
    public void AgeBandRule(int? birthYear, int currentYear, string band) =>
        Assert.Equal(band, AgeBands.For(birthYear, currentYear));
}
