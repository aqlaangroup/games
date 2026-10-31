using System.Threading.Tasks;
using Gahwit.Core;
using Gahwit.Platform;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Gahwit.Boot
{
    // The balance.json staff publish on the platform (missing/18; GAME_SYSTEMS.md section 16). The game plays with
    // the last good published copy, laid over its own, so starting never waits on the network; each launch then
    // fetches the newest one for the next. A copy that fails BalanceCheck is never used or kept.
    public static class RemoteConfig
    {
        const string Slot = "gahwit-sitti-config";

        public static string Version { get; private set; } = "bundled";

        public static BalanceConfig Load()
        {
            string bundled = Resources.Load<TextAsset>(BalanceConfig.ResourcePath).text;
            var cached = SaveStore.LoadText(Slot);
            if (cached != null)
            {
                try
                {
                    var kept = JObject.Parse(cached);
                    string merged = BalanceCheck.Merge(bundled, (string)kept["json"]);
                    if (BalanceCheck.Problems(merged).Count == 0)
                    {
                        Version = (string)kept["version"] ?? "published";
                        return BalanceConfig.Parse(merged);
                    }
                }
                catch (Newtonsoft.Json.JsonException) { }
                Debug.LogWarning("[Config] The kept published config is broken; using the game's own.");
            }
            Version = "bundled";
            return BalanceConfig.Parse(bundled);
        }

        // Fetches the newest published config and keeps it for the next launch, if it's safe.
        public static async Task Refresh(PlatformClient client)
        {
            var reply = await client.Config();
            if (!reply.Ok) return;
            string bundled = Resources.Load<TextAsset>(BalanceConfig.ResourcePath).text;
            var problems = BalanceCheck.Problems(BalanceCheck.Merge(bundled, reply.Value.Json));
            if (problems.Count > 0)
            {
                Debug.LogWarning("[Config] Published config refused: " + string.Join(", ", problems));
                return;
            }
            SaveStore.SaveText(Slot, new JObject { ["version"] = reply.Value.Version, ["json"] = reply.Value.Json }.ToString());
            Debug.Log($"[Config] Kept published config version {reply.Value.Version} for the next launch.");
        }
    }
}
