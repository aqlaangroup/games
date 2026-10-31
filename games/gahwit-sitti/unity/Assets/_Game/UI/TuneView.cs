using System;
using Gahwit.Core;
using Gahwit.Memories;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Tune: drag along the radio's dial; the signal bars rise near the station, and holding still on it tunes it in.
    // The bars show strength by height as well as colour.
    public sealed class TuneView : MemoryGameView, IPointerDownHandler, IDragHandler
    {
        const int Bars = 5;

        TuneGame game;
        TuneConfig config;
        RectTransform dial, needle;
        RTLTextMeshPro frequency;
        readonly Image[] bars = new Image[Bars];
        int tenthsShown = -1;

        public void Init(TuneConfig tune)
        {
            config = tune;
            Build("tune.title", 900);
            var radio = UiKit.Panel(Area, "Radio", Palette.Hex("#6B4A33"), 36);
            radio.rectTransform.Stretch(0, 0, 0, 0);
            frequency = UiKit.Text(radio.transform, "Frequency", "", 56, Palette.Hex("#F6E3C4"), TextAlignmentOptions.Center);
            frequency.rectTransform.Top(30, 80);
            for (int i = 0; i < Bars; i++)
            {
                bars[i] = UiKit.Panel(radio.transform, "Bar", Palette.Hex("#F6E3C4"), 6);
                bars[i].rectTransform.Pin(new Vector2(0.5f, 1), new Vector2((i - 2) * 40, -200), new Vector2(26, 20 + i * 14));
                bars[i].rectTransform.pivot = new Vector2(0.5f, 0);
            }
            var face = UiKit.Panel(radio.transform, "Dial", Palette.Hex("#F6E3C4"), 20);
            face.rectTransform.Bottom(40, UiKit.MinTouch + 40, 40, 40);
            dial = face.rectTransform;
            needle = UiKit.Panel(face.transform, "Needle", Palette.Rose, 4).rectTransform;
            needle.anchorMin = new Vector2(0, 0);
            needle.anchorMax = new Vector2(0, 1);
            needle.pivot = new Vector2(0.5f, 0.5f);
            needle.sizeDelta = new Vector2(8, -16);
            face.gameObject.AddComponent<DialInput>().View = this;
        }

        public void Play(TuneGame tune, Action<int> done)
        {
            game = tune;
            tenthsShown = -1;
            Hint.text = Loc.T("tune.hint");
            Begin(tune, done);
        }

        // Dragging anywhere on the card turns the dial too, so the target is never small.
        public void OnPointerDown(PointerEventData eventData) => Turn(eventData);

        public void OnDrag(PointerEventData eventData) => Turn(eventData);

        internal void Turn(PointerEventData eventData)
        {
            if (game == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dial, eventData.position, eventData.pressEventCamera, out var local);
            float t = Mathf.Clamp01((local.x - dial.rect.xMin) / dial.rect.width);
            if (UiKit.LeftToRight == false) t = 1 - t; // the dial reads from the start side
            game.Turn(Mathf.Lerp(config.DialRange[0], config.DialRange[1], t));
        }

        protected override void Draw()
        {
            ShowClock(game.SecondsLeft);
            int tenths = Mathf.RoundToInt(game.Dial * 10);
            if (tenths != tenthsShown)
            {
                tenthsShown = tenths;
                frequency.text = $"{tenths / 10}.{tenths % 10} MHz";
            }
            float t = Mathf.InverseLerp(config.DialRange[0], config.DialRange[1], game.Dial);
            if (!UiKit.LeftToRight) t = 1 - t;
            needle.anchorMin = new Vector2(t, 0);
            needle.anchorMax = new Vector2(t, 1);
            needle.anchoredPosition = Vector2.zero;
            int lit = Mathf.RoundToInt(game.Clarity * Bars);
            for (int i = 0; i < Bars; i++)
                bars[i].color = i < lit ? Palette.Hex("#F6E3C4") : new Color(0.96f, 0.89f, 0.77f, 0.2f);
            if (game.Tuned) Hint.text = Loc.T("tune.found");
        }

        sealed class DialInput : MonoBehaviour, IPointerDownHandler, IDragHandler
        {
            public TuneView View;
            public void OnPointerDown(PointerEventData eventData) => View.Turn(eventData);
            public void OnDrag(PointerEventData eventData) => View.Turn(eventData);
        }
    }
}
