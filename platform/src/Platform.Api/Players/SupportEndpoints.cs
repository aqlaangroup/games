using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Players;

public record SupportRequest(string? Message, string? Email, string? GameVersion, string? Device, string? Language);

/// <summary>The in-game Help form (COMPLETE_SPEC.md section 16). The game adds the version and device itself.</summary>
public static class SupportEndpoints
{
    public const int MaxMessage = 4000;

    public static void Map(RouteGroupBuilder me) => me.MapPost("/support", Create);

    static async Task<IResult> Create(SupportRequest request, HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrEmpty(message) || message.Length > MaxMessage) return RequestContext.Error("invalid_message");
        var email = AccountService.Trim(request.Email, 320);
        if (email is not null && !email.Contains('@')) return RequestContext.Error("invalid_email");

        var game = http.Game();
        var ticket = new SupportTicket
        {
            GameId = game.Id,
            PlayerId = http.Player().Id,
            Message = message,
            Email = email,
            GameVersion = AccountService.Trim(request.GameVersion, 50),
            Device = AccountService.Trim(request.Device, 200),
            Language = AccountService.Trim(request.Language, 10),
            CreatedAt = clock.GetUtcNow(),
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/games/{game.Slug}/players/me/support/{ticket.Id}", new { ticketId = ticket.Id });
    }
}
