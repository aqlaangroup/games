using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Gahwit.Cafe
{
    // Orbits the café about 120° around the room, with pinch (or the mouse wheel) to zoom (SYSTEMS_B.md section 2).
    [RequireComponent(typeof(Camera))]
    public sealed class CafeCamera : MonoBehaviour
    {
        // Slightly toward the counter and sign, so the whole room fits a portrait screen.
        static readonly Vector3 Home = new Vector3(0.8f, 0.6f, -0.4f);
        const float FocusRadius = 13f;
        const float HomeTheta = 0.62f, HomePhi = 1.02f;
        const float ThetaHalfRange = 1.05f, PhiMin = 0.6f, PhiMax = 1.3f;
        const float RadiusMin = 11f, RadiusMax = 34f;
        const float Smoothing = 10f;

        Camera cam;
        float theta = HomeTheta, phi = HomePhi, radius, thetaGoal = HomeTheta, phiGoal = HomePhi, radiusGoal;
        float lastPinch, homeRadius;
        Vector3 target = Home, targetGoal = Home;
        bool visible = true;
        public bool Focused { get; private set; }

        void Awake()
        {
            cam = GetComponent<Camera>();
            bool portrait = cam.aspect < 0.75f;
            cam.fieldOfView = portrait ? 46f : 34f;
            radius = radiusGoal = homeRadius = portrait ? 28f : 17f;
            Place();
        }

        // While the kitchen covers the screen the café draws nothing, which saves the phone's GPU.
        public void SetVisible(bool show)
        {
            visible = show;
            cam.cullingMask = show ? ~0 : 0;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = show;
        }

        public void Orbit(Vector2 screenDelta)
        {
            if (TwoFingersDown()) return;
            float perPixel = Mathf.PI / Mathf.Max(1, Screen.width);
            thetaGoal = Mathf.Clamp(thetaGoal - screenDelta.x * perPixel, HomeTheta - ThetaHalfRange, HomeTheta + ThetaHalfRange);
            phiGoal = Mathf.Clamp(phiGoal + screenDelta.y * perPixel * 0.6f, PhiMin, PhiMax);
        }

        // Double-tap a table to look closer (SYSTEMS_B.md section 2); with reduced motion the camera cuts there.
        public void Focus(Vector3 point, bool cut)
        {
            targetGoal = new Vector3(point.x, Home.y, point.z);
            radiusGoal = FocusRadius;
            Focused = true;
            if (cut) Snap();
        }

        public void Unfocus(bool cut)
        {
            targetGoal = Home;
            radiusGoal = homeRadius;
            Focused = false;
            if (cut) Snap();
        }

        void Snap()
        {
            target = targetGoal;
            radius = radiusGoal;
            Place();
        }

        public void Zoom(float factor) => radiusGoal = Mathf.Clamp(radiusGoal * factor, RadiusMin, RadiusMax);

        void Update()
        {
            if (!visible) return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) Zoom(scroll > 0 ? 0.92f : 1.08f);
            }

            if (TwoFingersDown())
            {
                var touches = Touchscreen.current.touches;
                float distance = Vector2.Distance(touches[0].position.ReadValue(), touches[1].position.ReadValue());
                if (lastPinch > 0 && distance > 0) Zoom(lastPinch / distance);
                lastPinch = distance;
            }
            else lastPinch = 0;
        }

        void LateUpdate()
        {
            float k = 1 - Mathf.Exp(-Smoothing * Time.unscaledDeltaTime);
            theta = Mathf.Lerp(theta, thetaGoal, k);
            phi = Mathf.Lerp(phi, phiGoal, k);
            radius = Mathf.Lerp(radius, radiusGoal, k);
            target = Vector3.Lerp(target, targetGoal, k);
            Place();
        }

        void Place()
        {
            transform.position = target + new Vector3(
                radius * Mathf.Sin(phi) * Mathf.Sin(theta),
                radius * Mathf.Cos(phi),
                radius * Mathf.Sin(phi) * Mathf.Cos(theta));
            transform.LookAt(target);
        }

        static bool TwoFingersDown()
        {
            var screen = Touchscreen.current;
            return screen != null && screen.touches[0].press.isPressed && screen.touches[1].press.isPressed;
        }
    }
}
