using UnityEngine;

namespace Gahwit.UI
{
    // Blush, lilac, sage and cream (CONCEPT.md, Look).
    public static class Palette
    {
        public static readonly Color Blush = Hex("#F3E4E4");
        public static readonly Color Cream = Hex("#FFF8F3");
        public static readonly Color Card = new Color(1f, 0.984f, 0.973f, 0.94f);
        public static readonly Color Slot = new Color(1f, 1f, 1f, 0.55f);
        public static readonly Color Track = Hex("#F0E2E4");
        public static readonly Color Ready = Hex("#EAF4EC");
        public static readonly Color Muted = Hex("#EDE3E0");
        public static readonly Color Ink = Hex("#3A2731");
        public static readonly Color InkSoft = Hex("#7A6670");
        public static readonly Color Rose = Hex("#B84A60");
        public static readonly Color RoseSoft = Hex("#E9A0AE");
        public static readonly Color Lilac = Hex("#B9A8DE");
        public static readonly Color SageDark = Hex("#4F7D5C");
        public static readonly Color Gold = Hex("#D9B06C");
        public static readonly Color Teal = Hex("#1D6C67");
        public static readonly Color Shade = new Color(0.16f, 0.1f, 0.13f, 0.45f);
        public static readonly Color ToastBg = new Color(0.23f, 0.15f, 0.19f, 0.92f);

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;

        public static Color TextOn(Color background) =>
            0.299f * background.r + 0.587f * background.g + 0.114f * background.b > 0.62f ? Ink : Color.white;
    }
}
