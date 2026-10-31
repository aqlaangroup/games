using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
        // Midday a week ago, so the first player's first day is exactly 7 days back whatever the time now.
        long weekAgo = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-7).AddHours(12), TimeSpan.Zero).ToUnixTimeMilliseconds();
        var first = api.Client((await api.Guest(GameB)).Str("accessToken"));
        var secondGuest = await api.Guest(GameB);
        var second = api.Client(secondGuest.Str("accessToken"));

        var sent = await first.PostAsJsonAsync($"/games/{GameB}/players/me/events", new
        {
            gameVersion = "0.1.0",
            device = "iPhone15,2",
            events = new object[]
            {
                new { name = "session_start", sessionId = "s0", timeMs = weekAgo, fields = new { energy = 100 } },
                new { name = "session_start", sessionId = "s1", timeMs = now, fields = new { energy = 100 } },
                new { name = "tutorial_step", sessionId = "s1", timeMs = now, fields = new { step = 1 } },
                new { name = "tutorial_step", sessionId = "s1", timeMs = now, fields = new { step = 2 } },
                new { name = "memory_played", sessionId = "s1", timeMs = now, fields = new { memory = "m1_pour", stars = 3 } },
                new { name = "session_end", sessionId = "s1", timeMs = now, fields = new { seconds = 300 } },
                new { name = "Bad Name!", sessionId = "s1", timeMs = now, fields = new { } },
            },
        });
        Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        Assert.Equal(6, (await sent.Json()).GetProperty("accepted").GetInt32());

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

        // The second player bought something (Apple, so it has a price).
        await api.WithDb(async db =>
        {
            var gameId = db.Games.Single(g => g.Slug == GameB).Id;
            db.Purchases.Add(new Purchase
            {
                GameId = gameId, PlayerId = Guid.Parse(secondGuest.Str("playerId")), Store = Store.Apple, Sku = "gems.s",
                StoreTransactionId = $"metrics-{Guid.NewGuid():N}", Price = 4.99m, Currency = "USD", PurchasedAt = DateTimeOffset.UtcNow,
            });
            return await db.SaveChangesAsync();
        });

        var (_, viewer) = await api.Staff($"metrics-{Guid.NewGuid():N}@aqlaan.com", (GameB, StaffRole.Viewer));
        var metrics = await (await api.Client(viewer).GetAsync($"/games/{GameB}/admin/metrics?days=8")).Json();
        Assert.Equal(2, metrics.GetProperty("players").GetInt32());
        Assert.Equal(2, metrics.GetProperty("tutorialStepPlayers").GetProperty("1").GetInt32());
        Assert.Equal(1, metrics.GetProperty("tutorialStepPlayers").GetProperty("2").GetInt32());
        Assert.Equal(1, metrics.GetProperty("reachedFirstMemory").GetInt32());
        Assert.Equal(200, metrics.GetProperty("averageSessionSeconds").GetInt32());
        Assert.Equal(2.0 / 3, metrics.GetProperty("errorFreeSessions").GetDouble(), 3);

        // The soft-launch gates (PLAN.md section 2): only the first player started 7 days ago, and came back today.
        Assert.Equal(1.0, metrics.GetProperty("day7Return").GetDouble());
        Assert.Equal(0.0, metrics.GetProperty("day1Return").GetDouble());
        Assert.Equal(JsonValueKind.Null, metrics.GetProperty("day30Return").ValueKind);
        Assert.Equal(0.5, metrics.GetProperty("payerShare").GetDouble());
        Assert.Equal(1, metrics.GetProperty("payersByCountry").GetProperty("?").GetProperty("payers").GetInt32());
        Assert.Equal(4.99m, metrics.GetProperty("storeRevenue").GetProperty("USD").GetDecimal());
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
