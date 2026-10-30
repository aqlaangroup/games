using UnityEngine;

namespace Gahwit.UI
{
    // Keeps the interface inside the phone's safe area (notch, home bar).
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screen;

        void Update()
        {
            var area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (area == applied && size == screen) return;
            applied = area;
            screen = size;

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            rect.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
