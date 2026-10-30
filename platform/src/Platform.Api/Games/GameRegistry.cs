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
            var game = await UpsertAsync(doc.RootElement, ct);
            await db.SaveChangesAsync(ct);
            await PublishBalanceAsync(game, doc.RootElement, Path.GetDirectoryName(file)!, ct);
        }
        await db.SaveChangesAsync(ct);
    }

    // The game's balance.json becomes config version 1 when nothing is published yet. In Development a changed
    // file is published again, so local numbers stay in step; elsewhere staff publish new versions (missing/22).
    async Task PublishBalanceAsync(Game game, JsonElement json, string gameFolder, CancellationToken ct)
    {
        if (!json.TryGetProperty("balanceConfig", out var path) || path.GetString() is not { } relative) return;
        var file = Path.Combine(gameFolder, relative);
        if (!File.Exists(file)) return;
        var text = await File.ReadAllTextAsync(file, ct);
        var active = await db.Configs.Where(c => c.GameId == game.Id && c.IsActive).OrderByDescending(c => c.Version).FirstOrDefaultAsync(ct);
        if (active is not null && (!env.IsDevelopment() || active.Json == text)) return;
        int next = await db.Configs.Where(c => c.GameId == game.Id).Select(c => (int?)c.Version).MaxAsync(ct) ?? 0;
        if (active is not null) active.IsActive = false;
        db.Configs.Add(new ConfigVersion { GameId = game.Id, Version = next + 1, Json = text, IsActive = true });
        log.LogInformation("Published {Slug} balance config version {Version}.", game.Slug, next + 1);
    }

    async Task<Game> UpsertAsync(JsonElement json, CancellationToken ct)
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
        game.MinGameVersion = json.TryGetProperty("minGameVersion", out var min) && IsSet(min.GetString()) ? min.GetString() : null;
        game.GooglePackageName = Value(stores, "google", "packageName");
        game.GoogleSignInClientIds = stores.ValueKind == JsonValueKind.Object
            && stores.TryGetProperty("google", out var google)
            && google.TryGetProperty("signInClientIds", out var ids)
                ? ids.EnumerateArray().Select(i => i.GetString()).Where(IsSet).Select(i => i!).ToList()
                : new List<string>();

        if (!json.TryGetProperty("products", out var products)) return game;
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
                "non-renewing" => ProductType.NonRenewing, // a season pass: Apple has no 28-day auto-renewing period
                _ => ProductType.Consumable,
            };
            product.OncePerPlayer = p.TryGetProperty("oncePerPlayer", out var once) && once.GetBoolean();
            product.Grants = p.TryGetProperty("grants", out var grants)
                ? grants.EnumerateObject().ToDictionary(g => g.Name,
                    g => g.Value.ValueKind == JsonValueKind.String ? g.Value.GetString()! : g.Value.GetRawText())
                : new Dictionary<string, string>();
        }
        return game;
    }

    static string? Value(JsonElement stores, string store, string key) =>
        stores.ValueKind == JsonValueKind.Object && stores.TryGetProperty(store, out var s)
        && s.TryGetProperty(key, out var v) && IsSet(v.GetString()) ? v.GetString() : null;

    // game.json marks values still to be filled in as "[set after ...]".
    static bool IsSet(string? value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith('[');
}
