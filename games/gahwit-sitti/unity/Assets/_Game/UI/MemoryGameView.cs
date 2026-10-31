using System;
using Gahwit.Core;
using Gahwit.Memories;
using RTLTMPro;
using TMPro;
using UnityEngine;

namespace Gahwit.UI
{
    // What every memory game's sheet shares: a card at the bottom of the screen with a title, a clock and a hint
    // line, the game's tick, and a short pause on the result before handing back the stars.
    public abstract class MemoryGameView : MonoBehaviour
    {
        const float ResultPauseSeconds = 1.2f;

        protected RectTransform Card, Area;
        protected RTLTextMeshPro Title, Clock, Hint;
        IMemoryGame game;
        Action<int> done;
        float finishedAt = -1;
        int clockShown = -1;

        public bool IsPlaying => game != null;

        protected void Build(string titleKey, float height)
        {
            var card = UiKit.Panel(transform, "Sheet", Palette.Hex("#FBF3E6"), 44);
            card.rectTransform.Bottom(40, height, 28, 28);
            card.gameObject.AddComponent<SpringIn>();
            Card = card.rectTransform;
            Title = UiKit.Text(Card, "Title", Loc.T(titleKey), 38, Palette.Ink, TextAlignmentOptions.Center);
            Title.rectTransform.Top(30, 54, 160, 160);
            Clock = UiKit.Text(Card, "Clock", "", 30, Palette.InkSoft, TextAlignmentOptions.Left);
            Clock.rectTransform.Top(34, 50, 40, 40);
            Hint = UiKit.Text(Card, "Hint", "", 28, Palette.InkSoft, TextAlignmentOptions.Center);
            Hint.rectTransform.Bottom(30, 50, 40, 40);
            Area = UiKit.Rect(Card, "Area").Stretch(40, 40, 100, 90);
            gameObject.SetActive(false);
        }

        protected void Begin(IMemoryGame memoryGame, Action<int> onDone)
        {
            game = memoryGame;
            done = onDone;
            finishedAt = -1;
            clockShown = -1;
            Clock.text = "";
            Hint.text = "";
            gameObject.SetActive(true);
        }

        // The seconds left, written only when the shown second changes.
        protected void ShowClock(float secondsLeft)
        {
            int whole = Mathf.CeilToInt(secondsLeft);
            if (whole == clockShown) return;
            clockShown = whole;
            Clock.text = $"{whole / 60}:{whole % 60:00}";
        }

        protected abstract void Draw();

        protected virtual void Result(int stars) => Hint.text = Loc.T("memory.stars", stars, 3);

        void Update()
        {
            if (game == null) return;
            if (!game.Finished)
            {
                game.Tick(Time.unscaledDeltaTime);
                Draw();
                if (!game.Finished) return;
            }
            if (finishedAt < 0)
            {
                finishedAt = Time.unscaledTime;
                Draw();
                Result(game.Stars);
                UiSound.Play(game.Stars >= 3 ? "ui_level_up" : "ui_coin");
                return;
            }
            if (Time.unscaledTime - finishedAt < ResultPauseSeconds) return;
            gameObject.SetActive(false);
            int result = game.Stars;
            var then = done;
            game = null;
            done = null;
            then?.Invoke(result);
        }
    }
}
