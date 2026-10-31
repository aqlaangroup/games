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
            Assert.AreEqual(20, catalog.Pieces.Count(p => p.Source == "coins"), "ring 1's shop and its wall pieces");
            Assert.AreEqual(6, catalog.Pieces.Count(p => p.Wall), "two wall pieces in each style");
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

        // A café's two walls, 10 × 8 m, in half-metre cells, with the door kept clear on the side wall.
        static WallLayout Walls() => new WallLayout(10, 8, 0.5f, new[] { (1.0f, 2.6f) });

        [Test]
        public void WallPiecesHangOnOneWallKeepClearAndNeverOverlap()
        {
            var walls = Walls();
            var plates = catalog.Get("old_hebron_plates");
            var clock = catalog.Get("modern_wall_clock");
            var placed = new System.Collections.Generic.List<PlacedDecor>();

            Assert.AreEqual(16, walls.SideCells);
            Assert.IsTrue(walls.FindSpot(plates, placed, catalog, out int x));
            Assert.AreEqual(walls.SideCells, x, "the back wall first");
            Assert.IsFalse(walls.Fits(plates, walls.SideCells - 1, placed, catalog), "never round the corner");
            Assert.IsFalse(walls.Fits(clock, 3, placed, catalog), "the door");
            Assert.IsTrue(walls.Fits(clock, 6, placed, catalog));

            placed.Add(new PlacedDecor { Id = plates.Id, X = x });
            Assert.IsFalse(walls.Fits(clock, x + 2, placed, catalog), "over the plates");
            Assert.IsTrue(walls.Fits(clock, x + plates.W, placed, catalog), "beside them");
            placed.Add(new PlacedDecor { Id = "old_rug", X = 0, Z = 0 });
            Assert.IsTrue(walls.Fits(clock, 0, placed, catalog), "floor pieces don't count on the walls");
            Assert.IsTrue(Layout().Fits(catalog.Get("basil_pot"), x, 0, 0, placed.GetRange(0, 1), catalog), "nor wall pieces on the floor");
        }

        [Test]
        public void AWallPieceSlidesAlongTheNearerWallAndFacesTheRoom()
        {
            var walls = Walls();
            var clock = catalog.Get("modern_wall_clock");

            // A finger behind the back wall, in the middle: the back wall's middle.
            int back = walls.Snap(0f, -6f, clock);
            Assert.GreaterOrEqual(back, walls.SideCells);
            var (bx, bz, facing) = walls.Hang(clock, back);
            Assert.AreEqual(0f, bx, 0.3f);
            Assert.AreEqual(-4f, bz, 0.05f);
            Assert.AreEqual(0, facing);

            // A finger beyond the side wall, near the front: low on the side wall, facing across the room.
            int side = walls.Snap(-7f, 3f, clock);
            Assert.Less(side, walls.SideCells);
            var (sx, sz, sideFacing) = walls.Hang(clock, side);
            Assert.AreEqual(-5f, sx, 0.05f);
            Assert.AreEqual(3f, sz, 0.3f);
            Assert.AreEqual(90, sideFacing);
        }

        [Test]
        public void BoughtWallPiecesHangAndDoNotTurn()
        {
            var game = NewGame();
            var rules = new DecorRules(game, catalog, Layout(), Walls());
            Assert.IsTrue(rules.Buy("old_hebron_plates"));
            var plates = game.State.PlacedDecor.Single(p => p.Id == "old_hebron_plates");
            Assert.IsTrue(rules.IsWall(plates.Id));
            Assert.AreEqual(0, rules.Facing(plates));
            Assert.IsFalse(rules.Rotate(plates.Id));
            Assert.IsTrue(rules.MoveTo(plates.Id, -7f, -1f), "slides onto the side wall, behind the door");
            Assert.AreEqual(90, rules.Facing(plates));
            Assert.AreEqual(catalog.Get("old_hebron_plates").Cosiness, game.State.Cosiness);

            var noWalls = new DecorRules(NewGame(), catalog, Layout());
            Assert.IsTrue(noWalls.Buy("modern_wall_clock"));
            CollectionAssert.Contains(noWalls.Stored.Select(p => p.Id).ToList(), "modern_wall_clock", "no walls: kept in storage");
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
