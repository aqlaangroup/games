using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Gahwit.EditorTools
{
    // design/balance.json is the single source of truth for numbers, and game.json for the products the shop sells
    // (the platform registers the same file). The game reads copies in Resources/Config, refreshed when the Editor
    // loads, before every build, and from the menu.
    public sealed class BalanceSync : IPreprocessBuildWithReport
    {
        static readonly (string source, string target)[] Files =
        {
            ("../../design/balance.json", "Assets/Content/Resources/Config/balance.json"),
            ("../../game.json", "Assets/Content/Resources/Config/game.json"),
        };

        static string Full(string relative) => Path.GetFullPath(Path.Combine(Application.dataPath, relative));

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Sync();

        [InitializeOnLoadMethod]
        static void SyncWhenStale() => EditorApplication.delayCall += () => { if (IsStale()) Sync(); };

        [MenuItem("Gahwit Sitti/Sync balance.json")]
        public static void Sync()
        {
            foreach (var (source, target) in Files)
            {
                if (!File.Exists(Full(source)))
                {
                    Debug.LogError($"[BalanceSync] Not found: {Full(source)}");
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(Full(source), target, true);
                AssetDatabase.ImportAsset(target);
            }
            Debug.Log("[BalanceSync] Copied design/balance.json and game.json into the game.");
        }

        static bool IsStale()
        {
            foreach (var (source, target) in Files)
                if (File.Exists(Full(source)) && (!File.Exists(target) || File.ReadAllText(Full(source)) != File.ReadAllText(target))) return true;
            return false;
        }
    }
}
