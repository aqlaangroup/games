using Gahwit.Core;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Builders for the code-made interface. Text goes through RTLTMPro. Screens are laid out right to left; in English
    // (LeftToRight) every side, anchor and row below is mirrored, so the same code lays out left to right.
    public static class UiKit
    {
        const int UiLayer = 5;

        // 44 pt (CLAUDE.md) on the narrowest phone we support, 360 points wide: the 1080-unit canvas makes it 132 units.
        // Every button is at least this tall and wide (Editor: Gahwit Sitti > Check touch targets). Boot sets it from
        // balance.json through Core.UiRules.
        public static float MinTouch { get; set; } = 132f;

        // Set by Boot before the interface is built (COMPLETE_SPEC.md sections 4 and 5).
        public static float TextScale { get; set; } = 1f;
        public static float MinText { get; set; }
        public static bool LeftToRight { get; set; }

        public static float FontSize(float size) => Mathf.Max(size * TextScale, MinText);

        // Swaps left and right in a text alignment for left-to-right screens.
        public static TextAlignmentOptions Side(TextAlignmentOptions alignment)
        {
            if (!LeftToRight) return alignment;
            int value = (int)alignment;
            int left = value & (int)HorizontalAlignmentOptions.Left, right = value & (int)HorizontalAlignmentOptions.Right;
            if (left == 0 && right == 0) return alignment;
            value &= ~((int)HorizontalAlignmentOptions.Left | (int)HorizontalAlignmentOptions.Right);
            return (TextAlignmentOptions)(value | (left != 0 ? (int)HorizontalAlignmentOptions.Right : (int)HorizontalAlignmentOptions.Left));
        }

        static Vector2 X(Vector2 v) => LeftToRight ? new Vector2(1 - v.x, v.y) : v;

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image Panel(Transform parent, string name, Color color, float radius = 28)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.color = color;
            if (radius > 0)
            {
                image.sprite = UiSprites.Rounded;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = UiSprites.RoundedRadius / radius;
            }
            return image;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        public static RTLTextMeshPro Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Right)
        {
            var label = Rect(parent, name).gameObject.AddComponent<RTLTextMeshPro>();
            label.font = GameFonts.Arabic;
            label.Farsi = false;
            label.PreserveNumbers = true;
            label.fontSize = FontSize(size);
            label.color = color;
            label.alignment = Side(alignment);
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = text;
            return label;
        }

        public static Button Button(Transform parent, string name, string label, Color background, Color foreground,
            float fontSize, UnityAction onClick, float radius = 32)
        {
            var image = Panel(parent, name, background, radius);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => UiSound.Play("ui_tap"));
            if (onClick != null) button.onClick.AddListener(onClick);
            Text(image.transform, "Label", label, fontSize, foreground, TextAlignmentOptions.Center).rectTransform.Stretch(10, 10, 4, 4);
            return button;
        }

        public static RTLTextMeshPro Label(this Button button) => button.GetComponentInChildren<RTLTextMeshPro>(true);

        public static HorizontalLayoutGroup Row(GameObject go, float spacing)
        {
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.reverseArrangement = !LeftToRight; // the first child at the start side
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return row;
        }

        // Hide first so this frame's layout already ignores the old children.
        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        public static RectTransform Stretch(this RectTransform rect, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            if (LeftToRight) (left, right) = (right, left);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Top(this RectTransform rect, float top, float height, float left = 0, float right = 0)
        {
            if (LeftToRight) (left, right) = (right, left);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Bottom(this RectTransform rect, float bottom, float height, float left = 0, float right = 0)
        {
            if (LeftToRight) (left, right) = (right, left);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
            return rect;
        }

        // Pins a fixed-size rect to one anchor point; the pivot matches the anchor.
        public static RectTransform Pin(this RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            anchor = X(anchor);
            if (LeftToRight) position.x = -position.x;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // A bar that fills from the start side: the right in Arabic, the left in English.
        public static void SetFill(this Image fill, float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            fill.rectTransform.anchorMin = LeftToRight ? Vector2.zero : new Vector2(1 - fraction, 0);
            fill.rectTransform.anchorMax = LeftToRight ? new Vector2(fraction, 1) : Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
