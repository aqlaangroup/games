using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gahwit.Core
{
    // Remote config (missing/18): the balance.json staff publish on the platform lays over the copy in the game, so a
    // section the published file lacks keeps the game's own numbers. The result is checked before it's used, so a
    // broken file can't break the game.
    public static class BalanceCheck
    {
        // The game's copy with the published values laid over it.
        public static string Merge(string bundled, string published)
        {
            var merged = JObject.Parse(bundled);
            merged.Merge(JObject.Parse(published), new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                MergeNullValueHandling = MergeNullValueHandling.Ignore
            });
            return merged.ToString(Formatting.None);
        }

        // What's wrong with a config, or an empty list when it's safe to play with.
        public static List<string> Problems(string json)
        {
            var problems = new List<string>();
            BalanceConfig config;
            try
            {
                config = BalanceConfig.Parse(json);
            }
            catch (JsonException e)
            {
                problems.Add("unreadable: " + e.Message);
                return problems;
            }
            if (config == null) problems.Add("empty");
            else
            {
                if (config.Board == null || config.Board.Cols < 3 || config.Board.Rows < 3) problems.Add("board");
                if (config.Energy == null || config.Energy.Cap <= 0 || config.Energy.RegenSeconds <= 0) problems.Add("energy");
                if (config.Orders == null || config.Orders.SlotsByPlayerLevel == null || config.Orders.SlotsByPlayerLevel.Count == 0) problems.Add("orders");
                if (config.Chains == null || config.Chains.Count == 0) problems.Add("chains");
                else
                    foreach (var chain in config.Chains)
                        if (chain.Value?.Items == null || chain.Value.Items.Count < 2) problems.Add("chain " + chain.Key);
                if (config.Generators?.List == null || config.Generators.List.Count == 0) problems.Add("generators");
                else
                    foreach (var generator in config.Generators.List)
                        if (config.Chains == null || !config.Chains.ContainsKey(generator.Chain)) problems.Add("generator " + generator.Id);
                if (config.Chapters == null || config.Chapters.Count == 0) problems.Add("chapters");
                if (config.PlayerLevels == null || config.Formulas == null || config.Unlocks == null || config.LevelB == null) problems.Add("sections");
                if (!PassReads(config)) problems.Add("pass");
            }
            return problems;
        }

        // The season pass: its seasons, tiers and both tracks' reward cycles.
        static bool PassReads(BalanceConfig config)
        {
            if (!(config.Events?.Templates?["pass"] is JObject)) return false;
            PassConfig pass;
            try
            {
                pass = config.Pass;
            }
            catch (System.Exception e) when (e is JsonException || e is System.ArgumentException)
            {
                return false;
            }
            return pass.Tiers > 0 && pass.PointsPerTier > 0 && pass.DurationDays > 0
                && System.DateTime.TryParseExact(pass.SeasonStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _)
                && pass.Rewards?.Free?.Cycle?.Count > 0 && pass.Rewards.Premium?.Cycle?.Count > 0;
        }
    }
}
