using System.Text.Json;
using Platform.Domain;

namespace Platform.Api.Players;

/// <summary>
/// What a reward pays and what a gem spend costs, read from the game's published balance.json. The game never
/// grants gems itself (CLAUDE.md); it asks for them here, and the latest cloud save proves they were earned.
/// </summary>
public static class RewardRules
{
    /// <summary>"levelUp:N" needs the save at level N or above; "chapter:N" needs chapter N finished.</summary>
    public static (long Gems, string? Error) Reward(string rewardId, JsonElement balance, PlayerSave? save)
    {
        var parts = rewardId.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out int n) || n < 1) return (0, "unknown_reward");
        switch (parts[0])
        {
            case "levelUp" when n >= 2:
                if ((save?.Level ?? 1) < n) return (0, "not_earned_yet");
                var formula = balance.GetProperty("formulas").GetProperty("levelUpGems");
                return (formula.GetProperty("base").GetInt64() + n / formula.GetProperty("levelDivisor").GetInt32(), null);
            case "chapter":
                var chapters = balance.GetProperty("chapters");
                if (n > chapters.GetArrayLength()) return (0, "unknown_reward");
                if (!ChapterFinished(n, chapters, save)) return (0, "not_earned_yet");
                return (chapters[n - 1].GetProperty("reward").GetProperty("gems").GetInt64(), null);
            default:
                return (0, "unknown_reward");
        }
    }

    static bool ChapterFinished(int n, JsonElement chapters, PlayerSave? save)
    {
        if (save?.Chapter is not int chapter || chapter < n) return false;
        if (chapter > n) return true;
        // The last chapter has no next one to move on to, so all its tasks must be done.
        if (n != chapters.GetArrayLength()) return false;
        using var json = JsonDocument.Parse(save.Json);
        int task = json.RootElement.TryGetProperty("story", out var story) && story.TryGetProperty("task", out var t) ? t.GetInt32() : 0;
        return task >= chapters[n - 1].GetProperty("taskCosts").GetArrayLength();
    }

    /// <summary>
    /// The gem price of an item and what the game applies once it's paid. refillsToday is how many energy refills the
    /// player already bought today (UTC), because each one costs more (energy.gemRefill.gemCostsByPurchaseOfDay).
    /// </summary>
    public static (long Gems, object? Grant, string? Error) Spend(string item, JsonElement balance, int refillsToday)
    {
        var parts = item.Split(':', 2);
        switch (parts[0])
        {
            case "energyRefill" when parts.Length == 1:
                var refill = balance.GetProperty("energy").GetProperty("gemRefill");
                var costs = refill.GetProperty("gemCostsByPurchaseOfDay");
                return (costs[Math.Min(refillsToday, costs.GetArrayLength() - 1)].GetInt64(),
                    new { energy = refill.GetProperty("amount").GetInt32() }, null);
            case "storageSlot" when parts.Length == 2 && int.TryParse(parts[1], out int slot):
                var slotCosts = balance.GetProperty("board").GetProperty("inventorySlotGemCosts");
                if (slot < 1 || slot > slotCosts.GetArrayLength()) return (0, null, "unknown_item");
                return (slotCosts[slot - 1].GetInt64(), new { storageSlot = slot }, null);
            // "bubble:5" or "bubble:5:coffee": the chain comes back in the grant, so a replayed purchase knows what to add.
            case "bubble" when parts.Length == 2 && int.TryParse(parts[1].Split(':')[0], out int level):
                var bubble = balance.GetProperty("formulas").GetProperty("bubbleGems");
                int from = bubble.GetProperty("fromLevel").GetInt32();
                if (level < from || level > 20) return (0, null, "unknown_item");
                var bits = parts[1].Split(':');
                string? chain = bits.Length > 1 && bits[1].Length is > 0 and <= 32 ? bits[1] : null;
                return (bubble.GetProperty("base").GetInt64() << (level - from), new { bubbleLevel = level, chain }, null);
            case "memoryRetry" when parts.Length == 2 && parts[1].Length is > 0 and <= 64:
                return (balance.GetProperty("levelB").GetProperty("memories").GetProperty("skipRetryGems").GetInt64(),
                    new { memory = parts[1] }, null);
            default:
                return (0, null, "unknown_item");
        }
    }
}
