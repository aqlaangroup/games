using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Platform.Api.Players;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class RewardTests(PlatformFactory api)
{
    static object Save(int level, int chapter, int task = 0) =>
        new { schemaVersion = 2, player = new { level }, story = new { chapter, task } };

    static async Task<(HttpClient Client, Guid Id)> PlayerAt(PlatformFactory api, int level, int chapter, int task = 0)
    {
        var guest = await api.Guest();
        var client = api.Client(guest.Str("accessToken"));
        var put = await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 0, save = Save(level, chapter, task), device = "test" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        return (client, Guid.Parse(guest.Str("playerId")));
    }

    static long Gems(JsonElement reply) =>
        reply.GetProperty("wallet").TryGetProperty("gems", out var gems) ? gems.GetInt64() : 0;

    [Fact]
    public async Task ALevelUpGrantsItsGemsOnceEvenWhenAskedTwice()
    {
        var (client, _) = await PlayerAt(api, level: 7, chapter: 1);

        var first = await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = "levelUp:7" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(5 + 7 / 2, Gems(await first.Json()));

        var again = await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = "levelUp:7" })).Json();
        Assert.True(again.GetProperty("alreadyClaimed").GetBoolean());
        Assert.Equal(8, Gems(again));
    }

    [Fact]
    public async Task ARewardTheSaveDoesNotShowYetWaits()
    {
        var (client, _) = await PlayerAt(api, level: 3, chapter: 1);
        var early = await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = "levelUp:4" });
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal("not_earned_yet", (await early.Json()).Str("error"));

        var chapter = await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = "chapter:1" });
        Assert.Equal(HttpStatusCode.Conflict, chapter.StatusCode);
        var unknown = await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = "gift:1" });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [Fact]
    public async Task AFinishedChapterPaysItsGemsIncludingTheLastChapter()
    {
        var (client, _) = await PlayerAt(api, level: 5, chapter: 2, task: 1);
        Assert.Equal(20, Gems(await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = "chapter:1" })).Json()));
        // Chapter 2 is the test game's last: its one task is done.
        Assert.Equal(45, Gems(await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/rewards", new { id = "chapter:2" })).Json()));
    }

    [Fact]
    public async Task SpendingTakesGemsOncePerRequestAndRefillsCostMoreEachTime()
    {
        var (client, id) = await PlayerAt(api, level: 2, chapter: 1);
        var game = await api.WithDb(db => Task.FromResult(db.Games.Single(g => g.Slug == GameA)));
        await api.With<WalletService, int>(async w => { await w.AddAsync(game.Id, id, "gems", 100, "admin", "test", null, null, default); return 0; });

        var refill = await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "energyRefill", requestId = "r1" })).Json();
        Assert.Equal(30, refill.GetProperty("gems").GetInt64());
        Assert.Equal(100, refill.GetProperty("grant").GetProperty("energy").GetInt32());
        Assert.Equal(70, Gems(refill));

        var repeated = await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "energyRefill", requestId = "r1" })).Json();
        Assert.True(repeated.GetProperty("alreadySpent").GetBoolean());
        Assert.Equal(70, Gems(repeated));

        var second = await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "energyRefill", requestId = "r2" })).Json();
        Assert.Equal(45, second.GetProperty("gems").GetInt64());
        Assert.Equal(25, Gems(second));

        var tooMuch = await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "storageSlot:1", requestId = "r3" });
        Assert.Equal(HttpStatusCode.Conflict, tooMuch.StatusCode);
        Assert.Equal("not_enough_gems", (await tooMuch.Json()).Str("error"));
        Assert.Equal(25, Gems(await (await client.GetAsync($"/games/{GameA}/players/me/wallet")).Json()));
    }

    [Fact]
    public async Task AStorageSlotIsBoughtOnlyOnceAndBubblesDoubleByLevel()
    {
        var (client, id) = await PlayerAt(api, level: 2, chapter: 1);
        var game = await api.WithDb(db => Task.FromResult(db.Games.Single(g => g.Slug == GameA)));
        await api.With<WalletService, int>(async w => { await w.AddAsync(game.Id, id, "gems", 200, "admin", "test", null, null, default); return 0; });

        Assert.Equal(30, (await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "storageSlot:1", requestId = "s1" })).Json()).GetProperty("gems").GetInt64());
        var twice = await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "storageSlot:1", requestId = "s2" });
        Assert.Equal("already_owned", (await twice.Json()).Str("error"));
        Assert.Equal(20, (await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "bubble:6", requestId = "s3" })).Json()).GetProperty("gems").GetInt64());
        Assert.Equal(10, (await (await client.PostAsJsonAsync($"/games/{GameA}/players/me/spend", new { item = "memoryRetry:m2_rhythm", requestId = "s4" })).Json()).GetProperty("gems").GetInt64());
    }

    [Fact]
    public async Task TheGameLearnsItsMinimumVersionAndItsPublishedConfig()
    {
        var client = api.Client();
        Assert.Equal("1.2.0", (await (await client.GetAsync($"/games/{GameA}/version")).Json()).Str("minVersion"));
        var config = await (await client.GetAsync($"/games/{GameA}/config")).Json();
        Assert.Equal(20, config.GetProperty("chapters")[0].GetProperty("reward").GetProperty("gems").GetInt32());
    }
}
