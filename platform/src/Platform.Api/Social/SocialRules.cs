using System.Text.Json;
using Platform.Api.Players;
using Platform.Domain;

namespace Platform.Api.Social;

/// <summary>Shared rules for the social features (games/gahwit-sitti/missing/20).</summary>
public static class SocialRules
{
    /// <summary>
    /// Social features need a player of 13 or older, or a parent's consent (COMPLETE_SPEC.md section 14). A player
    /// of unknown age counts as a child.
    /// </summary>
    public static bool Allowed(Player player, DateTimeOffset now) =>
        AgeBands.For(player.BirthYear, now.Year) is AgeBands.From13To15 or AgeBands.From16 || player.ParentConsent;

    public static string Day(DateTimeOffset now) => now.UtcDateTime.ToString("yyyy-MM-dd");

    /// <summary>A number from the published balance.json, by path, or a fallback when it's missing.</summary>
    public static double Number(JsonElement balance, double fallback, params string[] path)
    {
        var node = balance;
        foreach (var key in path)
            if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(key, out node)) return fallback;
        return node.ValueKind == JsonValueKind.Number ? node.GetDouble() : fallback;
    }

    // A list of strings from the balance, or none when the path or the list is missing.
    public static IReadOnlyList<string> Strings(JsonElement balance, params string[] path)
    {
        var node = balance;
        foreach (var key in path)
            if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(key, out node)) return [];
        return node.ValueKind == JsonValueKind.Array
            ? node.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList()
            : [];
    }

    // Easy to read aloud and type: no 0/O, 1/I/L.
    const string CodeLetters = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string NewFriendCode() =>
        new(Enumerable.Range(0, 8).Select(_ => CodeLetters[System.Security.Cryptography.RandomNumberGenerator.GetInt32(CodeLetters.Length)]).ToArray());
}
