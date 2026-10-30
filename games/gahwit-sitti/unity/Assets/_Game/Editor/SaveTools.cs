using Gahwit.Core;
using Gahwit.Save;
using UnityEditor;
using UnityEngine;

namespace Gahwit.EditorTools
{
    public static class SaveTools
    {
        [MenuItem("Gahwit Sitti/Reset save")]
        public static void ResetSave()
        {
            SaveStore.Delete(SaveService.Slot);
            Debug.Log("[SaveTools] Save deleted. The next Play starts a new game.");
        }
    }
}
