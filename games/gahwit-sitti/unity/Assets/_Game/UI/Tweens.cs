using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The game's small motions (COMPLETE_SPEC.md section 8), from fixed pools so nothing is allocated per frame:
    // pops and squashes, slides, and round "flyers" for sparkles, flashes, dust, coins and the memory wipe.
    // Reduced motion turns all of them off.
    public sealed class Tweens : MonoBehaviour
    {
        const int Pops = 32, Slides = 8, Flyers = 64;
        const float PopSeconds = 0.32f;

        struct Pop
        {
            public RectTransform Target;
            public float Started, Amount;
        }

        struct Slide
        {
            public RectTransform Target;
            public Vector2 From, To;
            public float Started, Seconds;
        }

        struct Flyer
        {
            public Vector2 From, To;
            public float Started, Seconds, Arc, SizeFrom, SizeTo, AlphaFrom, AlphaTo;
            public bool EaseIn;
        }

        readonly Pop[] pops = new Pop[Pops];
        readonly Slide[] slides = new Slide[Slides];
        readonly Flyer[] flyers = new Flyer[Flyers];
        readonly Image[] dots = new Image[Flyers];
        RectTransform layer;
        int nextFlyer;

        public static Tweens Instance { get; private set; }
        public static bool Reduced { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            Reduced = false;
        }

        public void Init()
        {
            Instance = this;
            layer = (RectTransform)transform;
            for (int i = 0; i < Flyers; i++)
            {
                dots[i] = UiKit.Icon(transform, "Dot", UiSprites.Circle, Color.white, 20);
                dots[i].gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // A quick grow and settle with an overshoot; a negative amount squashes (wider and shorter) instead.
        public void PopOf(RectTransform target, float amount = 0.25f)
        {
            if (Reduced || target == null) return;
            int slot = 0;
            for (int i = 0; i < Pops; i++)
            {
                if (pops[i].Target == target) { slot = i; break; }
                if (pops[i].Target == null) slot = i;
            }
            pops[slot] = new Pop { Target = target, Started = Time.unscaledTime, Amount = amount };
        }

        // Moves a rect from one place to another, both given as rects on screen, then leaves it hidden.
        public void SlideOf(RectTransform moving, RectTransform from, RectTransform to, float seconds)
        {
            if (Reduced || moving == null || from == null || to == null) return;
            var parent = (RectTransform)moving.parent;
            int slot = 0;
            for (int i = 0; i < Slides; i++)
                if (slides[i].Target == null || slides[i].Target == moving) { slot = i; break; }
            slides[slot] = new Slide { Target = moving, From = Local(parent, from), To = Local(parent, to), Started = Time.unscaledTime, Seconds = seconds };
            moving.anchoredPosition = slides[slot].From;
            moving.gameObject.SetActive(true);
        }

        // A ring of dots bursting from a rect, in the item's colour.
        public void Sparkle(RectTransform at, Color color, int count = 8)
        {
            if (Reduced || at == null) return;
            var centre = Local(layer, at);
            for (int n = 0; n < count; n++)
            {
                float angle = n * Mathf.PI * 2f / count;
                Fly(color, new Flyer
                {
                    From = centre, To = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 110f,
                    Seconds = 0.5f, SizeFrom = 22, SizeTo = 10, AlphaFrom = 1, AlphaTo = 0
                });
            }
        }

        // A soft round flash over a rect.
        public void Flash(RectTransform at, Color color)
        {
            if (Reduced || at == null) return;
            var centre = Local(layer, at);
            float size = Mathf.Max(at.rect.width, at.rect.height);
            Fly(color, new Flyer { From = centre, To = centre, Seconds = 0.28f, SizeFrom = size * 0.6f, SizeTo = size * 1.5f, AlphaFrom = 0.7f, AlphaTo = 0 });
        }

        // One dot arcing from one rect to another, such as a new item leaving its generator.
        public void Arc(RectTransform from, RectTransform to, Color color)
        {
            if (Reduced || from == null || to == null) return;
            Fly(color, new Flyer
            {
                From = Local(layer, from), To = Local(layer, to), Seconds = 0.3f, Arc = 120f,
                SizeFrom = 40, SizeTo = 28, AlphaFrom = 1, AlphaTo = 0.6f
            });
        }

        // Coins arc from one rect to another, such as a served order to the coin counter.
        public void CoinsFly(RectTransform from, RectTransform to, int count = 6)
        {
            if (Reduced || from == null || to == null) return;
            var start = Local(layer, from);
            var end = Local(layer, to);
            for (int n = 0; n < count; n++)
                Fly(Palette.Gold, new Flyer
                {
                    From = start + new Vector2((n - count / 2f) * 22f, 0), To = end, Started = Time.unscaledTime + n * 0.05f,
                    Seconds = 0.7f, Arc = 160f, SizeFrom = 44, SizeTo = 30, AlphaFrom = 1, AlphaTo = 1, EaseIn = true
                });
        }

        // A few soft puffs rising from a point on screen, such as a decor piece set down.
        public void Dust(Vector2 screenPoint)
        {
            if (Reduced) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, null, out var centre);
            for (int n = 0; n < 6; n++)
            {
                float side = (n - 2.5f) * 34f;
                Fly(Palette.Hex("#E8DCCB"), new Flyer
                {
                    From = centre + new Vector2(side * 0.4f, 0), To = centre + new Vector2(side, 50 + n % 3 * 20),
                    Seconds = 0.55f, SizeFrom = 30, SizeTo = 70, AlphaFrom = 0.8f, AlphaTo = 0
                });
            }
        }

        // A colour that spreads over the whole screen from a point, then clears: the way into a memory.
        public void Wipe(Vector2 screenPoint, Color color, float seconds)
        {
            if (Reduced) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, null, out var centre);
            float cover = 2f * (Mathf.Abs(centre.x) + Mathf.Abs(centre.y) + layer.rect.width + layer.rect.height);
            Fly(color, new Flyer { From = centre, To = centre, Seconds = seconds, SizeFrom = 40, SizeTo = cover, AlphaFrom = 0.9f, AlphaTo = 0 });
        }

        void Fly(Color color, Flyer flyer)
        {
            if (flyer.Started == 0) flyer.Started = Time.unscaledTime;
            int i = nextFlyer;
            nextFlyer = (nextFlyer + 1) % Flyers;
            flyers[i] = flyer;
            dots[i].color = new Color(color.r, color.g, color.b, 0);
            dots[i].rectTransform.SetAsLastSibling();
            dots[i].gameObject.SetActive(true);
        }

        static Vector2 Local(RectTransform space, RectTransform target)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screen, null, out var local);
            return local;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < Pops; i++)
            {
                if (pops[i].Target == null) continue;
                float t = (now - pops[i].Started) / PopSeconds;
                if (t >= 1f)
                {
                    pops[i].Target.localScale = Vector3.one;
                    pops[i].Target = null;
                    continue;
                }
                // Up, a little past, and back, like a spring.
                float s = pops[i].Amount * (Mathf.Sin(t * Mathf.PI) + 0.25f * Mathf.Sin(t * Mathf.PI * 2f)) * (1f - t);
                pops[i].Target.localScale = pops[i].Amount >= 0 ? new Vector3(1 + s, 1 + s, 1) : new Vector3(1 - s, 1 + s, 1);
            }
            for (int i = 0; i < Slides; i++)
            {
                if (slides[i].Target == null) continue;
                float t = (now - slides[i].Started) / slides[i].Seconds;
                if (t >= 1f)
                {
                    slides[i].Target.gameObject.SetActive(false);
                    slides[i].Target = null;
                    continue;
                }
                slides[i].Target.anchoredPosition = Vector2.Lerp(slides[i].From, slides[i].To, t * t);
            }
            for (int i = 0; i < Flyers; i++)
            {
                var dot = dots[i];
                if (!dot.gameObject.activeSelf) continue;
                ref var f = ref flyers[i];
                float t = (now - f.Started) / f.Seconds;
                if (t < 0) continue;
                if (t >= 1f) { dot.gameObject.SetActive(false); continue; }
                float move = f.EaseIn ? t * t : 1 - (1 - t) * (1 - t);
                dot.rectTransform.anchoredPosition = Vector2.Lerp(f.From, f.To, move) + new Vector2(0, Mathf.Sin(t * Mathf.PI) * f.Arc);
                float size = Mathf.Lerp(f.SizeFrom, f.SizeTo, move);
                dot.rectTransform.sizeDelta = new Vector2(size, size);
                var c = dot.color;
                dot.color = new Color(c.r, c.g, c.b, Mathf.Lerp(f.AlphaFrom, f.AlphaTo, t));
            }
        }
    }

    // A panel's card springs up from below each time it opens (COMPLETE_SPEC.md section 8: about 0.35 s, no hard cuts).
    public sealed class SpringIn : MonoBehaviour
    {
        public static float Seconds = 0.35f; // Boot sets it from balance.json ui.panelSlideSeconds

        RectTransform rect;
        Vector2 home;
        float started = -10;

        void OnEnable()
        {
            rect = (RectTransform)transform;
            home = rect.anchoredPosition;
            started = Tweens.Reduced ? -10 : Time.unscaledTime;
        }

        void OnDisable()
        {
            if (rect != null) rect.anchoredPosition = home;
        }

        void Update()
        {
            if (started < 0) return;
            float t = (Time.unscaledTime - started) / Seconds;
            if (t >= 1f)
            {
                rect.anchoredPosition = home;
                started = -10;
                return;
            }
            // Ease out with a small overshoot (easeOutBack).
            const float c = 1.4f;
            float u = t - 1f;
            float k = 1f + (c + 1f) * u * u * u + c * u * u;
            rect.anchoredPosition = home + new Vector2(0, -260f * (1f - k));
        }
    }
}
