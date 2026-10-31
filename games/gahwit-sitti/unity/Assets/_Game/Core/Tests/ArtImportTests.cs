using NUnit.Framework;
using UnityEditor;

namespace Gahwit.Core.Tests
{
    // The runtime art's import rules (Editor/ArtImport.cs): no mipmaps, at most 512 px, compressed.
    public sealed class ArtImportTests
    {
        [TestCase("Assets/Content/Resources/Art/Portraits/teta_smile.png")]
        [TestCase("Assets/Content/Resources/Art/Items/coffee_1.png")]
        public void PortraitsAndIconsImportLightForPhones(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, path);
            Assert.IsFalse(importer.mipmapEnabled, "drawn at one size on the interface");
            Assert.LessOrEqual(importer.maxTextureSize, 512);
            Assert.AreEqual(TextureImporterCompression.Compressed, importer.textureCompression);
        }
    }
}
