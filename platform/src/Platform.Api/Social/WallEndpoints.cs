using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Admin;
using Platform.Api.Auth;
using Platform.Api.Data;
using Platform.Api.Games;
using Platform.Api.Players;
using Platform.Domain;

namespace Platform.Api.Social;

public record WallPostRequest(string? Text);
public record ReviewRequest(string? Note);

/// <summary>
/// The automatic first check on Sitti wall posts. It refuses contact details and links (a child's safety, and the
/// wall's rule against them) and listed words, after folding Arabic spelling variants, diacritics and tatweel. It
/// never approves: every post that passes still waits for a person.
/// </summary>
public static partial class WallFilter
{
    // A short starting list; staff extend it in configuration (Wall:BlockedWords) without a release.
    static readonly string[] Words = ["fuck", "shit", "bitch", "sex", "porn", "كس", "زب", "شرموط", "منيوك", "قحب"];

    public static string Fold(string text)
    {
        var sb = new StringBuilder();
        foreach (char c in text.Normalize(NormalizationForm.FormC))
        {
            if (c is >= 'ً' and <= 'ٟ' || c == 'ٰ' || c == 'ـ') continue; // harakat, dagger alef, tatweel
            sb.Append(c switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
                >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
                _ => char.ToLowerInvariant(c),
            });
        }
        return sb.ToString();
    }

    /// <summary>Why the post can't go up, or null to send it to a person.</summary>
    public static string? Refuse(string text, IEnumerable<string>? extraWords = null)
    {
        string folded = Fold(text);
        if (Link().IsMatch(folded)) return "link";
        if (folded.Contains('@')) return "email";
        if (Regex.Replace(folded, @"[\s\-\.\(\)\+]", "").Any(char.IsAsciiDigit) && Phone().IsMatch(Regex.Replace(folded, @"[\s\-\.\(\)\+]", ""))) return "phone";
        var words = Words.Concat(extraWords ?? []).Select(Fold);
        var tokens = Tokens().Split(folded);
        foreach (var word in words)
            if (word.Contains(' ') ? folded.Contains(word) : tokens.Contains(word)) return "word";
        return null;
    }

    [GeneratedRegex(@"(https?:|www\.|\.(com|net|org|me|io|sa|ae|eg|jo)\b|t\.me|wa\.me)")]
    private static partial Regex Link();

    [GeneratedRegex(@"\d{7,}")]
    private static partial Regex Phone();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex Tokens();
}

/// <summary>
/// The Sitti wall (SYSTEMS_B.md section 8; games/gahwit-sitti/missing/20): grandmothers' sayings and recipes, up to
/// levelB.sittiWall.maxChars. Nothing shows until a person approves it, posts carry no names, and players can like,
/// report or block, but not reply. Enough reports send a post back to a person.
/// </summary>
public static class WallEndpoints
{
    public static void Map(RouteGroupBuilder me)
    {
        var wall = me.MapGroup("/wall").AddEndpointFilter(new WallAgeFilter());
        wall.MapGet("", List);
        wall.MapPost("", Post);
        wall.MapPost("/{id:guid}/like", Like);
        wall.MapPost("/{id:guid}/report", Report);
        wall.MapPost("/{id:guid}/block", Block);
    }

    public static void MapAdmin(RouteGroupBuilder game)
    {
        var admin = game.MapGroup("/admin/wall").RequireAuthorization(AuthSchemes.Admin);
        admin.MapGet("", Queue).AddEndpointFilter(new StaffFilter(StaffRole.Support));
        admin.MapPost("/{id:guid}/approve", (Guid id, HttpContext http, PlatformDb db, StaffAccounts staff, TimeProvider clock, CancellationToken ct) =>
            Review(id, WallStatus.Approved, http, db, staff, clock, ct)).AddEndpointFilter(new StaffFilter(StaffRole.Support));
        admin.MapPost("/{id:guid}/reject", (Guid id, HttpContext http, PlatformDb db, StaffAccounts staff, TimeProvider clock, CancellationToken ct) =>
            Review(id, WallStatus.Rejected, http, db, staff, clock, ct)).AddEndpointFilter(new StaffFilter(StaffRole.Support));
    }

    sealed class WallAgeFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var clock = http.RequestServices.GetRequiredService<TimeProvider>();
            if (!SocialRules.Allowed(http.Player(), clock.GetUtcNow())) return RequestContext.Error("parent_needed", StatusCodes.Status403Forbidden);
            return await next(context);
        }
    }

    // Approved posts, newest first, without posts the player reported or by players they blocked; and their own posts.
    static async Task<IResult> List(HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var blocked = db.Blocks.Where(b => b.PlayerId == me).Select(b => b.BlockedId);
        var reported = db.WallReactions.Where(r => r.PlayerId == me && r.Kind == "report").Select(r => r.PostId);
        var liked = await db.WallReactions.Where(r => r.PlayerId == me && r.Kind == "like").Select(r => r.PostId).ToListAsync(ct);
        var posts = await db.WallPosts.AsNoTracking()
            .Where(p => p.GameId == game.Id && p.Status == WallStatus.Approved && !blocked.Contains(p.PlayerId) && !reported.Contains(p.Id))
            .OrderByDescending(p => p.CreatedAt).Take(50).ToListAsync(ct);
        var mine = await db.WallPosts.AsNoTracking().Where(p => p.PlayerId == me).OrderByDescending(p => p.CreatedAt).Take(10)
            .Select(p => new { id = p.Id, text = p.Text, status = p.Status, likes = p.Likes, createdAt = p.CreatedAt }).ToListAsync(ct);
        return Results.Ok(new
        {
            posts = posts.Select(p => new { id = p.Id, text = p.Text, likes = p.Likes, liked = liked.Contains(p.Id), mine = p.PlayerId == me }),
            mine,
        });
    }

    static async Task<IResult> Post(WallPostRequest request, HttpContext http, PlatformDb db, IConfiguration config, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        using var balance = await GameBalance.ActiveAsync(db, game.Id, ct);
        var root = balance?.RootElement ?? default;
        int maxChars = (int)SocialRules.Number(root, 140, "levelB", "sittiWall", "maxChars");
        int perDay = (int)SocialRules.Number(root, 1, "levelB", "sittiWall", "postsPerDay");
        string text = (request.Text ?? "").Trim();
        if (text.Length == 0) return RequestContext.Error("empty");
        if (new StringInfo(text).LengthInTextElements > maxChars) return RequestContext.Error("too_long");
        var now = clock.GetUtcNow();
        if (await db.WallPosts.CountAsync(p => p.PlayerId == me && p.CreatedAt > now.AddDays(-1), ct) >= perDay) return RequestContext.Error("daily_limit");

        string? refused = WallFilter.Refuse(text, config.GetSection("Wall:BlockedWords").Get<string[]>());
        var post = new WallPost
        {
            GameId = game.Id, PlayerId = me, Text = text, CreatedAt = now,
            Status = refused is null ? WallStatus.Pending : WallStatus.Rejected, AutoReason = refused,
        };
        db.WallPosts.Add(post);
        await db.SaveChangesAsync(ct);
        return refused is null
            ? Results.Ok(new { id = post.Id, status = post.Status })
            : RequestContext.Error("refused_" + refused);
    }

    // Likes toggle; a player's own post can't be liked by them.
    static async Task<IResult> Like(Guid id, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var post = await db.WallPosts.FirstOrDefaultAsync(p => p.Id == id && p.GameId == game.Id && p.Status == WallStatus.Approved, ct);
        if (post is null || post.PlayerId == me) return RequestContext.Error("unknown_post", StatusCodes.Status404NotFound);
        var like = await db.WallReactions.FirstOrDefaultAsync(r => r.PlayerId == me && r.PostId == id && r.Kind == "like", ct);
        bool liked = like == null;
        if (liked) db.WallReactions.Add(new WallReaction { GameId = game.Id, PlayerId = me, PostId = id, Kind = "like" });
        else db.WallReactions.Remove(like!);
        await db.SaveChangesAsync(ct);
        int change = liked ? 1 : -1;
        await db.WallPosts.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Likes, p => p.Likes + change), ct);
        return Results.Ok(new { liked });
    }

    // A report hides the post from the reporter at once; enough reports send it back to a person.
    static async Task<IResult> Report(Guid id, HttpContext http, PlatformDb db, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var post = await db.WallPosts.FirstOrDefaultAsync(p => p.Id == id && p.GameId == game.Id, ct);
        if (post is null) return RequestContext.Error("unknown_post", StatusCodes.Status404NotFound);
        db.WallReactions.Add(new WallReaction { GameId = game.Id, PlayerId = me, PostId = id, Kind = "report", CreatedAt = clock.GetUtcNow() });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (PlayerEndpoints.IsUniqueViolation(e))
        {
            return Results.NoContent();
        }
        using var balance = await GameBalance.ActiveAsync(db, game.Id, ct);
        int toReview = (int)SocialRules.Number(balance?.RootElement ?? default, 3, "levelB", "sittiWall", "reportsToReview");
        await db.WallPosts.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Reports, p => p.Reports + 1), ct);
        await db.WallPosts.Where(p => p.Id == id && p.Status == WallStatus.Approved && p.Reports >= toReview)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, WallStatus.Pending).SetProperty(p => p.AutoReason, "reports"), ct);
        return Results.NoContent();
    }

    // Blocking a post's writer hides everything they post from this player. Names stay hidden.
    static async Task<IResult> Block(Guid id, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var me = http.Player().Id;
        var author = await db.WallPosts.Where(p => p.Id == id && p.GameId == game.Id).Select(p => (Guid?)p.PlayerId).FirstOrDefaultAsync(ct);
        if (author is null || author == me) return RequestContext.Error("unknown_post", StatusCodes.Status404NotFound);
        if (!await db.Blocks.AnyAsync(b => b.PlayerId == me && b.BlockedId == author, ct))
        {
            db.Blocks.Add(new PlayerBlock { GameId = game.Id, PlayerId = me, BlockedId = author.Value });
            await db.SaveChangesAsync(ct);
        }
        return Results.NoContent();
    }

    // Staff: posts waiting for a person (new, or sent back by reports), oldest first.
    static async Task<IResult> Queue(string? status, HttpContext http, PlatformDb db, CancellationToken ct)
    {
        var game = http.Game();
        var wanted = Enum.TryParse<WallStatus>(status, true, out var s) ? s : WallStatus.Pending;
        return Results.Ok(await db.WallPosts.AsNoTracking().Where(p => p.GameId == game.Id && p.Status == wanted)
            .OrderBy(p => p.CreatedAt).Take(100)
            .Select(p => new { p.Id, p.PlayerId, p.Text, p.Status, p.AutoReason, p.Likes, p.Reports, p.CreatedAt, p.ReviewedAt })
            .ToListAsync(ct));
    }

    static async Task<IResult> Review(Guid id, WallStatus decision, HttpContext http, PlatformDb db, StaffAccounts staff, TimeProvider clock, CancellationToken ct)
    {
        var game = http.Game();
        var post = await db.WallPosts.FirstOrDefaultAsync(p => p.Id == id && p.GameId == game.Id, ct);
        if (post is null) return RequestContext.Error("unknown_post", StatusCodes.Status404NotFound);
        post.Status = decision;
        post.ReviewedAt = clock.GetUtcNow();
        post.ReviewedBy = http.Admin().Id;
        staff.Audit(game.Id, http.Admin(), decision == WallStatus.Approved ? "wall.approve" : "wall.reject", $"post:{id}");
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { post.Id, post.Status });
    }
}
