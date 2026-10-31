using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Platform.Api.Auth;
using Platform.Domain;

namespace Platform.Api.Stores;

/// <summary>
/// Refunds and revocations from the stores: App Store Server Notifications v2 and Google Play's real-time developer
/// notifications (a Pub/Sub push). Both always answer 200 once read, or the store keeps retrying.
/// </summary>
public static class StoreNotificationEndpoints
{
    public static void Map(RouteGroupBuilder games)
    {
        games.MapPost("/stores/apple/notifications", Apple);
        games.MapPost("/stores/google/notifications", Google);
    }

    // Apple signs the notification, but rather than checking Apple's certificate chain here, the platform asks Apple
    // for the transaction it names (over TLS, with its own key) and acts only on what that says.
    static async Task<IResult> Apple(JsonElement body, HttpContext http, IEnumerable<IStoreVerifier> verifiers, Refunds refunds, CancellationToken ct)
    {
        var game = http.Game();
        var notification = Jws.Payload(body.TryGetProperty("signedPayload", out var signed) ? signed.GetString() : null);
        if (notification is not { } n || !n.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("signedTransactionInfo", out var info)) return Results.Ok();
        var transaction = Jws.Payload(info.GetString());
        if (transaction is not { } t || !t.TryGetProperty("transactionId", out var id)) return Results.Ok();
        var now = await verifiers.First(v => v.Store == Store.Apple).LookUpAsync(game, id.GetString()!, ct);
        if (now is { Revoked: true }) await refunds.RefundAsync(game, Store.Apple, now.TransactionId, ct);
        return Results.Ok();
    }

    // Google's push carries no signature we check here; the subscription's secret token in the URL proves it's ours.
    static async Task<IResult> Google(JsonElement body, string? token, HttpContext http, GooglePlayOptions options, Refunds refunds, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(options.PushToken) || token is null ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(options.PushToken)))
            return Results.Unauthorized();
        if (!body.TryGetProperty("message", out var message) || !message.TryGetProperty("data", out var data)) return Results.Ok();
        JsonElement notification;
        try
        {
            notification = JsonDocument.Parse(Convert.FromBase64String(data.GetString()!)).RootElement;
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return Results.Ok();
        }
        if (notification.TryGetProperty("voidedPurchaseNotification", out var voided) &&
            voided.TryGetProperty("orderId", out var order) && order.GetString() is { Length: > 0 } orderId)
            await refunds.RefundAsync(http.Game(), Store.Google, orderId, ct);
        return Results.Ok();
    }
}
