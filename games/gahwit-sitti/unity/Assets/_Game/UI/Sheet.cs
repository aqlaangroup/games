using Gahwit.Core;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Shared pieces for the full-screen panels: a dimmed backdrop with a card, a title row with a close button,
    // a scrolling column, and a text field that shows Arabic joined and right to left.
    public static class Sheet
    {
        // Tapping the backdrop does nothing, so a choice can't be lost by accident.
        public static RectTransform Card(Transform root, float width, float height, Color? background = null)
        {
            var shade = root.gameObject.AddComponent<Image>();
            shade.color = Palette.Shade;
            var card = UiKit.Panel(root, "Card", background ?? Palette.Cream, 44);
            card.rectTransform.Pin(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            return card.rectTransform;
        }

        public static RTLTextMeshPro Title(RectTransform card, string text, System.Action close)
        {
            var title = UiKit.Text(card, "Title", text, 44, Palette.Ink);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.Top(48, 64, close != null ? 200 : 48, 48);
            if (close != null)
            {
                var button = UiKit.Button(card, "Close", Loc.T("common.close"), Palette.Muted, Palette.Ink, 26, () => close(), 30);
                ((RectTransform)button.transform).Pin(new Vector2(0, 1), new Vector2(28, -20), new Vector2(160, 120));
            }
            return title;
        }

        // A vertical list that scrolls when it is taller than the card.
        public static RectTransform Column(RectTransform card, float top, float bottom, float spacing = 20)
        {
            var view = UiKit.Rect(card, "Scroll").Stretch(40, 40, top, bottom);
            view.gameObject.AddComponent<RectMask2D>();
            view.gameObject.AddComponent<Image>().color = Color.clear; // catches drags between rows
            var content = UiKit.Rect(view, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = spacing;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            return content;
        }

        public static T Sized<T>(T component, float height) where T : Component
        {
            var element = component.GetComponent<LayoutElement>();
            if (element == null) element = component.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = element.minHeight = height;
            return component;
        }

        // TextMesh Pro's input field can't join Arabic letters, so its own text is hidden and an RTLTMPro label shows
        // what was typed. On the phone the system keyboard's own text box shows it too.
        public static TMP_InputField Input(Transform parent, string name, string placeholder, float height, bool multiline)
        {
            var box = UiKit.Panel(parent, name, Color.white, 28);
            Sized(box, height);
            var area = UiKit.Rect(box.transform, "Area").Stretch(28, 28, 18, 18);
            area.gameObject.AddComponent<RectMask2D>();

            var raw = UiKit.Rect(area, "Raw").Stretch().gameObject.AddComponent<TextMeshProUGUI>();
            raw.font = GameFonts.Arabic;
            raw.fontSize = 30;
            raw.color = Color.clear;
            var shown = UiKit.Text(area, "Shown", "", 30, Palette.Ink, TextAlignmentOptions.TopRight);
            shown.rectTransform.Stretch();
            var hint = UiKit.Text(area, "Placeholder", placeholder, 30, Palette.InkSoft, TextAlignmentOptions.TopRight);
            hint.rectTransform.Stretch();

            var input = box.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = raw;
            input.fontAsset = GameFonts.Arabic;
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            input.caretColor = Color.clear;
            input.selectionColor = new Color(0.72f, 0.29f, 0.38f, 0.2f);
            input.onValueChanged.AddListener(value =>
            {
                shown.text = value;
                hint.enabled = value.Length == 0;
            });
            return input;
        }

        // Inside a Column the layout gives the text its wrapped height.
        public static RTLTextMeshPro Paragraph(Transform column, string text, float size, Color color) =>
            UiKit.Text(column, "Text", text, size, color);
    }
}
