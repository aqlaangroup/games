using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Admin;

/// <summary>
/// The numbers the ring 1 tester gate and soft launch watch (COMPLETE_SPEC.md section 17, PLAN.md section 2), from
/// game events. Worked out in memory over the chosen days, which is fine for playtests; move it to SQL or a
/// warehouse before soft launch.
/// </summary>
public static class Metrics
{
    static readonly string[] Names = ["session_start", "session_end", "tutorial_step", "memory_played", "letter_found", "client_error"];

    public static async Task<IResult> Get(HttpContext http, PlatformDb db, TimeProvider clock, int? days, CancellationToken ct)
    {
        var game = http.Game();
        var since = clock.GetUtcNow().AddDays(-Math.Clamp(days ?? 30, 1, 365));
        var rows = await db.AnalyticsEvents.AsNoTracking()
            .Where(e => e.GameId == game.Id && e.ClientTime >= since && Names.Contains(e.Name))
            .Select(e => new { e.PlayerId, e.SessionId, e.Name, e.FieldsJson, e.ClientTime })
            .ToListAsync(ct);

        var starts = rows.Where(r => r.Name == "session_start").ToList();
        var players = starts.Select(r => r.PlayerId).Distinct().ToList();
        var firstDay = starts.GroupBy(r => r.PlayerId).ToDictionary(g => g.Key, g => g.Min(r => r.ClientTime.UtcDateTime.Date));
        var playedDays = starts.GroupBy(r => r.PlayerId).ToDictionary(g => g.Key, g => g.Select(r => r.ClientTime.UtcDateTime.Date).ToHashSet());
        var today = clock.GetUtcNow().UtcDateTime.Date;

        // Day N return (PLAN.md soft-launch gate: day 1 40%+, day 7 15%+, day 30 7%+): of the players whose first day
        // in these days is at least N days ago, the share who played on day N. Day 30 needs 31 days or more.
        double? ReturnOn(int day)
        {
            var cohort = firstDay.Where(p => p.Value.AddDays(day) <= today).Select(p => p.Key).ToList();
            return cohort.Count == 0 ? null : Math.Round((double)cohort.Count(id => playedDays[id].Contains(firstDay[id].AddDays(day))) / cohort.Count, 3);
        }

        // Paying players (PLAN.md: 3%+ pay in Saudi Arabia), from verified purchases, overall and by the country each
        // player gave. Revenue per day of play uses Apple's prices; Google's records carry none (the Play Console has them).
        var paid = await db.Purchases.AsNoTracking()
            .Where(p => p.GameId == game.Id && p.PurchasedAt >= since && p.Status == PurchaseStatus.Verified && p.PlayerId != null)
            .Select(p => new { PlayerId = p.PlayerId!.Value, p.Price, p.Currency }).ToListAsync(ct);
        var payers = paid.Select(p => p.PlayerId).Where(players.Contains).ToHashSet();
        var countries = await db.Players.AsNoTracking().Where(p => players.Contains(p.Id))
            .Select(p => new { p.Id, p.Country }).ToDictionaryAsync(p => p.Id, p => p.Country ?? "?", ct);
        int playerDays = playedDays.Values.Sum(d => d.Count);

        var tutorial = rows.Where(r => r.Name == "tutorial_step")
            .Select(r => (r.PlayerId, Step: Int(r.FieldsJson, "step")))
            .Where(x => x.Step > 0)
            .GroupBy(x => x.Step).OrderBy(g => g.Key)
            .ToDictionary(g => g.Key.ToString(), g => g.Select(x => x.PlayerId).Distinct().Count());

        var lengths = rows.Where(r => r.Name == "session_end").Select(r => Int(r.FieldsJson, "seconds")).Where(s => s > 0).ToList();
        var sessions = starts.Select(r => r.SessionId).Distinct().Count();
        var crashed = rows.Where(r => r.Name == "client_error").Select(r => r.SessionId).Distinct().Count();

        return Results.Ok(new
        {
            days = days ?? 30,
            players = players.Count,
            sessions,
            tutorialStepPlayers = tutorial,
            reachedFirstMemory = rows.Where(r => r.Name == "memory_played").Select(r => r.PlayerId).Distinct().Count(),
            foundTheLetter = rows.Where(r => r.Name == "letter_found").Select(r => r.PlayerId).Distinct().Count(),
            day1Return = ReturnOn(1),
            day7Return = ReturnOn(7),
            day30Return = ReturnOn(30),
            payerShare = players.Count == 0 ? (double?)null : Math.Round((double)payers.Count / players.Count, 4),
            payersByCountry = players.GroupBy(id => countries.GetValueOrDefault(id, "?")).OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => new { players = g.Count(), payers = g.Count(payers.Contains), share = Math.Round((double)g.Count(payers.Contains) / g.Count(), 4) }),
            averageDailyPlayers = Math.Round((double)playerDays / Math.Clamp(days ?? 30, 1, 365), 1),
            storeRevenue = paid.Where(p => p.Price > 0).GroupBy(p => p.Currency ?? "?").ToDictionary(g => g.Key, g => g.Sum(p => p.Price)),
            storeRevenuePerPlayerDay = playerDays == 0 ? null : paid.Where(p => p.Price > 0).GroupBy(p => p.Currency ?? "?")
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(p => p.Price) / playerDays, 4)),
            averageSessionSeconds = lengths.Count == 0 ? 0 : (int)lengths.Average(),
            errorFreeSessions = sessions == 0 ? (double?)null : Math.Round(1 - (double)crashed / sessions, 4),
        });
    }

    static int Int(string json, string field)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(field, out var v) && v.TryGetInt32(out int n) ? n : 0;
    }
}
