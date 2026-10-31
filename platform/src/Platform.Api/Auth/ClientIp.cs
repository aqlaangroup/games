using System.Net;

namespace Platform.Api.Auth;

/// <summary>
/// The player's or staff member's IP address. In production every request comes through Cloudflare and then
/// Traefik, so the connection's address is Traefik's; Cloudflare puts the real one in CF-Connecting-IP. The container
/// has no open port, so only that path can reach it. Use this for anything that logs or limits by IP.
/// </summary>
public static class ClientIp
{
    public const string Header = "CF-Connecting-IP";

    /// <summary>CF-Connecting-IP when it holds an IP address, otherwise the connection's address, or null.</summary>
    public static string? Get(HttpContext http)
    {
        var header = http.Request.Headers[Header].ToString().Trim();
        if (IPAddress.TryParse(header, out var fromCloudflare)) return Normalize(fromCloudflare);
        return http.Connection.RemoteIpAddress is { } remote ? Normalize(remote) : null;
    }

    // "::ffff:1.2.3.4" and "1.2.3.4" are the same client.
    static string Normalize(IPAddress ip) => (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString();
}
