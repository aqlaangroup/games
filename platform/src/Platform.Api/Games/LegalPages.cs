namespace Platform.Api.Games;

/// <summary>
/// Each game's privacy policy and terms, from its folder: games/{slug}/legal/{page}.{lang}.html. Public pages, so
/// the stores and the game's Settings can link to them (COMPLETE_SPEC.md section 14).
/// </summary>
public static class LegalPages
{
    static readonly string[] Pages = ["privacy", "terms"];
    static readonly string[] Languages = ["ar", "en"];

    public static void Map(RouteGroupBuilder games) => games.MapGet("/legal/{page}", Get);

    static IResult Get(string slug, string page, string? lang, IConfiguration config, IHostEnvironment env)
    {
        if (!Pages.Contains(page)) return Results.NotFound();
        string language = Languages.Contains(lang) ? lang! : "ar";
        var root = Path.GetFullPath(Path.Combine(env.ContentRootPath, config["Games:Path"] ?? "../../../games"));
        var file = Path.Combine(root, slug, "legal", $"{page}.{language}.html");
        // slug comes from the route; GameFilter has already checked it's a registered game, so it names a folder.
        return File.Exists(file) ? Results.File(file, "text/html; charset=utf-8") : Results.NotFound();
    }
}
