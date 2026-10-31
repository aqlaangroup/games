using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Admin;

/// <summary>
/// Live operations for staff (missing/18, 22): publish a new balance.json or go back to an older one, and schedule
/// events. The game picks up both on its next launch, without an app update.
/// </summary>
public static class LiveOpsEndpoints
{
    /// <summary>Templates the game knows (GAME_SYSTEMS.md section 13).</summary>
    public static readonly string[] Templates = ["miniBoard", "frenzy", "pass", "album", "hospitality"];

    /// <summary>Sections a balance.json must have before the game is given it.</summary>
    static readonly string[] RequiredSections = ["energy", "orders", "board", "chains", "formulas", "chapters"];

    const int MaxEventDays = 60;

    public record EventRequest(string? Template, DateTimeOffset StartsAt, DateTimeOffset EndsAt, JsonElement? Settings);

    public static void Map(RouteGroupBuilder game)
    {
        var admin = game.MapGroup("/admin").RequireAuthorization(AuthSchemes.Admin);
        admin.MapGet("/config", Versions).AddEndpointFilter(new StaffFilter(StaffRole.Viewer));
        admin.MapPost("/config", Publish).AddEndpointFilter(new StaffFilter(StaffRole.Producer));
        admin.MapPost("/config/{version:int}/activate", Activate).AddEndpointFilter(new StaffFilter(StaffRole.Producer));
        admin.MapGet("/events", Events).AddEndpointFilter(new StaffFilter(StaffRole.Viewer));
        admin.MapPost("/events", Schedule).AddEndpointFilter(new StaffFilter(StaffRole.Producer));
        admin.MapDelete("/events/{id:guid}", Cancel).AddEndpointFilter(new StaffFilter(StaffRole.Producer));
    }

    static async Task<IResult> Versions(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var gameId = http.Game().Id;
        return Results.Ok(await db.Configs.AsNoTracking().Where(c => c.GameId == gameId).OrderByDescending(c => c.Version)
            .Select(c => new { c.Version, c.IsActive, c.PublishedAt }).ToListAsync(ct));
    }

    // The body is the whole balance.json. A broken one is refused here, before any phone sees it.
    static async Task<IResult> Publish(HttpContext http, PlatformDb db, StaffAccounts staff, CancellationToken ct)
    {
        var game = http.Game();
        string text = await new StreamReader(http.Request.Body).ReadToEndAsync(ct);
        string? problem = Check(text);
        if (problem is not null) return RequestContext.Error(problem);
        int next = await db.Configs.Where(c => c.GameId == game.Id).Select(c => (int?)c.Version).MaxAsync(ct) ?? 0;
        await db.Configs.Where(c => c.GameId == game.Id && c.IsActive).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), ct);
        db.Configs.Add(new ConfigVersion { GameId = game.Id, Version = next + 1, Json = text, IsActive = true });
        staff.Audit(game.Id, http.Admin(), "config.publish", $"config:{next + 1}");
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { version = next + 1 });
    }

    public static string? Check(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return "invalid_config";
            foreach (var section in RequiredSections)
                if (!doc.RootElement.TryGetProperty(section, out _)) return "missing_" + section;
            return null;
        }
        catch (JsonException)
        {
            return "invalid_json";
        }
    }

    static async Task<IResult> Activate(int version, HttpContext http, PlatformDb db, StaffAccounts staff, CancellationToken ct)
    {
        var game = http.Game();
        var target = await db.Configs.FirstOrDefaultAsync(c => c.GameId == game.Id && c.Version == version, ct);
        if (target is null) return RequestContext.Error("unknown_version", StatusCodes.Status404NotFound);
        await db.Configs.Where(c => c.GameId == game.Id && c.IsActive).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), ct);
        target.IsActive = true;
        staff.Audit(game.Id, http.Admin(), "config.activate", $"config:{version}");
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { version });
    }

    static async Task<IResult> Events(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var gameId = http.Game().Id;
        return Results.Ok(await db.Events.AsNoTracking().Where(e => e.GameId == gameId).OrderByDescending(e => e.StartsAt).Take(200).ToListAsync(ct));
    }

    // Settings override the template's numbers in balance.json events.templates (a mini board's chain, milestones
    // and rewards, for example), so a new theme needs no app update.
    static async Task<IResult> Schedule(EventRequest request, HttpContext http, PlatformDb db, StaffAccounts staff, CancellationToken ct)
    {
        var game = http.Game();
        if (request.Template is null || !Templates.Contains(request.Template)) return RequestContext.Error("unknown_template");
        if (request.EndsAt <= request.StartsAt || (request.EndsAt - request.StartsAt).TotalDays > MaxEventDays) return RequestContext.Error("invalid_dates");
        if (request.Settings is { } s && s.ValueKind != JsonValueKind.Object) return RequestContext.Error("invalid_settings");
        var live = new LiveEvent
        {
            GameId = game.Id, Template = request.Template, StartsAt = request.StartsAt, EndsAt = request.EndsAt,
            SettingsJson = request.Settings?.GetRawText() ?? "{}",
        };
        db.Events.Add(live);
        staff.Audit(game.Id, http.Admin(), "event.schedule", $"event:{live.Id}", new { request.Template, request.StartsAt, request.EndsAt });
        await db.SaveChangesAsync(ct);
        return Results.Ok(live);
    }

    static async Task<IResult> Cancel(Guid id, HttpContext http, PlatformDb db, StaffAccounts staff, CancellationToken ct)
    {
        var game = http.Game();
        var live = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.GameId == game.Id, ct);
        if (live is null) return RequestContext.Error("unknown_event", StatusCodes.Status404NotFound);
        db.Events.Remove(live);
        staff.Audit(game.Id, http.Admin(), "event.cancel", $"event:{id}");
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
