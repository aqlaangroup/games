using System;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Recipes;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The cooking pan (SYSTEMS_B.md section 4; COMPLETE_SPEC.md section 12): two slots, cook, and the way to the book.
    // Tapping an item in the pan takes it back to the board.
    public sealed class PanView : MonoBehaviour
    {
        RecipeBook book;
        Game game;
        ItemLook look;
        RecipeBookView bookView;
        readonly ItemTile[] slots = new ItemTile[2];
        readonly RTLTextMeshPro[] empty = new RTLTextMeshPro[2];
        Button cook;
        RTLTextMeshPro hint, result;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(RecipeBook book, Game game, ItemLook look, RecipeBookView bookView)
        {
            this.book = book;
            this.game = game;
            this.look = look;
            this.bookView = bookView;
            var card = Sheet.Card(transform, 920, 1180);
            Sheet.Title(card, Loc.T("pan.title"), Close);
            UiKit.Text(card, "Hint", Loc.T("pan.how"), 26, Palette.InkSoft).rectTransform.Top(130, 80, 48, 48);
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var slot = UiKit.Panel(card, "Slot" + i, Palette.Slot, 32);
                slot.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(i == 0 ? 170 : -170, -240), new Vector2(280, 280));
                var outline = slot.gameObject.AddComponent<Outline>();
                outline.effectColor = Palette.Gold;
                outline.effectDistance = new Vector2(3, -3);
                var tap = slot.gameObject.AddComponent<Button>();
                tap.targetGraphic = slot;
                tap.onClick.AddListener(() => book.TakeOut(index));
                slots[i] = new ItemTile(slot.transform, "Item", 72, 30);
                slots[i].Root.Stretch(16, 16, 16, 16);
                empty[i] = UiKit.Text(slot.transform, "Empty", Loc.T("pan.empty"), 26, Palette.InkSoft, TextAlignmentOptions.Center);
                empty[i].rectTransform.Stretch(12, 12, 12, 12);
            }
            UiKit.Text(card, "Plus", "+", 72, Palette.InkSoft, TextAlignmentOptions.Center).rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0, -360), new Vector2(60, 100));
            cook = UiKit.Button(card, "Cook", Loc.T("pan.cook"), Palette.Rose, Color.white, 34, Cook, 40);
            ((RectTransform)cook.transform).Top(560, UiKit.MinTouch + 10, 120, 120);
            result = UiKit.Text(card, "Result", "", 30, Palette.Teal, TextAlignmentOptions.Center);
            result.rectTransform.Top(720, 90, 48, 48);
            hint = UiKit.Text(card, "BookHint", "", 26, Palette.Rose, TextAlignmentOptions.Center);
            hint.rectTransform.Top(820, 110, 48, 48);
            var open = UiKit.Button(card, "Book", Loc.T("pan.book"), Palette.Lilac, Palette.Ink, 30, () => bookView.Show(), 36);
            ((RectTransform)open.transform).Bottom(44, UiKit.MinTouch, 160, 160);
            game.Changed += () => { if (IsOpen) Refresh(); };
            gameObject.SetActive(false);
        }

        public void Show()
        {
            UiSound.Play("ui_panel_open");
            transform.SetAsLastSibling();
            result.text = "";
            gameObject.SetActive(true);
            Refresh();
        }

        void Close()
        {
            UiSound.Play("ui_panel_close");
            gameObject.SetActive(false);
        }

        void Refresh()
        {
            for (int i = 0; i < 2; i++)
            {
                bool full = i < book.Pan.Count;
                if (full) slots[i].Show(book.Pan[i], look);
                else slots[i].Hide();
                empty[i].enabled = !full;
            }
            cook.interactable = book.Pan.Count == 2;
            var tip = book.Hint;
            hint.text = tip == null ? "" : Loc.T("pan.hint", game.ItemName(tip.ChainA, tip.LevelA), Loc.T("chain." + tip.ChainB));
        }

        void Cook()
        {
            switch (book.Cook(out var recipe))
            {
                case CookResult.Discovered:
                    UiSound.Play("page_turn");
                    result.text = "";
                    gameObject.SetActive(false); // the discovery card takes over
                    break;
                case CookResult.AlreadyKnown:
                    UiSound.Play("ui_tap");
                    result.text = Loc.T("pan.known", Loc.T("recipe." + recipe.Id));
                    break;
                case CookResult.Wrong:
                    UiSound.Play("ui_error");
                    result.text = Loc.T("pan.wrong");
                    break;
            }
            Refresh();
        }
    }

    // Teta's recipe book: a page per recipe. Found pages show the dish and its two items; the rest stay blank.
    public sealed class RecipeBookView : MonoBehaviour
    {
        RecipeBook book;
        Game game;
        RectTransform list;
        RTLTextMeshPro count;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(RecipeBook book, Game game)
        {
            this.book = book;
            this.game = game;
            var card = Sheet.Card(transform, 980, 1640, Palette.Hex("#FBF3E6"));
            Sheet.Title(card, Loc.T("recipes.title"), Close);
            count = UiKit.Text(card, "Count", "", 28, Palette.InkSoft);
            count.rectTransform.Top(124, 44, 48, 48);
            list = Sheet.Column(card, 190, 40, 12);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            UiSound.Play("page_turn");
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
            count.text = Loc.T("recipes.count", book.FoundCount, book.Content.Recipes.Count);
            foreach (var recipe in book.Content.Recipes)
            {
                bool found = book.Found(recipe.Id);
                var row = UiKit.Panel(list, recipe.Id, found ? Color.white : Palette.Muted, 24);
                Sheet.Sized(row, 120);
                var name = UiKit.Text(row.transform, "Name", found ? Loc.T("recipe." + recipe.Id) : Loc.T("recipes.blank"), 30,
                    found ? Palette.Ink : Palette.InkSoft);
                name.fontStyle = found ? FontStyles.Bold : FontStyles.Italic;
                name.rectTransform.Stretch(28, 28, 12, 60);
                if (!found) continue;
                UiKit.Text(row.transform, "Items", Loc.T("recipes.items", game.ItemName(recipe.ChainA, recipe.LevelA), game.ItemName(recipe.ChainB, recipe.LevelB)),
                    24, Palette.InkSoft).rectTransform.Stretch(28, 28, 62, 12);
            }
        }
    }
}
