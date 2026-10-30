using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Platform.Api;
using Platform.Api.Admin;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Tests;

/// <summary>
/// The platform on a throwaway PostgreSQL database, dropped when the tests finish. Two test games are registered
/// from a temporary games folder. Set PLATFORM_TEST_DB to point at another server.
/// </summary>
public class PlatformFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string GameA = "test-a", GameB = "test-b";
    public const string AppleAudienceA = "com.aqlaan.testa", GoogleClientA = "test-a.apps.googleusercontent.com";

    readonly string database = $"platform_test_{Guid.NewGuid():N}";
    readonly string gamesDir = Path.Combine(Path.GetTempPath(), $"platform_games_{Guid.NewGuid():N}");

    public TestJwks Jwks { get; } = new();
    protected virtual int AuthPerMinute => 100_000;

    static string DbServer => Environment.GetEnvironmentVariable("PLATFORM_TEST_DB") ?? "Host=localhost;Username=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        WriteGames();
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Platform", $"{DbServer};Database={database}");
        builder.UseSetting("Games:Path", gamesDir);
        builder.UseSetting("Auth:SigningKey", "a-test-signing-key-that-is-long-enough-for-hmac");
        builder.UseSetting("RateLimits:AuthPerMinute", AuthPerMinute.ToString());
        builder.UseSetting("Accounts:PurgeIntervalMinutes", "0");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IJwksProvider>();
            services.AddSingleton<IJwksProvider>(Jwks);
            services.RemoveAll<IStoreVerifier>();
            services.AddSingleton<IStoreVerifier>(new FakeStoreVerifier(Store.Apple));
            services.AddSingleton<IStoreVerifier>(new FakeStoreVerifier(Store.Google));
        });
    }

    void WriteGames()
    {
        Write(GameA, $$"""
            {
              "slug": "{{GameA}}",
              "name": { "ar": "أ", "en": "Test A" },
              "stores": {
                "apple": { "bundleId": "{{AppleAudienceA}}" },
                "google": { "packageName": "com.aqlaan.testa", "signInClientIds": ["{{GoogleClientA}}", "[set later]"] }
              },
              "products": [
                { "sku": "gems.s", "type": "consumable", "grants": { "gems": 100 } },
                { "sku": "decor", "type": "non-consumable", "grants": { "decorSet": "eid", "gems": 60 } }
              ]
            }
            """);
        Write(GameB, $$"""
            {
              "slug": "{{GameB}}",
              "name": { "ar": "ب", "en": "Test B" },
              "stores": { "apple": { "bundleId": "com.aqlaan.testb" }, "google": { "packageName": "com.aqlaan.testb" } }
            }
            """);

        void Write(string slug, string json)
        {
            Directory.CreateDirectory(Path.Combine(gamesDir, slug));
            File.WriteAllText(Path.Combine(gamesDir, slug, "game.json"), json);
        }
    }

    public Task InitializeAsync()
    {
        // Starts the host, which creates and migrates the new database.
        CreateClient().Dispose();
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection($"{DbServer};Database=postgres");
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
        Directory.Delete(gamesDir, recursive: true);
    }

    public async Task<T> WithDb<T>(Func<PlatformDb, Task<T>> work)
    {
        await using var scope = Services.CreateAsyncScope();
        return await work(scope.ServiceProvider.GetRequiredService<PlatformDb>());
    }

    public async Task<T> With<TService, T>(Func<TService, Task<T>> work) where TService : notnull
    {
        await using var scope = Services.CreateAsyncScope();
        return await work(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public HttpClient Client(string? bearer = null)
    {
        var client = CreateClient();
        if (bearer is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return client;
    }

    /// <summary>A new guest on <paramref name="game"/>: its auth response.</summary>
    public async Task<JsonElement> Guest(string game = GameA, string device = "test-phone")
    {
        var response = await Client().PostAsJsonAsync($"/games/{game}/players/guest", new { device, gameVersion = "0.1.0" });
        response.EnsureSuccessStatusCode();
        return await response.Json();
    }

    /// <summary>A staff account with a known password and the given roles, signed in. Returns its access token.</summary>
    public async Task<(Guid Id, string Token)> Staff(string email, params (string Game, StaffRole Role)[] roles)
    {
        const string password = "correct-horse-battery";
        var id = await WithDb(async db =>
        {
            var admin = new AdminUser { Email = StaffAccounts.Normalize(email), PasswordHash = "" };
            admin.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<AdminUser>().HashPassword(admin, password);
            db.AdminUsers.Add(admin);
            foreach (var (game, role) in roles)
            {
                var gameId = db.Games.Single(g => g.Slug == game).Id;
                db.AdminRoles.Add(new AdminRole { AdminUserId = admin.Id, GameId = gameId, Role = role });
            }
            await db.SaveChangesAsync();
            return admin.Id;
        });
        var response = await Client().PostAsJsonAsync("/admin/sign-in", new { email, password });
        response.EnsureSuccessStatusCode();
        return (id, (await response.Json()).GetProperty("accessToken").GetString()!);
    }
}

[CollectionDefinition("platform")]
public class PlatformCollection : ICollectionFixture<PlatformFactory>;

/// <summary>Stands in for Apple's and Google's published keys, and signs identity tokens like theirs.</summary>
public sealed class TestJwks : IJwksProvider
{
    readonly RsaSecurityKey key = new(RSA.Create(2048)) { KeyId = "test-key" };

    public Task<IReadOnlyCollection<SecurityKey>> GetKeysAsync(string provider, bool refresh, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<SecurityKey>>([key]);

    public string Apple(string subject, string audience = PlatformFactory.AppleAudienceA, string? nonce = null,
        DateTime? expires = null, RsaSecurityKey? signWith = null) =>
        Token("https://appleid.apple.com", subject, audience, nonce, expires, signWith);

    public string Google(string subject, string audience = PlatformFactory.GoogleClientA) =>
        Token("https://accounts.google.com", subject, audience, null, null, null);

    string Token(string issuer, string subject, string audience, string? nonce, DateTime? expires, RsaSecurityKey? signWith)
    {
        var claims = new Dictionary<string, object> { ["sub"] = subject };
        // Like the game: it hands the provider SHA-256(nonce), and the provider puts that in the token.
        if (nonce is not null) claims["nonce"] = TokenService.Hash(nonce);
        var exp = expires ?? DateTime.UtcNow.AddMinutes(10);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = exp.AddMinutes(-20),
            NotBefore = exp.AddMinutes(-20),
            Expires = exp,
            SigningCredentials = new SigningCredentials(signWith ?? key, SecurityAlgorithms.RsaSha256),
        });
    }
}

/// <summary>Accepts receipts written "ok:transactionId".</summary>
public sealed class FakeStoreVerifier(Store store) : IStoreVerifier
{
    public Store Store { get; } = store;

    public Task<VerifiedReceipt?> VerifyAsync(Game game, VerifyRequest request, CancellationToken ct) =>
        Task.FromResult(request.Receipt.StartsWith("ok:")
            ? new VerifiedReceipt(request.Receipt[3..], request.Sku, 0.99m, "USD", DateTimeOffset.UtcNow)
            : null);
}

public static class HttpExtensions
{
    public static async Task<JsonElement> Json(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>());

    public static string Str(this JsonElement json, string property) => json.GetProperty(property).GetString()!;
}
