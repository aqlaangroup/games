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

        public ItemLook(Game game)
        {
            this.game = game;
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
            string name = game.ItemName(chain, level);
            int space = name.IndexOf(' ');
            return space > 0 ? name.Substring(0, space) : name;
        }
    }

    public sealed class ItemTile
    {
        public readonly RectTransform Root;
        readonly Image body;
        readonly CanvasGroup group;
        readonly RTLTextMeshPro level;
        readonly RTLTextMeshPro name;

        public ItemTile(Transform parent, string objectName, float levelSize, float nameSize)
        {
            body = UiKit.Panel(parent, objectName, Color.white, 24);
            body.raycastTarget = false;
            Root = body.rectTransform;
            group = Root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            level = UiKit.Text(Root, "Level", "", levelSize, Color.white, TextAlignmentOptions.Center);
            level.fontStyle = FontStyles.Bold;
            level.rectTransform.Stretch(0, 0, 0, nameSize > 0 ? nameSize * 1.2f : 0);

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
            var color = look.Tint(item.Chain, item.Level);
            body.color = color;
            var text = Palette.TextOn(color);
            level.text = item.Level.ToString();
            level.color = text;
            if (name == null) return;
            name.text = look.ShortName(item.Chain, item.Level);
            name.color = text;
        }

        public void Hide() => Root.gameObject.SetActive(false);

        public void SetAlpha(float alpha) => group.alpha = alpha;
    }
}
