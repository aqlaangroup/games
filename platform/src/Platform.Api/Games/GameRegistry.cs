using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Games;

/// <summary>Registers every game from its game.json (README rule: adding a game needs no platform code change).</summary>
public class GameRegistry(PlatformDb db, IConfiguration config, IHostEnvironment env, ILogger<GameRegistry> log)
{
    public async Task SyncAsync(CancellationToken ct = default)
    {
        var root = Path.GetFullPath(Path.Combine(env.ContentRootPath, config["Games:Path"] ?? "../../../games"));
        if (!Directory.Exists(root))
        {
            log.LogWarning("Games folder {Root} not found; no games registered.", root);
            return;
        }

        foreach (var file in Directory.GetDirectories(root).Select(d => Path.Combine(d, "game.json")).Where(File.Exists))
        {
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(file, ct));
            await UpsertAsync(doc.RootElement, ct);
        }
        await db.SaveChangesAsync(ct);
    }

    async Task UpsertAsync(JsonElement json, CancellationToken ct)
    {
        string slug = json.GetProperty("slug").GetString()!;
        var game = await db.Games.FirstOrDefaultAsync(g => g.Slug == slug, ct);
        if (game is null)
        {
            game = new Game { Slug = slug, NameAr = "", NameEn = "" };
            db.Games.Add(game);
        }

        var name = json.GetProperty("name");
        game.NameAr = name.GetProperty("ar").GetString() ?? slug;
        game.NameEn = name.GetProperty("en").GetString() ?? slug;
        var stores = json.TryGetProperty("stores", out var s) ? s : default;
        game.AppleBundleId = Value(stores, "apple", "bundleId");
        game.GooglePackageName = Value(stores, "google", "packageName");
        game.GoogleSignInClientIds = stores.ValueKind == JsonValueKind.Object
            && stores.TryGetProperty("google", out var google)
            && google.TryGetProperty("signInClientIds", out var ids)
                ? ids.EnumerateArray().Select(i => i.GetString()).Where(IsSet).Select(i => i!).ToList()
                : new List<string>();

        if (!json.TryGetProperty("products", out var products)) return;
        var existing = await db.Products.Where(p => p.GameId == game.Id).ToDictionaryAsync(p => p.Sku, ct);
        foreach (var p in products.EnumerateArray())
        {
            string sku = p.GetProperty("sku").GetString()!;
            if (!existing.TryGetValue(sku, out var product))
            {
                product = new Product { GameId = game.Id, Sku = sku };
                db.Products.Add(product);
            }
            product.Type = p.GetProperty("type").GetString() switch
            {
                "non-consumable" => ProductType.NonConsumable,
                "subscription" => ProductType.Subscription,
                _ => ProductType.Consumable,
            };
            product.OncePerPlayer = p.TryGetProperty("oncePerPlayer", out var once) && once.GetBoolean();
            product.Grants = p.TryGetProperty("grants", out var grants)
                ? grants.EnumerateObject().ToDictionary(g => g.Name,
                    g => g.Value.ValueKind == JsonValueKind.String ? g.Value.GetString()! : g.Value.GetRawText())
                : new Dictionary<string, string>();
        }
    }

    static string? Value(JsonElement stores, string store, string key) =>
        stores.ValueKind == JsonValueKind.Object && stores.TryGetProperty(store, out var s)
        && s.TryGetProperty(key, out var v) && IsSet(v.GetString()) ? v.GetString() : null;

    // game.json marks values still to be filled in as "[set after ...]".
    static bool IsSet(string? value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith('[');
}
