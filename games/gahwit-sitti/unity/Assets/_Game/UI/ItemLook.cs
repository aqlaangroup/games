using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.Merge;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // How an item looks until item art exists: a tile in its chain's colour (lighter at low levels), its level and a short name.
    public sealed class ItemLook
    {
        readonly Game game;
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        public ItemLook(Game game)
        {
            this.game = game;
        }

        // The item's picture: Art/Items/<chain>_<level>, or the generator's id (missing/13). Null where none is drawn.
        public Sprite Icon(Item item)
        {
            string name = item.IsGenerator ? item.Generator : item.Chain + "_" + item.Level;
            if (icons.TryGetValue(name, out var sprite)) return sprite;
            var texture = Resources.Load<Texture2D>("Art/Items/" + name);
            return icons[name] = texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f)) : null;
        }

        public Color Tint(string chain, int level)
        {
            var stops = game.Content.ChainColors[chain];
            int top = game.MaxLevel(chain);
            float t = top <= 1 ? 0 : (level - 1f) / (top - 1f);
            return Color.Lerp(Palette.Hex(stops[0]), Palette.Hex(stops[1]), t);
        }

        public string ShortName(string chain, int level)
        {
            // One word fits a square: the noun, which comes first in Arabic and last in English.
            string name = game.ItemName(chain, level);
            if (game.English)
            {
                int last = name.LastIndexOf(' ');
                return last > 0 ? name.Substring(last + 1) : name;
            }
            int space = name.IndexOf(' ');
            return space > 0 ? name.Substring(0, space) : name;
        }

        // Generators use their chain's deepest colour and show their name instead of a number.
        public Color GeneratorTint(string chain) => Tint(chain, game.MaxLevel(chain));

        public string GeneratorName(string id) => game.GeneratorName(id);
    }

    public sealed class ItemTile
    {
        public readonly RectTransform Root;
        readonly Image body;
        readonly CanvasGroup group;
        readonly RTLTextMeshPro level;
        readonly RTLTextMeshPro name;
        readonly Image icon, badge;
        readonly float levelSize, nameSize;

        public ItemTile(Transform parent, string objectName, float levelSize, float nameSize)
        {
            this.levelSize = levelSize;
            this.nameSize = nameSize;
            body = UiKit.Panel(parent, objectName, Color.white, 24);
            body.raycastTarget = false;
            Root = body.rectTransform;
            group = Root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            icon = UiKit.Icon(Root, "Icon", null, Color.white, 0);
            icon.rectTransform.Stretch(6, 6, 6, 6);
            icon.gameObject.SetActive(false);
            badge = UiKit.Panel(Root, "Badge", new Color(0.23f, 0.15f, 0.19f, 0.82f), 14);
            badge.raycastTarget = false;
            badge.gameObject.SetActive(false);

            level = UiKit.Text(Root, "Level", "", levelSize, Color.white, TextAlignmentOptions.Center);
            level.fontStyle = FontStyles.Bold;
            level.enableAutoSizing = true;
            level.fontSizeMax = levelSize;
            level.fontSizeMin = levelSize * 0.35f;
            level.rectTransform.Stretch(4, 4, 0, nameSize > 0 ? nameSize * 1.2f : 0);

            if (nameSize <= 0) return;
            name = UiKit.Text(Root, "Name", "", nameSize, Color.white, TextAlignmentOptions.Center);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true;
            name.fontSizeMax = nameSize;
            name.fontSizeMin = nameSize * 0.6f;
            name.rectTransform.Bottom(2, nameSize * 1.5f, 4, 4);
        }

        public void Show(Item item, ItemLook look)
        {
            Root.gameObject.SetActive(true);
            var color = item.IsGenerator ? look.GeneratorTint(item.Chain) : look.Tint(item.Chain, item.Level);
            body.color = color;
            var text = Palette.TextOn(color);
            var picture = look.Icon(item);
            icon.sprite = picture;
            icon.gameObject.SetActive(picture != null);
            badge.gameObject.SetActive(picture != null);
            if (picture != null)
            {
                // The picture fills the tile, and the level stays readable in a badge in the corner (not colour alone).
                var corner = new Vector2(levelSize * 0.9f, levelSize * 0.75f);
                badge.rectTransform.Pin(new Vector2(0, 1), new Vector2(4, -4), corner);
                level.rectTransform.Pin(new Vector2(0, 1), new Vector2(4, -4), corner);
                level.fontSizeMax = levelSize * 0.55f;
                level.lineSpacing = 0;
                level.text = item.Level.ToString();
                level.color = Color.white;
                if (name != null) name.gameObject.SetActive(false);
                return;
            }
            level.rectTransform.Stretch(4, 4, 0, nameSize > 0 ? nameSize * 1.2f : 0);
            if (name != null) name.gameObject.SetActive(true);
            level.text = item.IsGenerator ? look.GeneratorName(item.Generator) : item.Level.ToString();
            // A generator shows its two-word name where an item shows a number: smaller, with tighter lines.
            level.fontSizeMax = item.IsGenerator ? levelSize * 0.55f : levelSize;
            level.lineSpacing = item.IsGenerator ? -24 : 0;
            level.color = text;
            if (name == null) return;
            name.text = item.IsGenerator ? Loc.T("gen.level", item.Level) : look.ShortName(item.Chain, item.Level);
            name.color = text;
        }

        public void Hide() => Root.gameObject.SetActive(false);

        public void SetAlpha(float alpha) => group.alpha = alpha;
    }
}
