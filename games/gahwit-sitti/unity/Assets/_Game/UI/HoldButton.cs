using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gahwit.UI
{
    // Reports press and release, for "hold to pour" style games. Leaving the button counts as letting go.
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action Pressed;
        public event Action Released;

        bool down;

        public void OnPointerDown(PointerEventData eventData)
        {
            down = true;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) => Let();

        public void OnPointerExit(PointerEventData eventData) => Let();

        void Let()
        {
            if (!down) return;
            down = false;
            Released?.Invoke();
        }
    }
}
