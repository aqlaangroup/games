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
    // Creates the localization settings, the Arabic and English locales and the UI string table if they are missing.
    // The tables in Assets/Content/Localization are the source for every on-screen string (COMPLETE_SPEC.md section 6).
    public static class LocalizationSetup
    {
        public const string Folder = "Assets/Content/Localization";
        public const string UiTable = "UI";
        static readonly string[] LocaleCodes = { "ar", "en" };

        [MenuItem("Gahwit Sitti/Set up localization")]
        public static StringTableCollection EnsureSetup()
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

            var collection = LocalizationEditorSettings.GetStringTableCollection(UiTable)
                             ?? LocalizationEditorSettings.CreateStringTableCollection(UiTable, Folder + "/Tables", locales);
            foreach (var table in collection.StringTables) LocalizationEditorSettings.SetPreloadTableFlag(table, true);
            AssetDatabase.SaveAssets();
            return collection;
        }

        // Writes key → text pairs for one locale into the UI table, adding keys that are missing.
        public static void Import(string localeCode, IDictionary<string, string> strings)
        {
            var collection = EnsureSetup();
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
    }
}
