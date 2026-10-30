using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Gahwit.EditorTools
{
    // design/balance.json is the single source of truth. The game reads a copy in Resources/Config,
    // refreshed when the Editor loads, before every build, and from the menu.
    public sealed class BalanceSync : IPreprocessBuildWithReport
    {
        const string Target = "Assets/Content/Resources/Config/balance.json";

        static string Source => Path.GetFullPath(Path.Combine(Application.dataPath, "../../design/balance.json"));

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Sync();

        [InitializeOnLoadMethod]
        static void SyncWhenStale() => EditorApplication.delayCall += () => { if (IsStale()) Sync(); };

        [MenuItem("Gahwit Sitti/Sync balance.json")]
        public static void Sync()
        {
            if (!File.Exists(Source))
            {
                Debug.LogError($"[BalanceSync] Not found: {Source}");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Target));
            File.Copy(Source, Target, true);
            AssetDatabase.ImportAsset(Target);
            Debug.Log("[BalanceSync] Copied design/balance.json into the game.");
        }

        static bool IsStale() =>
            File.Exists(Source) && (!File.Exists(Target) || File.ReadAllText(Source) != File.ReadAllText(Target));
    }
}
