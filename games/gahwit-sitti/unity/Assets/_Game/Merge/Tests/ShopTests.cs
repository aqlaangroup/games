using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Merge.Tests
{
    public sealed class ShopTests
    {
        const long Now = 1_000_000_000;
        const string Today = "2026-10-31", Tomorrow = "2026-11-01";

        BalanceConfig config;
        ChapterContent content;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
        }

        Game NewGame(int level = 3)
        {
            var game = new Game(config, content, Game.NewState(config, content, 8, Now));
            game.State.Level = level;
            return game;
        }

        [Test]
        public void AnEnergyAdGivesItsAmountUpToTheDailyCapAndCountsAgainTomorrow()
        {
            var game = NewGame();
            int start = game.State.Energy;
            for (int i = 0; i < config.Energy.RewardedAdsPerDay; i++) Assert.IsTrue(game.RewardAd(AdPlacement.Energy, Today));
            Assert.AreEqual(start + config.Energy.RewardedAdsPerDay * config.Energy.RewardedAdAmount, game.State.Energy);
            Assert.IsFalse(game.CanWatchAd(AdPlacement.Energy, Today));
            Assert.IsFalse(game.RewardAd(AdPlacement.Energy, Today));
            Assert.AreEqual(config.Orders.RewardedAdDoubleCoinsPerDay, game.AdsLeft(AdPlacement.DoubleCoins, Today), "each placement has its own cap");
            Assert.IsTrue(game.CanWatchAd(AdPlacement.Energy, Tomorrow));
        }

        [Test]
        public void NoAdsBeforeTheShopLevelAndDoubleCoinsPaysTheOrderAgain()
        {
            Assert.IsFalse(NewGame(level: config.Unlocks.NoShopOrAdsBeforeLevel - 1).CanWatchAd(AdPlacement.Energy, Today));
            var game = NewGame();
            int coins = game.State.Coins;
            Assert.IsTrue(game.RewardAd(AdPlacement.DoubleCoins, Today, 42));
            Assert.AreEqual(coins + 42, game.State.Coins);
        }

        [Test]
        public void APurchasesItemsApplyOnceAndUnknownOnesGoToTheirSystems()
        {
            var game = NewGame();
            int energy = game.State.Energy;
            var other = new List<string>();
            var items = new Dictionary<string, string> { ["energy"] = "200", ["generatorCopy"] = "gen_coffee", ["decorSet"] = "eid_lanterns", ["passPremium"] = "true" };
            int generators = game.State.Board.Count(i => i != null && i.IsGenerator);

            Assert.IsTrue(game.ApplyPurchase("p1", items, (key, value) => other.Add(key)));
            Assert.IsFalse(game.ApplyPurchase("p1", items, (key, value) => other.Add(key)), "a purchase applies once");

            Assert.AreEqual(energy + 200, game.State.Energy);
            Assert.AreEqual(generators + 1, game.State.Board.Count(i => i != null && i.IsGenerator));
            CollectionAssert.AreEqual(new[] { "decorSet" }, other);
            CollectionAssert.Contains(game.State.Entitlements, "passPremium");
        }
    }
}
