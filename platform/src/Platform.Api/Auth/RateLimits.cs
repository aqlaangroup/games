using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Platform.Api.Auth;

namespace Platform.Api;

/// <summary>Per-IP limits on the routes that create accounts or check passwords. The IP comes from <see cref="ClientIp"/>.</summary>
public static class RateLimits
{
    public const string Auth = "auth";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(Auth, http =>
        {
            int perMinute = http.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimits:AuthPerMinute", 30);
            return RateLimitPartition.GetFixedWindowLimiter(ClientIp.Get(http) ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1) });
        });
    }
}
