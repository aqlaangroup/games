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

        int shown = -1;

        // The cards are rebuilt only when the orders or their readiness change, not on every tap.
        public void Refresh()
        {
            int key = 17;
            foreach (var order in game.Orders) key = (key * 31 + order.Id) * 2 + (game.CanServe(order) ? 1 : 0);
            if (key == shown && transform.childCount == game.Orders.Count) return;
            shown = key;
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
            UiKit.Text(card.transform, "Who", who, 28, color, TextAlignmentOptions.Center).OneLine().rectTransform.Top(10, 40, 8, 8);
            var picture = order.IsCharacter ? Portraits.For(order.Customer, ready ? "smile" : "neutral") : null;
            if (picture != null)
            {
                var portrait = UiKit.Icon(card.transform, "Face", picture, Color.white, 64);
                portrait.rectTransform.Pin(new Vector2(1, 1), new Vector2(-4, 4), new Vector2(64, 70));
            }

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

            // A signature dish names its recipe, with a star and a gold edge, so it doesn't rely on colour alone.
            // A story order says so in words, with a rose edge (DEVLOG 2026-10-31).
            string reward = order.Story != null ? Loc.T("order.story", order.Coins)
                : order.Recipe != null ? Loc.T("order.signature", Loc.T("recipe." + order.Recipe), order.Coins)
                : Loc.T("order.reward", order.Coins);
            bool special = order.Recipe != null || order.Story != null;
            UiKit.Text(card.transform, "Reward", reward, 24, special ? Palette.Rose : Palette.InkSoft, TextAlignmentOptions.Center)
                .OneLine().rectTransform.Top(150, 32, 8, 8);
            if (special)
            {
                var edge = card.gameObject.AddComponent<Outline>();
                edge.effectColor = order.Story != null ? Palette.Rose : Palette.Gold;
                edge.effectDistance = new Vector2(4, -4);
            }

            // A ready card is one big button to serve (44 pt and more), with a tick so it doesn't rely on colour.
            var label = UiKit.Panel(card.transform, "Serve", ready ? Palette.SageDark : Palette.Muted, 22);
            label.raycastTarget = false;
            label.rectTransform.Bottom(10, 56, 14, 14);
            UiKit.Text(label.transform, "Label", Loc.T(ready ? "order.serve" : "order.notReady"), 26, ready ? Color.white : Palette.InkSoft,
                TextAlignmentOptions.Center).rectTransform.Stretch(8, 8, 2, 2);
            if (ready)
            {
                var tick = UiKit.Icon(card.transform, "Ready", UiSprites.Tick, Palette.SageDark, 44);
                tick.rectTransform.Pin(new Vector2(0, 1), new Vector2(12, -10), new Vector2(44, 44));
                var tap = card.gameObject.AddComponent<Button>();
                tap.targetGraphic = card;
                tap.onClick.AddListener(() => UiSound.Play("ui_tap"));
                tap.onClick.AddListener(() => serve(order));
            }
            serveButtons[order.Id] = card.rectTransform;
        }
    }

    // Marks an order card, so the board can serve by dropping an item on it.
    public sealed class OrderCard : MonoBehaviour
    {
        public int OrderId;
    }
}
