using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Lists every visible button smaller than UiKit.MinTouch (44 pt, CLAUDE.md) in canvas units. Used by the play-mode
    // test and by the Editor menu Gahwit Sitti > Check touch targets.
    public static class TouchTargets
    {
        public static List<string> TooSmall()
        {
            var small = new List<string>();
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!button.isActiveAndEnabled) continue;
                var rect = (RectTransform)button.transform;
                var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
                float scale = rect.lossyScale.y / canvas.transform.lossyScale.y;
                float height = rect.rect.height * scale, width = rect.rect.width * scale;
                // A little slack for rounding in the layout.
                if (height < UiKit.MinTouch - 0.5f || width < UiKit.MinTouch - 0.5f)
                    small.Add($"{Path(rect)}: {width:0} × {height:0}");
            }
            return small;
        }

        static string Path(Transform t)
        {
            string path = t.name;
            for (var parent = t.parent; parent != null && parent.parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }
    }
}
