using System;
using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.Merge;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The season pass (missing/18): the season's 30 tiers, each paying on the free track and on the premium track. A
    // reached tier's reward is a button; a claimed one says so in words, not only in colour. Without this season's
    // premium pass, a button at the bottom offers it from the store.
    public sealed class PassView : MonoBehaviour
    {
        Game game;
        Action claimed; // asks the platform for the gems just earned
        RectTransform card, list;
        RTLTextMeshPro header;
        Image fill;
        Button premium;
        IShopUi shop;

        public bool IsOpen => gameObject.activeSelf;

        public IShopUi Shop
        {
            set
            {
                shop = value;
                shop.Finished += (_, _) => { if (IsOpen) Build(); };
            }
        }

        public void Init(Game game, Action claimed)
        {
            this.game = game;
            this.claimed = claimed;
            card = Sheet.Card(transform, 980, 1640);
            Sheet.Title(card, Loc.T("pass.title"), Close);
            header = UiKit.Text(card, "Header", "", 26, Palette.InkSoft);
            header.rectTransform.Top(128, 40, 40, 40);
            var track = UiKit.Panel(card, "Progress", Palette.Track, 10);
            track.rectTransform.Top(178, 20, 40, 40);
            fill = UiKit.Panel(track.transform, "Fill", Palette.Gold, 10);

            // Column heads over the two tracks: free on the start side, premium on the end side.
            var free = UiKit.Text(card, "Free", Loc.T("pass.free"), 26, Palette.Ink, TextAlignmentOptions.Center);
            free.fontStyle = FontStyles.Bold;
            free.rectTransform.Pin(new Vector2(1, 1), new Vector2(-40, -214), new Vector2(390, 44));
            var paid = UiKit.Text(card, "Premium", Loc.T("pass.premium"), 26, Palette.Rose, TextAlignmentOptions.Center);
            paid.fontStyle = FontStyles.Bold;
            paid.rectTransform.Pin(new Vector2(0, 1), new Vector2(40, -214), new Vector2(390, 44));

            premium = UiKit.Button(card, "BuyPremium", "", Palette.Rose, Color.white, 28, BuyPremium, 34);
            ((RectTransform)premium.transform).Bottom(32, UiKit.MinTouch, 40, 40);
            list = Sheet.Column(card, 270, 32 + UiKit.MinTouch + 20, 12);
            game.Changed += () => { if (IsOpen) Build(); };
            gameObject.SetActive(false);
        }

        public void Show()
        {
            UiSound.Play("ui_panel_open");
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Build();
        }

        void Close()
        {
            UiSound.Play("ui_panel_close");
            gameObject.SetActive(false);
        }

        void BuyPremium()
        {
            if (shop != null && shop.Ready) shop.Buy(game.Config.Pass.PremiumSku);
        }

        void Build()
        {
            var pass = game.Config.Pass;
            int tier = game.PassTier;
            int points = game.PassPoints;
            int intoTier = tier >= pass.Tiers ? pass.PointsPerTier : points % pass.PointsPerTier;
            header.text = Loc.T("pass.header", tier, pass.Tiers, intoTier, pass.PointsPerTier, game.PassDaysLeft);
            fill.SetFill(Mathf.Clamp01(points / (float)(pass.Tiers * pass.PointsPerTier)));

            bool canBuy = !game.HasPremiumPass && shop != null && shop.Ready;
            premium.gameObject.SetActive(canBuy);
            if (canBuy) premium.Label().text = Loc.T("pass.buyPremium", shop.Price(pass.PremiumSku));

            UiKit.Clear(list);
            for (int n = 1; n <= pass.Tiers; n++) Row(n, n <= tier);
        }

        void Row(int tier, bool reached)
        {
            var row = UiKit.Panel(list, "Tier " + tier, reached ? Color.white : Palette.Cream, 28);
            Sheet.Sized(row, 150);
            var badge = UiKit.Panel(row.transform, "Tier", reached ? Palette.Gold : Palette.Muted, 40);
            badge.rectTransform.Pin(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
            var number = UiKit.Text(badge.transform, "N", tier.ToString(), 32, Palette.Ink, TextAlignmentOptions.Center);
            number.fontStyle = FontStyles.Bold;
            number.rectTransform.Stretch();
            Chip(row.transform, Game.FreeTrack, tier, new Vector2(1, 0.5f));
            Chip(row.transform, Game.PremiumTrack, tier, new Vector2(0, 0.5f));
        }

        // One track's reward at a tier: a button to claim it, "claimed", or what it will pay (dimmed until reached, and
        // marked premium while the premium pass isn't bought).
        void Chip(Transform row, string track, int tier, Vector2 side)
        {
            string reward = Describe(track, tier);
            bool claimable = game.CanClaimPass(track, tier);
            bool done = game.PassClaimed(track, tier);
            if (claimable)
            {
                var button = UiKit.Button(row, track, Loc.T("pass.claim", reward), Palette.Rose, Color.white, 24, () =>
                {
                    if (game.ClaimPass(track, tier)) claimed();
                }, 28);
                ((RectTransform)button.transform).Pin(side, new Vector2(side.x > 0.5f ? -18 : 18, 0), new Vector2(380, UiKit.MinTouch - 16));
                button.Label().OneLine();
                return;
            }
            string text = done ? Loc.T("today.claimed") + " · " + reward
                : track == Game.PremiumTrack && !game.HasPremiumPass ? Loc.T("pass.premium") + " · " + reward
                : reward;
            var label = UiKit.Text(row, track, text, 24, done ? Palette.SageDark : Palette.InkSoft, TextAlignmentOptions.Center);
            if (done) label.fontStyle = FontStyles.Bold;
            label.rectTransform.Pin(side, new Vector2(side.x > 0.5f ? -18 : 18, 0), new Vector2(380, 110));
        }

        // "+2 gems", "+100 energy", "300 coins", and the premium items by name.
        string Describe(string track, int tier)
        {
            var reward = game.PassRewardAt(track, tier);
            var parts = new List<string>();
            if (reward.Gems > 0) parts.Add(Loc.T("shop.gems", reward.Gems));
            if (reward.Energy > 0) parts.Add(Loc.T("shop.energy", reward.Energy));
            if (reward.Coins > 0) parts.Add(Loc.T("today.coins", reward.Coins));
            if (track == Game.PremiumTrack)
                foreach (var item in game.PassItemsAt(tier))
                    parts.Add(item.StartsWith("decor:") ? Loc.T("decor." + item.Substring(6)) : Loc.T("pass.outfit"));
            return string.Join(" · ", parts);
        }
    }
}
