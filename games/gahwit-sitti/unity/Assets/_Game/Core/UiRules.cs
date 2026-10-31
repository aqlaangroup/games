using System;

namespace Gahwit.Core
{
    // Sizes and graphics choices for the interface (COMPLETE_SPEC.md sections 4 and 5), from balance.json "ui".
    // The canvas is 1080 units wide; sizes are checked on the narrowest phone we support.
    public static class UiRules
    {
        public const float CanvasWidth = 1080f;

        public static float UnitsPerPt(UiConfig ui) => CanvasWidth / ui.SmallestPhoneWidthPt;

        public static float MinTouchUnits(UiConfig ui) => ui.MinTouchTargetPt * UnitsPerPt(ui);

        // Large text multiplies every size and lifts anything still under the minimum to it.
        public static float TextScale(UiConfig ui, bool large) => large ? ui.LargeTextScale : 1f;

        public static float MinTextUnits(UiConfig ui, bool large) => large ? ui.MinTextPt * UnitsPerPt(ui) : 0f;

        // "auto" picks low on small-memory phones.
        public static bool HighGraphics(string choice, int memoryMb, GraphicsConfig graphics)
        {
            switch (choice)
            {
                case "high": return true;
                case "low": return false;
                default: return memoryMb > graphics.AutoLowAtOrBelowMb;
            }
        }

        public static int FrameRate(bool high, GraphicsConfig graphics) => high ? graphics.HighFps : graphics.LowFps;

        public static readonly string[] Qualities = { "auto", "low", "high" };

        public static string ValidQuality(string choice) => Array.IndexOf(Qualities, choice) >= 0 ? choice : "auto";
    }
}
