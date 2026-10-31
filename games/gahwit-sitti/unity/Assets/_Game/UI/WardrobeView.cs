using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Wardrobe;
using TMPro;
using UnityEngine;

namespace Gahwit.UI
{
    // Salma's wardrobe (missing/09, SYSTEMS_B.md section 6): the chapter's occasion with its hearts, her pieces to wear
    // or take off, and pieces for sale. Dressing for the occasion never blocks the story.
    public sealed class WardrobeView : MonoBehaviour
    {
        WardrobeRules rules;
        Game game;
        Toast toast;
        RectTransform list;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(WardrobeRules rules, Game game, Toast toast)
        {
            this.rules = rules;
            this.game = game;
            this.toast = toast;
            var card = Sheet.Card(transform, 980, 1720);
            Sheet.Title(card, Loc.T("wardrobe.title"), Close);
            list = Sheet.Column(card, 150, 40, 14);
            rules.Changed += () => { if (IsOpen) Build(); };
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

        void Build()
        {
            UiKit.Clear(list);
            OccasionCard();

            Heading(Loc.T("wardrobe.owned"));
            foreach (var piece in rules.Content.Pieces.Where(p => rules.Owns(p.Id)))
            {
                bool worn = rules.Worn(piece.Slot) == piece.Id;
                Row(piece, Loc.T(worn ? "wardrobe.takeOff" : "wardrobe.wear"), true, worn, () => rules.Toggle(piece.Id));
            }

            var forSale = rules.ForSale.OrderBy(rules.Price).ToList();
            if (forSale.Count == 0) return;
            Heading(Loc.T("wardrobe.forSale"));
            foreach (var piece in forSale)
            {
                int price = rules.Price(piece);
                Row(piece, Loc.T("wardrobe.buy", price), game.State.Coins >= price, false, () => rules.Buy(piece.Id));
            }
        }

        void OccasionCard()
        {
            var occasion = rules.CurrentOccasion;
            if (occasion == null) return;
            var card = UiKit.Panel(list, "Occasion", Palette.Cream, 30);
            Sheet.Sized(card, 300);
            var name = UiKit.Text(card.transform, "Name", Loc.T("wardrobe.occasion", Loc.T("occasion." + occasion.Id)), 30, Palette.Ink);
            name.fontStyle = FontStyles.Bold;
            name.rectTransform.Top(20, 48, 28, 28);
            string likes = string.Join("، ", occasion.Likes.Select(tag => Loc.T("tag." + tag)));
            UiKit.Text(card.transform, "Likes", Loc.T("wardrobe.likes", likes), 24, Palette.InkSoft).rectTransform.Top(72, 40, 28, 28);

            bool done = rules.OccasionDone(occasion);
            int hearts = done ? game.State.OccasionHearts[occasion.Chapter] : rules.Hearts(occasion);
            UiKit.Text(card.transform, "Hearts", done ? Loc.T("wardrobe.done", hearts) : Loc.T("wardrobe.hearts", hearts), 28, Palette.Rose)
                .rectTransform.Top(116, 44, 28, 28);
            if (done) return;
            var ready = UiKit.Button(card.transform, "Ready", Loc.T("wardrobe.ready"), Palette.Rose, Color.white, 30, () =>
            {
                int won = rules.DressFor(occasion);
                if (won > 0) toast.Show(Loc.T("wardrobe.paid", won, rules.BonusCoins(occasion, won)));
            }, 34);
            ((RectTransform)ready.transform).Bottom(20, 124, 28, 28);
        }

        void Heading(string text)
        {
            var label = UiKit.Text(list, "Heading", text, 28, Palette.InkSoft);
            label.fontStyle = FontStyles.Bold;
            Sheet.Sized(label, 56);
        }

        void Row(WardrobePiece piece, string action, bool enabled, bool worn, UnityEngine.Events.UnityAction act)
        {
            var row = UiKit.Panel(list, "Piece " + piece.Id, Color.white, 28);
            Sheet.Sized(row, 140);
            var swatch = UiKit.Icon(row.transform, "Colour", UiSprites.Circle, Palette.Hex(piece.Color), 72);
            swatch.rectTransform.Pin(new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(72, 72));
            var name = UiKit.Text(row.transform, "Name", Loc.T("wardrobe." + piece.Id), 30, Palette.Ink);
            name.fontStyle = worn ? FontStyles.Bold : FontStyles.Normal;
            name.rectTransform.Stretch(290, 116, 18, 70);
            string tags = string.Join("، ", piece.Tags.Select(tag => Loc.T("tag." + tag)));
            UiKit.Text(row.transform, "Details", Loc.T("slot." + piece.Slot) + " · " + tags, 22, Palette.InkSoft)
                .rectTransform.Stretch(290, 116, 72, 18);
            var button = UiKit.Button(row.transform, "Act", action, worn ? Palette.Muted : enabled ? Palette.Rose : Palette.Muted,
                worn || !enabled ? Palette.Ink : Color.white, 26, act, 30);
            button.interactable = enabled;
            ((RectTransform)button.transform).Pin(new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(240, 120));
        }
    }
}
