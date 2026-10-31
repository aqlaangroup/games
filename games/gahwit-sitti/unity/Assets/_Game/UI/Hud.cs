using System;
using Gahwit.Core;
using Gahwit.Merge;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Top bar, right to left: level and XP, energy with its refill timer, coins, gems.
    public sealed class Hud : MonoBehaviour
    {
        Game game;
        Func<long> now;
        RTLTextMeshPro level, energy, timer, coins, gems;
        Image xpFill;
        GameObject energyPill;
        float nextTimerUpdate;

        public RectTransform CoinsRect { get; private set; } // where served coins fly to
        public Action GemsTapped;

        public void Init(Game game, Func<long> now)
        {
            this.game = game;
            this.now = now;
            var row = UiKit.Row(gameObject, 14);
            row.padding = new RectOffset(20, 20, 18, 14);
            row.childForceExpandHeight = true;

            var levelPill = Pill("Level", 250);
            var badge = UiKit.Icon(levelPill, "Badge", UiSprites.Circle, Palette.Rose, 76);
            badge.rectTransform.Pin(new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(76, 76));
            level = UiKit.Text(badge.transform, "Number", "1", 34, Color.white, TextAlignmentOptions.Center);
            level.fontStyle = FontStyles.Bold;
            level.rectTransform.Stretch();
            var track = UiKit.Panel(levelPill, "XpTrack", Palette.Track, 10);
            track.rectTransform.Pin(new Vector2(1, 0.5f), new Vector2(-96, 0), new Vector2(134, 20));
            xpFill = UiKit.Panel(track.transform, "Fill", Palette.RoseSoft, 10);

            var energyRect = Pill("Energy", 270);
            energyPill = energyRect.gameObject;
            Badge(energyRect, UiSprites.Bolt, Palette.SageDark);
            energy = Value(energyRect, 34);
            energy.rectTransform.Stretch(14, 76, 2, 30);
            timer = UiKit.Text(energyRect, "Timer", "", 20, Palette.InkSoft, TextAlignmentOptions.Center);
            timer.rectTransform.Stretch(14, 76, 50, 2);

            var coinPill = Pill("Coins", 200);
            CoinsRect = coinPill;
            Badge(coinPill, UiSprites.Circle, Palette.Gold);
            coins = Value(coinPill, 34);

            var gemPill = Pill("Gems", 180);
            Badge(gemPill, UiSprites.Gem, Palette.Lilac);
            gems = Value(gemPill, 34);
            // Tapping the gems opens the shop. The pill is shorter than a touch target, so an invisible area of the
            // full size sits over it.
            var tap = UiKit.Rect(gemPill, "Tap").Pin(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, UiKit.MinTouch));
            tap.gameObject.AddComponent<Image>().color = Color.clear;
            tap.gameObject.AddComponent<Button>().onClick.AddListener(() => GemsTapped?.Invoke());
        }

        RectTransform Pill(string name, float width)
        {
            var pill = UiKit.Panel(transform, name, Palette.Card, 40);
            pill.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            return pill.rectTransform;
        }

        static void Badge(RectTransform pill, Sprite sprite, Color color)
        {
            UiKit.Icon(pill, "Icon", sprite, color, 54).rectTransform.Pin(new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(54, 54));
        }

        static RTLTextMeshPro Value(RectTransform pill, float size)
        {
            var text = UiKit.Text(pill, "Value", "", size, Palette.Ink, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            text.rectTransform.Stretch(14, 76, 4, 4);
            return text;
        }

        // The energy bar appears when the tutorial first teaches generators (COMPLETE_SPEC.md section 1, step 5).
        public void ShowEnergy(bool show)
        {
            if (energyPill.activeSelf != show) energyPill.SetActive(show);
        }

        public void Refresh()
        {
            var s = game.State;
            level.text = s.Level.ToString();
            xpFill.SetFill((float)s.Xp / game.XpToNext);
            energy.text = s.Energy.ToString();
            coins.text = s.Coins.ToString();
            gems.text = Wallet.Gems.ToString();
            RefreshTimer();
        }

        void RefreshTimer()
        {
            int seconds = game.SecondsToNextEnergy(now());
            timer.text = game.State.Energy >= game.EnergyCap
                ? Loc.T("hud.energyFull")
                : Loc.T("hud.energyNext", $"{seconds / 60}:{seconds % 60:00}");
        }

        void Update()
        {
            if (Time.unscaledTime < nextTimerUpdate) return;
            nextTimerUpdate = Time.unscaledTime + 0.5f;
            RefreshTimer();
        }
    }
}
