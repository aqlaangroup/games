using System.Collections.Generic;
using Gahwit.Core;
using UnityEngine;

namespace Gahwit.Cafe
{
    // The café's other spaces (SYSTEMS_B.md section 1, missing/07), built from simple shapes like the floor until the 3D
    // art arrives: Teta's kitchen corner, the rooftop over the city (open once its stairs are built, chapter 6), and
    // Teta's back room (seen only in memories). Each stands far from the others on a layer of its own; the camera draws
    // one space at a time, and only that one is active.
    public sealed partial class CafeRoom
    {
        sealed class SpaceSetup
        {
            public Vector3 Origin; // where it stands, far from the others
            public Vector3 Look;   // the camera's home target, from the origin
            public float Zoom;     // the camera's home distance, as a share of the floor's
            public float Phi = 1.02f; // the camera's home angle from straight down; the rooftop's is lower, to see the sky
            public int Layer;
        }

        static readonly Dictionary<string, SpaceSetup> Setups = new Dictionary<string, SpaceSetup>
        {
            [ChapterContent.FloorSpace] = new SpaceSetup { Origin = Vector3.zero, Look = new Vector3(0.8f, 0.6f, -0.4f), Zoom = 1f, Layer = 0 },
            ["kitchen"] = new SpaceSetup { Origin = new Vector3(100, 0, 0), Look = new Vector3(0.2f, 0.8f, -0.5f), Zoom = 0.62f, Layer = 20 },
            ["rooftop"] = new SpaceSetup { Origin = new Vector3(200, 0, 0), Look = new Vector3(0.3f, 0.9f, -1.0f), Zoom = 1.0f, Phi = 1.22f, Layer = 21 },
            ["backroom"] = new SpaceSetup { Origin = new Vector3(300, 0, 0), Look = new Vector3(0.1f, 0.8f, -0.5f), Zoom = 0.6f, Layer = 22 },
        };
        const int OtherSpaces = (1 << 20) | (1 << 21) | (1 << 22);
        const string Rooftop = "rooftop";

        readonly Dictionary<string, Transform> spaceRoots = new Dictionary<string, Transform>();
        readonly List<Light> spaceLamps = new List<Light>();
        readonly List<(Material material, Color color)> glows = new List<(Material, Color)>();
        Material cityWindows;

        public string Space { get; private set; } = ChapterContent.FloorSpace;

        // Moves the camera to a space and draws only it; an unknown space shows the floor.
        public void ShowSpace(string id, bool cut)
        {
            if (id == null || !Setups.ContainsKey(id)) id = ChapterContent.FloorSpace;
            Space = id;
            foreach (var pair in spaceRoots) pair.Value.gameObject.SetActive(pair.Key == id);
            var setup = Setups[id];
            int mask = id == ChapterContent.FloorSpace ? ~OtherSpaces : 1 << setup.Layer;
            cam.GetComponent<CafeCamera>().GoTo(setup.Origin + setup.Look, setup.Zoom, setup.Phi, mask, cut);
            ApplyTime();
        }

        Transform SpaceRoot(string id) => id != null && spaceRoots.TryGetValue(id, out var root) ? root : transform;

        static int LayerOf(string id) => id != null && Setups.TryGetValue(id, out var setup) ? setup.Layer : 0;

        static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayer(child, layer);
        }

        // A light that shines (a task's string of bulbs, a lantern): brighter as the day gets darker (ApplyTime).
        Material Glow(string hex)
        {
            string key = "glow/" + hex;
            if (materials.TryGetValue(key, out var material)) return material;
            material = new Material(Mat(hex)) { name = "Cafe glow " + hex };
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            materials[key] = material;
            glows.Add((material, Hex(hex)));
            material.SetColor("_EmissionColor", Hex(hex) * Mathf.Max(0.8f, (time ?? Presets[TimeOfDay.Morning]).Bulbs));
            return material;
        }

        // Called from ApplyTime: each space's own lamps, its glowing parts, and the city's windows at dusk.
        void ApplySpaceTime()
        {
            cam.backgroundColor = Space == Rooftop ? time.Sky : time.Background;
            foreach (var lamp in spaceLamps) lamp.intensity = Mathf.Max(1.1f, time.Warm);
            foreach (var (material, color) in glows) material.SetColor("_EmissionColor", color * Mathf.Max(0.8f, time.Bulbs));
            if (cityWindows != null)
                cityWindows.SetColor("_EmissionColor", Hex("#FFD58A") * Mathf.Max(0f, time.Bulbs - 0.7f) * 0.7f);
        }

        void BuildSpaces()
        {
            BuildKitchenCorner(NewSpace("kitchen"));
            BuildRooftop(NewSpace(Rooftop));
            BuildBackRoom(NewSpace("backroom"));
            foreach (var pair in spaceRoots)
            {
                SetLayer(pair.Value, Setups[pair.Key].Layer);
                pair.Value.gameObject.SetActive(false);
            }
        }

        Transform NewSpace(string id)
        {
            var root = Group("Space " + id, transform, Setups[id].Origin);
            spaceRoots[id] = root;
            return root;
        }

        // Back and left walls, like the floor, so the camera sees in from the open sides.
        void SmallRoom(Transform root, float width, float depth, float height, Material floor, Material upper, Material lower, float lowerHeight)
        {
            Box("Floor", root, new Vector3(width, 0.2f, depth), new Vector3(0, -0.1f, 0), floor, castShadows: false);
            Box("Base", root, new Vector3(width + 0.4f, 0.5f, depth + 0.4f), new Vector3(0, -0.45f, 0), Mat("#E9D3CC"), castShadows: false);
            foreach (var (name, length, position, rotation) in new[]
                     {
                         ("BackWall", width, new Vector3(0, 0, -depth / 2 - 0.1f), 0f),
                         ("SideWall", depth, new Vector3(-width / 2 - 0.1f, 0, 0), 90f)
                     })
            {
                var wall = Group(name, root, position, rotation);
                Box("Upper", wall, new Vector3(length, height, 0.2f), new Vector3(0, height / 2, 0), upper, castShadows: false);
                Box("Lower", wall, new Vector3(length, lowerHeight, 0.24f), new Vector3(0, lowerHeight / 2, 0), lower, castShadows: false);
                Box("Rail", wall, new Vector3(length, 0.06f, 0.28f), new Vector3(0, lowerHeight + 0.02f, 0), Mat("#FFFFFF"));
            }
        }

        // A hanging bulb with its warm light, for the rooms without a window to the sky.
        void HangingBulb(Transform root, Vector3 at, float range)
        {
            Cylinder("Cord", root, 0.012f, 0.9f, at + new Vector3(0, 0.55f, 0), Mat("#3A2731"));
            Cylinder("Shade", root, 0.2f, 0.14f, at + new Vector3(0, 0.12f, 0), Mat("#B9853C", 0.5f, 0.6f));
            Ball("Bulb", root, 0.08f, at, Glow("#FFE2A8"), castShadows: false);
            spaceLamps.Add(Point("BulbLight", root, at - new Vector3(0, 0.15f, 0), Hex("#FFC98A"), 1.2f, range));
        }

        // ---------- Teta's kitchen corner (memory 5: the 1974 kitchen, the first roast) ----------

        void BuildKitchenCorner(Transform root)
        {
            const float w = 6f, d = 5f, h = 3.4f;
            var floor = new Material(litTemplate) { name = "Kitchen floor" };
            floor.SetTexture("_BaseMap", CheckerTexture());
            floor.SetTextureScale("_BaseMap", new Vector2(6, 5));
            floor.SetFloat("_Smoothness", 0.3f);
            var tiles = new Material(litTemplate) { name = "Kitchen tiles" };
            tiles.SetTexture("_BaseMap", GridTexture(Hex("#F7F3EC"), Hex("#A9C7B2"), 6));
            tiles.SetTextureScale("_BaseMap", new Vector2(10, 3));
            tiles.SetFloat("_Smoothness", 0.55f);
            SmallRoom(root, w, d, h, floor, Mat("#F3E3D3"), tiles, 1.55f);

            var sage = Mat("#8FB59C");
            var marble = Mat("#F4EEE8", 0.5f);
            var wood = Mat("#A57A62");
            var copper = Mat("#C47A4A", 0.55f, 0.7f);

            // The counter along the back wall: cupboards, the sink, and the old enamel stove.
            var counter = Group("Counter", root, new Vector3(0.2f, 0, -d / 2 + 0.35f));
            Box("Cupboards", counter, new Vector3(4.4f, 0.86f, 0.62f), new Vector3(0, 0.43f, 0), sage);
            Box("Top", counter, new Vector3(4.5f, 0.06f, 0.68f), new Vector3(0, 0.89f, 0.02f), marble);
            foreach (float x in new[] { -1.65f, -0.55f, 0.55f })
            {
                Box("Door", counter, new Vector3(0.95f, 0.66f, 0.03f), new Vector3(x, 0.45f, 0.32f), Mat("#9DC2A9"));
                Box("Handle", counter, new Vector3(0.04f, 0.16f, 0.04f), new Vector3(x + 0.35f, 0.55f, 0.35f), brass);
            }
            Box("Sink", counter, new Vector3(0.8f, 0.05f, 0.44f), new Vector3(-1.1f, 0.9f, 0.02f), Mat("#C9D3D6", 0.8f, 0.5f));
            Cylinder("Tap", counter, 0.025f, 0.32f, new Vector3(-1.1f, 1.06f, -0.22f), brass);
            var spout = Cylinder("Spout", counter, 0.02f, 0.22f, new Vector3(-1.1f, 1.2f, -0.13f), brass);
            spout.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var stove = Group("Stove", counter, new Vector3(1.75f, 0, 0.02f));
            Box("Body", stove, new Vector3(0.86f, 0.9f, 0.64f), new Vector3(0, 0.45f, 0), Mat("#FBF7F0", 0.6f));
            Box("Oven", stove, new Vector3(0.66f, 0.42f, 0.02f), new Vector3(0, 0.38f, 0.33f), Mat("#3A2731", 0.7f));
            foreach (var (x, z) in new[] { (-0.2f, -0.15f), (0.2f, -0.15f), (-0.2f, 0.15f), (0.2f, 0.15f) })
                Cylinder("Burner", stove, 0.12f, 0.03f, new Vector3(x, 0.92f, z), Mat("#2B2327"));
            // The dallah on its flame, and Teta's roasting pan with its long handle.
            Dallah(stove, new Vector3(-0.2f, 0.94f, 0.15f), 0.5f, brass);
            Ball("Flame", stove, 0.07f, new Vector3(-0.2f, 0.95f, 0.15f), Glow("#FF9A4A"), castShadows: false).transform.localScale = new Vector3(0.2f, 0.06f, 0.2f);
            Cylinder("RoastingPan", stove, 0.16f, 0.05f, new Vector3(0.2f, 0.96f, -0.15f), Mat("#4A3B35", 0.3f, 0.6f));
            var handle = Cylinder("PanHandle", stove, 0.015f, 0.6f, new Vector3(0.55f, 0.98f, -0.15f), Mat("#4A3B35", 0.3f, 0.6f));
            handle.transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < 9; i++)
                Ball("Bean", stove, 0.022f, new Vector3(0.2f + (i % 3 - 1) * 0.06f, 0.99f, -0.15f + (i / 3 - 1) * 0.06f), Mat("#5A3726"));

            // An open shelf of jars over the counter: cardamom, coffee, sugar, saffron, sumac, and Teta's two big coffee
            // jars, one for the house and one "for later".
            var shelf = Group("Shelf", root, new Vector3(0.2f, 2.1f, -d / 2 + 0.2f));
            Box("Plank", shelf, new Vector3(3.2f, 0.06f, 0.3f), Vector3.zero, wood);
            string[] spices = { "#7FA36A", "#5A3726", "#FBF7F0", "#E39B3A", "#9E3B3B", "#C9A14A" };
            for (int i = 0; i < spices.Length; i++)
            {
                float x = -1.3f + i * 0.34f;
                Cylinder("Jar", shelf, 0.1f, 0.24f, new Vector3(x, 0.15f, 0), Mat("#EAF2F2", 0.85f));
                Cylinder("Spice", shelf, 0.085f, 0.15f, new Vector3(x, 0.11f, 0), Mat(spices[i]));
                Cylinder("Lid", shelf, 0.105f, 0.04f, new Vector3(x, 0.29f, 0), brass);
            }
            foreach (float x in new[] { 0.95f, 1.35f })
            {
                Cylinder("CoffeeJar", shelf, 0.15f, 0.42f, new Vector3(x, 0.24f, 0), copper);
                Ball("JarLid", shelf, 0.1f, new Vector3(x, 0.47f, 0), copper);
            }

            // Copper pots on a rail below the shelf.
            Cylinder("Rail", root, 0.015f, 2.4f, new Vector3(-0.6f, 1.75f, -d / 2 + 0.25f), brass).transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < 4; i++)
            {
                float x = -1.5f + i * 0.6f;
                Cylinder("Hook", root, 0.01f, 0.14f, new Vector3(x, 1.68f, -d / 2 + 0.25f), brass);
                Cylinder("Pot", root, 0.11f + i * 0.02f, 0.16f, new Vector3(x, 1.52f - i * 0.02f, -d / 2 + 0.25f), copper);
            }

            // The window on the side wall, with basil on the sill.
            var window = Group("Window", root, new Vector3(-w / 2 + 0.02f, 0, -0.4f), 90);
            Box("Frame", window, new Vector3(1.3f, 1.2f, 0.12f), new Vector3(0, 2.15f, 0.06f), Mat("#FFFFFF"));
            Box("Glass", window, new Vector3(1.1f, 1.0f, 0.02f), new Vector3(0, 2.15f, 0.13f), glassMat);
            Box("Mullion", window, new Vector3(0.05f, 1.0f, 0.04f), new Vector3(0, 2.15f, 0.15f), Mat("#FFFFFF"));
            Box("Sill", window, new Vector3(1.5f, 0.08f, 0.32f), new Vector3(0, 1.58f, 0.16f), Mat("#FFFFFF"));
            Plant(window, new Vector3(0.35f, 1.62f, 0.2f), 0.6f, "#C47A4A");

            // The work table: the wooden coffee mortar, a tray of cups, and a sack of green beans beside it.
            var table = Group("Table", root, new Vector3(0.4f, 0, 0.6f));
            Box("Top", table, new Vector3(1.6f, 0.08f, 0.9f), new Vector3(0, 0.78f, 0), Mat("#C99E83"));
            foreach (var (x, z) in new[] { (-0.7f, -0.35f), (0.7f, -0.35f), (-0.7f, 0.35f), (0.7f, 0.35f) })
                Box("Leg", table, new Vector3(0.07f, 0.76f, 0.07f), new Vector3(x, 0.38f, z), wood);
            Cylinder("Mortar", table, 0.13f, 0.34f, new Vector3(-0.4f, 0.99f, 0), Mat("#7A5340", 0.4f));
            var pestle = Cylinder("Pestle", table, 0.03f, 0.5f, new Vector3(-0.36f, 1.25f, 0.02f), Mat("#7A5340", 0.4f));
            pestle.transform.localRotation = Quaternion.Euler(0, 0, -15);
            Cylinder("Tray", table, 0.3f, 0.02f, new Vector3(0.35f, 0.83f, 0), brass);
            foreach (var (x, z) in new[] { (0.25f, -0.1f), (0.45f, -0.08f), (0.35f, 0.12f) })
                Cylinder("Cup", table, 0.045f, 0.07f, new Vector3(x, 0.88f, z), Mat("#FFFFFF", 0.6f));
            var sack = Group("Sack", root, new Vector3(1.75f, 0, 1.0f));
            Cylinder("Burlap", sack, 0.3f, 0.6f, new Vector3(0, 0.3f, 0), Mat("#C8AE84"));
            Ball("Top", sack, 0.3f, new Vector3(0, 0.58f, 0), Mat("#C8AE84")).transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
            Ball("Beans", sack, 0.22f, new Vector3(0, 0.66f, 0), Mat("#9DAF7A")).transform.localScale = new Vector3(0.44f, 0.12f, 0.44f);

            HangingBulb(root, new Vector3(0.4f, 2.6f, 0.4f), 6f);
            spaceLamps.Add(Point("StoveGlow", root, new Vector3(1.95f, 1.3f, -d / 2 + 0.8f), Hex("#FFB070"), 0.6f, 2.2f));
        }

        // ---------- The rooftop (chapter 6) ----------

        void BuildRooftop(Transform root)
        {
            const float w = W, d = D;
            var stone = new Material(litTemplate) { name = "Rooftop stone" };
            stone.SetTexture("_BaseMap", GridTexture(Hex("#EADFCC"), Hex("#D8C8AF"), 3));
            stone.SetTextureScale("_BaseMap", new Vector2(10, 8));
            stone.SetFloat("_Smoothness", 0.15f);
            Box("Floor", root, new Vector3(w, 0.2f, d), new Vector3(0, -0.1f, 0), stone, castShadows: false);
            Box("Base", root, new Vector3(w + 0.5f, 3.2f, d + 0.5f), new Vector3(0, -1.75f, 0), Mat("#EFE6D6"), castShadows: false);

            // The parapet: white Amman stone with a cap, lower at the front so the camera sees over it.
            var limestone = Mat("#F2EBDD");
            var cap = Mat("#E3D7C3");
            foreach (var (name, size, at) in new[]
                     {
                         ("Back", new Vector3(w + 0.5f, 1.0f, 0.25f), new Vector3(0, 0.5f, -d / 2 - 0.125f)),
                         ("Left", new Vector3(0.25f, 1.0f, d), new Vector3(-w / 2 - 0.125f, 0.5f, 0)),
                         ("Right", new Vector3(0.25f, 1.0f, d), new Vector3(w / 2 + 0.125f, 0.5f, 0)),
                         ("Front", new Vector3(w + 0.5f, 0.45f, 0.25f), new Vector3(0, 0.225f, d / 2 + 0.125f))
                     })
            {
                Box("Parapet" + name, root, size, at, limestone);
                Box("Cap" + name, root, new Vector3(size.x + 0.06f, 0.06f, size.z + 0.06f), at + new Vector3(0, size.y / 2 + 0.03f, 0), cap);
            }

            // The stairwell hut the new stairs come up through, with a door like the café's and bougainvillea over it.
            var hut = Group("StairHut", root, new Vector3(3.85f, 0, -3.0f));
            Box("Walls", hut, new Vector3(1.8f, 2.4f, 1.7f), new Vector3(0, 1.2f, 0), limestone);
            Box("Roof", hut, new Vector3(1.95f, 0.1f, 1.85f), new Vector3(0, 2.45f, 0), cap);
            Box("Door", hut, new Vector3(0.85f, 1.9f, 0.06f), new Vector3(-0.2f, 0.95f, 0.86f), Mat("#D8CDEB"));
            Ball("Knob", hut, 0.05f, new Vector3(0.12f, 0.95f, 0.9f), brass);
            // A lamp over the door, so the roof is never dark.
            Box("LampBack", hut, new Vector3(0.16f, 0.22f, 0.06f), new Vector3(-0.2f, 2.08f, 0.88f), Mat("#3A3540", 0.4f, 0.5f));
            Ball("Lamp", hut, 0.09f, new Vector3(-0.2f, 2.08f, 0.94f), Glow("#FFD9A0"), castShadows: false);
            spaceLamps.Add(Point("DoorLight", hut, new Vector3(-0.2f, 2.0f, 1.4f), Hex("#FFC98A"), 1f, 5f));
            var bloom = new[] { Mat("#C2407A"), Mat("#E05A93"), Mat("#9DBFA6") };
            var random = new System.Random(1974);
            for (int i = 0; i < 22; i++)
            {
                var at = new Vector3(-0.9f + (float)random.NextDouble() * 1.9f, 1.9f + (float)random.NextDouble() * 0.7f, 0.8f + (float)random.NextDouble() * 0.15f);
                if (i < 8) at = new Vector3(0.75f + (float)random.NextDouble() * 0.2f, 0.4f + i * 0.22f, 0.8f);
                Ball("Bougainvillea", hut, 0.12f + (float)random.NextDouble() * 0.08f, at, bloom[i % 3], castShadows: false);
            }

            // Water tanks on their stand and a solar heater: every roof in Amman has them.
            var tanks = Group("Tanks", root, new Vector3(-3.7f, 0, -3.1f));
            var iron = Mat("#5B5560", 0.3f, 0.5f);
            foreach (var (x, z) in new[] { (-0.7f, -0.35f), (0.7f, -0.35f), (-0.7f, 0.35f), (0.7f, 0.35f) })
                Box("Leg", tanks, new Vector3(0.06f, 0.7f, 0.06f), new Vector3(x, 0.35f, z), iron);
            Box("Deck", tanks, new Vector3(1.6f, 0.06f, 0.9f), new Vector3(0, 0.72f, 0), iron);
            foreach (float x in new[] { -0.4f, 0.4f })
            {
                Cylinder("Tank", tanks, 0.36f, 0.9f, new Vector3(x, 1.2f, 0), Mat("#26222B", 0.35f));
                Cylinder("Lid", tanks, 0.16f, 0.06f, new Vector3(x, 1.68f, 0), Mat("#3A3540", 0.35f));
            }
            var solar = Group("Solar", root, new Vector3(-1.6f, 0, -3.35f));
            var panel = Box("Panel", solar, new Vector3(1.4f, 0.05f, 0.9f), new Vector3(0, 0.8f, 0), Mat("#2E3D63", 0.8f, 0.4f));
            panel.transform.localRotation = Quaternion.Euler(-35, 0, 0);
            Box("Leg", solar, new Vector3(0.05f, 0.9f, 0.05f), new Vector3(-0.6f, 0.45f, -0.25f), iron);
            Box("Leg", solar, new Vector3(0.05f, 0.9f, 0.05f), new Vector3(0.6f, 0.45f, -0.25f), iron);
            Cylinder("Heater", solar, 0.14f, 1.4f, new Vector3(0, 1.25f, -0.4f), Mat("#E8E4DE", 0.5f)).transform.localRotation = Quaternion.Euler(0, 0, 90);

            // A satellite dish on the parapet.
            var dish = Group("Dish", root, new Vector3(4.75f, 1.0f, 1.6f));
            Cylinder("Pole", dish, 0.03f, 0.5f, new Vector3(0, 0.25f, 0), iron);
            var plate = Disc("Plate", dish, 0.35f, 0.04f, new Vector3(-0.08f, 0.55f, 0), Mat("#F4F1EC", 0.4f));
            plate.transform.localRotation = Quaternion.Euler(60, -60, 0);

            // A clothesline down the left side.
            var line = Group("Clothesline", root, new Vector3(-4.55f, 0, 0.6f));
            Cylinder("PoleA", line, 0.03f, 1.9f, new Vector3(0, 0.95f, -1.6f), iron);
            Cylinder("PoleB", line, 0.03f, 1.9f, new Vector3(0, 0.95f, 1.6f), iron);
            Box("Line", line, new Vector3(0.01f, 0.01f, 3.2f), new Vector3(0, 1.85f, 0), Mat("#FFFFFF"));
            string[] cloth = { "#B84A60", "#F6E4C4", "#9DBFA6", "#C9BCE6" };
            for (int i = 0; i < cloth.Length; i++)
                Box("Cloth", line, new Vector3(0.03f, 0.55f - i % 2 * 0.15f, 0.5f), new Vector3(0, 1.55f + i % 2 * 0.07f, -1.1f + i * 0.72f), Mat(cloth[i]), castShadows: false);

            // The jalsa: floor cushions against the back parapet around a low tray of tea, and a lantern.
            var jalsa = Group("Jalsa", root, new Vector3(0.4f, 0, -3.2f));
            var cushion = Mat("#B84A60");
            var pattern = Mat("#E9C99B");
            Box("Seat", jalsa, new Vector3(3.0f, 0.2f, 0.75f), new Vector3(0, 0.1f, 0), cushion);
            Box("Stripe", jalsa, new Vector3(3.02f, 0.05f, 0.77f), new Vector3(0, 0.16f, 0), pattern);
            for (int i = 0; i < 4; i++)
                Box("Back", jalsa, new Vector3(0.68f, 0.5f, 0.18f), new Vector3(-1.1f + i * 0.73f, 0.45f, -0.3f), i % 2 == 0 ? cushion : Mat("#7E5A9B"));
            var sini = Group("Tray", root, new Vector3(0.4f, 0, -1.95f));
            Cylinder("Stand", sini, 0.18f, 0.32f, new Vector3(0, 0.16f, 0), Mat("#7A5340"));
            Cylinder("Tray", sini, 0.5f, 0.04f, new Vector3(0, 0.34f, 0), brass);
            Dallah(sini, new Vector3(-0.15f, 0.36f, 0), 0.4f, brass);
            foreach (var (x, z) in new[] { (0.2f, -0.15f), (0.28f, 0.1f), (0.05f, 0.25f) })
                Cylinder("TeaGlass", sini, 0.035f, 0.08f, new Vector3(x, 0.4f, z), Mat("#E7A35A", 0.8f));
            var lantern = Group("Lantern", root, new Vector3(-1.5f, 0, -2.2f));
            Box("Frame", lantern, new Vector3(0.24f, 0.36f, 0.24f), new Vector3(0, 0.18f, 0), Mat("#3A3540", 0.4f, 0.5f));
            Box("Light", lantern, new Vector3(0.18f, 0.28f, 0.18f), new Vector3(0, 0.18f, 0), Glow("#FFC27A"), castShadows: false);
            Ball("Top", lantern, 0.1f, new Vector3(0, 0.4f, 0), Mat("#3A3540", 0.4f, 0.5f));
            spaceLamps.Add(Point("LanternLight", lantern, new Vector3(0, 0.5f, 0), Hex("#FFC27A"), 1f, 4f));

            // Two café tables at the front for the opening night.
            var wood = Mat("#C99E83");
            var woodDark = Mat("#A57A62");
            foreach (var spot in new[] { new Vector2(-2.0f, 1.8f), new Vector2(1.6f, 1.2f) })
            {
                var table = Group("Table", root, new Vector3(spot.x, 0, spot.y));
                Cylinder("Top", table, 0.55f, 0.06f, new Vector3(0, 0.76f, 0), Mat("#FFFFFF"));
                Cylinder("Leg", table, 0.05f, 0.74f, new Vector3(0, 0.37f, 0), woodDark);
                foreach (int side in new[] { -1, 1 })
                    Chair(root, new Vector3(spot.x + side * 1.0f, 0, spot.y), side, wood, wood, woodDark);
            }

            // Herbs and jasmine in old olive-oil tins along the parapets.
            foreach (var (at, size) in new[]
                     {
                         (new Vector3(-4.6f, 0, 3.4f), 0.75f), (new Vector3(-4.6f, 0, -1.6f), 0.6f), (new Vector3(4.6f, 0, 3.4f), 0.7f),
                         (new Vector3(2.9f, 0, 3.5f), 0.55f), (new Vector3(-2.6f, 0, -3.5f), 0.65f)
                     })
                Plant(root, at, size, "#B9BCC2", tin: true);

            BuildCity(root);
        }

        // Amman beyond the parapet: neighbours' roofs just below, then hills of white houses, a minaret and a dome,
        // and the Citadel's columns on the farthest hill. Their windows light up at dusk (ApplySpaceTime).
        void BuildCity(Transform root)
        {
            var city = Group("City", root, Vector3.zero);
            cityWindows = new Material(Mat("#F1E6D2")) { name = "City windows" };
            cityWindows.EnableKeyword("_EMISSION");
            var random = new System.Random(2006);
            string[] stones = { "#F4EEE3", "#EFE5D3", "#E8DCC6", "#F7F1E6" };
            var tank = Mat("#26222B", 0.35f);

            // Roofs of the street: lower than ours, on both sides and behind.
            for (int i = 0; i < 26; i++)
            {
                float angle = Mathf.Lerp(150f, 345f, i / 25f) * Mathf.Deg2Rad;
                float distance = 9f + (float)random.NextDouble() * 7f;
                var at = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance * 0.8f - 2f);
                float top = -2.6f + (float)random.NextDouble() * 2.2f;
                var size = new Vector3(3f + (float)random.NextDouble() * 2.5f, 8f, 3f + (float)random.NextDouble() * 2.5f);
                var house = Group("House", city, at, (float)random.NextDouble() * 20f - 10f);
                Shape(PrimitiveType.Cube, "Walls", house, new Vector3(0, top - 4f, 0), size, Mat(stones[i % stones.Length]), false);
                for (int k = 0; k < 2; k++)
                    Shape(PrimitiveType.Cube, "Window", house, new Vector3(-size.x / 4 + k * size.x / 2, top - 0.9f, size.z / 2 + 0.01f), new Vector3(0.5f, 0.6f, 0.02f), cityWindows, false);
                if (i % 3 == 0)
                    Shape(PrimitiveType.Cylinder, "Tank", house, new Vector3(0.6f, top + 0.4f, -0.4f), new Vector3(0.6f, 0.4f, 0.6f), tank, false);
            }

            // The valley the street looks down on, with houses scattered across it; their windows are the city's
            // lights at night.
            Shape(PrimitiveType.Cube, "Valley", city, new Vector3(0, -9.5f, -60f), new Vector3(320f, 1f, 260f), Mat("#D6C6AB"), false);
            for (int i = 0; i < 40; i++)
            {
                float angle = Mathf.Lerp(150f, 350f, (float)random.NextDouble()) * Mathf.Deg2Rad;
                float distance = 22f + (float)random.NextDouble() * 40f;
                var at = new Vector3(Mathf.Cos(angle) * distance, -9f, Mathf.Sin(angle) * distance);
                var house = Group("ValleyHouse", city, at, Mathf.Atan2(-at.x, -at.z) * Mathf.Rad2Deg);
                var size = new Vector3(3f + (float)random.NextDouble() * 3f, 3f + (float)random.NextDouble() * 4f, 3f + (float)random.NextDouble() * 2f);
                Shape(PrimitiveType.Cube, "Walls", house, new Vector3(0, size.y / 2, 0), size, Mat(stones[i % stones.Length]), false);
                Shape(PrimitiveType.Cube, "Window", house, new Vector3(0, size.y * 0.6f, size.z / 2 + 0.01f), new Vector3(0.6f, 0.7f, 0.02f), cityWindows, false);
                if (i % 4 == 0)
                    Shape(PrimitiveType.Cylinder, "Tank", house, new Vector3(0.5f, size.y + 0.4f, 0), new Vector3(0.7f, 0.4f, 0.7f), tank, false);
            }

            // The hills: a ring of mounds far off, rising against the sky, each turned to face the rooftop and covered
            // in small white houses on that side, paler the farther they are.
            string[] earth = { "#D9C6A8", "#DFCFB5", "#E6D9C4" };
            for (int i = 0; i < 7; i++)
            {
                float angle = Mathf.Lerp(160f, 340f, i / 6f) * Mathf.Deg2Rad;
                float distance = 72f + i % 3 * 12f;
                var centre = new Vector3(Mathf.Cos(angle) * distance, -10f, Mathf.Sin(angle) * distance);
                var hill = Group("Hill", city, centre, Mathf.Atan2(-centre.x, -centre.z) * Mathf.Rad2Deg);
                var size = new Vector3(38f + i % 3 * 8f, 28f + i % 2 * 6f, 26f);
                Shape(PrimitiveType.Sphere, "Mound", hill, Vector3.zero, size, Mat(earth[i % 3]), false);
                for (int k = 0; k < 34; k++)
                {
                    // A point on the mound's near face: across (u), and up its slope (h) toward the top.
                    float u = ((float)random.NextDouble() * 2f - 1f) * 0.8f, h = 0.2f + (float)random.NextDouble() * 0.72f;
                    float across = Mathf.Sqrt(1f - u * u);
                    var at = new Vector3(u * size.x * 0.5f, across * h * size.y * 0.5f, across * Mathf.Sqrt(1f - h * h) * size.z * 0.5f);
                    float s = 1.6f + (float)random.NextDouble() * 1.4f;
                    var house = Shape(PrimitiveType.Cube, "HillHouse", hill, at, new Vector3(s * 1.4f, s, s), Mat(stones[k % stones.Length]), false);
                    if (k % 2 == 0)
                        Shape(PrimitiveType.Cube, "Window", house.transform, new Vector3(0.15f, 0.05f, 0.51f), new Vector3(0.16f, 0.2f, 0.02f), cityWindows, false);
                }
                if (i == 2) Mosque(hill, new Vector3(6f, size.y * 0.5f * 0.82f, size.z * 0.5f * 0.45f));
                if (i == 5) Citadel(hill, new Vector3(-4f, size.y * 0.5f * 0.96f, 2f));
            }
        }

        // A minaret beside a green dome, big enough to read from across the valley.
        void Mosque(Transform hill, Vector3 at)
        {
            var mosque = Group("Mosque", hill, at);
            var stone = Mat("#F4EEE3");
            var green = Mat("#7FA38A");
            Shape(PrimitiveType.Cylinder, "Minaret", mosque, new Vector3(0, 7f, 0), new Vector3(1.4f, 7f, 1.4f), stone, false);
            Shape(PrimitiveType.Cylinder, "Balcony", mosque, new Vector3(0, 11.5f, 0), new Vector3(2.2f, 0.2f, 2.2f), Mat("#E3D7C3"), false);
            Shape(PrimitiveType.Cylinder, "Top", mosque, new Vector3(0, 13f, 0), new Vector3(1f, 1.4f, 1f), stone, false);
            Shape(PrimitiveType.Sphere, "Cap", mosque, new Vector3(0, 14.8f, 0), new Vector3(1.2f, 1.8f, 1.2f), green, false);
            Shape(PrimitiveType.Sphere, "Light", mosque, new Vector3(0, 11.9f, 0), new Vector3(0.5f, 0.5f, 0.5f), Glow("#B9F0C8"), false);
            Shape(PrimitiveType.Cube, "Hall", mosque, new Vector3(5f, 2f, 0), new Vector3(7f, 4.5f, 6f), stone, false);
            Shape(PrimitiveType.Sphere, "Dome", mosque, new Vector3(5f, 4.4f, 0), new Vector3(5f, 4.6f, 5f), green, false);
        }

        // The Citadel's Roman columns on the far hill.
        void Citadel(Transform hill, Vector3 at)
        {
            var citadel = Group("Citadel", hill, at);
            var stone = Mat("#E3C9A0");
            for (int i = 0; i < 3; i++)
                Shape(PrimitiveType.Cylinder, "Column", citadel, new Vector3(i * 3f, 5f, 0), new Vector3(1.4f, 5f, 1.4f), stone, false);
            Shape(PrimitiveType.Cube, "Beam", citadel, new Vector3(3f, 10.4f, 0), new Vector3(8.4f, 0.9f, 1.6f), stone, false);
        }

        // ---------- Teta's back room (memory 3: a busy evening, the locked tin box) ----------

        void BuildBackRoom(Transform root)
        {
            const float w = 6f, d = 5f, h = 3.2f;
            var floor = new Material(litTemplate) { name = "Back room floor" };
            floor.SetTexture("_BaseMap", PlankTexture());
            floor.SetTextureScale("_BaseMap", new Vector2(3, 5));
            floor.SetFloat("_Smoothness", 0.2f);
            SmallRoom(root, w, d, h, floor, Mat("#E6D5C3"), Mat("#CDB49B"), 1.0f);

            var wood = Mat("#8C6A52");
            var woodLight = Mat("#B08968");

            // A small high window, so evening light falls in.
            var window = Group("Window", root, new Vector3(-0.6f, 0, -d / 2 + 0.02f));
            Box("Frame", window, new Vector3(1.0f, 0.6f, 0.12f), new Vector3(0, 2.55f, 0.06f), Mat("#FFFFFF"));
            Box("Glass", window, new Vector3(0.85f, 0.45f, 0.02f), new Vector3(0, 2.55f, 0.13f), glassMat);

            // Tall shelves along the back wall: boxes, folded cloth, jars, and the tin box up high.
            var shelves = Group("Shelves", root, new Vector3(1.4f, 0, -d / 2 + 0.3f));
            Box("SideA", shelves, new Vector3(0.06f, 2.4f, 0.45f), new Vector3(-1.2f, 1.2f, 0), wood);
            Box("SideB", shelves, new Vector3(0.06f, 2.4f, 0.45f), new Vector3(1.2f, 1.2f, 0), wood);
            string[] boxes = { "#C9A27A", "#B98E68", "#D8B58E" };
            string[] cloth = { "#B84A60", "#F6E4C4", "#7E5A9B", "#9DBFA6" };
            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = 0.3f + shelf * 0.62f;
                Box("Plank", shelves, new Vector3(2.4f, 0.05f, 0.45f), new Vector3(0, y, 0), woodLight);
                if (shelf == 0)
                    for (int i = 0; i < 3; i++)
                        Box("Box", shelves, new Vector3(0.55f, 0.4f, 0.38f), new Vector3(-0.75f + i * 0.72f, y + 0.23f, 0), Mat(boxes[i]));
                if (shelf == 1)
                    for (int i = 0; i < 4; i++)
                        Box("Cloth", shelves, new Vector3(0.45f, 0.08f, 0.32f), new Vector3(-0.8f + (i % 2) * 0.5f, y + 0.07f + (i / 2) * 0.09f, 0), Mat(cloth[i]));
                if (shelf == 1)
                    for (int i = 0; i < 3; i++)
                        Cylinder("Jar", shelves, 0.09f, 0.26f, new Vector3(0.35f + i * 0.25f, y + 0.15f, 0), Mat("#EAF2F2", 0.85f));
                if (shelf == 2)
                {
                    // Teta's old radio (memory 7).
                    Box("Radio", shelves, new Vector3(0.6f, 0.34f, 0.26f), new Vector3(-0.5f, y + 0.2f, 0), Mat("#7A5340", 0.5f));
                    Disc("Speaker", shelves, 0.1f, 0.02f, new Vector3(-0.62f, y + 0.2f, 0.14f), Mat("#E9D9BC"));
                    Ball("Knob", shelves, 0.035f, new Vector3(-0.32f, y + 0.24f, 0.14f), brass);
                    Ball("Knob", shelves, 0.035f, new Vector3(-0.32f, y + 0.12f, 0.14f), brass);
                    Box("Photo", shelves, new Vector3(0.32f, 0.4f, 0.03f), new Vector3(0.5f, y + 0.23f, 0.05f), Mat("#3A2731"));
                    Box("Print", shelves, new Vector3(0.24f, 0.3f, 0.01f), new Vector3(0.5f, y + 0.24f, 0.07f), Mat("#E3C9A0"));
                }
                if (shelf == 3)
                {
                    // The locked tin box (memory 3, letter 2).
                    var tin = Group("TinBox", shelves, new Vector3(0.2f, y + 0.02f, 0));
                    Box("Tin", tin, new Vector3(0.5f, 0.24f, 0.32f), new Vector3(0, 0.12f, 0), Mat("#2F6F7E", 0.6f, 0.3f));
                    Box("Lid", tin, new Vector3(0.53f, 0.05f, 0.35f), new Vector3(0, 0.26f, 0), Mat("#C9A14A", 0.6f, 0.6f));
                    Box("Lock", tin, new Vector3(0.08f, 0.1f, 0.03f), new Vector3(0, 0.17f, 0.17f), brass);
                }
            }

            // The old trunk, rolled carpets in the corner, sacks, and the sewing machine on its table (memory 6).
            var trunk = Group("Trunk", root, new Vector3(-1.5f, 0, -1.6f), 20);
            Box("Body", trunk, new Vector3(1.1f, 0.55f, 0.6f), new Vector3(0, 0.28f, 0), wood);
            Box("Lid", trunk, new Vector3(1.14f, 0.12f, 0.64f), new Vector3(0, 0.6f, 0), woodLight);
            foreach (float x in new[] { -0.35f, 0.35f })
                Box("Band", trunk, new Vector3(0.06f, 0.7f, 0.66f), new Vector3(x, 0.33f, 0), brass);
            string[] rugs = { "#9E3B3B", "#7E5A9B", "#C9A14A" };
            for (int i = 0; i < 3; i++)
            {
                var rug = Cylinder("Rug", root, 0.13f, 1.9f, new Vector3(-w / 2 + 0.3f + i * 0.2f, 0.95f, -d / 2 + 0.3f + i * 0.12f), Mat(rugs[i]));
                rug.transform.localRotation = Quaternion.Euler(4 - i * 4, 0, -8 + i * 6);
            }
            foreach (var at in new[] { new Vector3(-2.3f, 0, 0.6f), new Vector3(-1.9f, 0, 1.1f) })
                Cylinder("Sack", root, 0.26f, 0.55f, at + new Vector3(0, 0.27f, 0), Mat("#C8AE84"));
            var desk = Group("SewingTable", root, new Vector3(1.2f, 0, 0.9f));
            Box("Top", desk, new Vector3(1.2f, 0.06f, 0.6f), new Vector3(0, 0.75f, 0), woodLight);
            Box("Legs", desk, new Vector3(1.1f, 0.72f, 0.5f), new Vector3(0, 0.36f, 0), wood);
            Box("Machine", desk, new Vector3(0.5f, 0.12f, 0.22f), new Vector3(0, 0.84f, 0), Mat("#26222B", 0.7f));
            Box("Arm", desk, new Vector3(0.12f, 0.3f, 0.18f), new Vector3(0.2f, 1.0f, 0), Mat("#26222B", 0.7f));
            Box("Head", desk, new Vector3(0.45f, 0.12f, 0.16f), new Vector3(0.0f, 1.15f, 0), Mat("#26222B", 0.7f));
            Cylinder("Wheel", desk, 0.09f, 0.03f, new Vector3(0.26f, 1.08f, 0.1f), brass).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Box("Fabric", desk, new Vector3(0.5f, 0.02f, 0.4f), new Vector3(-0.25f, 0.79f, 0.05f), Mat("#F6E4C4"));
            Chair(root, new Vector3(1.2f, 0, 1.75f), 1, woodLight, Mat("#B84A60"), wood);

            HangingBulb(root, new Vector3(0.2f, 2.5f, 0.2f), 5.5f);
        }

        // A potted plant: a clay pot, or an old olive-oil tin with a label band.
        void Plant(Transform parent, Vector3 at, float size, string pot, bool tin = false)
        {
            var plant = Group("Plant", parent, at);
            Cylinder("Pot", plant, 0.2f * size, 0.36f * size, new Vector3(0, 0.18f * size, 0), Mat(pot, tin ? 0.6f : 0.12f, tin ? 0.5f : 0f));
            if (tin) Cylinder("Label", plant, 0.205f * size, 0.12f * size, new Vector3(0, 0.2f * size, 0), Mat("#D9B06C"));
            var leaf = Mat("#7FA36A");
            var light = Mat("#9DBFA6");
            for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * Mathf.PI * 2;
                var ball = Ball("Leaf", plant, 0.16f * size, new Vector3(Mathf.Cos(a) * 0.12f * size, (0.5f + k % 3 * 0.1f) * size, Mathf.Sin(a) * 0.12f * size), k % 2 == 0 ? leaf : light);
                ball.transform.localScale = new Vector3(0.24f, 0.36f, 0.24f) * size;
            }
            if (tin) Ball("Jasmine", plant, 0.05f * size, new Vector3(0.05f, 0.72f * size, 0.08f), Mat("#FFFFFF"), castShadows: false);
        }

        // ---------- Textures ----------

        static Texture2D CheckerTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Kitchen checker", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            Color light = Hex("#F3EADF"), dark = Hex("#3E3A44");
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = (x < size / 2) == (y < size / 2) ? light : dark;
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        // Square tiles with a grout line: the kitchen's wall tiles and the rooftop's stone.
        static Texture2D GridTexture(Color tile, Color grout, int line)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Grid tile", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // A faint shading toward one corner, so the tiles don't look flat.
                float shade = 1f - 0.04f * (x + y) / (2f * size);
                pixels[y * size + x] = x < line || y < line ? grout : tile * shade;
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        static Texture2D PlankTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Planks", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            Color[] tones = { Hex("#B08968"), Hex("#A57E5E"), Hex("#BA9371"), Hex("#A98262") };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int plank = x / 32;
                float grain = 0.03f * Mathf.Sin(y * 0.35f + plank * 2.1f);
                pixels[y * size + x] = x % 32 < 2 ? Hex("#6E5040") : tones[plank] * (1f + grain);
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }
    }
}
