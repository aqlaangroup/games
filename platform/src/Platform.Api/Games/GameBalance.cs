using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;

namespace Platform.Api.Games;

/// <summary>The game's active published balance.json (ConfigVersion), which rewards and prices are read from.</summary>
public static class GameBalance
{
    public static async Task<JsonDocument?> ActiveAsync(PlatformDb db, Guid gameId, CancellationToken ct)
    {
        var json = await db.Configs.Where(c => c.GameId == gameId && c.IsActive)
            .OrderByDescending(c => c.Version).Select(c => c.Json).FirstOrDefaultAsync(ct);
        return json is null ? null : JsonDocument.Parse(json);
    }
}
