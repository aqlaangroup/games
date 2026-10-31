using System;
using System.Threading.Tasks;
using Gahwit.Core;
using Gahwit.Merge;
using TMPro;
using UnityEngine;

namespace Gahwit.UI
{
    // Storage (GAME_SYSTEMS.md section 2; screens/Storage): what's kept off the board, taking pieces back, and a
    // slot bought with gems through the platform.
    public sealed class StorageView : MonoBehaviour
    {
        Game game;
        ItemLook look;
        Toast toast;
        RectTransform list;
        RTLTMPro.RTLTextMeshPro count;
        Func<Task<ActionResult>> buySlot;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(Game game, ItemLook look, Toast toast, Func<Task<ActionResult>> buySlot)
        {
            this.game = game;
            this.look = look;
            this.toast = toast;
            this.buySlot = buySlot;
            var card = Sheet.Card(transform, 980, 1500);
            Sheet.Title(card, Loc.T("storage.title"), Close);
            count = UiKit.Text(card, "Count", "", 28, Palette.InkSoft);
            count.rectTransform.Top(124, 44, 48, 48);
            UiKit.Text(card, "Hint", Loc.T("storage.hint"), 24, Palette.InkSoft).rectTransform.Top(170, 70, 48, 48);
            list = Sheet.Column(card, 250, 40, 12);
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

        void Build()
        {
            UiKit.Clear(list);
            count.text = Loc.T("storage.count", game.State.Storage.Count, game.StorageCapacity);
            for (int i = 0; i < game.State.Storage.Count; i++)
            {
                int index = i;
                var piece = game.State.Storage[i];
                var row = UiKit.Panel(list, "Stored", Color.white, 28);
                Sheet.Sized(row, 140);
                var tile = new ItemTile(row.transform, "Item", 30, 0);
                tile.Root.Pin(new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(110, 110));
                tile.Show(piece, look);
                var back = UiKit.Button(row.transform, "Back", Loc.T("storage.takeOut"), Palette.SageDark, Color.white, 26, () =>
                {
                    if (!game.TakeOut(index)) toast.Show(Loc.T("kitchen.boardFull"));
                }, 30);
                ((RectTransform)back.transform).Pin(new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(300, 120));
            }

            int next = game.NextStorageSlot;
            if (next < 0) return;
            int gems = game.Config.Board.InventorySlotGemCosts[next - 1];
            var buy = UiKit.Button(list, "Buy", Loc.T("storage.buySlot", gems), Palette.Lilac, Palette.Ink, 28, async () =>
            {
                var result = await buySlot();
                toast.Show(Loc.T("gems." + result.ToString().ToLowerInvariant()));
            }, 34);
            Sheet.Sized(buy, 124);
        }
    }
}
