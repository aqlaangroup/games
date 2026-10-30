using Gahwit.Cafe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gahwit.UI
{
    // An invisible layer over the café that turns a drag into a camera orbit.
    public sealed class OrbitPad : MonoBehaviour, IDragHandler
    {
        public CafeCamera Target;

        public void OnDrag(PointerEventData eventData) => Target.Orbit(eventData.delta);
    }
}
