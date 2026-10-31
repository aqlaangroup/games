using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using NUnit.Framework;

namespace Gahwit.Decor.Tests
{
    public sealed class DecorRulesTests
    {
        const long Now = 1_000_000_000;

        BalanceConfig config;
        ChapterContent content;
        DecorCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
            catalog = DecorCatalog.Load();
        }

        // A 10 × 8 m floor with one corner kept clear, like the counter.
        DecorLayout Layout() => new DecorLayout(10, 8, config.LevelB.Cafe.GridCellMeters,
            new[] { new DecorLayout.Area(3, 2, 5, 4) });

        Game NewGame(int level = 6, int coins = 10_000)
        {
            var game = new Game(config, content, Game.NewState(config, content, 3, Now));
            game.State.Level = level;
            game.State.Coins = coins;
            return game;
        }

        [Test]
        public void TheCatalogHasItsShopPiecesAndARewardForEachMemoryAndRegular()
        {
            Assert.AreEqual(14, catalog.Pieces.Count(p => p.Source == "coins"), "ring 1's shop");
            var content = ChapterContent.Load();
            CollectionAssert.AreEquivalent(content.Memories.Keys, catalog.Pieces.Where(p => p.Source == "memory").Select(p => p.Memory),
                "one 3-star piece per memory (SYSTEMS_B.md section 5)");
            CollectionAssert.AreEquivalent(config.LevelB.Regulars.Schedule.Keys,
                catalog.Pieces.Where(p => p.Source == "friendship").Select(p => p.Regular), "one level-10 gift per regular (section 3)");
            Assert.IsTrue(catalog.Pieces.All(p => p.Cosiness >= config.LevelB.Cafe.CosinessPerPiece[0] && p.Cosiness <= config.LevelB.Cafe.CosinessPerPiece[1]));
            Assert.IsTrue(catalog.Pieces.Where(p => p.Source == "coins").All(p => p.PriceTier < config.LevelB.Cafe.DecorPriceCoinsByTier.Count));
            Assert.AreEqual(5, catalog.Pieces.Count(p => p.FromLevel == config.Unlocks.Level("decor_shop")), "5 pieces when the shop opens");
        }

        [Test]
        public void PiecesSnapNeverOverlapAndKeepClear()
        {
            var layout = Layout();
            var rug = catalog.Get("old_rug");
            var placed = new System.Collections.Generic.List<PlacedDecor> { new PlacedDecor { Id = "old_rug", X = 0, Z = 0 } };
            Assert.IsFalse(layout.Fits(rug, 4, 2, 0, placed, catalog), "overlaps the first rug");
            Assert.IsTrue(layout.Fits(rug, rug.W, 0, 0, placed, catalog), "right beside it");
            Assert.IsFalse(layout.Fits(catalog.Get("basil_pot"), 34, 26, 0, placed, catalog), "inside the kept-clear corner");
            Assert.IsFalse(layout.Fits(rug, layout.Cols - 2, 0, 0, placed, catalog), "past the edge");
            Assert.AreEqual((6, 8), DecorLayout.Footprint(rug, 1), "a quarter turn swaps the footprint");
            var (x, z) = layout.Snap(0, 0, rug, 0);
            var (cx, cz) = layout.Centre(rug, x, z, 0);
            Assert.AreEqual(0, cx, 0.13f);
            Assert.AreEqual(0, cz, 0.13f);
        }

        [Test]
        public void BuyingPaysPlacesAndAddsCosinessWithAStyleBonus()
        {
            var game = NewGame();
            var rules = new DecorRules(game, catalog, Layout());
            int coins = game.State.Coins;

            Assert.IsTrue(rules.Buy("old_lantern"));
            Assert.AreEqual(coins - config.LevelB.Cafe.DecorPriceCoinsByTier[0], game.State.Coins);
            Assert.IsTrue(rules.IsPlaced("old_lantern"));
            Assert.AreEqual(1, game.State.Cosiness);

            rules.Buy("old_rug");
            rules.Buy("old_copper_table");
            Assert.AreEqual(1 + 3 + 3 + config.LevelB.Cafe.StyleSetBonus, game.State.Cosiness, "three Old Levantine pieces");
            Assert.IsFalse(rules.Buy("old_rug"), "already owned");
        }

        [Test]
        public void PiecesMoveTurnAndGoToStorage()
        {
            var game = NewGame();
            var rules = new DecorRules(game, catalog, Layout());
            rules.Buy("modern_shelf");
            var shelf = game.State.PlacedDecor.Single();
            Assert.IsTrue(rules.Move("modern_shelf", 10, 10));
            Assert.AreEqual((10, 10), (shelf.X, shelf.Z));
            Assert.IsTrue(rules.Rotate("modern_shelf"));
            Assert.AreEqual(1, shelf.Rot);
            Assert.IsTrue(rules.Store("modern_shelf"));
            Assert.AreEqual(0, game.State.Cosiness);
            CollectionAssert.Contains(rules.Stored.Select(p => p.Id).ToList(), "modern_shelf");
            Assert.IsTrue(rules.Place("modern_shelf"));
        }

        [Test]
        public void TheShopOpensAtItsLevelAndCoinsMustCover()
        {
            var early = NewGame(level: 1);
            Assert.IsFalse(new DecorRules(early, catalog, Layout()).Buy("basil_pot"));
            var poor = NewGame(coins: 10);
            Assert.IsFalse(new DecorRules(poor, catalog, Layout()).Buy("basil_pot"));
            var mid = NewGame(level: 2);
            Assert.AreEqual(5, new DecorRules(mid, catalog, Layout()).InShop.Count());
        }

        [Test]
        public void ThreeStarsInThePourMemoryGiveTetasRadio()
        {
            var game = NewGame();
            var rules = new DecorRules(game, catalog, Layout());
            game.RecordMemory("m1_pour", 2);
            Assert.IsFalse(rules.Owns("teta_radio"));
            game.RecordMemory("m1_pour", 3);
            Assert.IsTrue(rules.Owns("teta_radio"));
            Assert.IsTrue(rules.IsPlaced("teta_radio"));
        }

        [Test]
        public void ARegularAtFriendshipTenGivesTheirPiece()
        {
            var game = NewGame();
            var rules = new DecorRules(game, catalog, Layout());
            var gift = catalog.Get("abu_fadi_clock");
            game.State.Regulars["abu_fadi"] = new RegularState { Level = gift.FriendshipLevel - 1 };
            game.Touch();
            Assert.IsFalse(rules.Owns(gift.Id));
            game.State.Regulars["abu_fadi"].Level = gift.FriendshipLevel;
            game.Touch();
            Assert.IsTrue(rules.Owns(gift.Id));
        }

        [Test]
        public void TaskStylesChangeForFreeOnceTheTaskIsDone()
        {
            var game = NewGame();
            var rules = new DecorRules(game, catalog, Layout());
            Assert.IsFalse(rules.SetTaskStyle("sign", "gulf"), "not restored yet");
            game.CompleteTask("old");
            int coins = game.State.Coins;
            Assert.IsTrue(rules.SetTaskStyle("sign", "gulf"));
            Assert.AreEqual("gulf", game.State.TaskStyles["sign"]);
            Assert.AreEqual(coins, game.State.Coins);
        }
    }
}
