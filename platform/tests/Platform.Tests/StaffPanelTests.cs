using System.Net;
using System.Net.Http.Json;
using Platform.Api.Admin;
using Platform.Domain;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class StaffPanelTests(PlatformFactory api)
{
    static string Email(string who) => $"{who}-{Guid.NewGuid():N}@panel.test";

    [Fact]
    public void TotpMatchesTheRfcTestVector()
    {
        // RFC 6238 appendix B, SHA-1: the secret "12345678901234567890" at 59 seconds gives 94287082 (8 digits).
        const string secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ"; // base32 of the ASCII secret
        Assert.Equal("287082", Totp.Code(secret, Totp.Step(DateTimeOffset.FromUnixTimeSeconds(59))));
    }

    [Fact]
    public async Task TwoFactorAsksForACodeAndACodeWorksOnce()
    {
        string email = Email("support");
        var (_, token) = await api.Staff(email, (GameA, StaffRole.Support));
        var staff = api.Client(token);
        var setUp = await (await staff.PostAsync("/admin/me/two-factor", null)).Json();
        string secret = setUp.Str("secret");
        Assert.StartsWith("otpauth://totp/", setUp.Str("uri"));

        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/admin/me/two-factor/enable", new { code = "000000" })).StatusCode);
        string code = Totp.Code(secret, Totp.Step(DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.NoContent, (await staff.PostAsJsonAsync("/admin/me/two-factor/enable", new { code })).StatusCode);

        const string password = "correct-horse-battery";
        var noCode = await api.Client().PostAsJsonAsync("/admin/sign-in", new { email, password });
        Assert.Equal("code_required", (await noCode.Json()).Str("error"));
        var reused = await api.Client().PostAsJsonAsync("/admin/sign-in", new { email, password, code });
        Assert.Equal("invalid_code", (await reused.Json()).Str("error"));
        string next = Totp.Code(secret, Totp.Step(DateTimeOffset.UtcNow) + 1);
        var signedIn = await (await api.Client().PostAsJsonAsync("/admin/sign-in", new { email, password, code = next })).Json();
        Assert.True(signedIn.GetProperty("admin").GetProperty("twoFactorEnabled").GetBoolean());
    }

    [Fact]
    public async Task StaffSeeTheGamePurchasesDeletionsConfigAndRevenue()
    {
        var (_, viewerToken) = await api.Staff(Email("viewer"), (GameA, StaffRole.Viewer));
        var (_, supportToken) = await api.Staff(Email("support"), (GameA, StaffRole.Support));
        var viewer = api.Client(viewerToken);

        var game = await (await viewer.GetAsync($"/games/{GameA}/admin/game")).Json();
        Assert.Contains(game.GetProperty("products").EnumerateArray(), p => p.Str("sku") == "gems.s");

        var player = await api.Guest();
        var client = api.Client(player.Str("accessToken"));
        string transaction = $"panel-{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "gems.s", receipt = $"ok:{transaction}" })).EnsureSuccessStatusCode();
        var found = await (await viewer.GetAsync($"/games/{GameA}/admin/purchases?query={transaction}")).Json();
        Assert.Equal("gems.s", found[0].Str("sku"));

        var revenue = await (await viewer.GetAsync($"/games/{GameA}/admin/revenue?days=7")).Json();
        Assert.Contains(revenue.GetProperty("bySku").EnumerateArray(), r => r.Str("sku") == "gems.s" && r.GetProperty("sales").GetInt32() >= 1);

        int version = (await (await viewer.GetAsync($"/games/{GameA}/admin/config")).Json())[0].GetProperty("version").GetInt32();
        var text = await viewer.GetAsync($"/games/{GameA}/admin/config/{version}");
        Assert.Equal(HttpStatusCode.OK, text.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"/games/{GameA}/admin/deletions")).StatusCode);
        (await client.DeleteAsync($"/games/{GameA}/players/me")).EnsureSuccessStatusCode();
        var deletions = await (await api.Client(supportToken).GetAsync($"/games/{GameA}/admin/deletions")).Json();
        Assert.Contains(deletions.EnumerateArray(), d => d.Str("playerId") == player.GetProperty("account").Str("playerId"));
    }

    [Fact]
    public async Task ThePanelIsServedWithStrictHeaders()
    {
        var page = await api.Client().GetAsync("/staff/");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("default-src 'self'", page.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("admin.js", await page.Content.ReadAsStringAsync());
    }
}
