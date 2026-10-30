using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Platform.Api.Auth;

public static class AuthSchemes
{
    public const string Player = "player";
    public const string Admin = "admin";
}

/// <summary>
/// The key that signs the platform's own access tokens. Comes from configuration (a secret store in production).
/// Outside Development the app refuses to start without one.
/// </summary>
public sealed class SigningKeys
{
    public SymmetricSecurityKey Key { get; }

    public SigningKeys(IConfiguration config, IHostEnvironment env, ILogger<SigningKeys> log)
    {
        var configured = config["Auth:SigningKey"];
        if (!string.IsNullOrEmpty(configured))
        {
            var bytes = Encoding.UTF8.GetBytes(configured);
            if (bytes.Length < 32) throw new InvalidOperationException("Auth:SigningKey must be at least 32 bytes.");
            Key = new SymmetricSecurityKey(bytes);
        }
        else if (env.IsDevelopment())
        {
            log.LogWarning("Auth:SigningKey is not set. Using a random key for this run; every token dies on restart.");
            Key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
        }
        else
        {
            throw new InvalidOperationException("Auth:SigningKey is required outside Development. Set it from a secret store.");
        }
    }

    public TokenValidationParameters Parameters(string audience) => new()
    {
        ValidIssuer = TokenService.Issuer,
        ValidAudience = audience,
        IssuerSigningKey = Key,
        ClockSkew = TimeSpan.FromSeconds(30),
    };
}

/// <summary>Issues access tokens and refresh tokens.</summary>
public sealed class TokenService(SigningKeys keys, TimeProvider clock)
{
    public const string Issuer = "aqlaan-platform";
    public const string GameClaim = "game";
    public static readonly TimeSpan PlayerAccessLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan AdminAccessLifetime = TimeSpan.FromHours(8);
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(180);

    readonly JsonWebTokenHandler handler = new();

    /// <summary>A player's access token works only on its own game's routes.</summary>
    public (string Token, DateTimeOffset ExpiresAt) PlayerAccess(Guid playerId, string gameSlug) =>
        Create(AuthSchemes.Player, PlayerAccessLifetime, new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = playerId.ToString(),
            [GameClaim] = gameSlug,
        });

    public (string Token, DateTimeOffset ExpiresAt) AdminAccess(Guid adminId) =>
        Create(AuthSchemes.Admin, AdminAccessLifetime, new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = adminId.ToString(),
        });

    (string, DateTimeOffset) Create(string audience, TimeSpan lifetime, Dictionary<string, object> claims)
    {
        var now = clock.GetUtcNow();
        var expires = now + lifetime;
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(keys.Key, SecurityAlgorithms.HmacSha256),
        });
        return (token, expires);
    }

    /// <summary>32 random bytes. The player keeps the token; the platform keeps only its hash.</summary>
    public static string NewRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
