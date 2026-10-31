using System.Linq;
using System;
using Gahwit.Core;
using Gahwit.Merge;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The merge kitchen: orders on top, the board with its generators, and a line about the selected piece.
    public sealed class KitchenScreen : MonoBehaviour
    {

        Game game;
        GameUi ui;
        Func<long> now;
        OrdersView orders;
        BoardView board;

        public BoardView Board => board;
        public OrdersView Orders => orders;
        RTLTextMeshPro info, bubbleTimer;
        Button sell, store, storage, bubble;
        float nextBubbleCheck;

        public void Init(Game game, ItemLook look, GameUi ui, Func<long> now, RectTransform dragLayer)
        {
            this.game = game;
            this.ui = ui;
            this.now = now;
            var root = (RectTransform)transform;

            orders = UiKit.Rect(root, "Orders").Top(8, 250).gameObject.AddComponent<OrdersView>();
            orders.Init(game, look, Serve);

            board = UiKit.Rect(root, "Board").Stretch(12, 12, 268, 108).gameObject.AddComponent<BoardView>();
            board.Init(game, look, dragLayer, TapGenerator);
            board.DroppedOnOrder += ServeByDrop;
            board.SelectionChanged += RefreshInfo;

            var infoBar = UiKit.Panel(root, "Info", Palette.Card, 28);
            infoBar.rectTransform.Bottom(8, 92, 24, 24);
            info = UiKit.Text(infoBar.transform, "Text", "", 26, Palette.Ink);
            info.rectTransform.Stretch(232, 24, 6, 6);
            sell = UiKit.Button(infoBar.transform, "Sell", "", Palette.Gold, Palette.Ink, 26, Sell, 24);
            ((RectTransform)sell.transform).Pin(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(200, 66));
            // With an item picked: keep it in storage. With nothing picked: open storage.
            store = UiKit.Button(infoBar.transform, "Store", Loc.T("kitchen.store"), Palette.Lilac, Palette.Ink, 24, StoreSelected, 24);
            ((RectTransform)store.transform).Pin(new Vector2(0, 0.5f), new Vector2(222, 0), new Vector2(150, 66));
            storage = UiKit.Button(infoBar.transform, "Storage", "", Palette.Lilac, Palette.Ink, 24, () => ui.Storage.Show(), 24);
            ((RectTransform)storage.transform).Pin(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(200, 66));
            info.rectTransform.Stretch(390, 24, 6, 6);

            // A bubble floats over the board's top corner for its last seconds (GAME_SYSTEMS.md section 7).
            bubble = UiKit.Button(root, "Bubble", "", Palette.Lilac, Palette.Ink, 24, () => ui.BubbleTapped?.Invoke(), 75);
            ((RectTransform)bubble.transform).Pin(new Vector2(1, 1), new Vector2(-24, -278), new Vector2(150, 150));
            bubble.Label().rectTransform.Stretch(10, 10, 20, 70);
            bubbleTimer = UiKit.Text(bubble.transform, "Timer", "", 26, Palette.Ink, TextAlignmentOptions.Center);
            bubbleTimer.rectTransform.Stretch(10, 10, 84, 14);
            bubble.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            orders.Refresh();
            board.Refresh();
            RefreshInfo();
        }

        void TapGenerator(int cell)
        {
            var result = game.TapGenerator(cell, now(), out _);
            if (result == TapResult.NoEnergy || result == TapResult.BoardFull) UiSound.Play("ui_error");
            if (result == TapResult.NoEnergy)
            {
                if (ui.OutOfEnergy != null) ui.OutOfEnergy();
                else ui.Toast.Show(Loc.T("kitchen.noEnergy"));
            }
            else if (result == TapResult.BoardFull) ui.Toast.Show(Loc.T("kitchen.boardFull"));
        }

        // Dragging a finished item onto its customer's card serves the order (COMPLETE_SPEC.md section 1, step 6).
        void ServeByDrop(int cell, int orderId)
        {
            var item = game.ItemAt(cell);
            var order = game.Orders.FirstOrDefault(o => o.Id == orderId);
            bool wanted = order != null && item != null && order.Lines.Any(l => l.Chain == item.Chain && l.Level == item.Level);
            if (wanted && game.CanServe(order)) Serve(order);
            else UiSound.Play("ui_error");
        }

        void StoreSelected()
        {
            int cell = board.Selected;
            if (!game.Store(cell)) return;
            UiSound.Play("ui_panel_close");
            board.ClearSelection();
        }

        void Update()
        {
            if (Time.unscaledTime < nextBubbleCheck) return;
            nextBubbleCheck = Time.unscaledTime + 0.25f;
            var offer = game.Bubble;
            bool show = offer != null;
            if (bubble.gameObject.activeSelf != show) bubble.gameObject.SetActive(show);
            if (!show) return;
            bubble.Label().text = game.ItemName(offer.Chain, offer.Level);
            long left = Math.Max(0, (offer.ExpiresMs - now()) / 1000);
            bubbleTimer.text = $"0:{left:00}";
        }

        void Serve(Order order)
        {
            board.ClearSelection();
            game.Serve(order.Id);
        }

        void Sell()
        {
            int cell = board.Selected;
            if (cell < 0 || !game.CanSell(game.ItemAt(cell))) return;
            int coins = game.Sell(cell);
            UiSound.Play("sell");
            board.ClearSelection();
            ui.Toast.Show(Loc.T("toast.sold", coins));
        }

        void RefreshInfo()
        {
            int cell = board.Selected;
            var item = cell >= 0 ? game.ItemAt(cell) : null;
            sell.gameObject.SetActive(game.CanSell(item));
            store.gameObject.SetActive(item != null && game.CanStore(cell));
            storage.gameObject.SetActive(item == null && game.StorageOpen);
            storage.Label().text = Loc.T("kitchen.storage", game.State.Storage.Count);

            if (item != null && item.IsGenerator)
            {
                string makes = game.ItemName(item.Chain, 1);
                info.text = Loc.T("kitchen.generator", game.GeneratorName(item.Generator), item.Level) +
                            $"\n<size=22>{Loc.T("kitchen.generatorHint", makes, game.Config.Generators.TapEnergyCost)}</size>";
            }
            else if (item != null)
            {
                string next = item.Level < game.MaxLevel(item.Chain)
                    ? Loc.T("kitchen.next", game.ItemName(item.Chain, item.Level + 1))
                    : Loc.T("kitchen.top");
                info.text = Loc.T("kitchen.selected", game.ItemName(item.Chain, item.Level), item.Level) + $"\n<size=22>{next}</size>";
                sell.Label().text = Loc.T("kitchen.sell", game.SellValue(item));
            }
            else if (game.State.OrdersServed == 0) info.text = Loc.T("kitchen.hint.start") + $"\n<size=22>{Loc.T("kitchen.hint.merge")}</size>";
            else if (game.EmptyCells < game.Config.Ui.KitchenHints.NearlyFullEmptySquares) info.text = Loc.T("kitchen.hint.full");
            else if (game.State.Energy < game.Config.Ui.KitchenHints.LowEnergy) info.text = Loc.T("kitchen.hint.lowEnergy");
            else info.text = Loc.T("kitchen.hint.merge");
        }
    }
}
