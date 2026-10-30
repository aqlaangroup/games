using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Gahwit.EditorTools
{
    public static class BuildTools
    {
        public const string IosPath = "Builds/iOS";

        // Writes the Xcode project. Signing uses the team in Player Settings; Addressables
        // (the localization tables) build with the player. Also runs with -executeMethod.
        [MenuItem("Gahwit Sitti/Build iOS (Xcode project)")]
        public static void BuildIos()
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = IosPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };
            var summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"iOS build {summary.result} with {summary.totalErrors} errors");
            Debug.Log($"[BuildTools] Xcode project in {IosPath} ({summary.totalTime:mm\\:ss}).");
        }
    }
}
