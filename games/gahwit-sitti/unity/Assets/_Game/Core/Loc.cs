using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Gahwit.Core
{
    // On-screen text from the Unity Localization "UI" string table (Assets/Content/Localization), and the meanings of
    // tap-to-explain words from the "Glossary" table (COMPLETE_SPEC.md section 6).
    // Never type player-facing text into code or a scene (CLAUDE.md).
    public static class Loc
    {
        public const string UiTable = "UI";
        public const string GlossaryTable = "Glossary";

        static StringTable table, glossary;

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
            var loadingGlossary = LocalizationSettings.StringDatabase.GetTableAsync(GlossaryTable, locale);
            yield return loadingGlossary;
            glossary = loadingGlossary.Result;
        }

        public static string T(string key)
        {
            var entry = table != null ? table.GetEntry(key) : null;
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : key;
        }

        public static string T(string key, params object[] args) => string.Format(T(key), args);

        // A word's meaning in the interface language, or null when the glossary doesn't have it.
        public static string Gloss(string word)
        {
            var entry = glossary != null ? glossary.GetEntry(word) : null;
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : null;
        }
    }
}
