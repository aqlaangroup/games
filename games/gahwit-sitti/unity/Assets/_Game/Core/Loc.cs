using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Core
{
    // Text table. Every string on screen comes from Resources/Loc/<language>.json, never from code or scenes.
    public static class Loc
    {
        static Dictionary<string, string> table = new Dictionary<string, string>();

        public static void Load(string language)
        {
            var asset = Resources.Load<TextAsset>("Loc/" + language);
            table = asset != null
                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(asset.text)
                : new Dictionary<string, string>();
            if (asset == null) Debug.LogError($"[Loc] Missing Resources/Loc/{language}.json");
        }

        public static string T(string key) => table.TryGetValue(key, out var value) ? value : key;

        public static string T(string key, params object[] args) => string.Format(T(key), args);
    }
}
