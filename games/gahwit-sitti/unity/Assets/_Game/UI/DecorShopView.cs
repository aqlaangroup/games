using System.Linq;
using Gahwit.Core;
using Gahwit.Decor;
using Gahwit.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The decor shop (missing/07): pieces to buy with coins, pieces waiting in storage, and the sign's and chairs'
    // styles, which change for free once restored (GAME_SYSTEMS.md section 9).
    public sealed class DecorShopView : MonoBehaviour
    {
        static readonly string[] StyledTasks = { "sign", "chairs" };

        DecorRules rules;
        Game game;
        Toast toast;
        RectTransform list;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(DecorRules rules, Game game, Toast toast)
        {
            this.rules = rules;
            this.game = game;
            this.toast = toast;
            var card = Sheet.Card(transform, 980, 1720);
            Sheet.Title(card, Loc.T("decor.shopTitle"), Close);
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
            Heading(Loc.T("decor.forSale"));
            var forSale = rules.InShop.OrderBy(rules.Price).ToList();
            if (forSale.Count == 0) Note(Loc.T("decor.soldOut"));
            foreach (var def in forSale)
            {
                int price = rules.Price(def);
                Row(def, Loc.T("decor.buy", price), game.State.Coins >= price, () =>
                {
                    if (rules.Buy(def.Id))
                    {
                        UiSound.Play("decor_place");
                        toast.Show(Loc.T("decor.bought", Loc.T("decor." + def.Id)));
                    }
                    else toast.Show(Loc.T("decor.noRoom"));
                });
            }

            var stored = rules.Stored.ToList();
            if (stored.Count > 0)
            {
                Heading(Loc.T("decor.storage"));
                foreach (var def in stored)
                    Row(def, Loc.T("decor.place"), true, () =>
                    {
                        if (rules.Place(def.Id)) UiSound.Play("decor_place");
                        else toast.Show(Loc.T("decor.noRoom"));
                    });
            }

            var done = game.CompletedTaskIds();
            var styled = StyledTasks.Where(done.Contains).ToList();
            if (styled.Count == 0) return;
            Heading(Loc.T("decor.styles"));
            foreach (var task in styled) StyleRow(task);
        }

        void Heading(string text)
        {
            var label = UiKit.Text(list, "Heading", text, 28, Palette.InkSoft);
            label.fontStyle = FontStyles.Bold;
            Sheet.Sized(label, 56);
        }

        void Note(string text) => Sheet.Sized(UiKit.Text(list, "Note", text, 24, Palette.InkSoft), 48);

        // Name, style and cosiness on the right; the action on the left.
        void Row(DecorDef def, string action, bool enabled, UnityEngine.Events.UnityAction act)
        {
            var row = UiKit.Panel(list, "Piece " + def.Id, Color.white, 28);
            Sheet.Sized(row, 140);
            var name = UiKit.Text(row.transform, "Name", Loc.T("decor." + def.Id), 30, Palette.Ink);
            name.fontStyle = FontStyles.Bold;
            name.rectTransform.Stretch(290, 28, 18, 70);
            string style = def.Style != null ? Loc.T("style." + def.Style) + " · " : "";
            UiKit.Text(row.transform, "Details", style + Loc.T("decor.cosinessPlus", def.Cosiness), 24, Palette.InkSoft)
                .rectTransform.Stretch(290, 28, 72, 18);
            var button = UiKit.Button(row.transform, "Act", action, enabled ? Palette.Rose : Palette.Muted, enabled ? Color.white : Palette.InkSoft, 26, act, 30);
            button.interactable = enabled;
            ((RectTransform)button.transform).Pin(new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(240, 120));
        }

        void StyleRow(string task)
        {
            var row = UiKit.Panel(list, "Style " + task, Color.white, 28);
            Sheet.Sized(row, 210);
            UiKit.Text(row.transform, "Task", Loc.T($"task.{task}.title"), 30, Palette.Ink).rectTransform.Top(16, 50, 28, 28);
            var chips = UiKit.Rect(row.transform, "Chips").Bottom(14, 124, 20, 20);
            UiKit.Row(chips.gameObject, 12).childForceExpandWidth = true;
            game.State.TaskStyles.TryGetValue(task, out var current);
            foreach (var style in game.Config.LevelB.Cafe.Styles)
            {
                var def = game.Content.Styles[style];
                var chip = UiKit.Button(chips, style, Loc.T("style." + style), Palette.Hex(def.SignBoard), Palette.Hex(def.SignText), 24,
                    () => rules.SetTaskStyle(task, style), 28);
                chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                var outline = chip.gameObject.AddComponent<Outline>();
                bool chosen = style == current;
                outline.effectColor = chosen ? Palette.Rose : new Color(0.23f, 0.15f, 0.19f, 0.25f);
                outline.effectDistance = chosen ? new Vector2(5, -5) : new Vector2(1.5f, -1.5f);
                if (chosen) chip.Label().fontStyle = FontStyles.Bold;
            }
        }
    }
}
