using System;
using Gahwit.Core;
using Gahwit.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The mini board (سوق الجمعة; missing/18): its two orders, its tokens toward the next milestone, and the small
    // board. Tap the stall for a first item (1 energy). Tap an item, then another square, to move or merge it.
    public sealed class MiniBoardView : MonoBehaviour
    {
        Game game;
        Func<long> now;
        Action<MiniBoard> paid; // milestones reached: the café pays them
        MiniBoard board;
        RectTransform grid, orders;
        RTLTMPro.RTLTextMeshPro title, tokens;
        Image[] cells;
        RTLTMPro.RTLTextMeshPro[] labels;
        int selected = -1;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(Game game, Func<long> now, Action<MiniBoard> paid)
        {
            this.game = game;
            this.now = now;
            this.paid = paid;
            var card = Sheet.Card(transform, 1000, 1760, Palette.Hex("#FFF6E8"));
            title = Sheet.Title(card, "", Close);
            tokens = UiKit.Text(card, "Tokens", "", 28, Palette.Teal);
            tokens.rectTransform.Top(124, 50, 48, 48);
            orders = UiKit.Rect(card, "Orders").Top(190, 230, 40, 40);
            var row = UiKit.Row(orders.gameObject, 20);
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            grid = UiKit.Rect(card, "Grid").Stretch(40, 40, 450, 40);
            game.Changed += () => { if (IsOpen) Build(); };
            gameObject.SetActive(false);
        }

        public void Show(MiniBoard miniBoard, string name)
        {
            board = miniBoard;
            title.text = name;
            selected = -1;
            UiKit.Clear(grid);
            cells = new Image[board.State.Board.Length];
            labels = new RTLTMPro.RTLTextMeshPro[cells.Length];
            float size = Mathf.Min(920f / board.Cols, 1270f / board.Rows);
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                var cell = UiKit.Panel(grid, "Cell" + i, Palette.Slot, 22);
                cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(0.5f, 1);
                cell.rectTransform.pivot = new Vector2(0.5f, 1);
                cell.rectTransform.sizeDelta = new Vector2(size - 8, size - 8);
                int column = UiKit.LeftToRight ? i % board.Cols : board.Cols - 1 - i % board.Cols;
                cell.rectTransform.anchoredPosition = new Vector2((column - (board.Cols - 1) / 2f) * size, -(i / board.Cols) * size);
                var tap = cell.gameObject.AddComponent<Button>();
                tap.targetGraphic = cell;
                tap.onClick.AddListener(() => Tap(index));
                cells[i] = cell;
                labels[i] = UiKit.Text(cell.transform, "Label", "", 26, Color.white, TextAlignmentOptions.Center);
                labels[i].rectTransform.Stretch(6, 6, 6, 6);
            }
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

        void Tap(int cell)
        {
            var item = board.State.Board[cell];
            if (item != null && item.IsGenerator && selected < 0)
            {
                if (!board.Tap(cell, now())) UiSound.Play("ui_error");
                else UiSound.Play("gen_tap_coffee");
                return;
            }
            if (selected < 0)
            {
                selected = item != null ? cell : -1;
                Build();
                return;
            }
            int from = selected;
            selected = -1;
            if (from != cell && board.Move(from, cell) == MoveResult.Merged)
            {
                UiSound.Play("merge");
                Tweens.Instance?.PopOf(cells[cell].rectTransform, 0.25f);
            }
            Build();
        }

        void Serve(int order)
        {
            if (!board.Serve(order)) return;
            UiSound.Play("order_served");
            if (board.Due().Count > 0) paid(board);
        }

        void Build()
        {
            if (board == null) return;
            tokens.text = board.NextMilestoneTokens > 0
                ? Loc.T("mini.tokens", board.State.Tokens, board.NextMilestoneTokens)
                : Loc.T("mini.allMilestones", board.State.Tokens);
            UiKit.Clear(orders);
            for (int n = 0; n < board.State.Orders.Count; n++)
            {
                int order = n;
                bool ready = board.CanServe(n);
                var button = UiKit.Button(orders, "Order", Loc.T("mini.order", board.Name(board.State.Orders[n]), board.TokensPerOrder) + (ready ? " ✓" : ""),
                    ready ? Palette.SageDark : Palette.Card, ready ? Color.white : Palette.Ink, 28, () => Serve(order), 32);
                button.interactable = ready;
                button.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
            for (int i = 0; i < cells.Length; i++)
            {
                var item = board.State.Board[i];
                cells[i].color = item == null ? Palette.Slot
                    : item.IsGenerator ? Palette.Gold
                    : Color.Lerp(Palette.Hex("#9BB28B"), Palette.Hex("#4F6B3A"), (item.Level - 1f) / Mathf.Max(1, board.MaxLevel - 1));
                labels[i].text = item == null ? "" : item.IsGenerator ? Loc.T("mini.stall") : $"{item.Level}\n<size=60%>{board.Name(item.Level)}</size>";
                labels[i].color = item != null && item.IsGenerator ? Palette.Ink : Color.white;
                var outline = cells[i].GetComponent<Outline>();
                if (i == selected)
                {
                    if (outline == null) outline = cells[i].gameObject.AddComponent<Outline>();
                    outline.effectColor = Palette.Rose;
                    outline.effectDistance = new Vector2(5, -5);
                    outline.enabled = true;
                }
                else if (outline != null) outline.enabled = false;
            }
        }
    }
}
