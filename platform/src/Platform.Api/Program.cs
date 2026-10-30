using System.Collections.Concurrent;
using Platform.Api;
using Platform.Domain;

var builder = WebApplication.CreateBuilder(args);

// In-memory stores keep the skeleton runnable. Replace with EF Core + PostgreSQL.
builder.Services.AddSingleton<PlatformStore>();
builder.Services.AddSingleton<IStoreVerifier>(new NotConfiguredVerifier(Store.Apple));
builder.Services.AddSingleton<IStoreVerifier>(new NotConfiguredVerifier(Store.Google));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var games = app.MapGroup("/games/{slug}");

games.MapGet("/time", () => Results.Ok(new { now = DateTimeOffset.UtcNow }));

games.MapGet("/config", (string slug, PlatformStore db) =>
{
    var game = db.FindGame(slug);
    if (game is null) return Results.NotFound();
    var config = db.Configs.Values.Where(c => c.GameId == game.Id && c.IsActive).MaxBy(c => c.Version);
    return config is null ? Results.NotFound() : Results.Text(config.Json, "application/json");
});

games.MapGet("/events", (string slug, PlatformStore db) =>
{
    var game = db.FindGame(slug);
    if (game is null) return Results.NotFound();
    var now = DateTimeOffset.UtcNow;
    return Results.Ok(db.Events.Values.Where(e => e.GameId == game.Id && e.StartsAt <= now && e.EndsAt > now));
});

// TODO: replace the playerId query parameter with the player from a verified sign-in token.
games.MapPost("/purchases/verify", async (string slug, Guid playerId, VerifyRequest request,
    PlatformStore db, IEnumerable<IStoreVerifier> verifiers, CancellationToken ct) =>
{
    var game = db.FindGame(slug);
    if (game is null) return Results.NotFound();

    var product = db.Products.Values.FirstOrDefault(p => p.GameId == game.Id && p.Sku == request.Sku);
    if (product is null) return Results.BadRequest(new { error = "Unknown product for this game." });

    var verifier = verifiers.First(v => v.Store == request.Store);
    var receipt = await verifier.VerifyAsync(game, request, ct);
    if (receipt is null) return Results.BadRequest(new { error = "The store did not confirm this purchase." });

    // The same store transaction can only grant once.
    var key = $"{request.Store}:{receipt.StoreTransactionId}";
    var purchase = new Purchase
    {
        GameId = game.Id, PlayerId = playerId, Store = request.Store,
        StoreTransactionId = receipt.StoreTransactionId, Sku = receipt.Sku,
        Price = receipt.Price, Currency = receipt.Currency, PurchasedAt = receipt.PurchasedAt,
    };
    if (!db.Purchases.TryAdd(key, purchase)) return Results.Ok(new { alreadyGranted = true });

    var wallet = db.Wallets.GetOrAdd((game.Id, playerId), _ => new Wallet { GameId = game.Id, PlayerId = playerId });
    lock (wallet)
    {
        foreach (var (item, amount) in product.Grants)
            if (long.TryParse(amount, out var n))
                wallet.Balances[item] = wallet.Balances.GetValueOrDefault(item) + n;
    }
    return Results.Ok(new { granted = product.Grants, wallet = wallet.Balances });
});

app.MapPost("/webhooks/apple", () => Results.Ok());   // TODO: App Store Server Notifications v2
app.MapPost("/webhooks/google", () => Results.Ok());  // TODO: Real-time developer notifications

app.Run();

/// <summary>Temporary in-memory data. Every collection is keyed or filtered by GameId.</summary>
public class PlatformStore
{
    public ConcurrentDictionary<Guid, Game> Games { get; } = new();
    public ConcurrentDictionary<Guid, Product> Products { get; } = new();
    public ConcurrentDictionary<Guid, ConfigVersion> Configs { get; } = new();
    public ConcurrentDictionary<Guid, LiveEvent> Events { get; } = new();
    public ConcurrentDictionary<string, Purchase> Purchases { get; } = new();
    public ConcurrentDictionary<(Guid GameId, Guid PlayerId), Wallet> Wallets { get; } = new();

    public Game? FindGame(string slug) => Games.Values.FirstOrDefault(g => g.Slug == slug);
}
