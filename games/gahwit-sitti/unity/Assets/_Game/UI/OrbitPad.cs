using System;
using Gahwit.Cafe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gahwit.UI
{
    // An invisible layer over the café. A drag orbits the camera and a double tap focuses on a spot. In decor mode,
    // pressing a piece grabs it and dragging moves it instead.
    public sealed class OrbitPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
    {
        const float DoubleTapSeconds = 0.3f;

        public CafeCamera Target;
        public Func<Vector2, bool> PressPiece;  // decor mode: true when a piece was grabbed
        public Action<Vector2> DragPiece;
        public Action<Vector2> Tap;
        public Action<Vector2> DoubleTap;
        bool draggingPiece;
        float lastTap = -1;

        public void OnPointerDown(PointerEventData eventData) => draggingPiece = PressPiece != null && PressPiece(eventData.position);

        public void OnDrag(PointerEventData eventData)
        {
            if (draggingPiece) DragPiece?.Invoke(eventData.position);
            else Target.Orbit(eventData.delta);
        }

        public void OnPointerUp(PointerEventData eventData) => draggingPiece = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            if (Time.unscaledTime - lastTap < DoubleTapSeconds)
            {
                lastTap = -1;
                DoubleTap?.Invoke(eventData.position);
                return;
            }
            lastTap = Time.unscaledTime;
            Tap?.Invoke(eventData.position);
        }
    }
}
