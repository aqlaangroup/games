using System.Net;
using System.Net.Http.Json;
using Platform.Domain;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class EventTests(PlatformFactory api)
{
    // Game B: no other test sends events there, so the metrics count only these players.
    [Fact]
    public async Task EventsFeedTheTesterGateNumbers()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var first = api.Client((await api.Guest(GameB)).Str("accessToken"));
        var second = api.Client((await api.Guest(GameB)).Str("accessToken"));

        var sent = await first.PostAsJsonAsync($"/games/{GameB}/players/me/events", new
        {
            gameVersion = "0.1.0",
            device = "iPhone15,2",
            events = new object[]
            {
                new { name = "session_start", sessionId = "s1", timeMs = now, fields = new { energy = 100 } },
                new { name = "tutorial_step", sessionId = "s1", timeMs = now, fields = new { step = 1 } },
                new { name = "tutorial_step", sessionId = "s1", timeMs = now, fields = new { step = 2 } },
                new { name = "memory_played", sessionId = "s1", timeMs = now, fields = new { memory = "m1_pour", stars = 3 } },
                new { name = "session_end", sessionId = "s1", timeMs = now, fields = new { seconds = 300 } },
                new { name = "Bad Name!", sessionId = "s1", timeMs = now, fields = new { } },
            },
        });
        Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        Assert.Equal(5, (await sent.Json()).GetProperty("accepted").GetInt32());

        await second.PostAsJsonAsync($"/games/{GameB}/players/me/events", new
        {
            events = new object[]
            {
                new { name = "session_start", sessionId = "s2", timeMs = now, fields = new { } },
                new { name = "tutorial_step", sessionId = "s2", timeMs = now, fields = new { step = 1 } },
                new { name = "client_error", sessionId = "s2", timeMs = now, fields = new { message = "NullReferenceException" } },
                new { name = "session_end", sessionId = "s2", timeMs = now, fields = new { seconds = 100 } },
            },
        });

        var (_, viewer) = await api.Staff($"metrics-{Guid.NewGuid():N}@aqlaan.com", (GameB, StaffRole.Viewer));
        var metrics = await (await api.Client(viewer).GetAsync($"/games/{GameB}/admin/metrics?days=7")).Json();
        Assert.Equal(2, metrics.GetProperty("players").GetInt32());
        Assert.Equal(2, metrics.GetProperty("tutorialStepPlayers").GetProperty("1").GetInt32());
        Assert.Equal(1, metrics.GetProperty("tutorialStepPlayers").GetProperty("2").GetInt32());
        Assert.Equal(1, metrics.GetProperty("reachedFirstMemory").GetInt32());
        Assert.Equal(200, metrics.GetProperty("averageSessionSeconds").GetInt32());
        Assert.Equal(0.5, metrics.GetProperty("errorFreeSessions").GetDouble());
    }

    [Fact]
    public async Task AnEmptyBatchIsRefusedAndPlayersCantReadMetrics()
    {
        var guest = await api.Guest(GameB);
        var client = api.Client(guest.Str("accessToken"));
        var empty = await client.PostAsJsonAsync($"/games/{GameB}/players/me/events", new { events = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/games/{GameB}/admin/metrics")).StatusCode);
    }
}
