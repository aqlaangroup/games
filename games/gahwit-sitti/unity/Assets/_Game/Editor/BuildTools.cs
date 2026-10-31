using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Gahwit.EditorTools
{
    // Phone builds (missing/23). Each build gets the next build number: BUILD_NUMBER from CI, or the number of commits
    // here. Player Settings keep their own numbers; the build number is set only for the build. Also runs with
    // -executeMethod.
    public static class BuildTools
    {
        public const string IosPath = "Builds/iOS";
        public const string AndroidPath = "Builds/Android/gahwit-sitti.aab";

        // Google Play's minimum target for new apps and updates from 31 August 2026.
        const int AndroidTargetSdk = 36;

        // Writes the Xcode project. Signing uses the team in Player Settings; Addressables
        // (the localization tables) build with the player.
        [MenuItem("Gahwit Sitti/Build iOS (Xcode project)")]
        public static void BuildIos()
        {
            string before = PlayerSettings.iOS.buildNumber;
            PlayerSettings.iOS.buildNumber = BuildNumber().ToString();
            try
            {
                Build(BuildTarget.iOS, BuildTargetGroup.iOS, IosPath);
            }
            finally
            {
                PlayerSettings.iOS.buildNumber = before;
            }
        }

        // An app bundle for Google Play. The upload key comes from the environment, never the repo (CLAUDE.md):
        // GAHWIT_ANDROID_KEYSTORE (the file), GAHWIT_ANDROID_KEYSTORE_PASS, GAHWIT_ANDROID_KEY_ALIAS and
        // GAHWIT_ANDROID_KEY_PASS. Without them the bundle is signed with the debug key: fine on a test phone, refused
        // by Google Play.
        [MenuItem("Gahwit Sitti/Build Android (app bundle)")]
        public static void BuildAndroid()
        {
            int codeBefore = PlayerSettings.Android.bundleVersionCode;
            bool bundleBefore = EditorUserBuildSettings.buildAppBundle;
            bool customBefore = PlayerSettings.Android.useCustomKeystore;
            string keystoreBefore = PlayerSettings.Android.keystoreName, aliasBefore = PlayerSettings.Android.keyaliasName;
            PlayerSettings.Android.bundleVersionCode = BuildNumber();
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)AndroidTargetSdk;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = true;
            string keystore = Environment.GetEnvironmentVariable("GAHWIT_ANDROID_KEYSTORE");
            bool signed = !string.IsNullOrEmpty(keystore) && File.Exists(keystore);
            PlayerSettings.Android.useCustomKeystore = signed;
            if (signed)
            {
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("GAHWIT_ANDROID_KEYSTORE_PASS");
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("GAHWIT_ANDROID_KEY_ALIAS");
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("GAHWIT_ANDROID_KEY_PASS");
            }
            else Debug.LogWarning("[BuildTools] No upload key in the environment: the bundle is debug-signed and Google Play will refuse it.");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AndroidPath));
                Build(BuildTarget.Android, BuildTargetGroup.Android, AndroidPath);
                Debug.Log($"[BuildTools] {AndroidPath}: {new FileInfo(AndroidPath).Length / 1048576.0:0.0} MB, build {PlayerSettings.Android.bundleVersionCode}.");
            }
            finally
            {
                PlayerSettings.Android.bundleVersionCode = codeBefore;
                EditorUserBuildSettings.buildAppBundle = bundleBefore;
                // Nothing about the key stays in Player Settings, so nothing about it reaches Git.
                PlayerSettings.Android.useCustomKeystore = customBefore;
                PlayerSettings.Android.keystoreName = keystoreBefore;
                PlayerSettings.Android.keyaliasName = aliasBefore;
                PlayerSettings.Android.keystorePass = PlayerSettings.Android.keyaliasPass = "";
            }
        }

        static void Build(BuildTarget target, BuildTargetGroup group, string path)
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = target,
                targetGroup = group,
                options = BuildOptions.None,
            };
            var summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"{target} build {summary.result} with {summary.totalErrors} errors");
            Debug.Log($"[BuildTools] {target} build in {path} ({summary.totalTime:mm\\:ss}).");
        }

        // CI passes BUILD_NUMBER; on a desk it's the number of commits, which only goes up.
        public static int BuildNumber()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("BUILD_NUMBER"), out int fromCi) && fromCi > 0) return fromCi;
            try
            {
                var git = Process.Start(new ProcessStartInfo("git", "rev-list --count HEAD")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                string output = git.StandardOutput.ReadToEnd().Trim();
                git.WaitForExit();
                if (int.TryParse(output, out int commits) && commits > 0) return commits;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildTools] No git for the build number: " + e.Message);
            }
            return 1;
        }
    }
}
