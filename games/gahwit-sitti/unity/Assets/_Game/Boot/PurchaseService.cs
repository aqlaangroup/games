using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aqlaan.CozyKit.Platform;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Gahwit.Boot
{
    // Real-money purchases (missing/17; COMPLETE_SPEC.md section 10). The flow:
    //   1. the player buys; 2. the store reports it paid; 3. the platform checks it with Apple or Google;
    //   4. the game shows the new gem balance and applies the other items once per purchase id;
    //   5. only then is the store transaction finished.
    // A purchase the platform couldn't check (offline) stays unfinished, so the store sends it again next launch.
    public sealed class PurchaseService : IShopUi
    {
        readonly IStore store;
        readonly PlatformClient client;
        readonly Game game;
        readonly Action<string, string> applyOther; // decor sets and outfits, for their own systems
        readonly HashSet<string> owned = new HashSet<string>();

        public PurchaseService(IStore store, PlatformClient client, Game game, IReadOnlyList<ShopProduct> products, Action<string, string> applyOther)
        {
            this.store = store;
            this.client = client;
            this.game = game;
            this.applyOther = applyOther;
            Products = products;
            store.Purchased += receipt => _ = Check(receipt);
            store.Failed += (sku, cancelled) => Finished?.Invoke(sku, cancelled ? ActionResult.Cancelled : ActionResult.Failed);
        }

        public IReadOnlyList<ShopProduct> Products { get; }
        public bool Ready => store.Ready;
        public event Action<string, ActionResult> Finished;

        public string Price(string sku) => store.Price(sku);
        public bool Owns(string sku) => owned.Contains(sku);

        // The catalogue from game.json (Resources/Config/game.json, copied by the Editor's BalanceSync).
        public static List<ShopProduct> Catalogue()
        {
            var json = JObject.Parse(Resources.Load<TextAsset>("Config/game").text);
            return json["products"].Select(p => new ShopProduct
            {
                Sku = (string)p["sku"],
                Consumable = (string)p["type"] == "consumable",
                OncePerPlayer = p.Value<bool?>("oncePerPlayer") ?? false,
                Grants = ((JObject)p["grants"]).Properties().ToDictionary(g => g.Name, g => g.Value.ToString().ToLowerInvariant() == "true" ? "true" : g.Value.ToString())
            }).ToList();
        }

        public static Dictionary<string, float> UsdTiers() =>
            JObject.Parse(Resources.Load<TextAsset>("Config/game").text)["products"].ToDictionary(p => (string)p["sku"], p => (float)p["usdTier"]);

        public async Task Start()
        {
            await store.Start(Products.Select(p => (p.Sku, p.Consumable)).ToList());
            await Reconcile();
        }

        public void Buy(string sku)
        {
            if (!client.HasSession) Finished?.Invoke(sku, ActionResult.Offline);
            else store.Buy(sku);
        }

        async Task Check(StoreReceipt receipt)
        {
            var reply = await client.VerifyPurchase(receipt.Store, receipt.Sku, receipt.Receipt);
            if (reply.Outcome == Outcome.Offline)
            {
                Finished?.Invoke(receipt.Sku, ActionResult.Offline); // unfinished: checked again next launch
                return;
            }
            if (!reply.Ok)
            {
                if (reply.Error == "refunded") store.Finish(receipt); // nothing to give; stop the store asking again
                Debug.LogWarning($"[Store] {receipt.Sku} not confirmed: {reply.Error}");
                Finished?.Invoke(receipt.Sku, ActionResult.Failed);
                return;
            }
            Apply(reply.Value);
            if (Products.FirstOrDefault(p => p.Sku == receipt.Sku) is { Consumable: false } || Products.Any(p => p.Sku == receipt.Sku && p.OncePerPlayer))
                owned.Add(receipt.Sku);
            store.Finish(receipt);
            Finished?.Invoke(receipt.Sku, ActionResult.Done);
        }

        void Apply(JObject reply)
        {
            string purchaseId = (string)reply["purchaseId"];
            var items = (reply["items"] as JObject)?.Properties().ToDictionary(p => p.Name, p => p.Value.ToString().ToLowerInvariant() == "true" ? "true" : p.Value.ToString())
                        ?? new Dictionary<string, string>();
            game.ApplyPurchase(purchaseId, items, applyOther);
            if (reply["wallet"] != null) Wallet.ApplyPlatformBalance(PlatformClient.WalletGems(reply));
        }

        // Items of earlier purchases that this save doesn't have yet (a reinstall, another phone), and which one-offs
        // are already bought.
        async Task<ActionResult> Reconcile()
        {
            if (!client.HasSession) return ActionResult.Offline;
            var reply = await client.Purchases();
            if (!reply.Ok) return reply.Outcome == Outcome.Offline ? ActionResult.Offline : ActionResult.Failed;
            foreach (var purchase in reply.Value.OfType<JObject>())
            {
                if ((string)purchase["status"] != "verified") continue;
                owned.Add((string)purchase["sku"]);
                Apply(new JObject { ["purchaseId"] = purchase["purchaseId"], ["items"] = purchase["items"] });
            }
            return ActionResult.Done;
        }

        // Settings > Restore purchases (an Apple requirement): the store re-sends what this store account owns, and
        // the platform's list fills in the rest.
        public async Task<ActionResult> Restore()
        {
            store.Restore();
            return await Reconcile();
        }
    }
}
