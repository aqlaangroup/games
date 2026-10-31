using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

/// <summary>
/// Gems from daily systems, events and the season pass (missing/18). These come from play the platform can't see, so it
/// limits them instead: each id grants once, a day's claims must be near today (a pass tier's, in its season), and the
/// amounts come from the published balance.json or the event's own settings.
/// </summary>
public static class LiveRewards
{
    public static bool Handles(string rewardId) =>
        rewardId.StartsWith("daily:") || rewardId.StartsWith("task:") || rewardId.StartsWith("event:") || rewardId.StartsWith("pass:");

    public static async Task<(long Gems, string? Error)> RewardAsync(string rewardId, JsonElement balance, PlatformDb db,
        Guid gameId, Guid playerId, DateTimeOffset now, CancellationToken ct)
    {
        var parts = rewardId.Split(':');
        switch (parts[0])
        {
            // "daily:2026-10-31": the login gift. The day in the 7-day cycle is how many gifts were claimed before.
            case "daily" when parts.Length == 2:
            {
                if (!NearToday(parts[1], now)) return (0, "wrong_day");
                var days = balance.GetProperty("dailyLogin");
                int before = await db.RewardClaims.CountAsync(c => c.PlayerId == playerId && c.RewardId.StartsWith("daily:"), ct);
                var day = days[before % days.GetArrayLength()];
                return (day.TryGetProperty("gems", out var gems) ? gems.GetInt64() : 0, null);
            }
            // "task:2026-10-31:serve_10": one of the day's tasks, up to dailyTasks.perDay a day.
            case "task" when parts.Length == 3:
            {
                if (!NearToday(parts[1], now)) return (0, "wrong_day");
                var tasks = balance.GetProperty("dailyTasks");
                if (!tasks.GetProperty("list").EnumerateArray().Any(t => t.GetProperty("id").GetString() == parts[2])) return (0, "unknown_reward");
                string prefix = $"task:{parts[1]}:";
                if (await db.RewardClaims.CountAsync(c => c.PlayerId == playerId && c.RewardId.StartsWith(prefix), ct) >= tasks.GetProperty("perDay").GetInt32())
                    return (0, "daily_limit");
                return (tasks.GetProperty("rewardEach").GetProperty("gems").GetInt64(), null);
            }
            // "event:<id>:<n>": the n-th milestone of a running mini board, from its settings or the template's.
            case "event" when parts.Length == 3 && Guid.TryParse(parts[1], out var eventId) && int.TryParse(parts[2], out int n) && n >= 1:
            {
                var live = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId && e.GameId == gameId, ct);
                if (live is null || live.Template != "miniBoard") return (0, "unknown_reward");
                if (now < live.StartsAt || now > live.EndsAt.AddDays(1)) return (0, "event_over");
                using var settings = JsonDocument.Parse(live.SettingsJson);
                var rewards = settings.RootElement.TryGetProperty("milestoneRewards", out var own)
                    ? own : balance.GetProperty("events").GetProperty("templates").GetProperty("miniBoard").GetProperty("milestoneRewards");
                if (n > rewards.GetArrayLength()) return (0, "unknown_reward");
                return (rewards[n - 1].TryGetProperty("gems", out var gems) ? gems.GetInt64() : 0, null);
            }
            // "pass:<season>:<track>:<tier>": a season pass tier, from the track's 5-tier cycle. The season must be the
            // one running (a day's grace either side); the premium track needs a verified purchase of its product made
            // during that season, since the product lasts one season.
            case "pass" when parts.Length == 4 && int.TryParse(parts[1], out int season) && int.TryParse(parts[3], out int tier):
            {
                // A balance published before the pass was built has no seasons: nothing to pay against.
                if (!balance.TryGetProperty("events", out var events) || !events.TryGetProperty("templates", out var templates)
                    || !templates.TryGetProperty("pass", out var pass) || !pass.TryGetProperty("seasonStart", out var seasonStart)
                    || !DateTime.TryParseExact(seasonStart.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
                    return (0, "no_pass_config");
                string track = parts[2];
                if (season < 1 || tier < 1 || tier > pass.GetProperty("tiers").GetInt32() || (track != "free" && track != "premium"))
                    return (0, "unknown_reward");
                int length = pass.GetProperty("durationDays").GetInt32();
                var starts = new DateTimeOffset(first.AddDays((season - 1) * length), TimeSpan.Zero);
                var ends = starts.AddDays(length);
                if (now < starts.AddDays(-1) || now > ends.AddDays(1)) return (0, "wrong_season");
                if (track == "premium")
                {
                    if (!pass.TryGetProperty("premiumSku", out var premiumSku) || premiumSku.GetString() is not { } sku) return (0, "no_pass_config");
                    bool bought = await db.Purchases.AnyAsync(p => p.GameId == gameId && p.PlayerId == playerId && p.Sku == sku
                        && p.Status == PurchaseStatus.Verified && p.PurchasedAt >= starts.AddDays(-1) && p.PurchasedAt < ends, ct);
                    if (!bought) return (0, "no_premium_pass");
                }
                var cycle = pass.GetProperty("rewards").GetProperty(track).GetProperty("cycle");
                var reward = cycle[(tier - 1) % cycle.GetArrayLength()];
                return (reward.TryGetProperty("gems", out var gems) ? gems.GetInt64() : 0, null);
            }
            default:
                return (0, "unknown_reward");
        }
    }

    // The player's local day can be a day either side of the server's (UTC) day.
    static bool NearToday(string date, DateTimeOffset now) =>
        DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
        && Math.Abs((day.Date - now.UtcDateTime.Date).TotalDays) <= 1;
}
