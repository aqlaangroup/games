using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Gahwit.EditorTools
{
    // Creates the localization settings, the Arabic and English locales and the UI and Glossary string tables if they
    // are missing. The tables in Assets/Content/Localization are the source for every on-screen string
    // (COMPLETE_SPEC.md section 6).
    public static class LocalizationSetup
    {
        public const string Folder = "Assets/Content/Localization";
        public const string UiTable = "UI";
        public const string GlossaryTable = "Glossary";
        static readonly string[] LocaleCodes = { "ar", "en" };

        [MenuItem("Gahwit Sitti/Set up localization")]
        public static StringTableCollection EnsureSetup() => EnsureSetup(UiTable);

        public static StringTableCollection EnsureSetup(string tableName)
        {
            Directory.CreateDirectory(Folder + "/Locales");
            Directory.CreateDirectory(Folder + "/Tables");

            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, Folder + "/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            var locales = new List<Locale>();
            foreach (var code in LocaleCodes)
            {
                var locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code));
                if (locale == null)
                {
                    locale = Locale.CreateLocale(code);
                    AssetDatabase.CreateAsset(locale, $"{Folder}/Locales/{code}.asset");
                    LocalizationEditorSettings.AddLocale(locale);
                }
                locales.Add(locale);
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName)
                             ?? LocalizationEditorSettings.CreateStringTableCollection(tableName, Folder + "/Tables", locales);
            foreach (var table in collection.StringTables) LocalizationEditorSettings.SetPreloadTableFlag(table, true);
            AssetDatabase.SaveAssets();
            return collection;
        }

        // Writes key → text pairs for one locale into a table (the UI table unless named), adding keys that are missing.
        public static void Import(string localeCode, IDictionary<string, string> strings, string tableName = UiTable)
        {
            var collection = EnsureSetup(tableName);
            var table = collection.StringTables.First(t => t.LocaleIdentifier.Code == localeCode);
            foreach (var pair in strings)
            {
                if (collection.SharedData.GetEntry(pair.Key) == null) collection.SharedData.AddKey(pair.Key);
                table.AddEntry(pair.Key, pair.Value);
            }
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
        }

        // Imports a TSV of key, Arabic and English into a table, without the Editor open:
        // Unity -batchmode -quit -executeMethod Gahwit.EditorTools.LocalizationSetup.ImportTsv -tsv <file> [-table Glossary]
        public static void ImportTsv()
        {
            var args = System.Environment.GetCommandLineArgs();
            string Arg(string name)
            {
                int at = System.Array.IndexOf(args, name);
                return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
            }
            string path = Arg("-tsv") ?? throw new System.ArgumentException("Pass -tsv <file>.");
            string tableName = Arg("-table") ?? UiTable;
            var ar = new Dictionary<string, string>();
            var en = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(path))
            {
                var parts = line.Split('\t');
                if (parts.Length < 3 || parts[0].Length == 0) continue;
                ar[parts[0]] = parts[1];
                en[parts[0]] = parts[2];
            }
            Import("ar", ar, tableName);
            Import("en", en, tableName);
            Debug.Log($"[LocalizationSetup] Imported {ar.Count} keys into {tableName}.");
        }
    }
}
