using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.Localization;
using UnityEngine;

namespace Gahwit.Core.Tests
{
    // Every string the game asks for exists, in Arabic and English.
    public sealed class LocalizationTests
    {
        static StringTableCollection Ui => LocalizationEditorSettings.GetStringTableCollection(Loc.UiTable);

        static HashSet<string> Keys => new HashSet<string>(Ui.SharedData.Entries.Select(e => e.Key));

        [Test]
        public void EveryKeyHasArabicAndEnglishText()
        {
            var codes = Ui.StringTables.Select(t => t.LocaleIdentifier.Code).ToList();
            CollectionAssert.IsSupersetOf(codes, new[] { "ar", "en" });
            foreach (var table in Ui.StringTables)
            foreach (var entry in Ui.SharedData.Entries)
                Assert.IsFalse(string.IsNullOrEmpty(table.GetEntry(entry.Id)?.Value), $"{table.LocaleIdentifier.Code}: '{entry.Key}' is empty");
        }

        [Test]
        public void EveryKeyWrittenInCodeExists()
        {
            var keys = Keys;
            // Whole keys only; keys built from content ("char." + id) are checked in the next test.
            var pattern = new Regex(@"Loc\.T\(""([a-zA-Z0-9_.]+)""\s*[,)]");
            string root = Path.Combine(Application.dataPath, "_Game");
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
                Assert.IsTrue(keys.Contains(match.Groups[1].Value), $"'{match.Groups[1].Value}' used in {Path.GetFileName(file)} is not in the UI table");
        }

        [Test]
        public void EveryCharacterWalkInAndTaskHasText()
        {
            var keys = Keys;
            var content = ChapterContent.Load();
            foreach (var id in content.Characters.Keys.Where(id => id != "sys"))
                Assert.IsTrue(keys.Contains("char." + id), $"char.{id}");
            foreach (var id in content.WalkIns)
                Assert.IsTrue(keys.Contains("walkin." + id), $"walkin.{id}");
            foreach (var task in content.Chapters.SelectMany(c => c.Tasks))
            {
                Assert.IsTrue(keys.Contains($"task.{task.Id}.title"), $"task.{task.Id}.title");
                Assert.IsTrue(keys.Contains($"task.{task.Id}.desc"), $"task.{task.Id}.desc");
                foreach (var style in task.Styles ?? new System.Collections.Generic.List<string>())
                {
                    Assert.IsTrue(content.Styles.ContainsKey(style), $"task {task.Id} offers unknown style '{style}'");
                    Assert.IsTrue(keys.Contains("style." + style), $"style.{style}");
                }
            }
        }
    }
}
