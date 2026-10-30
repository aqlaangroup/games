using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Gahwit.Core
{
    // The Arabic UI font, built at runtime as a dynamic TextMeshPro font from Resources/Fonts.
    public static class GameFonts
    {
        static TMP_FontAsset arabic;

        public static TMP_FontAsset Arabic => arabic != null ? arabic : arabic = CreateArabic();

        static TMP_FontAsset CreateArabic()
        {
            var font = Resources.Load<Font>("Fonts/NotoNaskhArabic");
            if (font == null)
            {
                Debug.LogError("[Fonts] Missing Resources/Fonts/NotoNaskhArabic");
                return TMP_Settings.defaultFontAsset;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font);
            asset.name = "NotoNaskhArabic (dynamic)";
            if (TMP_Settings.defaultFontAsset != null)
                asset.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            return asset;
        }
    }
}
