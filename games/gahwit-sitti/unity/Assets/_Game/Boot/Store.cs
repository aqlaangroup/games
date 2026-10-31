using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Gahwit.Boot
{
    // A purchase the store says is paid, waiting for the platform to confirm it (missing/17). Handle is the store's
    // own object, given back to Finish.
    public sealed class StoreReceipt
    {
        public string Store;   // "apple" or "google", as the platform names them
        public string Sku;
        public string Receipt; // Apple: the transaction id; Google: the purchase token; the Editor: "dev:<id>"
        public object Handle;
    }

    // The phone's store. Purchases arrive through Purchased, new or left over from last time; each one is finished
    // only after the platform confirms it, so the store sends an unconfirmed one again on the next launch.
    public interface IStore
    {
        bool Ready { get; }
        string Price(string sku); // the store's own price text, in the player's currency; null before it's known
        event Action<StoreReceipt> Purchased;
        event Action<string, bool> Failed; // sku, cancelled by the player
        Task Start(IReadOnlyList<(string sku, bool consumable)> products);
        void Buy(string sku);
        void Finish(StoreReceipt receipt);
        void Restore(); // Apple's "restore purchases": owned non-consumables come back through Purchased
    }

    // The Editor's store: pays at once with a "dev:" receipt, which only a platform on a desk accepts
    // (Stores:AllowDevReceipts). Prices are the catalogue's US tiers.
    public sealed class EditorTestStore : IStore
    {
        readonly Dictionary<string, float> tiers;
        readonly List<StoreReceipt> owned = new List<StoreReceipt>();
        readonly HashSet<string> consumables = new HashSet<string>();

        public EditorTestStore(Dictionary<string, float> usdTiers) => tiers = usdTiers;

        public bool Ready { get; private set; }
        public event Action<StoreReceipt> Purchased;
        public event Action<string, bool> Failed;

        public string Price(string sku) => tiers.TryGetValue(sku, out float usd) ? $"${usd:0.00}" : null;

        public Task Start(IReadOnlyList<(string sku, bool consumable)> products)
        {
            foreach (var (sku, consumable) in products) if (consumable) consumables.Add(sku);
            Ready = true;
            return Task.CompletedTask;
        }

        public void Buy(string sku)
        {
            if (!tiers.ContainsKey(sku))
            {
                Failed?.Invoke(sku, false);
                return;
            }
            Debug.Log($"[Store] Editor purchase of {sku}.");
            var receipt = new StoreReceipt { Store = "apple", Sku = sku, Receipt = "dev:" + Guid.NewGuid().ToString("N") };
            if (!consumables.Contains(sku)) owned.Add(receipt);
            Purchased?.Invoke(receipt);
        }

        public void Finish(StoreReceipt receipt) { }

        public void Restore()
        {
            foreach (var receipt in owned.ToArray()) Purchased?.Invoke(receipt);
        }
    }

    // No store on this platform, or none configured: nothing can be bought.
    public sealed class NoStore : IStore
    {
        public bool Ready => false;
        public string Price(string sku) => null;
        public event Action<StoreReceipt> Purchased { add { } remove { } }
        public event Action<string, bool> Failed;
        public Task Start(IReadOnlyList<(string sku, bool consumable)> products) => Task.CompletedTask;
        public void Buy(string sku) => Failed?.Invoke(sku, false);
        public void Finish(StoreReceipt receipt) { }
        public void Restore() { }
    }
}
