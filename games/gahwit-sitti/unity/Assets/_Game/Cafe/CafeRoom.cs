using System;
using System.Collections.Generic;
using Gahwit.Core;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gahwit.Cafe
{
    public enum TimeOfDay { Morning, Evening, Night }

    // The café room built from simple shapes, ported from cafe-3d/index.html, until the 3D art arrives.
    // Each chapter 1 task changes one part of the room; Apply() shows the room for the tasks finished so far.
    public sealed class CafeRoom : MonoBehaviour
    {
        const float W = 10f, D = 8f, H = 4.6f;

        sealed class TimePreset
        {
            public Color Background, Sky, Sun;
            public float SunIntensity, Ambient, Warm, Bulbs;
            public Vector3 SunFrom;
        }

        static readonly Dictionary<TimeOfDay, TimePreset> Presets = new Dictionary<TimeOfDay, TimePreset>
        {
            [TimeOfDay.Morning] = new TimePreset { Background = Hex("#F3E4E4"), Sky = Hex("#D7E4F2"), Sun = Hex("#FFE9D6"), SunIntensity = 1.3f, SunFrom = new Vector3(6, 9, 6), Ambient = 1f, Warm = 0f, Bulbs = 0.6f },
            [TimeOfDay.Evening] = new TimePreset { Background = Hex("#F1D9CF"), Sky = Hex("#F6C7A6"), Sun = Hex("#FFC48E"), SunIntensity = 1.0f, SunFrom = new Vector3(-7, 4.5f, 5), Ambient = 0.8f, Warm = 1.5f, Bulbs = 2f },
            [TimeOfDay.Night] = new TimePreset { Background = Hex("#2B2540"), Sky = Hex("#2E3560"), Sun = Hex("#9FB0E8"), SunIntensity = 0.25f, SunFrom = new Vector3(-4, 8, 3), Ambient = 0.45f, Warm = 3f, Bulbs = 4f }
        };

        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly List<Light> lampLights = new List<Light>();
        readonly List<Renderer> shelfDallahs = new List<Renderer>();
        const float PastFadeSeconds = 1.2f;

        Material litTemplate, floorMat, glassMat, bulbMat, brass, tarnished, drawerMat, signBoardMat, chairSeatMat, chairBackMat;
        Light sun, signLight, counterLight;
        Camera cam;
        Transform plaque, drawer;
        Volume pastVolume;
        float pastTarget;
        bool drawerGlowing;
        RTLTextMeshPro3D signText;
        GameObject chairPile, tables, lights, counterSet;
        TimePreset time;
        bool lightsOn;

        public static TimeOfDay TimeFromClock(DateTime local) =>
            local.Hour >= 6 && local.Hour < 17 ? TimeOfDay.Morning : local.Hour >= 17 && local.Hour < 20 ? TimeOfDay.Evening : TimeOfDay.Night;

        public void Build(Light sunLight, Camera camera)
        {
            sun = sunLight;
            cam = camera;
            litTemplate = GraphicsSettings.currentRenderPipeline.defaultMaterial;
            brass = Mat("#D9B06C", 0.6f, 0.6f);
            tarnished = Mat("#7D6F62", 0.1f);

            BuildRoom();
            BuildWindowAndDoor();
            BuildSign();
            BuildCounterAndShelf();
            BuildTables();
            BuildLights();
            BuildPostProcessing();
        }

        // ---------- Task states ----------

        // Tasks with a style choice (sign, chairs) take their colours from the chosen style.
        public void Apply(ICollection<string> done, IDictionary<string, string> taskStyles = null, IDictionary<string, StyleDef> styles = null)
        {
            var signStyle = StyleFor("sign", taskStyles, styles);
            bool sign = done.Contains("sign");
            plaque.localRotation = Quaternion.Euler(0, 0, sign ? 0 : 9);
            signBoardMat.SetColor("_BaseColor", sign && signStyle != null ? Hex(signStyle.SignBoard) : Hex("#3A2731"));
            signText.color = !sign ? Hex("#8E8078") : signStyle != null ? Hex(signStyle.SignText) : Hex("#F6D9A0");
            signLight.enabled = sign;

            var chairStyle = StyleFor("chairs", taskStyles, styles);
            chairSeatMat.SetColor("_BaseColor", Hex(chairStyle?.Wood ?? "#C99E83"));
            chairBackMat.SetColor("_BaseColor", Hex(chairStyle?.Accent ?? "#C99E83"));

            floorMat.SetColor("_BaseColor", done.Contains("floor") ? Color.white : Hex("#A99C93"));

            tables.SetActive(done.Contains("chairs"));
            chairPile.SetActive(!done.Contains("chairs"));

            var dallahMaterial = done.Contains("dallahs") ? brass : tarnished;
            foreach (var r in shelfDallahs) r.sharedMaterial = dallahMaterial;

            lightsOn = done.Contains("lights");
            lights.SetActive(lightsOn);

            counterSet.SetActive(done.Contains("counter"));
            counterLight.enabled = done.Contains("counter");

            ApplyTime();
        }

        static StyleDef StyleFor(string task, IDictionary<string, string> taskStyles, IDictionary<string, StyleDef> styles) =>
            taskStyles != null && styles != null && taskStyles.TryGetValue(task, out var id) && styles.TryGetValue(id, out var style) ? style : null;

        public void SetTimeOfDay(TimeOfDay timeOfDay)
        {
            time = Presets[timeOfDay];
            ApplyTime();
        }

        // 1974: warm faded film colours and grain (CONCEPT.md, Look), faded in and out.
        public void SetPast(bool past) => pastTarget = past ? 1f : 0f;

        // The counter's middle drawer glows while a letter waits in it.
        public void SetDrawerGlow(bool glow)
        {
            drawerGlowing = glow;
            if (!glow) drawerMat.SetColor("_EmissionColor", Color.black);
        }

        public Vector3 DrawerPosition => drawer.position + new Vector3(0, 0.1f, 0.1f);

        void Update()
        {
            if (pastVolume != null && !Mathf.Approximately(pastVolume.weight, pastTarget))
                pastVolume.weight = Mathf.MoveTowards(pastVolume.weight, pastTarget, Time.unscaledDeltaTime / PastFadeSeconds);
            if (drawerGlowing)
                drawerMat.SetColor("_EmissionColor", Hex("#E9A0AE") * (0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f)));
        }

        void ApplyTime()
        {
            if (time == null) time = Presets[TimeOfDay.Morning];
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = time.Background;
            glassMat.SetColor("_BaseColor", time.Sky);
            sun.color = time.Sun;
            sun.intensity = time.SunIntensity;
            sun.transform.rotation = Quaternion.LookRotation(-time.SunFrom.normalized);
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#FFF6F2") * time.Ambient;
            RenderSettings.ambientEquatorColor = Hex("#EED1CC") * time.Ambient;
            RenderSettings.ambientGroundColor = Hex("#D9B9B4") * time.Ambient;

            float warm = lightsOn ? time.Warm : time.Warm * 0.3f;
            foreach (var lamp in lampLights) lamp.intensity = warm;
            counterLight.intensity = Mathf.Max(0.6f, warm);
            bulbMat.SetColor("_EmissionColor", Hex("#FFCF7A") * time.Bulbs);
        }

        // ---------- Room ----------

        void BuildRoom()
        {
            floorMat = new Material(litTemplate) { name = "Cafe floor" };
            floorMat.SetTexture("_BaseMap", TileTexture());
            floorMat.SetTextureScale("_BaseMap", new Vector2(10, 8));
            floorMat.SetFloat("_Smoothness", 0.2f);
            Box("Floor", transform, new Vector3(W, 0.2f, D), new Vector3(0, -0.1f, 0), floorMat, castShadows: false);
            Box("Base", transform, new Vector3(W + 0.4f, 0.5f, D + 0.4f), new Vector3(0, -0.45f, 0), Mat("#E9D3CC"), castShadows: false);

            Wall("BackWall", W, new Vector3(0, 0, -D / 2 - 0.1f), 0);
            Wall("SideWall", D, new Vector3(-W / 2 - 0.1f, 0, 0), 90);
        }

        void Wall(string name, float width, Vector3 position, float rotationY)
        {
            var wall = Group(name, transform, position, rotationY);
            Box("Upper", wall, new Vector3(width, H, 0.2f), new Vector3(0, H / 2, 0), Mat("#EED1CC"), castShadows: false);
            Box("Lower", wall, new Vector3(width, 1.2f, 0.24f), new Vector3(0, 0.6f, 0), Mat("#E2B3AE"), castShadows: false);
            Box("Rail", wall, new Vector3(width, 0.08f, 0.3f), new Vector3(0, 1.22f, 0), Mat("#FFFFFF"));
        }

        void BuildWindowAndDoor()
        {
            var white = Mat("#FFFFFF");
            glassMat = new Material(litTemplate) { name = "Cafe glass" };
            glassMat.SetFloat("_Smoothness", 0.9f);

            var window = Group("Window", transform, new Vector3(-1.6f, 0, -D / 2 + 0.02f));
            Box("Frame", window, new Vector3(2.2f, 1.8f, 0.12f), new Vector3(0, 2.35f, 0.06f), white);
            Disc("FrameArch", window, 1.1f, 0.12f, new Vector3(0, 3.25f, 0.06f), white);
            Box("Glass", window, new Vector3(1.9f, 1.7f, 0.02f), new Vector3(0, 2.43f, 0.14f), glassMat);
            Disc("GlassArch", window, 0.95f, 0.02f, new Vector3(0, 3.28f, 0.14f), glassMat);
            Box("Mullion", window, new Vector3(0.06f, 2.4f, 0.05f), new Vector3(0, 2.8f, 0.17f), white);
            Box("Sill", window, new Vector3(2.6f, 0.12f, 0.45f), new Vector3(0, 1.42f, 0.2f), white);

            var door = Group("Door", transform, new Vector3(-W / 2 + 0.02f, 0, 2.2f), 90);
            var lilac = Mat("#D8CDEB");
            Box("Panel", door, new Vector3(1.5f, 2.05f, 0.1f), new Vector3(0, 1.03f, 0.05f), lilac);
            Disc("Arch", door, 0.75f, 0.1f, new Vector3(0, 2.05f, 0.05f), lilac);
            Ball("Knob", door, 0.07f, new Vector3(0.45f, 1.25f, 0.14f), brass);
        }

        void BuildSign()
        {
            plaque = Group("SignPlaque", transform, new Vector3(2.7f, 3.55f, -D / 2 + 0.08f));
            signBoardMat = new Material(Mat("#3A2731", 0.3f)) { name = "Cafe sign board" };
            Box("Board", plaque, new Vector3(2.4f, 0.75f, 0.06f), Vector3.zero, signBoardMat);

            var textObject = new GameObject("SignText", typeof(RectTransform));
            textObject.transform.SetParent(plaque, false);
            textObject.transform.localPosition = new Vector3(0, 0, 0.04f);
            textObject.transform.localRotation = Quaternion.Euler(0, 180, 0); // face the room, away from the wall
            signText = textObject.AddComponent<RTLTextMeshPro3D>();
            signText.Farsi = false;
            signText.font = GameFonts.Arabic;
            signText.rectTransform.sizeDelta = new Vector2(2.1f, 0.6f);
            signText.enableAutoSizing = true;
            signText.fontSizeMin = 0.5f;
            signText.fontSizeMax = 6f;
            signText.alignment = TextAlignmentOptions.Center;
            signText.text = Loc.T("cafe.sign");

            signLight = Point("SignLight", transform, new Vector3(2.7f, 3.3f, -D / 2 + 1.2f), Hex("#FFD9A0"), 1.2f, 3f);
        }

        void BuildCounterAndShelf()
        {
            var counter = Group("Counter", transform, new Vector3(2.7f, 0, -2.9f));
            Box("Body", counter, new Vector3(3.6f, 1.1f, 1f), new Vector3(0, 0.55f, 0), Mat("#FFFFFF"));
            Box("Top", counter, new Vector3(3.8f, 0.1f, 1.15f), new Vector3(0, 1.15f, 0), Mat("#F2D3D6", 0.4f));
            drawerMat = new Material(Mat("#FAF1F0")) { name = "Cafe drawer" };
            drawerMat.EnableKeyword("_EMISSION");
            drawerMat.SetColor("_EmissionColor", Color.black);
            foreach (float x in new[] { -1.1f, 0f, 1.1f })
            {
                var front = Box("Drawer", counter, new Vector3(0.9f, 0.45f, 0.04f), new Vector3(x, 0.6f, 0.51f), x == 0f ? drawerMat : Mat("#FAF1F0"));
                if (x == 0f) drawer = front.transform;
                Box("Handle", counter, new Vector3(0.3f, 0.05f, 0.05f), new Vector3(x, 0.62f, 0.55f), brass);
            }

            counterSet = Group("CounterSet", counter, Vector3.zero).gameObject;
            Dallah(counterSet.transform, new Vector3(-1.2f, 1.2f, 0), 0.55f, brass);
            var cup = Mat("#FFFFFF", 0.6f);
            foreach (float x in new[] { 0f, 0.35f })
                Cylinder("Cup", counterSet.transform, 0.09f, 0.14f, new Vector3(x, 1.27f, 0.1f), cup);
            counterLight = Point("CounterLight", transform, new Vector3(2.7f, 3f, -2.2f), Hex("#FFC98A"), 1f, 6f);

            Box("Shelf", transform, new Vector3(2.6f, 0.08f, 0.4f), new Vector3(2.7f, 2.6f, -D / 2 + 0.22f), Mat("#A57A62"));
            for (int i = 0; i < 3; i++)
                shelfDallahs.AddRange(Dallah(transform, new Vector3(2.7f + (i - 1) * 0.8f, 2.64f, -D / 2 + 0.25f), 0.42f + i * 0.04f, tarnished));
        }

        void BuildTables()
        {
            tables = Group("Tables", transform, Vector3.zero).gameObject;
            var white = Mat("#FFFFFF");
            var woodDark = Mat("#A57A62");
            var wood = Mat("#C99E83");
            chairSeatMat = new Material(wood) { name = "Cafe chair seat" };
            chairBackMat = new Material(wood) { name = "Cafe chair back" };
            foreach (var spot in new[] { new Vector2(-2.3f, 0.3f), new Vector2(0.9f, 1.9f), new Vector2(-0.7f, -1.6f) })
            {
                var table = Group("Table", tables.transform, new Vector3(spot.x, 0, spot.y));
                Cylinder("Top", table, 0.6f, 0.07f, new Vector3(0, 0.78f, 0), white);
                Cylinder("Leg", table, 0.06f, 0.76f, new Vector3(0, 0.39f, 0), woodDark);
                Cylinder("Foot", table, 0.31f, 0.04f, new Vector3(0, 0.02f, 0), woodDark);
                foreach (int side in new[] { -1, 1 })
                    Chair(tables.transform, new Vector3(spot.x + side * 1.05f, 0, spot.y), side, chairSeatMat, chairBackMat, woodDark);
            }

            // Before the "chairs" task: the chairs stacked in a corner.
            chairPile = Group("ChairPile", transform, new Vector3(-3.9f, 0, -2.8f)).gameObject;
            for (int i = 0; i < 4; i++)
            {
                var chair = Chair(chairPile.transform, new Vector3(0.1f * i, 0.42f * i, 0.05f * i), i % 2 == 0 ? 1 : -1, wood, wood, woodDark);
                chair.localRotation = Quaternion.Euler(i * 6f, i * 37f, i % 2 == 0 ? 8f : -6f);
            }
        }

        Transform Chair(Transform parent, Vector3 position, int side, Material seat, Material back, Material legs)
        {
            var chair = Group("Chair", parent, position);
            Cylinder("Seat", chair, 0.32f, 0.08f, new Vector3(0, 0.46f, 0), seat);
            Cylinder("Leg", chair, 0.04f, 0.44f, new Vector3(0, 0.22f, 0), legs);
            Box("Back", chair, new Vector3(0.06f, 0.55f, 0.5f), new Vector3(side * 0.3f, 0.78f, 0), back);
            return chair;
        }

        void BuildLights()
        {
            lights = Group("Lights", transform, Vector3.zero).gameObject;
            bulbMat = new Material(Mat("#FFF1D0")) { name = "Cafe bulbs" };
            bulbMat.EnableKeyword("_EMISSION");
            bulbMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            for (int i = 0; i < 11; i++)
            {
                float t = i / 10f;
                var position = new Vector3(-W / 2 + 0.3f + t * (W - 0.6f), 4.25f - Mathf.Sin(t * Mathf.PI) * 0.45f, -D / 2 + 0.5f);
                Ball("Bulb", lights.transform, 0.07f, position, bulbMat, castShadows: false);
            }

            var shade = new Material(Mat("#F6E4C4")) { name = "Cafe lampshade" };
            shade.EnableKeyword("_EMISSION");
            shade.SetColor("_EmissionColor", Hex("#FFC98A") * 0.6f);
            var pole = Mat("#A57A62");
            foreach (var spot in new[] { new Vector3(-4.3f, 0, -3.3f), new Vector3(4.3f, 0, 1.6f) })
            {
                var lamp = Group("Lamp", lights.transform, spot);
                Cylinder("Pole", lamp, 0.03f, 1.6f, new Vector3(0, 0.8f, 0), pole);
                Cylinder("Foot", lamp, 0.24f, 0.05f, new Vector3(0, 0.025f, 0), pole);
                Cylinder("Shade", lamp, 0.26f, 0.36f, new Vector3(0, 1.62f, 0), shade);
                lampLights.Add(Point("LampLight", lamp, new Vector3(0, 1.5f, 0), Hex("#FFC98A"), 1f, 4.5f));
            }

            var potMat = Mat("#E7B4BE");
            var leaf = Mat("#9DBFA6");
            var sage = Mat("#B9D1BE");
            foreach (var spot in new[] { new Vector3(-0.2f, 0, -3.5f), new Vector3(-4.4f, 0, 0.6f) })
            {
                var plant = Group("Plant", lights.transform, spot);
                Cylinder("Pot", plant, 0.25f, 0.45f, new Vector3(0, 0.22f, 0), potMat);
                for (int k = 0; k < 7; k++)
                {
                    float a = k / 7f * Mathf.PI * 2;
                    var leafBall = Ball("Leaf", plant, 0.22f, new Vector3(Mathf.Cos(a) * 0.18f, 0.7f + k % 3 * 0.12f, Mathf.Sin(a) * 0.18f), k % 2 == 0 ? sage : leaf);
                    leafBall.transform.localScale = new Vector3(0.31f, 0.62f, 0.31f);
                }
            }
        }

        void BuildPostProcessing()
        {
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.6f);
            bloom.threshold.Override(0.9f);
            profile.Add<Vignette>().intensity.Override(0.22f);
            var volume = new GameObject("CafePostProcessing").AddComponent<Volume>();
            volume.transform.SetParent(transform, false);
            volume.isGlobal = true;
            volume.priority = 10;
            volume.sharedProfile = profile;

            var past = ScriptableObject.CreateInstance<VolumeProfile>();
            var colour = past.Add<ColorAdjustments>();
            colour.saturation.Override(-65f);
            colour.colorFilter.Override(Hex("#F3D6AE"));
            colour.contrast.Override(8f);
            colour.postExposure.Override(0.15f);
            var grain = past.Add<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.45f);
            past.Add<Vignette>().intensity.Override(0.38f);
            pastVolume = new GameObject("PastPostProcessing").AddComponent<Volume>();
            pastVolume.transform.SetParent(transform, false);
            pastVolume.isGlobal = true;
            pastVolume.priority = 20;
            pastVolume.weight = 0;
            pastVolume.sharedProfile = past;
        }

        // ---------- Shapes ----------

        List<Renderer> Dallah(Transform parent, Vector3 position, float s, Material material)
        {
            var dallah = Group("Dallah", parent, position);
            var parts = new List<Renderer>
            {
                Cylinder("Body", dallah, 0.3f * s, 0.45f * s, new Vector3(0, 0.225f * s, 0), material).GetComponent<Renderer>(),
                Ball("Shoulder", dallah, 0.24f * s, new Vector3(0, 0.48f * s, 0), material).GetComponent<Renderer>(),
                Cylinder("Neck", dallah, 0.12f * s, 0.3f * s, new Vector3(0, 0.7f * s, 0), material).GetComponent<Renderer>(),
                Ball("Lid", dallah, 0.15f * s, new Vector3(0, 0.88f * s, 0), material).GetComponent<Renderer>()
            };
            var spout = Cylinder("Spout", dallah, 0.035f * s, 0.5f * s, new Vector3(0.32f * s, 0.55f * s, 0), material);
            spout.transform.localRotation = Quaternion.Euler(0, 0, -57f);
            parts.Add(spout.GetComponent<Renderer>());
            return parts;
        }

        Texture2D TileTexture()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Cafe tile", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            Color baseColor = Hex("#F1E1DA"), sage = Hex("#A9C7B2"), rose = Hex("#DFA0AD"), line = Hex("#E2D2CB");
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Min(x, size - x), cy = Mathf.Min(y, size - y);
                float corner = Mathf.Sqrt(cx * cx + cy * cy);
                float diamond = Mathf.Abs(x - 128) + Mathf.Abs(y - 128);
                float centre = Mathf.Sqrt((x - 128) * (x - 128) + (y - 128) * (y - 128));
                Color c = baseColor;
                if (corner < 70) c = sage;
                if (diamond < 78) c = rose;
                if (centre < 26) c = baseColor;
                if (x < 2 || y < 2) c = line;
                pixels[y * size + x] = c;
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        Material Mat(string hex, float smoothness = 0.12f, float metallic = 0f)
        {
            string key = $"{hex}/{smoothness}/{metallic}";
            if (materials.TryGetValue(key, out var material)) return material;
            material = new Material(litTemplate) { name = "Cafe " + hex };
            material.SetColor("_BaseColor", Hex(hex));
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            materials[key] = material;
            return material;
        }

        static Transform Group(string name, Transform parent, Vector3 position, float rotationY = 0)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            group.localRotation = Quaternion.Euler(0, rotationY, 0);
            return group;
        }

        static GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool castShadows)
        {
            var shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            Destroy(shape.GetComponent<Collider>());
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            var renderer = shape.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return shape;
        }

        static GameObject Box(string name, Transform parent, Vector3 size, Vector3 position, Material material, bool castShadows = true) =>
            Shape(PrimitiveType.Cube, name, parent, position, size, material, castShadows);

        static GameObject Cylinder(string name, Transform parent, float radius, float height, Vector3 position, Material material) =>
            Shape(PrimitiveType.Cylinder, name, parent, position, new Vector3(radius * 2, height / 2, radius * 2), material, true);

        static GameObject Ball(string name, Transform parent, float radius, Vector3 position, Material material, bool castShadows = true) =>
            Shape(PrimitiveType.Sphere, name, parent, position, Vector3.one * radius * 2, material, castShadows);

        // A flat disc facing along z, used for the arched tops of the window and door.
        static GameObject Disc(string name, Transform parent, float radius, float depth, Vector3 position, Material material)
        {
            var disc = Shape(PrimitiveType.Cylinder, name, parent, position, new Vector3(radius * 2, depth / 2, radius * 2), material, false);
            disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
            return disc;
        }

        static Light Point(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
    }
}
