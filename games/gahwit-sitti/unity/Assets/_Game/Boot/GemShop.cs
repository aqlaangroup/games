using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aqlaan.CozyKit;
using Aqlaan.CozyKit.Platform;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gahwit.Boot
{
    // Spends gems through the platform (/players/me/spend) and applies what was bought only after its reply
    // (CLAUDE.md). Each purchase is kept on the phone until it's applied, so one interrupted by a lost connection or
    // a closed app is sent again with the same request id: the platform spends once and answers with the grant.
    public sealed class GemShop
    {
        const string PendingSlot = "gahwit-sitti-spends";

        sealed class Pending
        {
            public string RequestId;
            public string Item;
        }

        readonly PlatformClient client;
        readonly Game game;
        readonly List<Pending> pending;

        public GemShop(PlatformClient client, Game game)
        {
            this.client = client;
            this.game = game;
            string saved = SaveStore.LoadText(PendingSlot);
            pending = saved != null ? JsonConvert.DeserializeObject<List<Pending>>(saved) ?? new List<Pending>() : new List<Pending>();
        }

        public Task<ActionResult> Buy(string item)
        {
            var purchase = new Pending { RequestId = Guid.NewGuid().ToString("N"), Item = item };
            pending.Add(purchase);
            Keep();
            return Send(purchase);
        }

        // Purchases left over from last time (offline, or the app closed): sent again, once.
        public async Task Replay()
        {
            foreach (var purchase in pending.ToArray())
                if (await Send(purchase) == ActionResult.Offline) return;
        }

        async Task<ActionResult> Send(Pending purchase)
        {
            var reply = await client.Spend(purchase.Item, purchase.RequestId);
            if (reply.Outcome == Outcome.Offline) return ActionResult.Offline; // kept, sent again later
            pending.Remove(purchase);
            Keep();
            if (!reply.Ok) return reply.Error == "not_enough_gems" ? ActionResult.NotEnoughGems : ActionResult.Failed;
            Apply(reply.Value["grant"] as JObject);
            Wallet.ApplyPlatformBalance(PlatformClient.WalletGems(reply.Value));
            return ActionResult.Done;
        }

        void Apply(JObject grant)
        {
            if (grant == null) return;
            if (grant["energy"] != null) game.AddEnergy((int)grant["energy"]);
            if (grant["storageSlot"] != null) game.AddStorageSlot((int)grant["storageSlot"]);
            if (grant["bubbleLevel"] != null)
            {
                string chain = (string)grant["chain"] ?? game.Bubble?.Chain;
                if (chain != null) game.AddPiece(chain, (int)grant["bubbleLevel"]);
                game.LetBubbleGo();
            }
        }

        void Keep()
        {
            if (pending.Count == 0) SaveStore.Delete(PendingSlot);
            else SaveStore.SaveText(PendingSlot, JsonConvert.SerializeObject(pending));
        }
    }
}
