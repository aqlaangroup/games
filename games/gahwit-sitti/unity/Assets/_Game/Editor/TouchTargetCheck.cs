using UnityEditor;
using UnityEngine;

namespace Gahwit.EditorTools
{
    // Lists every visible button under 44 pt (CLAUDE.md) on the screen now showing. Run it in Play mode.
    public static class TouchTargetCheck
    {
        [MenuItem("Gahwit Sitti/Check touch targets (Play mode)")]
        public static string Run()
        {
            var small = Gahwit.UI.TouchTargets.TooSmall();
            string result = small.Count == 0 ? "Every visible button is 44 pt or more." : "Under 44 pt:\n" + string.Join("\n", small);
            Debug.Log("[TouchTargets] " + result);
            return result;
        }
    }
}
