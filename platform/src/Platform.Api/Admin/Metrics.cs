using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;

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
        int eligible = firstDay.Count(p => p.Value < clock.GetUtcNow().UtcDateTime.Date);
        int returned = firstDay.Count(p => playedDays[p.Key].Contains(p.Value.AddDays(1)));

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
            day1Return = eligible == 0 ? (double?)null : Math.Round((double)returned / eligible, 3),
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
