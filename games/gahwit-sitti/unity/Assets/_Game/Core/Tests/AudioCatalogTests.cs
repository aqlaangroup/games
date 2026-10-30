using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Gahwit.Core.Tests
{
    // Every cue the code asks for exists, so no action plays silence by a typo (missing/14).
    public sealed class AudioCatalogTests
    {
        [Test]
        public void EveryCueNamedInCodeIsInTheCatalog()
        {
            var catalog = AudioCatalog.Load();
            Assert.IsNotNull(catalog, "Gahwit Sitti > Rebuild audio catalog");
            var ids = catalog.Cues.Where(c => c.Clip != null).Select(c => c.Id).ToHashSet();
            var pattern = new Regex(@"\b(?:Play|PlayMusic|Loop)\(""([a-z0-9_]+)""");
            string root = Path.Combine(Application.dataPath, "_Game");
            int found = 0;
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                found++;
                Assert.IsTrue(ids.Contains(match.Groups[1].Value), $"cue '{match.Groups[1].Value}' in {Path.GetFileName(file)} is not in the catalog");
            }
            Assert.Greater(found, 10);
        }

        [Test]
        public void MusicAndEffectsLandInTheirGroups()
        {
            var catalog = AudioCatalog.Load();
            Assert.AreEqual(AudioGroup.Music, catalog.Cues.Single(c => c.Id == "music_memory").Group);
            Assert.AreEqual(AudioGroup.Interface, catalog.Cues.Single(c => c.Id == "ui_tap").Group);
            Assert.AreEqual(AudioGroup.Kitchen, catalog.Cues.Single(c => c.Id == "merge").Group);
            Assert.AreEqual(AudioGroup.Story, catalog.Cues.Single(c => c.Id == "letter_unfold").Group);
        }
    }
}
