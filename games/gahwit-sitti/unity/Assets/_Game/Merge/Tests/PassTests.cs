using System;
using System.Collections.Generic;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Merge.Tests
{
    // The season pass (missing/18): 28-day seasons of 30 tiers, a free and a premium track.
    public sealed class PassTests
    {
        static readonly long Now = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        const long Day = 86_400_000L;

        BalanceConfig config;
        ChapterContent content;
        PassConfig pass;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
            pass = config.Pass;
        }

        Game NewGame(int level = 10)
        {
            var game = new Game(config, content, Game.NewState(config, content, 4, Now));
            game.State.Level = level;
            game.State.TutorialDone = true;
            game.Tick(Now);
            return game;
        }

        [Test]
        public void SeasonsAreTwentyEightDaysFromTheFirst()
        {
            var game = NewGame();
            var start = DateTime.ParseExact(pass.SeasonStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(1, game.PassSeasonOn(start));
            Assert.AreEqual(1, game.PassSeasonOn(start.AddDays(pass.DurationDays - 1)));
            Assert.AreEqual(2, game.PassSeasonOn(start.AddDays(pass.DurationDays)));
            Assert.AreEqual(1, game.PassSeasonOn(start.AddDays(-3)), "before the first season counts as the first");
        }

        [Test]
        public void EnergySpentClimbsTheTiersAndEachFreeTierPaysOnce()
        {
            var game = NewGame();
            int coins = game.State.Coins;
            game.State.Energy = 10_000;
            game.SpendEnergy(pass.PointsPerTier * 5 / pass.PointsPerEnergySpent, Now);
            Assert.AreEqual(5, game.PassTier);

            Assert.IsTrue(game.ClaimPass(Game.FreeTrack, 1));
            Assert.AreEqual(coins + pass.Rewards.Free.Cycle[0].Coins, game.State.Coins);
            Assert.IsFalse(game.ClaimPass(Game.FreeTrack, 1), "once");
            Assert.IsFalse(game.ClaimPass(Game.FreeTrack, 6), "not reached yet");

            // The fifth free tier pays gems: the platform pays them, once, for this season's tier.
            Assert.IsTrue(game.ClaimPass(Game.FreeTrack, 5));
            CollectionAssert.Contains(game.State.PendingRewards, $"pass:{game.PassSeason}:free:5");
            Assert.AreEqual(3, game.PassToClaim, "tiers 2 to 4 wait on the free track");
        }

        [Test]
        public void ThePremiumTrackNeedsThisSeasonsPassAndGivesItsItems()
        {
            var game = NewGame();
            game.State.Energy = 100_000;
            game.SpendEnergy(pass.PointsPerTier * pass.Tiers, Now);
            Assert.AreEqual(pass.Tiers, game.PassTier, "the last tier and no further");
            Assert.IsFalse(game.ClaimPass(Game.PremiumTrack, 1), "no premium pass yet");

            Assert.IsTrue(game.ApplyPurchase("p-pass", new Dictionary<string, string> { ["passPremium"] = "true" }));
            Assert.IsTrue(game.HasPremiumPass);
            Assert.IsTrue(game.ClaimPass(Game.PremiumTrack, 20));
            Assert.IsNotEmpty(pass.Rewards.Premium.ItemsAt(20));
            foreach (var item in pass.Rewards.Premium.ItemsAt(20)) CollectionAssert.Contains(game.State.Entitlements, item);

            game.Tick(Now + pass.DurationDays * Day);
            Assert.IsFalse(game.HasPremiumPass, "the premium pass lasts one season");
        }

        [Test]
        public void ANewSeasonStartsAgainButASaveFromBeforeSeasonsKeepsItsPoints()
        {
            var game = NewGame();
            game.State.Energy = 10_000;
            game.SpendEnergy(pass.PointsPerTier * 2, Now);
            game.ClaimPass(Game.FreeTrack, 1);
            game.Tick(Now + pass.DurationDays * Day);
            Assert.AreEqual(0, game.PassPoints);
            Assert.AreEqual(0, game.State.PassClaimed.Count);

            var old = NewGame();
            old.State.PassSeason = 0;
            old.State.PassPoints = 900;
            Assert.AreEqual(900, old.PassPoints);
            Assert.AreEqual(old.PassSeason, old.State.PassSeason);
        }

        [Test]
        public void AConfigPublishedBeforeItemListsStillReads()
        {
            // Remote config laid over the game's own: an older published file named only the kind of item.
            string merged = BalanceCheck.Merge(UnityEngine.Resources.Load<UnityEngine.TextAsset>(BalanceConfig.ResourcePath).text,
                "{ \"events\": { \"templates\": { \"pass\": { \"rewards\": { \"premium\": { \"itemsAtTiers\": { \"10\": \"outfit\" } } } } } } }");
            CollectionAssert.IsEmpty(BalanceCheck.Problems(merged));
            var old = BalanceConfig.Parse(merged).Pass;
            CollectionAssert.IsEmpty(old.Rewards.Premium.ItemsAt(10), "a kind alone gives nothing");
            Assert.IsNotEmpty(old.SeasonStart, "keys the old file lacks come from the game's own");
        }

        [Test]
        public void BeforeItsLevelThePassGetsNoPoints()
        {
            var game = NewGame(level: config.Unlocks.Level("season_pass") - 1);
            game.State.Energy = 1_000;
            game.SpendEnergy(500, Now);
            Assert.AreEqual(0, game.PassPoints);
            Assert.IsFalse(game.ClaimPass(Game.FreeTrack, 1));
        }

        [Test]
        public void EveryPassItemIsSomethingTheGameHas()
        {
            var catalog = DecorCatalog.Load();
            foreach (int tier in pass.Rewards.Premium.ItemsAtTiers.Keys)
            foreach (var item in pass.Rewards.Premium.ItemsAt(tier))
            {
                Assert.IsTrue(item.StartsWith("decor:") || item.StartsWith("outfit:"), item);
                if (item.StartsWith("decor:")) Assert.AreEqual("event", catalog.Get(item.Substring(6))?.Source, item);
            }
        }
    }
}
