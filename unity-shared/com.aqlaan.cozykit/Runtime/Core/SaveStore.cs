using System.IO;
using UnityEngine;

namespace Aqlaan.CozyKit
{
    // Local save files in persistentDataPath. Writes go to a temporary file first, so a crash can't leave half a save.
    public static class SaveStore
    {
        static string PathFor(string slot) => Path.Combine(Application.persistentDataPath, slot + ".json");

        public static string LoadText(string slot)
        {
            string path = PathFor(slot);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public static void SaveText(string slot, string text)
        {
            string path = PathFor(slot);
            string temp = path + ".tmp";
            File.WriteAllText(temp, text);
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
