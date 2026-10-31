using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The shop (screens/Shop; missing/17): every product with its exact contents and the store's own price. Nothing
    // sold is random. One-offs already bought show as owned; the starter pack comes first while it's available.
    public sealed class ShopView : MonoBehaviour
    {
        IShopUi shop;
        Game game;
        Toast toast;
        RectTransform list;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(IShopUi shop, Game game, Toast toast)
        {
            this.shop = shop;
            this.game = game;
            this.toast = toast;
            var card = Sheet.Card(transform, 980, 1720);
            Sheet.Title(card, Loc.T("shop.title"), Close);
            UiKit.Text(card, "Note", Loc.T("shop.note"), 24, Palette.InkSoft).rectTransform.Top(124, 60, 48, 48);
            list = Sheet.Column(card, 200, 200, 14);
            var restore = UiKit.Button(card, "Restore", Loc.T("shop.restore"), Palette.Muted, Palette.Ink, 28, Restore, 36);
            ((RectTransform)restore.transform).Bottom(48, UiKit.MinTouch, 140, 140);
            shop.Finished += (sku, result) =>
            {
                if (result != ActionResult.Cancelled) toast.Show(Loc.T("shop.result." + result.ToString().ToLowerInvariant()));
                if (IsOpen) Build();
            };
            gameObject.SetActive(false);
        }

        public void Show()
        {
            UiSound.Play("ui_panel_open");
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Build();
        }

        void Close()
        {
            UiSound.Play("ui_panel_close");
            gameObject.SetActive(false);
        }

        async void Restore()
        {
            var result = await shop.Restore();
            toast.Show(Loc.T("shop.restored." + result.ToString().ToLowerInvariant()));
            if (IsOpen) Build();
        }

        void Build()
        {
            UiKit.Clear(list);
            // The starter pack first, then the rest in the catalogue's order.
            foreach (var product in shop.Products.OrderBy(p => p.OncePerPlayer ? 0 : 1))
            {
                bool owned = shop.Owns(product.Sku) && (product.OncePerPlayer || !product.Consumable);
                if (product.OncePerPlayer && owned) continue;
                Row(product, owned);
            }
        }

        void Row(ShopProduct product, bool owned)
        {
            var row = UiKit.Panel(list, product.Sku, product.OncePerPlayer ? Palette.Hex("#FFF1D6") : Color.white, 28);
            Sheet.Sized(row, 190);
            var name = UiKit.Text(row.transform, "Name", Loc.T("product." + product.Sku), 32, Palette.Ink);
            name.fontStyle = FontStyles.Bold;
            name.rectTransform.Stretch(290, 28, 22, 110);
            UiKit.Text(row.transform, "Contents", Contents(product), 24, Palette.InkSoft).rectTransform.Stretch(290, 28, 80, 16);

            // Without a store (offline, or no store on this device) the price can't come, so say so instead of waiting.
            bool available = owned || shop.Ready;
            string label = owned ? Loc.T("shop.owned") : !shop.Ready ? Loc.T("shop.unavailable") : shop.Price(product.Sku) ?? Loc.T("shop.loading");
            var color = available && !owned ? Palette.Rose : Palette.Muted;
            var buy = UiKit.Button(row.transform, "Buy", label, color, Palette.TextOn(color), 30, () =>
            {
                if (!shop.Ready) toast.Show(Loc.T("shop.notReady"));
                else shop.Buy(product.Sku);
            }, 32);
            buy.interactable = !owned;
            ((RectTransform)buy.transform).Pin(new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(240, UiKit.MinTouch));
        }

        // Exactly what the product gives, from game.json.
        string Contents(ShopProduct product)
        {
            var parts = new List<string>();
            foreach (var grant in product.Grants)
            {
                switch (grant.Key)
                {
                    case "gems": parts.Add(Loc.T("shop.gems", grant.Value)); break;
                    case "energy": parts.Add(Loc.T("shop.energy", grant.Value)); break;
                    case "generatorCopy": parts.Add(Loc.T("shop.generator", game.GeneratorName(grant.Value))); break;
                    case "passPremium": parts.Add(Loc.T("shop.pass")); break;
                    default: parts.Add(Loc.T("shop." + grant.Key)); break; // decorSet, outfit
                }
            }
            return string.Join(" · ", parts);
        }
    }
}
