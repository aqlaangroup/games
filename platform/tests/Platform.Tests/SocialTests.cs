using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Platform.Api.Social;
using Platform.Domain;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class SocialTests(PlatformFactory api)
{
    // A signed-in adult player: its client and player id.
    async Task<(HttpClient Client, string Id)> Adult()
    {
        var guest = await api.Guest();
        var client = api.Client(guest.Str("accessToken"));
        (await client.PatchAsJsonAsync($"/games/{GameA}/players/me", new { birthYear = 1990 })).EnsureSuccessStatusCode();
        return (client, guest.GetProperty("account").Str("playerId"));
    }

    async Task<(HttpClient A, string AId, HttpClient B, string BId)> Friends()
    {
        var (a, aId) = await Adult();
        var (b, bId) = await Adult();
        string code = (await (await b.GetAsync($"/games/{GameA}/players/me/social")).Json()).Str("friendCode");
        (await a.PostAsJsonAsync($"/games/{GameA}/players/me/friends", new { code })).EnsureSuccessStatusCode();
        return (a, aId, b, bId);
    }

    [Fact]
    public async Task ChildrenAndUnknownAgesGetNoSocialFeatures()
    {
        var unknown = api.Client((await api.Guest()).Str("accessToken"));
        Assert.Equal("parent_needed", (await (await unknown.GetAsync($"/games/{GameA}/players/me/social")).Json()).Str("error"));
        var child = api.Client((await api.Guest()).Str("accessToken"));
        (await child.PatchAsJsonAsync($"/games/{GameA}/players/me", new { birthYear = DateTime.UtcNow.Year - 9 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync($"/games/{GameA}/players/me/wall")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync($"/games/{GameA}/players/me/hosting")).StatusCode);
    }

    [Fact]
    public async Task FriendsAddEachOtherByCodeSendGiftsAndVisit()
    {
        var (a, aId, b, bId) = await Friends();
        var bSees = await (await b.GetAsync($"/games/{GameA}/players/me/social")).Json();
        Assert.Equal(aId, bSees.GetProperty("friends")[0].Str("playerId"));
        string aCode = (await (await a.GetAsync($"/games/{GameA}/players/me/social")).Json()).Str("friendCode");
        Assert.Equal("own_code", (await (await a.PostAsJsonAsync($"/games/{GameA}/players/me/friends", new { code = aCode })).Json()).Str("error"));
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/games/{GameA}/players/me/friends", new { code = "NOPE2345" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsJsonAsync($"/games/{GameA}/players/me/gifts", new { to = bId })).StatusCode);
        Assert.Equal("already_sent_today", (await (await a.PostAsJsonAsync($"/games/{GameA}/players/me/gifts", new { to = bId })).Json()).Str("error"));
        var claimed = await (await b.PostAsync($"/games/{GameA}/players/me/gifts/claim", null)).Json();
        Assert.Equal(5, claimed.GetProperty("energy").GetInt32());
        Assert.Equal(0, (await (await b.PostAsync($"/games/{GameA}/players/me/gifts/claim", null)).Json()).GetProperty("energy").GetInt32());

        var save = new { schemaVersion = 11, player = new { level = 7 }, story = new { chapter = 2, task = 3, choices = new { secret = "x" } },
            cafe = new { placed = new[] { new { id = "old_lantern", x = 3, z = 4, rot = 0 } }, taskStyles = new { sign = "old" }, cosiness = 12 } };
        (await b.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 0, save, device = "phone" })).EnsureSuccessStatusCode();
        var cafe = await (await a.GetAsync($"/games/{GameA}/players/me/friends/{bId}/cafe")).Json();
        Assert.Equal("old_lantern", cafe.GetProperty("cafe").GetProperty("placed")[0].Str("id"));
        Assert.Equal(3, cafe.GetProperty("story").GetProperty("task").GetInt32());
        Assert.False(cafe.GetProperty("story").TryGetProperty("choices", out _), "only what the café shows");
    }

    [Fact]
    public async Task FriendsFinishAHostingOrderTogetherAndBothArePaid()
    {
        var (host, hostId, helper, helperId) = await Friends();
        var order = await (await host.PostAsync($"/games/{GameA}/players/me/hosting", null)).Json();
        string id = order.Str("id");
        Assert.Equal("already_hosting", (await (await host.PostAsync($"/games/{GameA}/players/me/hosting", null)).Json()).Str("error"));
        (await host.PostAsJsonAsync($"/games/{GameA}/players/me/hosting/{id}/invite", new { friendId = helperId })).EnsureSuccessStatusCode();

        var invited = await (await helper.GetAsync($"/games/{GameA}/players/me/hosting")).Json();
        Assert.Equal(id, invited.GetProperty("helping")[0].Str("id"));
        (await helper.PostAsJsonAsync($"/games/{GameA}/players/me/hosting/{id}/message", new { preset = 3 })).EnsureSuccessStatusCode();
        (await helper.PostAsJsonAsync($"/games/{GameA}/players/me/hosting/{id}/give", new { count = 2 })).EnsureSuccessStatusCode();
        Assert.Equal("invalid_count", (await (await host.PostAsJsonAsync($"/games/{GameA}/players/me/hosting/{id}/give", new { count = 9 })).Json()).Str("error"));
        var done = await (await host.PostAsJsonAsync($"/games/{GameA}/players/me/hosting/{id}/give", new { count = 4 })).Json();
        Assert.Equal("done", done.Str("status"));

        long Gems(JsonElement wallet) => wallet.GetProperty("wallet").TryGetProperty("gems", out var g) ? g.GetInt64() : 0;
        Assert.Equal(15, Gems(await (await host.GetAsync($"/games/{GameA}/players/me/wallet")).Json()));
        Assert.Equal(5, Gems(await (await helper.GetAsync($"/games/{GameA}/players/me/wallet")).Json()));
        var friends = await (await host.GetAsync($"/games/{GameA}/players/me/social")).Json();
        Assert.Equal(10, friends.GetProperty("friends")[0].GetProperty("points").GetInt32());
        var hosting = await (await host.GetAsync($"/games/{GameA}/players/me/hosting")).Json();
        Assert.False(hosting.GetProperty("canStart").GetBoolean(), "once a week here");
        Assert.Equal("jallab", hosting.GetProperty("mine").Str("decor")); // the host's first finished order: the first piece

        // The helper sees the finished order too, for the shared postcard.
        var helped = (await (await helper.GetAsync($"/games/{GameA}/players/me/hosting")).Json()).GetProperty("helped");
        Assert.Equal(id, helped[0].Str("id"));
        Assert.Equal(1, helped[0].GetProperty("helpers").GetInt32());
    }

    [Fact]
    public async Task AHostAloneGetsTheSmallerReward()
    {
        var (host, _) = await Adult();
        string id = (await (await host.PostAsync($"/games/{GameA}/players/me/hosting", null)).Json()).Str("id");
        (await host.PostAsJsonAsync($"/games/{GameA}/players/me/hosting/{id}/give", new { count = 6 })).EnsureSuccessStatusCode();
        var wallet = await (await host.GetAsync($"/games/{GameA}/players/me/wallet")).Json();
        Assert.Equal(8, wallet.GetProperty("wallet").GetProperty("gems").GetInt64()); // 15 × 0.5, rounded
    }

    [Fact]
    public async Task AWallPostShowsOnlyAfterAPersonApprovesItAndReportsSendItBack()
    {
        var (writer, _) = await Adult();
        var (reader, _) = await Adult();
        var (other, _) = await Adult();
        var link = await writer.PostAsJsonAsync($"/games/{GameA}/players/me/wall", new { text = "زوروا موقعي www.example.com" });
        Assert.Equal("refused_link", (await link.Json()).Str("error"));
        var (writer2, _) = await Adult();
        var posted = await (await writer2.PostAsJsonAsync($"/games/{GameA}/players/me/wall", new { text = "اللي ما بيعرف الصقر بيشويه" })).Json();
        string id = posted.Str("id");
        Assert.Equal("pending", posted.Str("status"));
        Assert.Empty((await (await reader.GetAsync($"/games/{GameA}/players/me/wall")).Json()).GetProperty("posts").EnumerateArray());

        var (_, support) = await api.Staff($"support-{Guid.NewGuid():N}@wall.test", (GameA, StaffRole.Support));
        var queue = await (await api.Client(support).GetAsync($"/games/{GameA}/admin/wall")).Json();
        Assert.Contains(queue.EnumerateArray(), p => p.Str("id") == id);
        (await api.Client(support).PostAsync($"/games/{GameA}/admin/wall/{id}/approve", null)).EnsureSuccessStatusCode();

        var seen = await (await reader.GetAsync($"/games/{GameA}/players/me/wall")).Json();
        Assert.Equal("اللي ما بيعرف الصقر بيشويه", seen.GetProperty("posts")[0].Str("text"));
        Assert.False(seen.GetProperty("posts")[0].TryGetProperty("playerId", out _), "no names");
        Assert.True((await (await reader.PostAsync($"/games/{GameA}/players/me/wall/{id}/like", null)).Json()).GetProperty("liked").GetBoolean());

        (await reader.PostAsync($"/games/{GameA}/players/me/wall/{id}/report", null)).EnsureSuccessStatusCode();
        Assert.Empty((await (await reader.GetAsync($"/games/{GameA}/players/me/wall")).Json()).GetProperty("posts").EnumerateArray());
        (await other.PostAsync($"/games/{GameA}/players/me/wall/{id}/report", null)).EnsureSuccessStatusCode();
        var back = await (await api.Client(support).GetAsync($"/games/{GameA}/admin/wall")).Json();
        Assert.Contains(back.EnumerateArray(), p => p.Str("id") == id && p.Str("autoReason") == "reports");
    }

    [Theory]
    [InlineData("كلّمني على ٠٧٩٥١٢٣٤٥٦٧", "phone")]
    [InlineData("بريدي x@y.co", "email")]
    [InlineData("t.me/somebody", "link")]
    [InlineData("القهوة بدها صبر", null)]
    public void TheWallFilterCatchesContactDetailsInArabicToo(string text, string? reason) => Assert.Equal(reason, WallFilter.Refuse(text));

    [Fact]
    public void FoldingIgnoresDiacriticsTatweelAndAlefForms() =>
        Assert.Equal(WallFilter.Fold("القهوه"), WallFilter.Fold("القَهْـــوة"));
}
