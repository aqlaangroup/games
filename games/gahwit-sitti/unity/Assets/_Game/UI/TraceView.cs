using System;
using Gahwit.Core;
using Gahwit.Memories;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Trace: a dotted seam on the fabric; follow it with a finger from the first stitch. Stitched dots turn into
    // filled teal dots, the rest stay hollow rings, so progress doesn't rely on colour alone.
    public sealed class TraceView : MemoryGameView, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const float FallbackDpi = 160f;

        TraceGame game;
        RectTransform fabric;
        Image[] dots;
        Image start;
        float unitsPerCm;
        Vector2 centreCm;

        public void Init()
        {
            Build("trace.title", 1200);
            var cloth = UiKit.Panel(Area, "Fabric", Palette.Hex("#F4EDE4"), 24);
            cloth.rectTransform.Stretch(0, 0, 0, 20);
            fabric = cloth.rectTransform;
        }

        public void Play(TraceGame trace, Action<int> done)
        {
            game = trace;
            UiKit.Clear(fabric);
            // Centimetres to canvas units: the screen's dots per inch, through the canvas's scale.
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            float dpi = Screen.dpi > 0 ? Screen.dpi : FallbackDpi;
            unitsPerCm = dpi / 2.54f / canvas.scaleFactor;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < game.Count; i++)
            {
                var p = game.Point(i);
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            centreCm = new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
            // A seam wider than the fabric is drawn smaller; the finger is measured the same way, so it still matches.
            var room = fabric.rect.size * 0.85f;
            unitsPerCm = Mathf.Min(unitsPerCm, room.x / Mathf.Max(0.1f, maxX - minX), room.y / Mathf.Max(0.1f, maxY - minY));
            dots = new Image[game.Count];
            for (int i = 0; i < game.Count; i++)
            {
                var p = game.Point(i);
                dots[i] = UiKit.Icon(fabric, "Stitch", UiSprites.Circle, Palette.InkSoft, 18);
                dots[i].rectTransform.anchoredPosition = ToUnits(p.x, p.y);
            }
            start = UiKit.Icon(fabric, "Start", UiSprites.Circle, new Color(0.72f, 0.29f, 0.38f, 0.4f), 70);
            start.rectTransform.anchoredPosition = ToUnits(game.Point(0).x, game.Point(0).y);
            Hint.text = Loc.T("trace.start");
            Begin(trace, done);
        }

        Vector2 ToUnits(float xCm, float yCm) => new Vector2(xCm - centreCm.x, -(yCm - centreCm.y)) * unitsPerCm;

        public void OnPointerDown(PointerEventData eventData) => Touch(eventData);

        public void OnDrag(PointerEventData eventData) => Touch(eventData);

        public void OnPointerUp(PointerEventData eventData) => game?.Lift();

        void Touch(PointerEventData eventData)
        {
            if (game == null || game.Finished) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(fabric, eventData.position, eventData.pressEventCamera, out var local);
            int before = game.Next;
            game.Touch(local.x / unitsPerCm + centreCm.x, -local.y / unitsPerCm + centreCm.y);
            if (game.Next != before && before == 0) Hint.text = "";
        }

        protected override void Draw()
        {
            start.enabled = !game.Started;
            for (int i = 0; i < dots.Length; i++)
            {
                bool stitched = game.Stitched(i);
                dots[i].color = stitched ? Palette.Teal : (i < game.Next ? Palette.RoseSoft : Palette.InkSoft);
                float size = stitched ? 22 : 14;
                if (dots[i].rectTransform.sizeDelta.x != size) dots[i].rectTransform.sizeDelta = new Vector2(size, size);
            }
        }
    }
}
