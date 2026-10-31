using System;
using Gahwit.Core;
using Gahwit.Memories;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Rhythm: a ring closes in on the dough at each beat; tap "shape" as it lands.
    public sealed class RhythmView : MemoryGameView
    {
        RhythmGame game;
        RectTransform ring;
        Image dough;
        int lastBeat = -1, judged;

        public void Init()
        {
            Build("rhythm.title", 860);
            dough = UiKit.Icon(Area, "Dough", UiSprites.Circle, Palette.Hex("#E8C9A0"), 220);
            dough.rectTransform.Pin(new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(220, 220));
            ring = UiKit.Icon(Area, "Ring", UiSprites.Circle, new Color(0.72f, 0.29f, 0.38f, 0.35f), 220).rectTransform;
            ring.Pin(new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(220, 220));
            var tap = UiKit.Button(Area, "Shape", Loc.T("rhythm.tap"), Palette.Rose, Color.white, 34, Tap, 48);
            ((RectTransform)tap.transform).Bottom(0, UiKit.MinTouch + 40, 60, 60);
        }

        public void Play(RhythmGame rhythm, Action<int> done)
        {
            game = rhythm;
            lastBeat = -1;
            judged = 0;
            Begin(rhythm, done);
        }

        void Tap()
        {
            var beat = game.Tap();
            if (beat == Beat.Waiting) return;
            Hint.text = Loc.T(beat == Beat.Perfect ? "rhythm.perfect" : "rhythm.good");
            Tweens.Instance?.PopOf(dough.rectTransform, beat == Beat.Perfect ? 0.2f : 0.1f);
        }

        protected override void Draw()
        {
            int next = 0;
            while (next < game.BeatCount && game[next] != Beat.Waiting) next++;
            // A miss shows once.
            for (int i = judged; i < next; i++)
                if (game[i] == Beat.Missed) Hint.text = Loc.T("rhythm.miss");
            judged = next;
            if (next >= game.BeatCount) return;
            // The ring starts twice the dough's size one beat early and lands on it at the beat.
            float untilBeat = game.BeatTime(next) - game.Clock;
            float t = Mathf.Clamp01(untilBeat / game.BeatInterval);
            float size = 220f * (1f + t);
            ring.sizeDelta = new Vector2(size, size);
            // A soft tick on every beat keeps the tempo.
            int beatNow = Mathf.FloorToInt(game.Clock / game.BeatInterval);
            if (beatNow != lastBeat)
            {
                lastBeat = beatNow;
                UiSound.Play("ui_tap");
            }
        }
    }
}
