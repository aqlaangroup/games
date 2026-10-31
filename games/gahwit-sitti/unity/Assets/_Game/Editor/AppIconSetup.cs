using UnityEditor;
using UnityEngine;

namespace Gahwit.EditorTools
{
    // The app icon (missing/13): one 1024 px picture from Google Flow (art/flow/app_icon.png, copied to IconPath), set as
    // the default icon so Unity scales it for every iOS and Android size. Gahwit Sitti > Set app icon, or in batch:
    // Unity -batchmode -quit -executeMethod Gahwit.EditorTools.AppIconSetup.Apply
    public static class AppIconSetup
    {
        public const string IconPath = "Assets/Art/AppIcon/app_icon.png";

        [MenuItem("Gahwit Sitti/Set app icon")]
        public static void Apply()
        {
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(IconPath) is TextureImporter importer)
            {
                // Sharp at every size: no compression, no mipmaps, the full 1024 px.
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogError($"[AppIcon] No icon at {IconPath}.");
                return;
            }
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AppIcon] Set {IconPath} ({icon.width}×{icon.height}) as the default icon.");
        }
    }
}
