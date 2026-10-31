using System;
using System.Collections.Generic;

namespace Gahwit.Merge
{
    // Bought items (missing/17). The platform grants gems itself; everything else a product gives comes back in its
    // reply as items, which the game applies to the save once per purchase id: energy, a generator copy, and
    // switches such as the pass. Decor sets and outfits belong to their own systems, so they go to `other`.
    public sealed partial class Game
    {
        public bool PurchaseApplied(string purchaseId) => State.PurchasesApplied.Contains(purchaseId);

        // Returns false when this purchase is already in the save.
        public bool ApplyPurchase(string purchaseId, IDictionary<string, string> items, Action<string, string> other = null)
        {
            if (string.IsNullOrEmpty(purchaseId) || PurchaseApplied(purchaseId)) return false;
            State.PurchasesApplied.Add(purchaseId);
            foreach (var pair in items)
            {
                switch (pair.Key)
                {
                    case "energy" when int.TryParse(pair.Value, out int energy):
                        State.Energy += energy;
                        break;
                    case "generatorCopy":
                        var def = Config.GeneratorDef(pair.Value);
                        if (def != null) Place(new Item(def.Chain, 1, def.Id));
                        break;
                    case "passPremium":
                        if (!State.Entitlements.Contains(pair.Key)) State.Entitlements.Add(pair.Key);
                        State.PassPremiumSeason = PassSeason; // the premium track, for this season (the product lasts 28 days)
                        break;
                    default:
                        other?.Invoke(pair.Key, pair.Value);
                        break;
                }
            }
            Changed?.Invoke();
            return true;
        }

        // A decor piece earned outside the shop (a host's finished order): owned through its decor:<id> entitlement, once.
        public bool GrantDecorReward(string id)
        {
            if (string.IsNullOrEmpty(id) || State.Entitlements.Contains("decor:" + id)) return false;
            State.Entitlements.Add("decor:" + id);
            Changed?.Invoke();
            return true;
        }
    }
}
