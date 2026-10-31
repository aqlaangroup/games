using System.Net;
using System.Net.Http.Json;
using System.Text;
using Platform.Domain;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class LiveOpsTests(PlatformFactory api)
{
    static string Email(string who) => $"{who}-{Guid.NewGuid():N}@liveops.test";

    static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Fact]
    public async Task AProducerPublishesAConfigThatTheGameGetsAndCanGoBack()
    {
        var (_, producer) = await api.Staff(Email("producer"), (GameA, StaffRole.Producer));
        var (_, viewer) = await api.Staff(Email("viewer"), (GameA, StaffRole.Viewer));
        var staff = api.Client(producer);
        int before = int.Parse((await api.Client().GetAsync($"/games/{GameA}/config")).Headers.GetValues("X-Config-Version").Single());

        string json = """{ "energy": {}, "orders": {}, "board": {}, "chains": {}, "formulas": {}, "chapters": [], "marker": "new" }""";
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Client(viewer).PostAsync($"/games/{GameA}/admin/config", new StringContent(json, Encoding.UTF8, "application/json"))).StatusCode);
        var broken = await staff.PostAsync($"/games/{GameA}/admin/config", new StringContent("""{ "energy": {} }""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, broken.StatusCode);

        var published = await (await staff.PostAsync($"/games/{GameA}/admin/config", new StringContent(json, Encoding.UTF8, "application/json"))).Json();
        int version = published.GetProperty("version").GetInt32();
        var served = await api.Client().GetAsync($"/games/{GameA}/config");
        Assert.Equal(version.ToString(), served.Headers.GetValues("X-Config-Version").Single());
        Assert.Equal("new", (await served.Json()).Str("marker"));

        (await staff.PostAsync($"/games/{GameA}/admin/config/{before}/activate", null)).EnsureSuccessStatusCode();
        Assert.Equal(before.ToString(), (await api.Client().GetAsync($"/games/{GameA}/config")).Headers.GetValues("X-Config-Version").Single());
    }

    [Fact]
    public async Task AScheduledEventReachesTheGameWhileItRuns()
    {
        var (_, producer) = await api.Staff(Email("producer"), (GameA, StaffRole.Producer));
        var staff = api.Client(producer);
        var now = DateTimeOffset.UtcNow;
        var refused = await staff.PostAsJsonAsync($"/games/{GameA}/admin/events", new { template = "race", startsAt = now, endsAt = now.AddDays(1) });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

        var live = await (await staff.PostAsJsonAsync($"/games/{GameA}/admin/events",
            new { template = "miniBoard", startsAt = now.AddMinutes(-1), endsAt = now.AddDays(4), settings = new { theme = "olives" } })).Json();
        string id = live.Str("id");
        var events = await (await api.Client().GetAsync($"/games/{GameA}/events")).Json();
        Assert.Contains(events.EnumerateArray(), e => e.Str("id") == id);

        (await staff.DeleteAsync($"/games/{GameA}/admin/events/{id}")).EnsureSuccessStatusCode();
        events = await (await api.Client().GetAsync($"/games/{GameA}/events")).Json();
        Assert.DoesNotContain(events.EnumerateArray(), e => e.Str("id") == id);
    }

    [Fact]
    public async Task TheDailyGiftFollowsItsCycleOncePerDay()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        var first = await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"daily:{Today}" })).Json();
        Assert.Equal(0, first.GetProperty("granted").GetProperty("gems").GetInt64()); // day 1 is energy
        var again = await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"daily:{Today}" })).Json();
        Assert.True(again.GetProperty("alreadyClaimed").GetBoolean());

        string tomorrow = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
        var second = await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"daily:{tomorrow}" })).Json();
        Assert.Equal(5, second.GetProperty("granted").GetProperty("gems").GetInt64()); // day 2

        var far = await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = "daily:2020-01-01" });
        Assert.Equal("wrong_day", (await far.Json()).Str("error"));
    }

    [Fact]
    public async Task DailyTasksPayUpToTheDailyLimit()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        foreach (var task in new[] { "serve_10", "merge_30" })
            Assert.Equal(2, (await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"task:{Today}:{task}" })).Json())
                .GetProperty("granted").GetProperty("gems").GetInt64());
        var third = await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"task:{Today}:sell_5" });
        Assert.Equal("daily_limit", (await third.Json()).Str("error"));
        var unknown = await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"task:{Today}:nope" });
        Assert.Equal("unknown_reward", (await unknown.Json()).Str("error"));
    }

    [Fact]
    public async Task AMiniBoardMilestonePaysItsGemsOnce()
    {
        var (_, producer) = await api.Staff(Email("producer"), (GameA, StaffRole.Producer));
        var now = DateTimeOffset.UtcNow;
        var live = await (await api.Client(producer).PostAsJsonAsync($"/games/{GameA}/admin/events",
            new { template = "miniBoard", startsAt = now.AddMinutes(-1), endsAt = now.AddDays(4) })).Json();
        var client = api.Client((await api.Guest()).Str("accessToken"));
        string id = live.Str("id");
        Assert.Equal(5, (await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"event:{id}:2" })).Json())
            .GetProperty("granted").GetProperty("gems").GetInt64());
        Assert.True((await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"event:{id}:2" })).Json())
            .GetProperty("alreadyClaimed").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = $"event:{id}:9" })).StatusCode);
    }

    [Fact]
    public async Task EachGamesLegalPagesArePublicAndOnlyThoseTwo()
    {
        var privacy = await api.Client().GetAsync($"/games/{GameA}/legal/privacy?lang=ar");
        Assert.Equal(HttpStatusCode.OK, privacy.StatusCode);
        Assert.Equal("text/html", privacy.Content.Headers.ContentType!.MediaType);
        Assert.Contains("خصوصية", await privacy.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client().GetAsync($"/games/{GameA}/legal/privacy?lang=en")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client().GetAsync($"/games/{GameA}/legal/..%2Fgame.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client().GetAsync($"/games/{GameA}/legal/secrets")).StatusCode);
    }
}
