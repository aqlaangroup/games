using System;
using Gahwit.Core;
using Gahwit.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The row of customer orders above the board. A card turns green and its button lights up when the board has everything.
    public sealed class OrdersView : MonoBehaviour
    {
        Game game;
        ItemLook look;
        Action<Order> serve;

        public void Init(Game game, ItemLook look, Action<Order> serve)
        {
            this.game = game;
            this.look = look;
            this.serve = serve;
            var row = UiKit.Row(gameObject, 14);
            row.padding = new RectOffset(16, 16, 0, 0);
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
        }

        readonly System.Collections.Generic.Dictionary<int, RectTransform> serveButtons = new System.Collections.Generic.Dictionary<int, RectTransform>();

        // The serve button of one order, for the tutorial's pointer.
        public RectTransform ServeButton(int orderId) => serveButtons.TryGetValue(orderId, out var button) ? button : null;

        public void Refresh()
        {
            UiKit.Clear(transform);
            serveButtons.Clear();
            foreach (var order in game.Orders) Card(order);
        }

        void Card(Order order)
        {
            bool ready = game.CanServe(order);
            var card = UiKit.Panel(transform, "Order" + order.Id, ready ? Palette.Ready : Palette.Card, 30);
            card.gameObject.AddComponent<OrderCard>().OrderId = order.Id; // a drop target for dragged items
            card.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            string who = Loc.T((order.IsCharacter ? "char." : "walkin.") + order.Customer);
            var color = order.IsCharacter && game.Content.Characters.TryGetValue(order.Customer, out var character)
                ? Palette.Hex(character.Color)
                : Palette.InkSoft;
            UiKit.Text(card.transform, "Who", who, 28, color, TextAlignmentOptions.Center).rectTransform.Top(10, 40, 8, 8);

            var items = UiKit.Rect(card.transform, "Items").Top(54, 92, 8, 8);
            var row = UiKit.Row(items.gameObject, 10);
            row.childControlWidth = row.childControlHeight = false;
            foreach (var line in order.Lines)
            {
                var tile = new ItemTile(items, "Wanted", 36, 0);
                tile.Root.sizeDelta = new Vector2(88, 88);
                tile.Show(new Item(line.Chain, line.Level), look);
                if (line.Count <= 1) continue;
                var badge = UiKit.Panel(tile.Root, "Count", Palette.Ink, 16);
                badge.raycastTarget = false;
                badge.rectTransform.Pin(Vector2.zero, new Vector2(-8, -8), new Vector2(50, 34));
                UiKit.Text(badge.transform, "N", "×" + line.Count, 22, Color.white, TextAlignmentOptions.Center).rectTransform.Stretch();
            }

            UiKit.Text(card.transform, "Reward", Loc.T("order.reward", order.Coins), 24, Palette.InkSoft, TextAlignmentOptions.Center)
                .rectTransform.Top(150, 32, 8, 8);

            var button = UiKit.Button(card.transform, "Serve", Loc.T(ready ? "order.serve" : "order.notReady"),
                ready ? Palette.SageDark : Palette.Muted, ready ? Color.white : Palette.InkSoft, 26, () => serve(order), 22);
            button.interactable = ready;
            ((RectTransform)button.transform).Bottom(10, 56, 14, 14);
            serveButtons[order.Id] = (RectTransform)button.transform;
        }
    }

    // Marks an order card, so the board can serve by dropping an item on it.
    public sealed class OrderCard : MonoBehaviour
    {
        public int OrderId;
    }
}
