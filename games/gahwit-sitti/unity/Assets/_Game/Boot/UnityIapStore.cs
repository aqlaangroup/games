#if GAHWIT_IAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Gahwit.Boot
{
    // Apple and Google through Unity IAP 5 (com.unity.purchasing). Compiled only when the package is installed
    // (GAHWIT_IAP, from the Boot assembly's version defines); installing it needs a Unity Hub sign-in (missing/17).
    // A pending order stays unconfirmed until the platform has checked it, so the store sends it again next launch.
    public sealed class UnityIapStore : IStore
    {
        readonly string storeName;
        StoreController store;

        public UnityIapStore() => storeName = Application.platform == RuntimePlatform.Android ? "google" : "apple";

        public bool Ready { get; private set; }
        public event Action<StoreReceipt> Purchased;
        public event Action<string, bool> Failed;

        public string Price(string sku) => store?.GetProductById(sku)?.metadata.localizedPriceString;

        public async Task Start(IReadOnlyList<(string sku, bool consumable)> products)
        {
            store = UnityIAPServices.StoreController();
            store.OnPurchasePending += Pending;
            store.OnPurchaseFailed += failed =>
            {
                var sku = failed.CartOrdered.Items().FirstOrDefault()?.Product.definition.id;
                Failed?.Invoke(sku, failed.FailureReason == PurchaseFailureReason.UserCancelled);
            };
            store.OnProductsFetched += _ =>
            {
                Ready = true;
                store.FetchPurchases(); // unconfirmed purchases from last time come back as pending
            };
            store.OnProductsFetchFailed += failure => Debug.LogWarning($"[Store] Products not fetched: {failure.FailureReason}");
            await store.Connect();
            store.FetchProducts(products.Select(p => new ProductDefinition(p.sku, p.consumable ? ProductType.Consumable : ProductType.NonConsumable)).ToList());
        }

        void Pending(PendingOrder order)
        {
            var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product == null) return;
            Purchased?.Invoke(new StoreReceipt
            {
                Store = storeName,
                Sku = product.definition.id,
                // Apple: the StoreKit transaction id. Google: Unity IAP's transaction id is the purchase token.
                Receipt = order.Info.TransactionID,
                Handle = order
            });
        }

        public void Buy(string sku)
        {
            var product = store?.GetProductById(sku);
            if (product == null) Failed?.Invoke(sku, false);
            else store.PurchaseProduct(product);
        }

        public void Finish(StoreReceipt receipt)
        {
            if (receipt.Handle is PendingOrder order) store.ConfirmPurchase(order);
        }

        public void Restore() => store?.RestoreTransactions((ok, error) =>
        {
            if (!ok) Debug.LogWarning($"[Store] Restore failed: {error}");
        });
    }
}
#endif
