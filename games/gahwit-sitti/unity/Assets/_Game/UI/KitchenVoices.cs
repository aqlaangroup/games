using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.Merge;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The kitchen's voices (DEVLOG 2026-10-31): what customers say when they order and when they're served
    // (bark.<who>.ask/thanks.<n>, storyorder.<id>.ask/thanks), the page of Teta's notebook that opens the first time an
    // item is made (notebook.<chain>_<level>), and the serving streak. Nothing here blocks play; each note goes on its own.
    public sealed class KitchenVoices : MonoBehaviour
    {
        Game game;
        ItemLook look;
        float lineSeconds;
        Image bubble, face, page, streak;
        Outline bubbleEdge, tailEdge;
        RTLTextMeshPro who, says, pageTitle, pageNote, streakText;
        ItemTile pageItem;
        float bubbleUntil, pageUntil, streakUntil, bubbleFrom, pageFrom;
        const float Pop = 0.18f; // seconds a note takes to grow in
        readonly HashSet<int> known = new HashSet<int>();
        readonly Queue<(Order order, string line, string mood, float seconds)> lines = new Queue<(Order, string, string, float)>();
        readonly Queue<(Item item, string title, string note)> pages = new Queue<(Item, string, string)>();

        public void Init(Game game, ItemLook look, float boardTop, float boardBottom)
        {
            this.game = game;
            this.look = look;
            lineSeconds = game.Config.LevelB.Kitchen.BarkSeconds;

            // A speech bubble over the top of the board, solid so the board doesn't show through, with a tail up toward
            // the orders and the customer's face where it starts.
            bubble = UiKit.Panel(transform, "Bubble", Palette.Cream, 32);
            bubble.raycastTarget = false;
            bubble.rectTransform.Top(boardTop + 18, 150, 24, 24);
            var shadow = bubble.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(Palette.Ink.r, Palette.Ink.g, Palette.Ink.b, 0.18f);
            shadow.effectDistance = new Vector2(0, -6);
            bubbleEdge = bubble.gameObject.AddComponent<Outline>();
            var tail = UiKit.Panel(bubble.transform, "Tail", Palette.Cream, 0);
            tail.raycastTarget = false;
            // Centred over the face (70 from the edge): Pin puts the pivot at the corner, so it moves to the middle here.
            tail.rectTransform.Pin(new Vector2(1, 1), new Vector2(-70, 0), new Vector2(30, 30)).pivot = new Vector2(0.5f, 0.5f);
            tail.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            tailEdge = tail.gameObject.AddComponent<Outline>();
            var join = UiKit.Panel(bubble.transform, "Join", Palette.Cream, 0); // hides the tail's edge inside the bubble
            join.raycastTarget = false;
            join.rectTransform.Pin(new Vector2(1, 1), new Vector2(-70, -14), new Vector2(60, 26)).pivot = new Vector2(0.5f, 0.5f);
            face = UiKit.Icon(bubble.transform, "Face", null, Color.white, 130);
            face.rectTransform.Pin(new Vector2(1, 1), new Vector2(-10, -10), new Vector2(120, 130));
            who = UiKit.Text(bubble.transform, "Who", "", 24, Palette.Rose);
            who.fontStyle = FontStyles.Bold;
            who.rectTransform.Top(10, 34, 28, 144);
            says = UiKit.Text(bubble.transform, "Says", "", 28, Palette.Ink);
            says.rectTransform.Stretch(28, 144, 46, 10);
            bubble.gameObject.SetActive(false);

            // A notebook page, over the bottom of the board.
            page = UiKit.Panel(transform, "Page", Palette.Hex("#FBF3E6"), 28);
            page.raycastTarget = false;
            page.rectTransform.Bottom(boardBottom + 8, 190, 24, 24);
            pageItem = new ItemTile(page.transform, "Item", 40, 0);
            pageItem.Root.Pin(new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(130, 130));
            pageTitle = UiKit.Text(page.transform, "Title", "", 24, Palette.Rose);
            pageTitle.fontStyle = FontStyles.Bold;
            pageTitle.rectTransform.Top(12, 36, 24, 166);
            pageNote = UiKit.Text(page.transform, "Note", "", 26, Palette.Ink);
            pageNote.rectTransform.Stretch(24, 166, 50, 12);
            page.gameObject.SetActive(false);

            // The streak, in words and a number so it doesn't rely on colour.
            streak = UiKit.Panel(transform, "Streak", Palette.Gold, 30);
            streak.raycastTarget = false;
            streak.rectTransform.Pin(new Vector2(0, 1), new Vector2(24, -(boardTop + 182)), new Vector2(300, 64));
            streakText = UiKit.Text(streak.transform, "Text", "", 26, Palette.Ink, TextAlignmentOptions.Center);
            streakText.fontStyle = FontStyles.Bold;
            streakText.rectTransform.Stretch(8, 8, 4, 4);
            streak.gameObject.SetActive(false);

            foreach (var order in game.Orders) known.Add(order.Id); // only new customers speak up
            game.Changed += NoticeNewOrders;
            game.OrderServed += order => Say(order, "thanks", order.Story != null ? lineSeconds * 1.6f : lineSeconds);
            game.PageFound += (chain, level) =>
                pages.Enqueue((new Item(chain, level), Loc.T("kitchen.page"), Text("notebook." + Game.PageId(chain, level))));
            game.NotebookChainDone += chain =>
                pages.Enqueue((new Item(chain, game.MaxLevel(chain)), Loc.T("kitchen.chainDone", Loc.T("chain." + chain)), Text("notebook." + chain + ".done")));
            game.StreakServed += (count, bonus) =>
            {
                if (count < 2) return;
                streakText.text = bonus > 0 ? Loc.T("kitchen.streakBonus", count, bonus) : Loc.T("kitchen.streak", count);
                streak.gameObject.SetActive(true);
                streakUntil = Time.unscaledTime + lineSeconds;
                UiSound.Play("ui_coin");
            };
        }

        void NoticeNewOrders()
        {
            foreach (var order in game.Orders)
                if (known.Add(order.Id)) Say(order, "ask", order.Story != null ? lineSeconds * 1.6f : lineSeconds);
        }

        void Say(Order order, string kind, float seconds)
        {
            string key = order.Story != null ? $"storyorder.{order.Story}.{kind}"
                : order.IsCharacter ? $"bark.{order.Customer}.{kind}.{1 + order.Id % 3}"
                : $"bark.walkin.{kind}.{1 + order.Id % 6}";
            string line = Text(key);
            if (line != null) lines.Enqueue((order, line, kind == "thanks" ? "smile" : "neutral", seconds));
        }

        // A string from the table, or null while it isn't written yet.
        static string Text(string key)
        {
            string text = Loc.T(key);
            return text == key ? null : text;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (bubble.gameObject.activeSelf && now >= bubbleUntil) bubble.gameObject.SetActive(false);
            if (!bubble.gameObject.activeSelf && lines.Count > 0)
            {
                var (order, line, mood, seconds) = lines.Dequeue();
                // A story order's line has the rose edge its card has; the rest a soft one.
                var edge = order.Story != null ? Palette.Rose : Palette.RoseSoft;
                var width = new Vector2(order.Story != null ? 4 : 2, order.Story != null ? -4 : -2);
                bubbleEdge.effectColor = tailEdge.effectColor = edge;
                bubbleEdge.effectDistance = tailEdge.effectDistance = width;
                who.text = Loc.T((order.IsCharacter ? "char." : "walkin.") + order.Customer);
                says.text = line;
                var picture = order.IsCharacter ? Portraits.For(order.Customer, mood) : null;
                face.sprite = picture;
                face.gameObject.SetActive(picture != null);
                says.rectTransform.Stretch(28, picture != null ? 144 : 28, 46, 10);
                who.rectTransform.Top(10, 34, 28, picture != null ? 144 : 28);
                bubble.gameObject.SetActive(true);
                bubbleFrom = now;
                bubbleUntil = now + seconds;
            }
            if (bubble.gameObject.activeSelf) bubble.rectTransform.localScale = Grow(now - bubbleFrom);

            if (page.gameObject.activeSelf && now >= pageUntil) page.gameObject.SetActive(false);
            if (!page.gameObject.activeSelf && pages.Count > 0)
            {
                var (item, title, note) = pages.Dequeue();
                pageItem.Show(item, look);
                pageTitle.text = title + " · " + game.ItemName(item.Chain, item.Level);
                pageNote.text = note ?? "";
                page.gameObject.SetActive(true);
                pageFrom = now;
                pageUntil = now + lineSeconds * 1.5f;
                UiSound.Play("page_turn");
            }
            if (page.gameObject.activeSelf) page.rectTransform.localScale = Grow(now - pageFrom);

            if (streak.gameObject.activeSelf && now >= streakUntil) streak.gameObject.SetActive(false);
        }

        // A small pop as a note appears: from 92% to full size, easing out.
        static Vector3 Grow(float age)
        {
            float t = Mathf.Clamp01(age / Pop);
            float size = Mathf.Lerp(0.92f, 1f, 1f - (1f - t) * (1f - t));
            return new Vector3(size, size, 1f);
        }
    }
}
