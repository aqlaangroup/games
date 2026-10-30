using System.IO;
using System.Linq;
using Gahwit.Core;
using UnityEditor;
using UnityEngine;

namespace Gahwit.EditorTools
{
    // Lists every file under Assets/Audio as a cue named after the file (missing/14). Music streams; short effects
    // load decompressed in mono, which costs nothing per play and keeps the download small.
    public sealed class AudioCatalogBuilder : AssetPostprocessor
    {
        const string AudioFolder = "Assets/Audio";
        const string CatalogPath = "Assets/Content/Resources/" + AudioCatalog.ResourcePath + ".asset";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioFolder)) return;
            var importer = (AudioImporter)assetImporter;
            bool music = assetPath.Contains("/Music/");
            importer.forceToMono = !music;
            importer.loadInBackground = music;
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.5f : 0.7f;
            importer.defaultSampleSettings = settings;
        }

        [MenuItem("Gahwit Sitti/Rebuild audio catalog")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AudioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.Cues = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(p =>
                {
                    string id = Path.GetFileNameWithoutExtension(p);
                    return new AudioCatalog.Cue
                    {
                        Id = id,
                        Clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p),
                        Group = AudioCatalog.GroupFor(id, p.Contains("/Music/"))
                    };
                }).ToList();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Audio] Catalog has {catalog.Cues.Count} cues.");
        }
    }
}
