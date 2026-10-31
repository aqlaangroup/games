using System;
using Gahwit.Merge;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gahwit.UI
{
    // The 7×9 merge board. Squares are sized to fit the space it's given.
    public sealed class BoardView : MonoBehaviour
    {
        Game game;
        ItemLook look;
        Action<int> tapGenerator;
        RectTransform grid, dragLayer;
        CellView[] cells;
        ItemTile ghost;
        int selected = -1, dragFrom = -1;

        public int Selected => selected;
        public RectTransform CellRect(int cell) => (RectTransform)cells[cell].transform;
        public event Action SelectionChanged;
        public event Action<int, int> DroppedOnOrder; // cell, order id

        public void Init(Game game, ItemLook look, RectTransform dragLayer, Action<int> tapGenerator)
        {
            this.game = game;
            this.look = look;
            this.dragLayer = dragLayer;
            this.tapGenerator = tapGenerator;
            grid = UiKit.Rect(transform, "Grid").Pin(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cells = new CellView[game.CellCount];
            for (int i = 0; i < cells.Length; i++) cells[i] = CellView.Create(grid, i, this);
            game.ItemSpawned += Pop;
            game.ItemsMerged += Pop;
            Layout();
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.ItemSpawned -= Pop;
            game.ItemsMerged -= Pop;
        }

        void Pop(int cell) => cells[cell].Pop();

        void OnRectTransformDimensionsChange()
        {
            if (cells != null) Layout();
        }

        void Layout()
        {
            var area = ((RectTransform)transform).rect;
            float size = Mathf.Floor(Mathf.Min(area.width / game.Cols, area.height / game.Rows));
            if (size <= 0) return;
            grid.sizeDelta = new Vector2(size * game.Cols, size * game.Rows);
            for (int i = 0; i < cells.Length; i++)
            {
                var rect = (RectTransform)cells[i].transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(size - 6, size - 6);
                rect.anchoredPosition = new Vector2(i % game.Cols * size + 3, -(i / game.Cols) * size - 3);
            }
        }

        public void Refresh()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                var item = game.ItemAt(i);
                bool canMerge = selected >= 0 && game.CanMerge(selected, i);
                cells[i].Show(item, look, i == selected, canMerge, game.IsWanted(item), i == dragFrom);
            }
        }

        public void ClearSelection()
        {
            selected = -1;
            Refresh();
            SelectionChanged?.Invoke();
        }

        // Tap an item to select it, then tap an empty square or a matching piece to move or merge it there.
        // Tapping a generator always makes an item; generators move and merge by dragging.
        public void Click(int cell)
        {
            var target = game.ItemAt(cell);
            if (target != null && target.IsGenerator && (selected < 0 || selected == cell || !game.CanMerge(selected, cell)))
            {
                selected = cell;
                tapGenerator(cell);
            }
            else if (selected < 0) selected = target != null ? cell : -1;
            else if (selected == cell) selected = -1;
            else if (target == null || game.CanMerge(selected, cell))
            {
                int from = selected;
                selected = -1;
                game.Move(from, cell);
            }
            else selected = cell;
            Refresh();
            SelectionChanged?.Invoke();
        }

        public void BeginDrag(int cell, PointerEventData eventData)
        {
            var item = game.ItemAt(cell);
            if (item == null) return;
            dragFrom = cell;
            selected = cell;
            float size = ((RectTransform)cells[cell].transform).rect.width;
            ghost = new ItemTile(dragLayer, "DragGhost", 44, 18);
            ghost.Root.Pin(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            ghost.Root.pivot = new Vector2(0.5f, 0.5f);
            ghost.Root.localScale = Vector3.one * 1.12f;
            ghost.Show(item, look);
            MoveGhost(eventData);
            Refresh();
            SelectionChanged?.Invoke();
        }

        public void Drag(PointerEventData eventData)
        {
            if (ghost != null) MoveGhost(eventData);
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (dragFrom < 0) return;
            int from = dragFrom;
            dragFrom = -1;
            if (ghost != null)
            {
                Destroy(ghost.Root.gameObject);
                ghost = null;
            }

            var hit = eventData.pointerCurrentRaycast.gameObject;
            var target = hit != null ? hit.GetComponentInParent<CellView>() : null;
            if (target != null && target.Index != from)
            {
                selected = -1;
                game.Move(from, target.Index);
            }
            else if (target == null && hit != null && hit.GetComponentInParent<OrderCard>() is OrderCard card)
            {
                selected = -1;
                DroppedOnOrder?.Invoke(from, card.OrderId);
            }
            Refresh();
            SelectionChanged?.Invoke();
        }

        void MoveGhost(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, eventData.pressEventCamera, out var local);
            ghost.Root.anchoredPosition = local;
        }
    }
}
