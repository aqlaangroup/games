using System;
using Gahwit.Core;
using Gahwit.Memories;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The 1974 banner with film grain over the screen, and the "pour" memory game: a cup that fills while the button
    // is held, with the line to stop at.
    public sealed class MemoryView : MonoBehaviour
    {
        const float CupHeight = 330f, LiquidInset = 14f, ResultPauseSeconds = 1.1f, GrainFadeSeconds = 1.2f, GrainAlpha = 0.09f;
        const int GrainSize = 128;

        GameObject banner, sheet;
        RawImage grain;
        float grainSince, nextGrainShift;
        Image liquid, bandLow, bandHigh, band;
        RTLTextMeshPro hint;
        PourGame pour;
        Action<int> onDone;
        float finishedAt = -1;

        public RhythmView Rhythm { get; private set; }
        public FindView Find { get; private set; }
        public RestoreView Restore { get; private set; }
        public TraceView Trace { get; private set; }
        public TuneView Tune { get; private set; }
        public ReassembleView Reassemble { get; private set; }

        T Game<T>(string name) where T : MemoryGameView => UiKit.Rect(transform, name).Stretch().gameObject.AddComponent<T>();

        public void Init(MemoriesConfig memories)
        {
            grain = UiKit.Rect(transform, "Grain").Stretch().gameObject.AddComponent<RawImage>();
            grain.texture = GrainTexture();
            grain.uvRect = new Rect(0, 0, 4, 7);
            grain.raycastTarget = false;
            grain.gameObject.SetActive(false);

            var bannerImage = UiKit.Panel(transform, "Banner", new Color(0.36f, 0.25f, 0.17f, 0.85f), 40);
            bannerImage.raycastTarget = false;
            bannerImage.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(560, 72));
            UiKit.Text(bannerImage.transform, "Text", Loc.T("memory.banner"), 30, Palette.Hex("#F6E3C4"), TextAlignmentOptions.Center).rectTransform.Stretch();
            banner = bannerImage.gameObject;
            banner.SetActive(false);

            var card = UiKit.Panel(transform, "PourSheet", Palette.Hex("#FBF3E6"), 44);
            card.rectTransform.Bottom(40, 760, 28, 28);
            sheet = card.gameObject;
            UiKit.Text(card.transform, "Title", Loc.T("pour.title"), 38, Palette.Ink, TextAlignmentOptions.Center).rectTransform.Top(30, 54);

            var cup = UiKit.Panel(card.transform, "Cup", Color.white, 36);
            cup.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(300, CupHeight));
            var outline = cup.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.Hex("#D9818F");
            outline.effectDistance = new Vector2(4, -4);

            band = UiKit.Panel(cup.transform, "TargetBand", new Color(0.98f, 0.8f, 0.84f, 0.35f), 0);
            bandLow = UiKit.Panel(cup.transform, "LineLow", Palette.Hex("#D9818F"), 0);
            bandHigh = UiKit.Panel(cup.transform, "LineHigh", Palette.Hex("#D9818F"), 0);
            liquid = UiKit.Panel(cup.transform, "Coffee", Palette.Hex("#6B3F22"), 24);
            liquid.rectTransform.anchorMin = new Vector2(0, 0);
            liquid.rectTransform.anchorMax = new Vector2(1, 0);
            liquid.rectTransform.pivot = new Vector2(0.5f, 0);
            liquid.rectTransform.offsetMin = new Vector2(LiquidInset, LiquidInset);
            liquid.rectTransform.offsetMax = new Vector2(-LiquidInset, LiquidInset);
            // The line to stop at stays visible over the coffee.
            band.transform.SetAsLastSibling();
            bandLow.transform.SetAsLastSibling();
            bandHigh.transform.SetAsLastSibling();

            var hold = UiKit.Button(card.transform, "Hold", Loc.T("pour.hold"), Palette.Rose, Color.white, 32, null, 40);
            ((RectTransform)hold.transform).Bottom(110, UiKit.MinTouch, 80, 80);
            var holdInput = hold.gameObject.AddComponent<HoldButton>();
            holdInput.Pressed += () =>
            {
                pour?.Press();
                UiSound.Loop("pour", pour != null);
            };
            holdInput.Released += Release;

            hint = UiKit.Text(card.transform, "Hint", "", 28, Palette.InkSoft, TextAlignmentOptions.Center);
            hint.rectTransform.Bottom(40, 50, 40, 40);
            sheet.SetActive(false);

            // The other memory games (missing/12), each on its own sheet.
            (Rhythm = Game<RhythmView>("Rhythm")).Init();
            (Find = Game<FindView>("Find")).Init();
            (Restore = Game<RestoreView>("Restore")).Init();
            (Trace = Game<TraceView>("Trace")).Init();
            (Tune = Game<TuneView>("Tune")).Init(memories.Tune);
            (Reassemble = Game<ReassembleView>("Reassemble")).Init();
        }

        public bool IsPlaying => pour != null && !pour.Finished || Rhythm.IsPlaying || Find.IsPlaying || Restore.IsPlaying
                                 || Trace.IsPlaying || Tune.IsPlaying || Reassemble.IsPlaying;

        // In a memory (COMPLETE_SPEC.md section 8): the banner, and grain fading in over the faded café.
        public void ShowBanner(bool show)
        {
            banner.SetActive(show);
            grain.gameObject.SetActive(show);
            grainSince = Time.unscaledTime;
            grain.color = new Color(1, 1, 1, 0);
        }

        static Texture2D GrainTexture()
        {
            var texture = new Texture2D(GrainSize, GrainSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "Grain" };
            var pixels = new Color32[GrainSize * GrainSize];
            var random = new System.Random(1974);
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)random.Next(40, 230);
                pixels[i] = new Color32(v, (byte)(v * 0.9f), (byte)(v * 0.75f), (byte)random.Next(0, 255));
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        public void PlayPour(PourGame game, Action<int> done)
        {
            pour = game;
            onDone = done;
            finishedAt = -1;
            hint.text = "";
            PlaceLine(bandLow, game.TargetLow);
            PlaceLine(bandHigh, game.TargetHigh);
            float inner = CupHeight - 2 * LiquidInset;
            band.rectTransform.anchorMin = band.rectTransform.anchorMax = new Vector2(0.5f, 0);
            band.rectTransform.pivot = new Vector2(0.5f, 0);
            band.rectTransform.sizeDelta = new Vector2(300 - 2 * LiquidInset, (game.TargetHigh - game.TargetLow) * inner);
            band.rectTransform.anchoredPosition = new Vector2(0, LiquidInset + game.TargetLow * inner);
            sheet.SetActive(true);
            DrawLevel();
        }

        static void PlaceLine(Image line, float level)
        {
            float inner = CupHeight - 2 * LiquidInset;
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0.5f, 0);
            line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            line.rectTransform.sizeDelta = new Vector2(300 - 2 * LiquidInset, 4);
            line.rectTransform.anchoredPosition = new Vector2(0, LiquidInset + level * inner);
        }

        void DrawLevel()
        {
            float inner = CupHeight - 2 * LiquidInset;
            var size = liquid.rectTransform.sizeDelta;
            liquid.rectTransform.sizeDelta = new Vector2(size.x, Mathf.Max(0, pour.Level * inner));
            liquid.enabled = pour.Level > 0.005f;
        }

        void Release()
        {
            UiSound.Loop("pour", false);
            if (pour == null || pour.Finished) return;
            ShowResult(pour.Release());
        }

        void ShowResult(PourResult result)
        {
            UiSound.Loop("pour", false); // a spill ends the pour even while the finger is still down
            if (result == PourResult.KeepPouring) hint.text = Loc.T("pour.more");
            else if (result == PourResult.Perfect) hint.text = Loc.T("pour.perfect");
            else if (result == PourResult.Overfilled || result == PourResult.Spilled) hint.text = Loc.T("pour.over");
            if (pour.Finished) finishedAt = Time.unscaledTime;
        }

        void Update()
        {
            if (grain.gameObject.activeSelf)
            {
                float fade = Mathf.Clamp01((Time.unscaledTime - grainSince) / GrainFadeSeconds);
                grain.color = new Color(1, 1, 1, GrainAlpha * fade);
                // The grain moves like film; with reduced motion it stays still.
                if (!Tweens.Reduced && Time.unscaledTime >= nextGrainShift)
                {
                    nextGrainShift = Time.unscaledTime + 0.06f;
                    grain.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, 4, 7);
                }
            }
            if (pour == null || !sheet.activeSelf) return;
            if (!pour.Finished)
            {
                bool wasFinished = pour.Finished;
                pour.Tick(Time.unscaledDeltaTime);
                DrawLevel();
                if (!wasFinished && pour.Finished) ShowResult(pour.Result); // it spilled while held
                return;
            }
            if (finishedAt >= 0 && Time.unscaledTime - finishedAt >= ResultPauseSeconds)
            {
                sheet.SetActive(false);
                var done = onDone;
                int stars = pour.Stars;
                pour = null;
                onDone = null;
                done?.Invoke(stars);
            }
        }
    }
}
