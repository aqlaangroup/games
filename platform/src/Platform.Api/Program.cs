using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Platform.Api;
using Platform.Api.Admin;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Api.Games;
using Platform.Api.Players;
using Platform.Domain;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddDbContext<PlatformDb>((sp, options) => options.UseNpgsql(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Platform")
    ?? throw new InvalidOperationException("ConnectionStrings:Platform is not set.")));
services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

services.AddSingleton(TimeProvider.System);
services.AddSingleton<SigningKeys>();
services.AddSingleton<TokenService>();
services.AddHttpClient("jwks");
services.AddSingleton<IJwksProvider, HttpJwksProvider>();
services.AddSingleton<IdentityVerifier>();
services.AddSingleton<IAppleTokenRevoker, NotConfiguredAppleTokenRevoker>();
services.AddSingleton<SaveSanity>();
services.AddScoped<GameRegistry>();
services.AddScoped<AccountService>();
services.AddScoped<WalletService>();
services.AddScoped<AccountPurger>();
services.AddScoped<StaffAccounts>();
services.AddHostedService<AccountPurgeService>();

// Store verification is a stub that refuses every receipt until the real Apple and Google calls are added.
services.AddSingleton<IStoreVerifier>(new NotConfiguredVerifier(Store.Apple));
services.AddSingleton<IStoreVerifier>(new NotConfiguredVerifier(Store.Google));

// Two kinds of access token: players (one game each) and staff.
services.AddAuthentication().AddJwtBearer(AuthSchemes.Player).AddJwtBearer(AuthSchemes.Admin);
foreach (var scheme in new[] { AuthSchemes.Player, AuthSchemes.Admin })
    services.AddOptions<JwtBearerOptions>(scheme).Configure<SigningKeys>((o, keys) =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = keys.Parameters(scheme);
    });
services.AddAuthorizationBuilder()
    .AddPolicy(AuthSchemes.Player, p => p.AddAuthenticationSchemes(AuthSchemes.Player).RequireAuthenticatedUser())
    .AddPolicy(AuthSchemes.Admin, p => p.AddAuthenticationSchemes(AuthSchemes.Admin).RequireAuthenticatedUser());
services.AddRateLimiter(RateLimits.Configure);

var app = builder.Build();

// Fails at startup, not at the first sign-in, when the signing key is missing outside Development.
app.Services.GetRequiredService<SigningKeys>();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlatformDb>();
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup") || args is ["create-owner", ..])
        await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<GameRegistry>().SyncAsync();

    // dotnet run -- create-owner someone@example.com
    if (args is ["create-owner", var ownerEmail])
    {
        var password = await scope.ServiceProvider.GetRequiredService<StaffAccounts>().CreateOwnerAsync(ownerEmail, default);
        Console.WriteLine($"{StaffAccounts.Normalize(ownerEmail)} is an owner of every registered game.");
        Console.WriteLine($"Temporary password (shown once; change it after signing in): {password}");
        return;
    }
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
AdminEndpoints.MapStaff(app);

var games = app.MapGroup("/games/{slug}").AddEndpointFilter<GameFilter>();

games.MapGet("/time", (TimeProvider clock) =>
{
    var now = clock.GetUtcNow();
    return Results.Ok(new { now, nowMs = now.ToUnixTimeMilliseconds() });
});

// Older game versions show "update required" (COMPLETE_SPEC.md section 10; game.json minGameVersion).
games.MapGet("/version", (HttpContext http) => Results.Ok(new { minVersion = http.Game().MinGameVersion }));

games.MapGet("/config", async (HttpContext http, PlatformDb db, CancellationToken ct) =>
{
    var gameId = http.Game().Id;
    var config = await db.Configs.Where(c => c.GameId == gameId && c.IsActive)
        .OrderByDescending(c => c.Version).FirstOrDefaultAsync(ct);
    return config is null ? Results.NotFound() : Results.Text(config.Json, "application/json");
});

games.MapGet("/events", async (HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct) =>
{
    var gameId = http.Game().Id;
    var now = clock.GetUtcNow();
    return Results.Ok(await db.Events.Where(e => e.GameId == gameId && e.StartsAt <= now && e.EndsAt > now).ToListAsync(ct));
});

var signedIn = games.MapGroup("").RequireAuthorization(AuthSchemes.Player).AddEndpointFilter<PlayerFilter>();
var me = signedIn.MapGroup("/players/me");
PlayerEndpoints.Map(games, me);
PlayerEndpoints.MapSignOut(signedIn);
SaveEndpoints.Map(me);
SupportEndpoints.Map(me);
RewardEndpoints.Map(me);
EventEndpoints.Map(me);
PurchaseEndpoints.Map(signedIn);
AdminEndpoints.MapGame(games);

app.MapPost("/webhooks/apple", () => Results.Ok());   // TODO: App Store Server Notifications v2
app.MapPost("/webhooks/google", () => Results.Ok());  // TODO: Real-time developer notifications

app.Run();

public partial class Program;
