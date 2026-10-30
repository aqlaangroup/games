using Gahwit.Core;
using UnityEditor;
using UnityEngine;

namespace Gahwit.EditorTools
{
    public static class SaveTools
    {
        // Same slot name as GameBootstrap.SaveSlot.
        const string SaveSlot = "gahwit-sitti";

        [MenuItem("Gahwit Sitti/Reset save")]
        public static void ResetSave()
        {
            SaveStore.Delete(SaveSlot);
            Debug.Log("[SaveTools] Save deleted. The next Play starts a new game.");
        }
    }
}
