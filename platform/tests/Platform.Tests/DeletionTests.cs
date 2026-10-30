using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Api.Players;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class DeletionTests(PlatformFactory api)
{
    [Fact]
    public async Task DeletingEndsEverySessionAtOnceAndReleasesTheAppleId()
    {
        var subject = $"del.{Guid.NewGuid():N}";
        var player = await (await api.Client().PostAsJsonAsync($"/games/{GameA}/players/sign-in",
            new { provider = "apple", idToken = api.Jwks.Apple(subject) })).Json();
        var otherPhone = await (await api.Client().PostAsJsonAsync($"/games/{GameA}/players/sign-in",
            new { provider = "apple", idToken = api.Jwks.Apple(subject) })).Json();

        var delete = await api.Client(player.Str("accessToken")).DeleteAsync($"/games/{GameA}/players/me");
        Assert.Equal(HttpStatusCode.Accepted, delete.StatusCode);
        var deleteBy = (await delete.Json()).GetProperty("deleteBy").GetDateTimeOffset();
        Assert.InRange(deleteBy, DateTimeOffset.UtcNow.AddDays(29.9), DateTimeOffset.UtcNow.AddDays(30.1));

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client(player.Str("accessToken")).GetAsync($"/games/{GameA}/players/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client(otherPhone.Str("accessToken")).GetAsync($"/games/{GameA}/players/me/save")).StatusCode);
        foreach (var session in new[] { player, otherPhone })
        {
            var refresh = await api.Client().PostAsJsonAsync($"/games/{GameA}/players/refresh", new { refreshToken = session.Str("refreshToken") });
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        }

        // The same Apple id can start again, as a new player.
        var fresh = await (await api.Client().PostAsJsonAsync($"/games/{GameA}/players/sign-in",
            new { provider = "apple", idToken = api.Jwks.Apple(subject) })).Json();
        Assert.NotEqual(player.Str("playerId"), fresh.Str("playerId"));
    }

    [Fact]
    public async Task ThePurgeRemovesThePlayersDataButKeepsTheirPurchases()
    {
        var guest = await api.Guest();
        var id = Guid.Parse(guest.Str("playerId"));
        var client = api.Client(guest.Str("accessToken"));
        var transaction = $"tx-{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync($"/games/{GameA}/purchases/verify", new { store = "apple", sku = "gems.s", receipt = $"ok:{transaction}" })).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/games/{GameA}/players/me/save", new { baseVersion = 0, save = new { player = new { level = 2 } } })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/games/{GameA}/players/me/support", new { message = "help", email = "a@b.c" })).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/games/{GameA}/players/me")).EnsureSuccessStatusCode();

        // Not yet due: nothing is removed.
        await api.With<AccountPurger, int>(p => p.PurgeDueAsync(DateTimeOffset.UtcNow.AddDays(1)));
        Assert.True(await api.WithDb(db => db.Players.AnyAsync(p => p.Id == id)));

        await api.With<AccountPurger, int>(p => p.PurgeDueAsync(DateTimeOffset.UtcNow.AddDays(31)));
        await api.WithDb(async db =>
        {
            Assert.False(await db.Players.AnyAsync(p => p.Id == id));
            Assert.False(await db.Saves.AnyAsync(s => s.PlayerId == id));
            Assert.False(await db.WalletBalances.AnyAsync(b => b.PlayerId == id));
            Assert.False(await db.WalletEntries.AnyAsync(e => e.PlayerId == id));
            Assert.False(await db.Sessions.AnyAsync(s => s.PlayerId == id));
            Assert.False(await db.SupportTickets.AnyAsync(t => t.PlayerId == id));
            var purchase = await db.Purchases.SingleAsync(p => p.StoreTransactionId == transaction);
            Assert.Null(purchase.PlayerId);
            return 0;
        });
    }

    [Theory]
    [InlineData("45", 30)]
    [InlineData("7", 7)]
    [InlineData(null, 30)]
    public void TheGracePeriodIsNeverMoreThan30Days(string? configured, int days)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Accounts:DeletionGraceDays"] = configured })
            .Build();
        Assert.Equal(TimeSpan.FromDays(days), AccountPurger.GracePeriod(config));
    }
}
