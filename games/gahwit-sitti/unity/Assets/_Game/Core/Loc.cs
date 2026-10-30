using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Gahwit.Core
{
    // On-screen text from the Unity Localization "UI" string table (Assets/Content/Localization).
    // Never type player-facing text into code or a scene (CLAUDE.md).
    public static class Loc
    {
        public const string UiTable = "UI";

        static StringTable table;

        // Loads the table for "ar" or "en" synchronously; the table is marked to preload, so this is quick.
        public static void Load(string language)
        {
            LocalizationSettings.InitializationOperation.WaitForCompletion();
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(language));
            if (locale != null) LocalizationSettings.SelectedLocale = locale;
            table = LocalizationSettings.StringDatabase.GetTable(UiTable, locale);
            if (table == null) Debug.LogError($"[Loc] No '{UiTable}' string table for '{language}'.");
        }

        public static string T(string key)
        {
            var entry = table != null ? table.GetEntry(key) : null;
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : key;
        }

        public static string T(string key, params object[] args) => string.Format(T(key), args);
    }
}
