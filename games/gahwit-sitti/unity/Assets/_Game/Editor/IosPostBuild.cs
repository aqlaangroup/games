#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Gahwit.EditorTools
{
    // Adds what the iOS plugins need to the Xcode project: Sign in with Apple (Plugins/iOS/GahwitAppleSignIn.mm), and
    // the photo-library reason for the postcard share sheet (Plugins/iOS/GahwitShare.mm).
    public static class IosPostBuild
    {
        const string Entitlements = "Unity-iPhone/GahwitSitti.entitlements";

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            string projectPath = PBXProject.GetPBXProjectPath(path);

            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "AuthenticationServices.framework", false);
            string appTarget = project.GetUnityMainTargetGuid();
            project.WriteToFile(projectPath);

            var capabilities = new ProjectCapabilityManager(projectPath, Entitlements, null, appTarget);
            capabilities.AddSignInWithApple();
            capabilities.WriteToFile();
            UnityEngine.Debug.Log($"[IosPostBuild] Sign in with Apple added ({Path.Combine(path, Entitlements)}).");

            // The share sheet (Plugins/iOS/GahwitShare.mm) offers "Save Image", which needs a reason in Info.plist.
            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSPhotoLibraryAddUsageDescription", "To save your café postcards to your photos.");
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
