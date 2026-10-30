using System;
using Gahwit.Core;
using Gahwit.Merge;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The merge kitchen: orders on top, the board, the generators, and a line about the selected item.
    public sealed class KitchenScreen : MonoBehaviour
    {
        const int LowEnergy = 5, NearlyFullSquares = 8;

        Game game;
        ItemLook look;
        GameUi ui;
        Func<long> now;
        OrdersView orders;
        BoardView board;
        RectTransform generators;
        RTLTextMeshPro info;
        Button sell;

        public void Init(Game game, ItemLook look, GameUi ui, Func<long> now, RectTransform dragLayer)
        {
            this.game = game;
            this.look = look;
            this.ui = ui;
            this.now = now;
            var root = (RectTransform)transform;

            orders = UiKit.Rect(root, "Orders").Top(8, 250).gameObject.AddComponent<OrdersView>();
            orders.Init(game, look, Serve);

            board = UiKit.Rect(root, "Board").Stretch(12, 12, 268, 256).gameObject.AddComponent<BoardView>();
            board.Init(game, look, dragLayer);
            board.SelectionChanged += RefreshInfo;

            generators = UiKit.Rect(root, "Generators").Bottom(104, 144, 24, 24);
            UiKit.Row(generators.gameObject, 24).childForceExpandHeight = true;

            var infoBar = UiKit.Panel(root, "Info", Palette.Card, 28);
            infoBar.rectTransform.Bottom(8, 88, 24, 24);
            info = UiKit.Text(infoBar.transform, "Text", "", 26, Palette.Ink);
            info.rectTransform.Stretch(232, 24, 6, 6);
            sell = UiKit.Button(infoBar.transform, "Sell", "", Palette.Gold, Palette.Ink, 26, Sell, 24);
            ((RectTransform)sell.transform).Pin(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(200, 64));
        }

        public void Refresh()
        {
            orders.Refresh();
            board.Refresh();
            RefreshGenerators();
            RefreshInfo();
        }

        void RefreshGenerators()
        {
            UiKit.Clear(generators);
            int cost = game.Config.Generators.TapEnergyCost;
            for (int i = 0; i < game.Generators.Count; i++)
            {
                int index = i;
                var generator = game.Generators[i];
                var def = game.Config.GeneratorDef(generator.Id);
                var color = look.Tint(generator.Chain, game.MaxLevel(generator.Chain) / 2 + 1);
                string label = $"{def.Ar}\n<size=22>{Loc.T("gen.level", generator.Level)} · {Loc.T("gen.cost", cost)}</size>";
                var button = UiKit.Button(generators, "Generator " + generator.Id, label, color, Palette.TextOn(color), 30, () => Tap(index), 34);
                button.interactable = game.State.Energy >= cost;
                button.gameObject.AddComponent<LayoutElement>().preferredWidth = 330;
            }
        }

        void Tap(int index)
        {
            var result = game.TapGenerator(index, now(), out _);
            if (result == TapResult.NoEnergy) ui.Toast.Show(Loc.T("kitchen.noEnergy"));
            else if (result == TapResult.BoardFull) ui.Toast.Show(Loc.T("kitchen.boardFull"));
        }

        void Serve(Order order)
        {
            board.ClearSelection();
            game.Serve(order.Id);
        }

        void Sell()
        {
            int cell = board.Selected;
            if (cell < 0 || game.ItemAt(cell) == null) return;
            int coins = game.Sell(cell);
            board.ClearSelection();
            ui.Toast.Show(Loc.T("toast.sold", coins));
        }

        void RefreshInfo()
        {
            int cell = board.Selected;
            var item = cell >= 0 ? game.ItemAt(cell) : null;
            sell.gameObject.SetActive(item != null);

            if (item != null)
            {
                string next = item.Level < game.MaxLevel(item.Chain)
                    ? Loc.T("kitchen.next", game.ItemName(item.Chain, item.Level + 1))
                    : Loc.T("kitchen.top");
                info.text = Loc.T("kitchen.selected", game.ItemName(item.Chain, item.Level), item.Level) + $"\n<size=22>{next}</size>";
                sell.Label().text = Loc.T("kitchen.sell", game.SellValue(item));
            }
            else if (game.State.OrdersServed == 0) info.text = Loc.T("kitchen.hint.start") + $"\n<size=22>{Loc.T("kitchen.hint.merge")}</size>";
            else if (game.EmptyCells < NearlyFullSquares) info.text = Loc.T("kitchen.hint.full");
            else if (game.State.Energy < LowEnergy) info.text = Loc.T("kitchen.hint.lowEnergy");
            else info.text = Loc.T("kitchen.hint.merge");
        }
    }
}
