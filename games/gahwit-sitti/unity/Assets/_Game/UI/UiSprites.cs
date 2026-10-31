using System;
using UnityEngine;

namespace Gahwit.UI
{
    // Small sprites drawn in code (rounded panel, circle, energy bolt, gem, gear) until the UI art arrives.
    public static class UiSprites
    {
        public const float RoundedRadius = 48f;
        const int Size = 128;

        static Sprite rounded, circle, bolt, gem, gear, tick;

        public static Sprite Rounded => rounded != null ? rounded : rounded = Make("Rounded", RoundedAlpha, new Vector4(RoundedRadius, RoundedRadius, RoundedRadius, RoundedRadius));
        public static Sprite Circle => circle != null ? circle : circle = Make("Circle", CircleAlpha, Vector4.zero);
        public static Sprite Bolt => bolt != null ? bolt : bolt = Polygon("Bolt", new[] { V(0.58f, 0.02f), V(0.16f, 0.56f), V(0.46f, 0.56f), V(0.36f, 0.98f), V(0.86f, 0.40f), V(0.55f, 0.40f), V(0.68f, 0.02f) });
        public static Sprite Gem => gem != null ? gem : gem = Polygon("Gem", new[] { V(0.5f, 0.04f), V(0.94f, 0.38f), V(0.5f, 0.96f), V(0.06f, 0.38f) });
        public static Sprite Tick => tick != null ? tick : tick = Polygon("Tick", new[] { V(0.08f, 0.52f), V(0.22f, 0.38f), V(0.40f, 0.56f), V(0.78f, 0.14f), V(0.92f, 0.28f), V(0.40f, 0.84f) });
        public static Sprite Gear => gear != null ? gear : gear = Make("Gear", GearAlpha, Vector4.zero);

        static Vector2 V(float x, float yFromTop) => new Vector2(x, yFromTop);

        // A ring with eight teeth.
        static float GearAlpha(int px, int py)
        {
            var d = new Vector2(px + 0.5f - Size / 2f, py + 0.5f - Size / 2f) / Size;
            float r = d.magnitude, angle = Mathf.Atan2(d.y, d.x) * 8f / (2f * Mathf.PI);
            bool tooth = Mathf.Abs(angle - Mathf.Round(angle)) < 0.22f;
            float outer = tooth ? 0.47f : 0.37f;
            return Mathf.Clamp01((outer - r) * Size + 0.5f) * Mathf.Clamp01((r - 0.16f) * Size + 0.5f);
        }

        static float RoundedAlpha(int px, int py)
        {
            float x = px + 0.5f, y = py + 0.5f, r = RoundedRadius;
            float dx = Mathf.Max(r - x, x - (Size - r), 0), dy = Mathf.Max(r - y, y - (Size - r), 0);
            return Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
        }

        static float CircleAlpha(int px, int py) =>
            Mathf.Clamp01(Size / 2f - 1 - Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), new Vector2(Size / 2f, Size / 2f)) + 0.5f);

        // 4×4 supersampled point-in-polygon, with points given as (x, y from the top) in 0..1.
        static Sprite Polygon(string name, Vector2[] points) => Make(name, (px, py) =>
        {
            int inside = 0;
            for (int sy = 0; sy < 4; sy++)
            for (int sx = 0; sx < 4; sx++)
            {
                var p = new Vector2((px + (sx + 0.5f) / 4f) / Size, 1f - (py + (sy + 0.5f) / 4f) / Size);
                if (Inside(points, p)) inside++;
            }
            return inside / 16f;
        }, Vector4.zero);

        static bool Inside(Vector2[] polygon, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                    p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                    inside = !inside;
            }
            return inside;
        }

        static Sprite Make(string name, Func<int, int, float> alpha, Vector4 border)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha(x, y) * 255));
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }
    }
}
