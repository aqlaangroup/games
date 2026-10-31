using System.Collections.Generic;
using Gahwit.Core;
using UnityEngine;

namespace Gahwit.Cafe
{
    // Placed decor drawn as simple shapes (decor.json parts) until the 3D art arrives (missing/13). Each piece has a
    // collider, so a tap can pick it in decor mode. The rules are in Decor.DecorRules.
    public sealed class CafeDecor : MonoBehaviour
    {
        public struct Placement
        {
            public string Id;
            public float X, Z;  // the footprint's centre, in metres
            public int Degrees;
        }

        readonly Dictionary<string, Transform> pieces = new Dictionary<string, Transform>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly HashSet<string> shown = new HashSet<string>();
        readonly Dictionary<string, float> bounceAt = new Dictionary<string, float>();
        readonly Dictionary<string, Vector3> lastSpot = new Dictionary<string, Vector3>();
        Material template;
        string selected;
        bool firstShow = true;
        const float BounceSeconds = 0.35f;

        public bool Calm { get; set; }               // reduced motion: no bounce
        public event System.Action<Vector3> Settled; // a piece set down or moved, at its world position, for the dust puff

        public void Init() => template = Resources.Load<Material>("Materials/CafeLit");

        public void Show(IReadOnlyList<Placement> placed, DecorCatalog catalog, float cellMeters)
        {
            shown.Clear();
            foreach (var place in placed)
            {
                shown.Add(place.Id);
                if (!pieces.TryGetValue(place.Id, out var piece)) pieces[place.Id] = piece = Build(catalog.Get(place.Id), cellMeters);
                piece.gameObject.SetActive(true);
                piece.localPosition = new Vector3(place.X, 0, place.Z);
                piece.localRotation = Quaternion.Euler(0, place.Degrees, 0);
                // Decor placed (COMPLETE_SPEC.md section 8): a small bounce where it lands.
                var spot = new Vector3(place.X, place.Degrees, place.Z);
                bool moved = !lastSpot.TryGetValue(place.Id, out var before) || before != spot;
                lastSpot[place.Id] = spot;
                if (moved && !firstShow)
                {
                    if (!Calm) bounceAt[place.Id] = Time.unscaledTime;
                    Settled?.Invoke(piece.position);
                }
            }
            firstShow = false;
            foreach (var pair in pieces)
                if (!shown.Contains(pair.Key)) pair.Value.gameObject.SetActive(false);
        }

        public void Select(string id) => selected = id;

        // The piece under a screen ray, or null.
        public string PieceAt(Ray ray)
        {
            if (!Physics.Raycast(ray, out var hit, 100f)) return null;
            foreach (var pair in pieces)
                if (pair.Value.gameObject.activeSelf && hit.transform.IsChildOf(pair.Value)) return pair.Key;
            return null;
        }

        Transform Build(DecorDef def, float cellMeters)
        {
            var root = new GameObject("Decor " + def.Id).transform;
            root.SetParent(transform, false);
            var collider = root.gameObject.AddComponent<BoxCollider>();
            float height = 0.2f;
            foreach (var part in def.Parts)
            {
                var type = part.Shape == "sphere" ? PrimitiveType.Sphere : part.Shape == "cylinder" ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                var shape = GameObject.CreatePrimitive(type);
                Destroy(shape.GetComponent<Collider>());
                shape.transform.SetParent(root, false);
                // Unity's cylinder is 2 units tall, so its height scale is halved.
                var size = new Vector3(part.Size[0], type == PrimitiveType.Cylinder ? part.Size[1] / 2f : part.Size[1], part.Size[2]);
                shape.transform.localScale = size;
                shape.transform.localPosition = new Vector3(part.At[0], part.At[1], part.At[2]);
                shape.GetComponent<Renderer>().sharedMaterial = MaterialFor(part.Color);
                height = Mathf.Max(height, part.At[1] + part.Size[1] / 2f);
            }
            collider.size = new Vector3(def.W * cellMeters, height, def.D * cellMeters);
            collider.center = new Vector3(0, height / 2f, 0);
            if (def.Wall) FitToParts(collider, def);
            return root;
        }

        // A wall piece hangs high and thin: its collider wraps its parts, a little thicker so a finger finds it.
        static void FitToParts(BoxCollider collider, DecorDef def)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = -min;
            foreach (var part in def.Parts)
            {
                var at = new Vector3(part.At[0], part.At[1], part.At[2]);
                var half = new Vector3(part.Size[0], part.Size[1], part.Size[2]) / 2f;
                min = Vector3.Min(min, at - half);
                max = Vector3.Max(max, at + half);
            }
            collider.center = (min + max) / 2f + new Vector3(0, 0, 0.1f);
            collider.size = max - min + new Vector3(0.1f, 0.1f, 0.2f);
        }

        Material MaterialFor(string hex)
        {
            if (materials.TryGetValue(hex, out var material)) return material;
            material = new Material(template);
            material.SetColor("_BaseColor", ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta);
            return materials[hex] = material;
        }

        // The chosen piece lifts a little and bobs, so it's clear what moves.
        void Update()
        {
            foreach (var pair in pieces)
            {
                var position = pair.Value.localPosition;
                position.y = pair.Key == selected ? 0.08f + 0.04f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
                pair.Value.localPosition = position;
                // Squash as it lands, stretch, then settle.
                float scale = 1f;
                if (bounceAt.TryGetValue(pair.Key, out float at))
                {
                    float t = (Time.unscaledTime - at) / BounceSeconds;
                    if (t < 1f) scale = 1f - 0.18f * Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);
                }
                pair.Value.localScale = new Vector3(2f - scale, scale, 2f - scale);
            }
        }
    }
}
