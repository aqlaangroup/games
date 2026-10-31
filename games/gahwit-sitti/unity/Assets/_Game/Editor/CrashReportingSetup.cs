using System.IO;
using Gahwit.Boot;
using Sentry.Unity;
using UnityEditor;
using UnityEngine;

namespace Gahwit.EditorTools
{
    // Writes the crash reporter's options (missing/16): crashes and errors from phones go to our own GlitchTip
    // (errors.aqlaan.com) through the open-source Sentry SDK. No screenshots, no view hierarchy, no personal data, no
    // session or performance tracking. Also runs with -executeMethod Gahwit.EditorTools.CrashReportingSetup.Apply.
    public static class CrashReportingSetup
    {
        // GlitchTip's public client key for this game. Every app build carries it by design; it can only send events.
        public const string Dsn = "https://09109cbe98e8447a8b5266526625ca22@errors.aqlaan.com/1";
        const string ConfigurationPath = "Assets/Resources/Sentry/CrashReportingOptions.asset";

        [MenuItem("Gahwit Sitti/Set up crash reporting")]
        public static void Apply()
        {
            string path = ScriptableSentryUnityOptions.GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var options = AssetDatabase.LoadAssetAtPath<ScriptableSentryUnityOptions>(path);
            if (options == null)
            {
                options = ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();
                AssetDatabase.CreateAsset(options, path);
            }
            var configuration = AssetDatabase.LoadAssetAtPath<CrashReportingOptions>(ConfigurationPath);
            if (configuration == null)
            {
                configuration = ScriptableObject.CreateInstance<CrashReportingOptions>();
                AssetDatabase.CreateAsset(configuration, ConfigurationPath);
            }

            options.Enabled = true;
            options.Dsn = Dsn;
            options.CaptureInEditor = false;
            options.SendDefaultPii = false;
            options.IsEnvironmentUser = false;
            options.AttachScreenshot = false;
            options.AttachViewHierarchy = false;
            options.AutoSessionTracking = false;
            options.TracesSampleRate = 0;
            options.IosNativeSupportEnabled = true;
            options.AndroidNativeSupportEnabled = true;
            options.Il2CppLineNumberSupportEnabled = true;
            options.OptionsConfiguration = configuration;
            EditorUtility.SetDirty(options);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CrashReportingSetup] Wrote {path}.");
        }
    }
}
