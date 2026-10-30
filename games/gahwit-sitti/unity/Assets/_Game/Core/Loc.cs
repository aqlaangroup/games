using System.Collections;
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

        // Loads the table for "ar" or "en". Run it as a coroutine: it waits without blocking, because a blocking
        // wait (WaitForCompletion) never finishes on the phone.
        public static IEnumerator Load(string language)
        {
            yield return LocalizationSettings.InitializationOperation;
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(language));
            if (locale != null && LocalizationSettings.SelectedLocale != locale) LocalizationSettings.SelectedLocale = locale;
            var loading = LocalizationSettings.StringDatabase.GetTableAsync(UiTable, locale);
            yield return loading;
            table = loading.Result;
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
