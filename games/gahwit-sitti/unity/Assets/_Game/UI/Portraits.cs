using System.Collections.Generic;
using UnityEngine;

namespace Gahwit.UI
{
    // Character portraits drawn for the game (Art/Portraits/<tag>_<expression>, missing/13): the dialogue card and the
    // order cards share them. An expression that wasn't drawn falls back to the neutral face; a character without
    // portraits yet gets null.
    public static class Portraits
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite For(string tag, string expression = null)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            foreach (string name in new[] { $"{tag}_{expression ?? "neutral"}", tag + "_neutral" })
            {
                if (!Cache.TryGetValue(name, out var sprite))
                {
                    var texture = Resources.Load<Texture2D>("Art/Portraits/" + name);
                    sprite = texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0f)) : null;
                    Cache[name] = sprite;
                }
                if (sprite != null) return sprite;
            }
            return null;
        }
    }
}
