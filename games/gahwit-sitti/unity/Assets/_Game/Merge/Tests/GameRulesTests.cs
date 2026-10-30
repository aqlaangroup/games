using System.Linq;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Merge.Tests
{
    // Rules checked against the real balance.json and chapter content, so a data change that breaks a rule fails here.
    public sealed class GameRulesTests
    {
        const long Now = 1_000_000;

        BalanceConfig config;
        ChapterContent content;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
        }

        Game NewGame(ulong seed = 42)
        {
            var state = Game.NewState(config, content, seed, Now);
            return new Game(config, content, state);
        }

        static int FirstWith(Game game, string chain, int level)
        {
            for (int i = 0; i < game.CellCount; i++)
                if (game.ItemAt(i) != null && game.ItemAt(i).Is(chain, level)) return i;
            return -1;
        }

        [Test]
        public void XpToNextMatchesTheDesignExamples()
        {
            // GAME_SYSTEMS.md section 8: 92 XP from level 1, 560 from level 10, 1,460 from level 20.
            Assert.AreEqual(92, config.XpToNext(1));
            Assert.AreEqual(560, config.XpToNext(10));
            Assert.AreEqual(1460, config.XpToNext(20));
        }

        [Test]
        public void NewGameHasTwoGeneratorsAndTheFirstOrderFromAbuFadi()
        {
            var game = NewGame();
            CollectionAssert.AreEquivalent(new[] { "coffee", "sweets" }, game.Generators.Select(g => g.Chain));
            Assert.AreEqual(config.OrderSlots(1), game.Orders.Count);
            Assert.AreEqual("abufadi", game.Orders[0].Customer);
            Assert.AreEqual(config.Energy.StartingEnergy, game.State.Energy);
        }

        [Test]
        public void TwoIdenticalItemsMergeIntoTheNextLevel()
        {
            var game = NewGame();
            int a = FirstWith(game, "coffee", 1);
            game.State.Board[a] = null;
            int b = FirstWith(game, "coffee", 1);

            game.State.Board[0] = new Item("coffee", 1);
            Assert.AreEqual(MoveResult.Merged, game.Move(0, b));
            Assert.IsNull(game.ItemAt(0));
            Assert.IsTrue(game.ItemAt(b).Is("coffee", 2));
        }

        [Test]
        public void DifferentItemsSwapAndTopLevelItemsDoNotMerge()
        {
            var game = NewGame();
            game.State.Board[0] = new Item("coffee", 1);
            game.State.Board[1] = new Item("sweets", 1);
            Assert.AreEqual(MoveResult.Swapped, game.Move(0, 1));
            Assert.IsTrue(game.ItemAt(0).Is("sweets", 1));

            int top = game.MaxLevel("coffee");
            game.State.Board[0] = new Item("coffee", top);
            game.State.Board[1] = new Item("coffee", top);
            Assert.AreEqual(MoveResult.Swapped, game.Move(0, 1));
        }

        [Test]
        public void TappingAGeneratorCostsEnergyAndPlacesAnItem()
        {
            var game = NewGame();
            int empty = game.EmptyCells;
            Assert.AreEqual(TapResult.Spawned, game.TapGenerator(0, Now, out int cell));
            Assert.AreEqual(config.Energy.StartingEnergy - config.Generators.TapEnergyCost, game.State.Energy);
            Assert.AreEqual(empty - 1, game.EmptyCells);
            Assert.AreEqual(game.Generators[0].Chain, game.ItemAt(cell).Chain);
        }

        [Test]
        public void TappingWithNoEnergyOrAFullBoardDoesNothing()
        {
            var game = NewGame();
            game.State.Energy = 0;
            Assert.AreEqual(TapResult.NoEnergy, game.TapGenerator(0, Now, out _));

            game.State.Energy = 10;
            for (int i = 0; i < game.CellCount; i++) game.State.Board[i] = new Item("sweets", 1);
            Assert.AreEqual(TapResult.BoardFull, game.TapGenerator(0, Now, out _));
            Assert.AreEqual(10, game.State.Energy);
        }

        [Test]
        public void EnergyRefillsOneEveryRegenIntervalUpToTheCap()
        {
            var game = NewGame();
            game.TapGenerator(0, Now, out _);
            game.TapGenerator(0, Now, out _);
            int regen = config.Energy.RegenSeconds;

            game.Tick(Now + regen - 1);
            Assert.AreEqual(config.Energy.Cap - 2, game.State.Energy);
            game.Tick(Now + regen);
            Assert.AreEqual(config.Energy.Cap - 1, game.State.Energy);
            game.Tick(Now + regen * 50);
            Assert.AreEqual(config.Energy.Cap, game.State.Energy);
        }

        [Test]
        public void ServingRemovesTheItemsAndPaysCoinsAndXp()
        {
            var game = NewGame();
            var order = game.Orders[0];
            var line = order.Lines[0];
            for (int n = 0; n < line.Count; n++) game.State.Board[n] = new Item(line.Chain, line.Level);
            int before = game.CountOnBoard(line.Chain, line.Level);

            Assert.IsTrue(game.Serve(order.Id));
            Assert.AreEqual(before - line.Count, game.CountOnBoard(line.Chain, line.Level));
            Assert.AreEqual(order.Coins, game.State.Coins);
            Assert.AreEqual(config.OrderSlots(game.State.Level), game.Orders.Count);
            Assert.IsFalse(game.Orders.Any(o => o.Id == order.Id));
        }

        [Test]
        public void OrdersStayWithinTheLevelCapAndPayPerUnit()
        {
            var game = NewGame(7);
            for (int round = 0; round < 50; round++)
            {
                foreach (var order in game.Orders)
                {
                    foreach (var line in order.Lines)
                    {
                        Assert.LessOrEqual(line.Level, config.MaxOrderItemLevel(game.State.Level));
                        Assert.That(line.Count, Is.InRange(1, config.Formulas.OrderMaxItemsPerLine));
                    }
                    int units = order.Lines.Sum(l => l.Count * config.ItemUnits(l.Level));
                    Assert.AreEqual(units, order.Units);
                    Assert.GreaterOrEqual(order.Coins, config.Formulas.OrderMinCoins);
                }
                var first = game.Orders[0];
                foreach (var line in first.Lines)
                    for (int n = 0; n < line.Count; n++) game.State.Board[game.NearestEmptyCell()] = new Item(line.Chain, line.Level);
                Assert.IsTrue(game.Serve(first.Id));
            }
        }

        [Test]
        public void FinishingAllChapterOneTasksPaysTheChapterReward()
        {
            var game = NewGame();
            var chapter = config.Chapter(1);
            game.State.Coins = chapter.TaskCosts.Sum();
            int energyBefore = game.State.Energy;
            int coffeeLevel = game.Generators.First(g => g.Id == chapter.Reward.GeneratorCopy).Level;
            int finished = 0;
            game.ChapterCompleted += n => finished = n;

            for (int i = 0; i < chapter.TaskCosts.Count; i++) Assert.IsNotNull(game.CompleteTask());

            Assert.AreEqual(1, finished);
            Assert.AreEqual(2, game.State.Chapter);
            Assert.AreEqual(0, game.State.Coins);
            Assert.GreaterOrEqual(game.State.Energy, energyBefore + chapter.Reward.Energy);
            Assert.AreEqual(coffeeLevel + 1, game.Generators.First(g => g.Id == chapter.Reward.GeneratorCopy).Level);
            Assert.AreEqual(6, game.State.CompletedTasks.Count);
        }

        [Test]
        public void ATaskCannotBeBoughtWithoutEnoughCoins()
        {
            var game = NewGame();
            game.State.Coins = game.CurrentTaskCost - 1;
            Assert.IsFalse(game.CanAffordTask);
            Assert.IsNull(game.CompleteTask());
            Assert.AreEqual(0, game.State.TaskIndex);
        }
    }
}
