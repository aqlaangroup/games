using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

public record PutSaveRequest(long BaseVersion, JsonElement Save, string? Device, bool? Force);

/// <summary>
/// Cloud save (COMPLETE_SPEC.md section 9). Each upload names the version it was based on. If another phone
/// uploaded since, the platform answers 409 with that save's level and chapter so the game can ask which to keep.
/// </summary>
public static class SaveEndpoints
{
    public static void Map(RouteGroupBuilder me)
    {
        me.MapGet("/save", Get);
        me.MapPut("/save", Put);
    }

    static async Task<IResult> Get(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var save = await db.Saves.AsNoTracking().FirstOrDefaultAsync(s => s.PlayerId == http.Player().Id, ct);
        if (save is null) return Results.NoContent();
        using var json = JsonDocument.Parse(save.Json);
        return Results.Ok(new
        {
            version = save.Version,
            save = json.RootElement.Clone(),
            summary = new SaveSummaryDto(save.Level, save.Chapter),
            updatedAt = save.UpdatedAt,
            device = save.Device,
        });
    }

    static async Task<IResult> Put(PutSaveRequest request, HttpContext http, PlatformDb db,
        SaveSanity sanity, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var player = http.Player();
        if (request.Save.ValueKind != JsonValueKind.Object) return RequestContext.Error("invalid_save");
        string raw = request.Save.GetRawText();
        if (raw.Length > sanity.MaxBytes) return RequestContext.Error("save_too_large", StatusCodes.Status413PayloadTooLarge);

        var now = clock.GetUtcNow();
        var current = await db.Saves.FirstOrDefaultAsync(s => s.PlayerId == player.Id, ct);
        if (current is not null && current.Version != request.BaseVersion && request.Force != true)
            return Conflict(current);

        var next = SaveSanity.Read(request.Save);
        var flags = sanity.Check(
            current is null ? SaveNumbers.Start : new SaveNumbers(current.Level, current.Chapter, current.Coins),
            next, now - (current?.UpdatedAt ?? player.CreatedAt));

        if (current is null)
        {
            current = new PlayerSave { PlayerId = player.Id, GameId = game.Id, Json = raw, Version = 1 };
            db.Saves.Add(current);
        }
        else
        {
            current.Version++;
            current.Json = raw;
        }
        current.Level = next.Level;
        current.Chapter = next.Chapter;
        current.Coins = next.Coins;
        current.Device = AccountService.Trim(request.Device, 200);
        current.UpdatedAt = now;
        foreach (var (reason, details) in flags)
            db.SaveFlags.Add(new SaveFlag
            {
                GameId = game.Id, PlayerId = player.Id, Reason = reason, Details = details,
                FromVersion = current.Version - 1, ToVersion = current.Version, CreatedAt = now,
            });
        player.LastSeenAt = now;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e is DbUpdateConcurrencyException || PlayerEndpoints.IsUniqueViolation(e))
        {
            // Another upload for this player landed first.
            db.ChangeTracker.Clear();
            var winner = await db.Saves.AsNoTracking().FirstAsync(s => s.PlayerId == player.Id, ct);
            return Conflict(winner);
        }
        return Results.Ok(new { version = current.Version });
    }

    static IResult Conflict(PlayerSave server) =>
        Results.Json(new { error = "conflict", server = SaveInfo.From(server) }, statusCode: StatusCodes.Status409Conflict);
}

public record SaveNumbers(int? Level, int? Chapter, long? Coins)
{
    public static readonly SaveNumbers Start = new(1, 1, 0);
}

/// <summary>
/// Flags uploads that move faster than a person can play (COMPLETE_SPEC.md section 11). Flags are for staff to
/// look at; the upload is always kept. The limits are platform settings (<c>Saves:Sanity</c>) to tune from real data.
/// </summary>
public sealed class SaveSanity(IConfiguration config)
{
    public int MaxBytes => config.GetValue("Saves:MaxBytes", 1_000_000);

    public static SaveNumbers Read(JsonElement save) => new(
        Int(save, "player", "level"), Int(save, "story", "chapter"), (long?)Number(save, "player", "coins"));

    public List<(string Reason, string Details)> Check(SaveNumbers before, SaveNumbers after, TimeSpan elapsed)
    {
        // Short gaps get a full hour's allowance, so frequent uploads don't trip the check.
        double hours = Math.Max(1, elapsed.TotalHours);
        var flags = new List<(string, string)>();
        Rule("level_jump", before.Level, after.Level, config.GetValue("Saves:Sanity:MaxLevelsPerHour", 5.0));
        Rule("chapter_jump", before.Chapter, after.Chapter, config.GetValue("Saves:Sanity:MaxChaptersPerHour", 2.0));
        Rule("coins_jump", before.Coins, after.Coins, config.GetValue("Saves:Sanity:MaxCoinsPerHour", 20000.0));
        return flags;

        void Rule(string reason, double? from, double? to, double perHour)
        {
            if (from is null || to is null) return;
            double gain = to.Value - from.Value, allowed = perHour * hours;
            if (gain > allowed)
                flags.Add((reason, $"{from} -> {to} in {elapsed.TotalHours:0.##} h (allowed {allowed:0.##})"));
        }
    }

    static int? Int(JsonElement save, string section, string field) =>
        Number(save, section, field) is { } n && n >= int.MinValue && n <= int.MaxValue ? (int)n : null;

    static double? Number(JsonElement save, string section, string field) =>
        save.TryGetProperty(section, out var s) && s.ValueKind == JsonValueKind.Object
        && s.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
}
