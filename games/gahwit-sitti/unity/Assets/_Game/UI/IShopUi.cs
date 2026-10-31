using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gahwit.UI
{
    // One product in the shop: its sku, what it gives (game.json grants) and whether it's a one-off.
    public sealed class ShopProduct
    {
        public string Sku;
        public Dictionary<string, string> Grants = new Dictionary<string, string>();
        public bool OncePerPlayer;
        public bool Consumable;
    }

    // What the shop screen needs from the purchase side (Boot.PurchaseService).
    public interface IShopUi
    {
        IReadOnlyList<ShopProduct> Products { get; }
        bool Ready { get; }
        string Price(string sku);
        bool Owns(string sku);                      // a one-off already bought
        void Buy(string sku);
        Task<ActionResult> Restore();
        event Action<string, ActionResult> Finished; // sku, result
    }
}
