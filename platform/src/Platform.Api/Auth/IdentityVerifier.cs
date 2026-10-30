using System.Collections.Concurrent;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Platform.Domain;

namespace Platform.Api.Auth;

public static class Providers
{
    public const string Apple = "apple";
    public const string Google = "google";
}

/// <summary>The signing keys Apple or Google publish for their identity tokens. Tests swap in their own keys.</summary>
public interface IJwksProvider
{
    Task<IReadOnlyCollection<SecurityKey>> GetKeysAsync(string provider, bool refresh, CancellationToken ct);
}

public sealed class HttpJwksProvider(IHttpClientFactory http, TimeProvider clock) : IJwksProvider
{
    static readonly Dictionary<string, string> Urls = new()
    {
        [Providers.Apple] = "https://appleid.apple.com/auth/keys",
        [Providers.Google] = "https://www.googleapis.com/oauth2/v3/certs",
    };
    static readonly TimeSpan KeepFor = TimeSpan.FromHours(6);
    static readonly TimeSpan MinRefreshGap = TimeSpan.FromMinutes(1);

    readonly ConcurrentDictionary<string, (IReadOnlyCollection<SecurityKey> Keys, DateTimeOffset FetchedAt)> cache = new();

    public async Task<IReadOnlyCollection<SecurityKey>> GetKeysAsync(string provider, bool refresh, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (cache.TryGetValue(provider, out var cached))
        {
            var age = now - cached.FetchedAt;
            // A key we don't know may be a new one, but don't fetch more than once a minute.
            if (age < KeepFor && (!refresh || age < MinRefreshGap)) return cached.Keys;
        }
        var json = await http.CreateClient("jwks").GetStringAsync(Urls[provider], ct);
        var keys = new JsonWebKeySet(json).GetSigningKeys().ToList();
        cache[provider] = (keys, now);
        return keys;
    }
}

public sealed record IdentityCheck(string? Subject, string? Error)
{
    public static IdentityCheck Ok(string subject) => new(subject, null);
    public static IdentityCheck Fail(string error) => new(null, error);
}

/// <summary>Checks an Apple or Google identity token was issued for this game.</summary>
public sealed class IdentityVerifier(IJwksProvider jwks, ILogger<IdentityVerifier> log)
{
    readonly JsonWebTokenHandler handler = new();

    public async Task<IdentityCheck> VerifyAsync(Game game, string? provider, string? idToken, string? nonce, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(idToken)) return IdentityCheck.Fail("invalid_token");
        TokenValidationParameters parameters;
        switch (provider)
        {
            case Providers.Apple:
                if (game.AppleBundleId is null) return IdentityCheck.Fail("provider_not_configured");
                parameters = new TokenValidationParameters
                {
                    ValidIssuer = "https://appleid.apple.com",
                    ValidAudience = game.AppleBundleId,
                };
                break;
            case Providers.Google:
                if (game.GoogleSignInClientIds.Count == 0) return IdentityCheck.Fail("provider_not_configured");
                parameters = new TokenValidationParameters
                {
                    ValidIssuers = ["accounts.google.com", "https://accounts.google.com"],
                    ValidAudiences = game.GoogleSignInClientIds,
                };
                break;
            default:
                return IdentityCheck.Fail("unknown_provider");
        }
        parameters.ClockSkew = TimeSpan.FromMinutes(2);

        parameters.IssuerSigningKeys = await jwks.GetKeysAsync(provider, refresh: false, ct);
        var result = await handler.ValidateTokenAsync(idToken, parameters);
        if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            parameters.IssuerSigningKeys = await jwks.GetKeysAsync(provider, refresh: true, ct);
            result = await handler.ValidateTokenAsync(idToken, parameters);
        }
        if (!result.IsValid)
        {
            log.LogInformation("Rejected {Provider} identity token: {Reason}", provider, result.Exception?.Message);
            return IdentityCheck.Fail("invalid_token");
        }

        // The game sends the provider SHA-256(nonce) and sends the platform the nonce itself, so a stolen token can't be replayed.
        if (nonce is not null)
        {
            var claim = result.Claims.TryGetValue("nonce", out var n) ? n as string : null;
            if (!string.Equals(claim, TokenService.Hash(nonce), StringComparison.OrdinalIgnoreCase))
                return IdentityCheck.Fail("invalid_token");
        }

        var subject = result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub) ? sub as string : null;
        return string.IsNullOrEmpty(subject) ? IdentityCheck.Fail("invalid_token") : IdentityCheck.Ok(subject);
    }
}

/// <summary>
/// Apple asks apps to revoke Sign in with Apple tokens when an account is deleted. That call needs the app's
/// Sign in with Apple private key (.p8) and the Apple refresh token from the first sign-in, which the platform
/// doesn't collect yet.
/// </summary>
public interface IAppleTokenRevoker
{
    Task RevokeAsync(Game game, string subject, CancellationToken ct);
}

public sealed class NotConfiguredAppleTokenRevoker(ILogger<NotConfiguredAppleTokenRevoker> log) : IAppleTokenRevoker
{
    public Task RevokeAsync(Game game, string subject, CancellationToken ct)
    {
        log.LogWarning("Apple token revoke skipped for game {Game}: the Sign in with Apple key (.p8) is not configured.", game.Slug);
        return Task.CompletedTask;
    }
}
