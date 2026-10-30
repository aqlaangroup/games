using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Platform.Api;

/// <summary>Per-IP limits on the routes that create accounts or check passwords.</summary>
public static class RateLimits
{
    public const string Auth = "auth";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(Auth, http =>
        {
            int perMinute = http.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimits:AuthPerMinute", 30);
            return RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1) });
        });
    }
}
