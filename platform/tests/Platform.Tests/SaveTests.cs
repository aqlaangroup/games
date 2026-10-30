using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class SaveTests(PlatformFactory api)
{
    static object Save(int level, int chapter, long coins = 0) =>
        new { schemaVersion = 1, player = new { level, coins }, story = new { chapter } };

    [Fact]
    public async Task ANewPlayerHasNoSaveThenGetsBackWhatItUploaded()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync($"/games/{GameA}/players/me/save")).StatusCode);

        var put = await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 0, save = Save(2, 1, 40), device = "iPhone 14 Pro" });
        Assert.Equal(1, (await put.Json()).GetProperty("version").GetInt64());

        var got = await (await client.GetAsync($"/games/{GameA}/players/me/save")).Json();
        Assert.Equal(1, got.GetProperty("version").GetInt64());
        Assert.Equal(2, got.GetProperty("save").GetProperty("player").GetProperty("level").GetInt32());
        Assert.Equal(2, got.GetProperty("summary").GetProperty("level").GetInt32());
        Assert.Equal(1, got.GetProperty("summary").GetProperty("chapter").GetInt32());
        Assert.Equal("iPhone 14 Pro", got.Str("device"));
    }

    [Fact]
    public async Task AnUploadFromAnOlderVersionConflictsUntilForced()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 0, save = Save(1, 1), device = "phone A" });
        await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 1, save = Save(3, 2), device = "phone A" });

        // Phone B last synced at version 1.
        var stale = await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 1, save = Save(2, 1), device = "phone B" });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var body = await stale.Json();
        Assert.Equal("conflict", body.Str("error"));
        var server = body.GetProperty("server");
        Assert.Equal(2, server.GetProperty("version").GetInt64());
        Assert.Equal(3, server.GetProperty("summary").GetProperty("level").GetInt32());
        Assert.Equal(2, server.GetProperty("summary").GetProperty("chapter").GetInt32());
        Assert.Equal("phone A", server.Str("device"));

        // The player chose "keep this phone".
        var forced = await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 1, save = Save(2, 1), device = "phone B", force = true });
        Assert.Equal(3, (await forced.Json()).GetProperty("version").GetInt64());
        var got = await (await client.GetAsync($"/games/{GameA}/players/me/save")).Json();
        Assert.Equal("phone B", got.Str("device"));
    }

    [Fact]
    public async Task ImpossibleJumpsAreFlaggedButTheSaveIsKept()
    {
        var guest = await api.Guest();
        var id = Guid.Parse(guest.Str("playerId"));
        var client = api.Client(guest.Str("accessToken"));
        await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 0, save = Save(2, 1, 100) });
        var put = await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 1, save = Save(40, 9, 9_000_000) });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var reasons = await api.WithDb(db => db.SaveFlags.Where(f => f.PlayerId == id).Select(f => f.Reason).OrderBy(r => r).ToListAsync());
        Assert.Equal(["chapter_jump", "coins_jump", "level_jump"], reasons);
        Assert.Equal(40, (await (await client.GetAsync($"/games/{GameA}/players/me/save")).Json()).GetProperty("summary").GetProperty("level").GetInt32());
    }

    [Fact]
    public async Task NormalProgressIsNotFlagged()
    {
        var guest = await api.Guest();
        var id = Guid.Parse(guest.Str("playerId"));
        var client = api.Client(guest.Str("accessToken"));
        await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 0, save = Save(1, 1, 0) });
        await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 1, save = Save(2, 1, 300) });
        Assert.False(await api.WithDb(db => db.SaveFlags.AnyAsync(f => f.PlayerId == id)));
    }

    [Fact]
    public async Task ASaveMustBeAJsonObject()
    {
        var client = api.Client((await api.Guest()).Str("accessToken"));
        var put = await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 0, save = "not an object" });
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }
}
