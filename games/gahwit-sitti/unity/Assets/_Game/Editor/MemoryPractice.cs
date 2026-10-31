using UnityEditor;
using UnityEngine;

namespace Gahwit.EditorTools
{
    // Plays one practice memory of each game type in Play mode (Content/memory_practice.json, missing/12), while
    // chapters 2–9 that will use them aren't written yet.
    public static class MemoryPractice
    {
        const string Menu = "Gahwit Sitti/Play memory game/";

        [MenuItem(Menu + "Pour")] static void Pour() => Practice("pour");
        [MenuItem(Menu + "Rhythm")] static void Rhythm() => Practice("rhythm");
        [MenuItem(Menu + "Find")] static void Find() => Practice("find");
        [MenuItem(Menu + "Restore")] static void Restore() => Practice("restore");
        [MenuItem(Menu + "Trace")] static void Trace() => Practice("trace");
        [MenuItem(Menu + "Tune")] static void Tune() => Practice("tune");
        [MenuItem(Menu + "Choose")] static void Choose() => Practice("choose");
        [MenuItem(Menu + "Reassemble")] static void Reassemble() => Practice("reassemble");

        public static void Practice(string type)
        {
            var scene = Application.isPlaying ? Object.FindFirstObjectByType<Gahwit.Boot.CafeScene>() : null;
            if (scene == null) Debug.LogWarning("[Memory] Enter Play mode and wait for the café first.");
            else scene.PracticeMemory(type);
        }
    }
}
