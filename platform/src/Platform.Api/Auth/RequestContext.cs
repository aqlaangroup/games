using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Platform.Api.Data;
using Platform.Domain;

namespace Platform.Api.Auth;

/// <summary>The game, player or staff member a request is for, loaded once by the filters below.</summary>
public static class RequestContext
{
    public static Game Game(this HttpContext http) => (Game)http.Items[typeof(Game)]!;
    public static Player Player(this HttpContext http) => (Player)http.Items[typeof(Player)]!;
    public static AdminUser Admin(this HttpContext http) => (AdminUser)http.Items[typeof(AdminUser)]!;

    public static IResult Error(string error, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { error }, statusCode: status);

    internal static Guid? Subject(this HttpContext http) =>
        Guid.TryParse(http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
}

/// <summary>Every /games/{slug} route: 404 for a game the platform doesn't know.</summary>
public sealed class GameFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var slug = http.GetRouteValue("slug") as string;
        var db = http.RequestServices.GetRequiredService<PlatformDb>();
        var game = await db.Games.FirstOrDefaultAsync(g => g.Slug == slug, http.RequestAborted);
        if (game is null) return RequestContext.Error("unknown_game", StatusCodes.Status404NotFound);
        http.Items[typeof(Game)] = game;
        return await next(context);
    }
}

/// <summary>A signed-in player of this game whose account is not being deleted.</summary>
public sealed class PlayerFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var game = http.Game();
        // A token from one game never works on another game's routes.
        if (http.User.FindFirst(TokenService.GameClaim)?.Value != game.Slug || http.Subject() is not { } id)
            return Results.Unauthorized();
        var db = http.RequestServices.GetRequiredService<PlatformDb>();
        var player = await db.Players.FirstOrDefaultAsync(
            p => p.Id == id && p.GameId == game.Id && p.DeletionRequestedAt == null, http.RequestAborted);
        if (player is null) return Results.Unauthorized();
        http.Items[typeof(Player)] = player;
        return await next(context);
    }
}

/// <summary>A staff member who is not disabled. With a game, they also need at least <paramref name="min"/> on it.</summary>
public sealed class StaffFilter(StaffRole? min, bool allowPasswordChangeDue = false) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (http.Subject() is not { } id) return Results.Unauthorized();
        var db = http.RequestServices.GetRequiredService<PlatformDb>();
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Id == id && !a.Disabled, http.RequestAborted);
        if (admin is null) return Results.Unauthorized();
        if (admin.MustChangePassword && !allowPasswordChangeDue)
            return RequestContext.Error("password_change_required", StatusCodes.Status403Forbidden);

        if (min is { } needed)
        {
            var gameId = http.Game().Id;
            var role = await db.AdminRoles.Where(r => r.AdminUserId == id && r.GameId == gameId)
                .Select(r => (StaffRole?)r.Role).FirstOrDefaultAsync(http.RequestAborted);
            if (role is null || role < needed) return RequestContext.Error("forbidden", StatusCodes.Status403Forbidden);
        }
        http.Items[typeof(AdminUser)] = admin;
        return await next(context);
    }
}
