using UnityEngine;

namespace Gahwit.Cafe
{
    // Salma beside the counter, wearing her outfit (missing/09). A figure of simple shapes until the 3D Salma with
    // swappable pieces arrives (missing/13). Colours come from the wardrobe pieces she wears.
    public sealed class CafeSalma : MonoBehaviour
    {
        public struct Outfit
        {
            public Color Top, Bottom;
            public Color? Outer, Hijab;
            public bool Dress; // a dress reaches the floor; trousers show two legs
        }

        static readonly Color Skin = new Color(0.86f, 0.69f, 0.58f), Hair = new Color(0.2f, 0.13f, 0.1f);

        Renderer torso, outer, skirt, legLeft, legRight, hijab, hair;
        Material template;

        public void Init(Vector3 position)
        {
            template = Resources.Load<Material>("Materials/CafeLit");
            transform.localPosition = position;
            transform.localRotation = Quaternion.LookRotation(Vector3.forward); // over the counter, towards the room
            legLeft = Part(PrimitiveType.Cylinder, new Vector3(-0.09f, 0.38f, 0), new Vector3(0.14f, 0.38f, 0.14f));
            legRight = Part(PrimitiveType.Cylinder, new Vector3(0.09f, 0.38f, 0), new Vector3(0.14f, 0.38f, 0.14f));
            skirt = Part(PrimitiveType.Cylinder, new Vector3(0, 0.42f, 0), new Vector3(0.42f, 0.42f, 0.42f));
            torso = Part(PrimitiveType.Capsule, new Vector3(0, 1.12f, 0), new Vector3(0.38f, 0.34f, 0.3f));
            outer = Part(PrimitiveType.Capsule, new Vector3(0, 1.1f, 0), new Vector3(0.44f, 0.37f, 0.36f));
            var head = Part(PrimitiveType.Sphere, new Vector3(0, 1.62f, 0), Vector3.one * 0.3f);
            head.sharedMaterial = Colour(Skin);
            hair = Part(PrimitiveType.Sphere, new Vector3(0, 1.66f, -0.04f), Vector3.one * 0.31f);
            hair.sharedMaterial = Colour(Hair);
            hijab = Part(PrimitiveType.Sphere, new Vector3(0, 1.6f, -0.02f), new Vector3(0.36f, 0.4f, 0.36f));
        }

        Renderer Part(PrimitiveType type, Vector3 at, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(transform, false);
            part.transform.localPosition = at;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.material = new Material(template);
            return renderer;
        }

        static void Paint(Renderer part, Color color) => part.material.SetColor("_BaseColor", color);

        Material Colour(Color color)
        {
            var material = new Material(template);
            material.SetColor("_BaseColor", color);
            return material;
        }

        public void Dress(Outfit outfit)
        {
            Paint(torso, outfit.Top);
            skirt.gameObject.SetActive(outfit.Dress);
            legLeft.gameObject.SetActive(!outfit.Dress);
            legRight.gameObject.SetActive(!outfit.Dress);
            Paint(outfit.Dress ? skirt : legLeft, outfit.Bottom);
            if (!outfit.Dress) Paint(legRight, outfit.Bottom);
            outer.gameObject.SetActive(outfit.Outer.HasValue);
            if (outfit.Outer.HasValue) Paint(outer, outfit.Outer.Value);
            hijab.gameObject.SetActive(outfit.Hijab.HasValue);
            if (outfit.Hijab.HasValue) Paint(hijab, outfit.Hijab.Value);
        }
    }
}
