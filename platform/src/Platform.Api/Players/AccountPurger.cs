using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;

namespace Platform.Api.Players;

/// <summary>
/// Removes deleted accounts once their grace period ends (COMPLETE_SPEC.md section 4: within 30 days).
/// Purchases stay for accounting and tax records, but their link to the player is cut.
/// </summary>
public sealed class AccountPurger(PlatformDb db, ILogger<AccountPurger> log)
{
    public const int MaxGraceDays = 30;

    public static TimeSpan GracePeriod(IConfiguration config) =>
        TimeSpan.FromDays(Math.Clamp(config.GetValue("Accounts:DeletionGraceDays", MaxGraceDays), 0, MaxGraceDays));

    public async Task<int> PurgeDueAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var due = await db.Players.Where(p => p.DeleteBy != null && p.DeleteBy <= now)
            .OrderBy(p => p.DeleteBy).Select(p => p.Id).Take(500).ToListAsync(ct);
        foreach (var id in due)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Saves.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.SaveFlags.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.WalletBalances.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.WalletEntries.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.RewardClaims.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.GemSpends.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.AnalyticsEvents.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.Sessions.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.Identities.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.SupportTickets.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.Friendships.Where(x => x.PlayerId == id || x.FriendId == id).ExecuteDeleteAsync(ct);
            await db.Gifts.Where(x => x.FromPlayerId == id || x.ToPlayerId == id).ExecuteDeleteAsync(ct);
            await db.HostingHelpers.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            var hosted = db.HostingOrders.Where(x => x.HostId == id).Select(x => x.Id);
            await db.HostingHelpers.Where(x => hosted.Contains(x.OrderId)).ExecuteDeleteAsync(ct);
            await db.HostingOrders.Where(x => x.HostId == id).ExecuteDeleteAsync(ct);
            await db.WallReactions.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            var posts = db.WallPosts.Where(x => x.PlayerId == id).Select(x => x.Id);
            await db.WallReactions.Where(x => posts.Contains(x.PostId)).ExecuteDeleteAsync(ct);
            await db.WallPosts.Where(x => x.PlayerId == id).ExecuteDeleteAsync(ct);
            await db.Blocks.Where(x => x.PlayerId == id || x.BlockedId == id).ExecuteDeleteAsync(ct);
            await db.Purchases.Where(x => x.PlayerId == id)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.PlayerId, (Guid?)null), ct);
            await db.Players.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            await tx.CommitAsync(ct);
        }
        if (due.Count > 0) log.LogInformation("Purged {Count} deleted accounts.", due.Count);
        return due.Count;
    }
}

/// <summary>Runs the purge every <c>Accounts:PurgeIntervalMinutes</c> (default 60; 0 turns it off).</summary>
public sealed class AccountPurgeService(IServiceScopeFactory scopes, IConfiguration config, TimeProvider clock,
    ILogger<AccountPurgeService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int minutes = config.GetValue("Accounts:PurgeIntervalMinutes", 60);
        if (minutes <= 0) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes), clock);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<AccountPurger>().PurgeDueAsync(clock.GetUtcNow(), stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "Account purge failed; trying again next round.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
