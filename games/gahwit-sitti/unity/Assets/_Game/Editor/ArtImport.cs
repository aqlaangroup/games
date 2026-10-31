using UnityEditor;

namespace Gahwit.EditorTools
{
    // Import rules for the art the game loads at runtime (missing/13): portraits and item icons are drawn on the
    // interface at one size, so they skip mipmaps, stay within 512 px, keep their transparency, and are compressed for
    // each platform. Saves phone memory on a 3 GB Android.
    public sealed class ArtImport : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/Content/Resources/Art/";
        public const int MaxSize = 512;

        // Gahwit Sitti > Reimport art, or in batch: -executeMethod Gahwit.EditorTools.ArtImport.ReimportAll
        [MenuItem("Gahwit Sitti/Reimport art")]
        public static void ReimportAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder.TrimEnd('/') }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.isReadable = false;
            importer.maxTextureSize = MaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
