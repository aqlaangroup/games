using System.Text.Json;
using System.Text.RegularExpressions;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

public record GameEvent(string? Name, JsonElement? Fields, long TimeMs, string? SessionId);
public record EventBatch(List<GameEvent>? Events, string? GameVersion, string? Device);

/// <summary>
/// Game events (GAME_SYSTEMS.md section 15), sent by the game in batches. They feed the ring 1 tester gate and the
/// soft-launch numbers (admin metrics). Deleting an account deletes its events.
/// </summary>
public static partial class EventEndpoints
{
    public const int MaxBatch = 200, MaxFieldsChars = 4000;

    [GeneratedRegex("^[a-z][a-z0-9_]{0,63}$")]
    private static partial Regex NamePattern();

    public static void Map(RouteGroupBuilder me) => me.MapPost("/events", Add);

    static async Task<IResult> Add(EventBatch batch, HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var events = batch.Events ?? [];
        if (events.Count == 0 || events.Count > MaxBatch) return RequestContext.Error("invalid_batch");
        var game = http.Game();
        var player = http.Player();
        var now = clock.GetUtcNow();
        int accepted = 0;
        foreach (var e in events)
        {
            // A bad event is dropped, not the batch: the game would only send it again.
            string fields = e.Fields is { ValueKind: JsonValueKind.Object } f ? f.GetRawText() : "{}";
            if (e.Name is null || !NamePattern().IsMatch(e.Name) || fields.Length > MaxFieldsChars) continue;
            db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                GameId = game.Id,
                PlayerId = player.Id,
                SessionId = AccountService.Trim(e.SessionId, 64) ?? "",
                Name = e.Name,
                FieldsJson = fields,
                ClientTime = e.TimeMs > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(e.TimeMs) : now,
                ReceivedAt = now,
                GameVersion = AccountService.Trim(batch.GameVersion, 50),
                Device = AccountService.Trim(batch.Device, 200),
            });
            accepted++;
        }
        await db.SaveChangesAsync(ct);
        return Results.Accepted(value: new { accepted });
    }
}
