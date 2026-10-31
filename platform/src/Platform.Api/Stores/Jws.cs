using System.Text;
using System.Text.Json;

namespace Platform.Api.Stores;

/// <summary>Reads the payload of a JWS (header.payload.signature) without checking its signature.</summary>
public static class Jws
{
    /// <summary>
    /// Only for JWS the platform fetched itself from the store over TLS with its own credentials, or as a pointer to
    /// fetch the real record that way (refund notifications). Never trust a JWS a phone sent this way.
    /// </summary>
    public static JsonElement? Payload(string? jws)
    {
        var parts = jws?.Split('.');
        if (parts is not { Length: 3 }) return null;
        try
        {
            string body = parts[1].Replace('-', '+').Replace('_', '/');
            body = body.PadRight(body.Length + (4 - body.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(body)));
            return doc.RootElement.Clone();
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return null;
        }
    }

    public static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
