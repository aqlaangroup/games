using Gahwit.Merge;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // One square of the board: tap to select, tap or drag onto another square to move or merge.
    public sealed class CellView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        BoardView board;
        Image ring, wantedDot;
        ItemTile tile;

        public int Index { get; private set; }

        public static CellView Create(Transform parent, int index, BoardView board)
        {
            var slot = UiKit.Panel(parent, "Cell" + index, Palette.Slot, 22);
            var view = slot.gameObject.AddComponent<CellView>();
            view.Index = index;
            view.board = board;

            view.ring = UiKit.Panel(slot.transform, "Ring", Palette.Rose, 24);
            view.ring.raycastTarget = false;
            view.ring.rectTransform.Stretch(1, 1, 1, 1);

            view.tile = new ItemTile(slot.transform, "Tile", 44, 18);
            view.tile.Root.Stretch(7, 7, 7, 7);

            view.wantedDot = UiKit.Icon(slot.transform, "Wanted", UiSprites.Circle, Palette.SageDark, 22);
            view.wantedDot.rectTransform.Pin(new Vector2(1, 1), new Vector2(-4, -4), new Vector2(22, 22));
            return view;
        }

        public void Show(Item item, ItemLook look, bool selected, bool canMerge, bool wanted, bool dragging)
        {
            if (item == null) tile.Hide();
            else tile.Show(item, look);
            tile.SetAlpha(dragging ? 0.3f : 1f);
            ring.enabled = selected || canMerge;
            ring.color = selected ? Palette.Rose : Palette.Lilac;
            wantedDot.enabled = wanted;
        }

        public RectTransform Tile => tile.Root;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging) board.Click(Index);
        }

        public void OnBeginDrag(PointerEventData eventData) => board.BeginDrag(Index, eventData);

        public void OnDrag(PointerEventData eventData) => board.Drag(eventData);

        public void OnEndDrag(PointerEventData eventData) => board.EndDrag(eventData);
    }
}
