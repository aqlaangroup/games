using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Core
{
    // Local JSON saves in persistentDataPath. Cloud save comes later through the platform API.
    public static class SaveStore
    {
        static string PathFor(string slot) => Path.Combine(Application.persistentDataPath, slot + ".json");

        public static T Load<T>(string slot) where T : class
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Could not read {path}: {e.Message}");
                return null;
            }
        }

        public static void Save<T>(string slot, T data)
        {
            string path = PathFor(slot);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(data));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }

        public static void Delete(string slot)
        {
            string path = PathFor(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
