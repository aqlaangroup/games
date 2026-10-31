using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gahwit.Cafe
{
    // Tutorial step 2 (COMPLETE_SPEC.md section 1): a 15-second intro shot in the engine. The dusty café lies dark behind
    // its shutter, with dust in the slits of light. Salma rolls the shutter up and morning floods in over the piled chairs
    // and the tarnished dallahs; she comes in at the door, looks around, and goes behind the counter while the camera draws
    // back to the whole room. With reduced motion the camera stays at the room's usual view.
    public sealed partial class CafeRoom
    {
        public const float IntroSeconds = 15f;
        bool introSkipped;

        public void SkipIntro() => introSkipped = true;

        // sound: plays a cue by id; fade: how dark the screen is, 1 to 0, for the opening fade from black.
        public IEnumerator Intro(bool calm, Action<string> sound, Action<float> fade)
        {
            introSkipped = false;
            var camera = cam.GetComponent<CafeCamera>();
            var props = Group("Intro", transform, Vector3.zero);
            // She opens up in the morning, whatever the clock says; the café's own time comes back after.
            var timeBefore = time;
            time = Presets[TimeOfDay.Morning];
            ApplyTime();
            var preset = time;
            var ambient = (sky: RenderSettings.ambientSkyColor, equator: RenderSettings.ambientEquatorColor, ground: RenderSettings.ambientGroundColor);

            // The shutter over the window: corrugated slats that roll up into a box at the top.
            var shutter = Group("Shutter", props, new Vector3(-1.6f, 0, -D / 2 + 0.22f));
            var slats = Group("Slats", shutter, Vector3.zero);
            var grey = Mat("#8A8F98", 0.45f, 0.6f);
            var greyDark = Mat("#727780", 0.45f, 0.6f);
            for (int i = 0; i < 12; i++)
                Box("Slat", slats, new Vector3(2.35f, 0.17f, 0.05f), new Vector3(0, 1.5f + i * 0.17f, 0), i % 2 == 0 ? grey : greyDark);
            Box("Roll", shutter, new Vector3(2.5f, 0.32f, 0.32f), new Vector3(0, 3.62f, 0), greyDark);
            var glow = Glow("#FFE2B0");
            var slits = Group("Slits", shutter, Vector3.zero);
            for (int i = 1; i < 12; i += 2)
                Box("Slit", slits, new Vector3(2.0f, 0.012f, 0.06f), new Vector3(0, 1.415f + i * 0.17f, 0.01f), glow, castShadows: false);

            // Morning through the window once the shutter is up, and dust that shows in it.
            var beam = new GameObject("MorningBeam").AddComponent<Light>();
            beam.transform.SetParent(props, false);
            beam.transform.localPosition = new Vector3(-1.6f, 3.0f, -D / 2 + 0.6f);
            beam.transform.LookAt(transform.TransformPoint(new Vector3(0.6f, 0, 1.2f)));
            beam.type = LightType.Spot;
            beam.spotAngle = 75f;
            beam.range = 11f;
            beam.color = Hex("#FFD9A8");
            beam.intensity = 0f;
            var dust = BuildDust(props);

            // Salma comes in from the street.
            var salma = Salma.transform;
            var salmaHome = (salma.localPosition, salma.localRotation);
            Vector3[] path =
            {
                new Vector3(-W / 2 - 0.9f, 0, 2.2f), DoorInside, new Vector3(-0.6f, 0, 0.9f), new Vector3(0.55f, 0, -3.0f),
                new Vector3(0.75f, 0, -3.7f), salmaHome.localPosition
            };
            salma.localPosition = path[0];
            var doorHome = (doorRoot.localPosition, doorRoot.localRotation);
            var hinge = doorRoot.TransformPoint(new Vector3(-0.75f, 0, 0));
            float doorAngle = 0f;

            bool shutterSound = false, doorSound = false;
            for (float t = 0; t < IntroSeconds && !introSkipped; t += Time.unscaledDeltaTime)
            {
                fade(1f - Smooth(0f, 1.2f, t));

                // The shutter rises from 2.2 s to 5.2 s; the room wakes up from 2.5 s to 7 s.
                if (!shutterSound && t >= 2.2f) { shutterSound = true; sound("shutter_open"); }
                float up = Smooth(2.2f, 5.2f, t);
                slats.localPosition = new Vector3(0, up * 2.1f, 0);
                for (int i = 0; i < slats.childCount; i++)
                {
                    var slat = slats.GetChild(i);
                    slat.gameObject.SetActive(slat.localPosition.y + up * 2.1f < 3.5f); // rolled into the box
                }
                slits.gameObject.SetActive(up <= 0f);
                float dawn = Smooth(2.5f, 7f, t);
                sun.intensity = Mathf.Lerp(0.12f, preset.SunIntensity, dawn);
                float dim = Mathf.Lerp(0.3f, 1f, dawn);
                RenderSettings.ambientSkyColor = ambient.sky * dim;
                RenderSettings.ambientEquatorColor = ambient.equator * dim;
                RenderSettings.ambientGroundColor = ambient.ground * dim;
                beam.intensity = 4f * dawn * (1f - 0.6f * Smooth(10f, 14f, t));
                var emission = dust.emission;
                emission.rateOverTime = Mathf.Lerp(6f, 45f, dawn);

                // The door swings in at 5.5 s and closes behind her.
                if (!doorSound && t >= 5.5f) { doorSound = true; sound("door_creak"); }
                float open = Smooth(5.5f, 6.5f, t) * (1f - Smooth(8.6f, 9.6f, t));
                float angle = -80f * open;
                doorRoot.RotateAround(hinge, Vector3.up, angle - doorAngle);
                doorAngle = angle;

                // Salma: in at the door (6 to 9 s), a look round the room (9 to 11 s), then behind the counter (11 to 14 s).
                MoveSalma(salma, path, t);

                if (!calm && t < 11f) camera.Hold(CameraAt(t), LookAt(t));
                else camera.Release();
                yield return null;
            }

            // Leave the room as the game expects it, whether it played through or was skipped.
            fade(0f);
            camera.Release();
            doorRoot.localPosition = doorHome.localPosition;
            doorRoot.localRotation = doorHome.localRotation;
            salma.localPosition = salmaHome.localPosition;
            salma.localRotation = salmaHome.localRotation;
            Destroy(props.gameObject);
            time = timeBefore;
            ApplyTime();
        }

        // 0 before start, 1 after end, an S-curve between.
        static float Smooth(float start, float end, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, t));

        // Each leg of her walk and its seconds.
        static readonly (float from, float to, int leg)[] Legs = { (6f, 7.4f, 0), (7.4f, 9f, 1), (11f, 12.3f, 2), (12.3f, 13f, 3), (13f, 14f, 4) };

        static void MoveSalma(Transform salma, Vector3[] path, float t)
        {
            // She faces the way she walks, and turns to look round the room in the middle.
            foreach (var (from, to, leg) in Legs)
            {
                if (t < from || t > to) continue;
                float k = Mathf.InverseLerp(from, to, t);
                var a = path[leg];
                var b = path[leg + 1];
                salma.localPosition = Vector3.Lerp(a, b, k) + new Vector3(0, Mathf.Abs(Mathf.Sin(k * Mathf.PI * 6f)) * 0.03f, 0);
                var way = b - a;
                if (way.sqrMagnitude > 0.001f) salma.localRotation = Quaternion.LookRotation(way.normalized);
                return;
            }
            if (t > 9f && t < 11f)
                salma.localRotation = Quaternion.Euler(0, 140f + Mathf.Sin((t - 9f) / 2f * Mathf.PI) * 60f, 0);
            else if (t >= 14f)
                salma.localRotation = Quaternion.LookRotation(Vector3.forward);
        }

        // From close on the shuttered window, back and round to a three-quarter view of Salma in the room.
        static Vector3 CameraAt(float t)
        {
            var close = new Vector3(0.6f, 1.9f, 1.4f);
            var near = new Vector3(0.1f, 2.1f, -0.2f);
            var wide = new Vector3(5.5f, 6.0f, 8.5f);
            return t < 4f ? Vector3.Lerp(close, near, Smooth(0f, 4f, t)) : Vector3.Lerp(near, wide, Smooth(4f, 10.5f, t));
        }

        static Vector3 LookAt(float t)
        {
            var window = new Vector3(-1.6f, 2.3f, -D / 2);
            var room = new Vector3(-0.6f, 0.9f, 0.4f);
            return Vector3.Lerp(window, room, Smooth(4.5f, 10f, t));
        }

        // Dust motes drifting through the room, seen only where light falls.
        ParticleSystem BuildDust(Transform parent)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(-0.5f, 1.8f, -0.5f);
            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = 0.03f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            main.maxParticles = 350;
            main.startColor = new Color(1f, 0.93f, 0.8f, 0.45f);
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 3f, 6f);
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.06f;
            noise.frequency = 0.3f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var material = new Material(Resources.Load<Material>("Materials/CafeRain")) { name = "Intro dust" };
            material.SetTexture("_BaseMap", SoftDot());
            renderer.sharedMaterial = material;
            system.Play();
            return system;
        }

        // A round dot that fades to nothing at its edge, so a mote of dust looks soft.
        static Texture2D SoftDot()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Soft dot", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
