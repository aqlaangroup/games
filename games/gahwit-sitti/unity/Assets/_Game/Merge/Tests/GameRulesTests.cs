using System.Linq;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Merge.Tests
{
    // Rules checked against the real balance.json and chapter content, so a data change that breaks a rule fails here.
    public sealed class GameRulesTests
    {
        const long Now = 1_000_000_000;

        BalanceConfig config;
        ChapterContent content;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
        }

        Game NewGame(ulong seed = 42) => new Game(config, content, Game.NewState(config, content, seed, Now));

        static int Find(Game game, System.Func<Item, bool> match)
        {
            for (int i = 0; i < game.CellCount; i++)
                if (game.ItemAt(i) != null && match(game.ItemAt(i))) return i;
            return -1;
        }

        static int Generator(Game game, string id) => Find(game, p => p.Generator == id);

        [Test]
        public void XpToNextMatchesTheDesignExamples()
        {
            // GAME_SYSTEMS.md section 8: 92 XP from level 1, 560 from level 10, 1,460 from level 20.
            Assert.AreEqual(92, config.XpToNext(1));
            Assert.AreEqual(560, config.XpToNext(10));
            Assert.AreEqual(1460, config.XpToNext(20));
        }

        [Test]
        public void ANewGameStartsWithFourGreenBeansBothGeneratorsAndAbuFadisOrder()
        {
            var game = NewGame();
            Assert.AreEqual(4, game.CountOnBoard("coffee", 1)); // COMPLETE_SPEC.md section 1, step 4
            Assert.GreaterOrEqual(Generator(game, "gen_coffee"), 0);
            Assert.GreaterOrEqual(Generator(game, "gen_sweets"), 0);
            Assert.AreEqual(config.OrderSlots(1), game.Orders.Count);
            Assert.AreEqual("abu_fadi", game.Orders[0].Customer);
            Assert.AreEqual(config.Energy.StartingEnergy, game.State.Energy);
        }

        [Test]
        public void TwoIdenticalItemsMergeIntoTheNextLevel()
        {
            var game = NewGame();
            int a = Find(game, p => p.Is("coffee", 1));
            game.State.Board[a] = null;
            int b = Find(game, p => p.Is("coffee", 1));
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
        public void TappingAGeneratorCostsEnergyAndPlacesAnItemNextToIt()
        {
            var game = NewGame();
            int generator = Generator(game, "gen_coffee");
            int empty = game.EmptyCells;
            Assert.AreEqual(TapResult.Spawned, game.TapGenerator(generator, Now, out int cell));
            Assert.AreEqual(config.Energy.StartingEnergy - config.Generators.TapEnergyCost, game.State.Energy);
            Assert.AreEqual(empty - 1, game.EmptyCells);
            Assert.AreEqual("coffee", game.ItemAt(cell).Chain);
            Assert.IsFalse(game.ItemAt(cell).IsGenerator);
            Assert.LessOrEqual(System.Math.Abs(cell % game.Cols - generator % game.Cols), 1);
        }

        [Test]
        public void TappingWithNoEnergyOrAFullBoardOrANonGeneratorDoesNothing()
        {
            var game = NewGame();
            int generator = Generator(game, "gen_coffee");
            Assert.AreEqual(TapResult.NotAGenerator, game.TapGenerator(Find(game, p => p.Is("coffee", 1)), Now, out _));

            game.State.Energy = 0;
            Assert.AreEqual(TapResult.NoEnergy, game.TapGenerator(generator, Now, out _));

            game.State.Energy = 10;
            for (int i = 0; i < game.CellCount; i++)
                if (game.ItemAt(i) == null) game.State.Board[i] = new Item("sweets", 1);
            Assert.AreEqual(TapResult.BoardFull, game.TapGenerator(generator, Now, out _));
            Assert.AreEqual(10, game.State.Energy);
        }

        [Test]
        public void TwoGeneratorsOfTheSameLevelMergeAndGeneratorsCannotBeSold()
        {
            var game = NewGame();
            int generator = Generator(game, "gen_coffee");
            game.State.Board[0] = new Item("coffee", 1, "gen_coffee");
            Assert.AreEqual(0, game.Sell(0));
            Assert.AreEqual(MoveResult.Merged, game.Move(0, generator));
            Assert.AreEqual(2, game.ItemAt(generator).Level);
            Assert.IsTrue(game.ItemAt(generator).IsGenerator);

            game.State.Board[0] = new Item("sweets", 1, "gen_sweets");
            Assert.IsFalse(game.CanMerge(0, generator)); // a different generator
        }

        [Test]
        public void EnergyRefillsOneEveryRegenIntervalUpToTheCap()
        {
            var game = NewGame();
            int generator = Generator(game, "gen_coffee");
            game.TapGenerator(generator, Now, out _);
            game.TapGenerator(generator, Now, out _);
            long regen = config.Energy.RegenSeconds * 1000L;

            game.Tick(Now + regen - 1);
            Assert.AreEqual(config.Energy.Cap - 2, game.State.Energy);
            game.Tick(Now + regen);
            Assert.AreEqual(config.Energy.Cap - 1, game.State.Energy);
            game.Tick(Now + regen * 50);
            Assert.AreEqual(config.Energy.Cap, game.State.Energy);
        }

        [Test]
        public void ChangingThePhoneClockDoesNotAddEnergy()
        {
            var phone = new FakeClock { UtcNowMs = Now, UptimeMs = 1_000 };
            var time = new TimeService(phone, null);
            var game = new Game(config, content, Game.NewState(config, content, 1, time.NowMs));
            game.TapGenerator(Generator(game, "gen_coffee"), time.NowMs, out _);
            int energy = game.State.Energy;

            phone.UtcNowMs += 24L * 60 * 60 * 1000; // a day ahead on the phone's clock only
            game.Tick(time.NowMs);
            Assert.AreEqual(energy, game.State.Energy);
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
                    Assert.AreEqual(order.Lines.Sum(l => l.Count * config.ItemUnits(l.Level)), order.Units);
                    Assert.GreaterOrEqual(order.Coins, config.Formulas.OrderMinCoins);
                }
                var first = game.Orders[0];
                foreach (var line in first.Lines)
                    for (int n = 0; n < line.Count; n++) game.State.Board[game.NearestEmptyCell(0)] = new Item(line.Chain, line.Level);
                Assert.IsTrue(game.Serve(first.Id));
            }
        }

        [Test]
        public void FinishingChapterOnePaysEnergyAndPutsAGeneratorCopyOnTheBoard()
        {
            var game = NewGame();
            var chapter = config.Chapter(1);
            game.State.Coins = chapter.TaskCosts.Sum();
            int energyBefore = game.State.Energy;
            int finished = 0;
            game.ChapterCompleted += n => finished = n;

            for (int i = 0; i < chapter.TaskCosts.Count; i++) Assert.IsNotNull(game.CompleteTask());

            Assert.AreEqual(1, finished);
            Assert.AreEqual(2, game.State.Chapter);
            Assert.AreEqual(0, game.State.Coins);
            Assert.GreaterOrEqual(game.State.Energy, energyBefore + chapter.Reward.Energy);
            Assert.AreEqual(2, game.State.Board.Count(p => p != null && p.Generator == chapter.Reward.GeneratorCopy));
            CollectionAssert.AreEqual(content.ForChapter(1).Tasks.Select(t => t.Id), game.CompletedTaskIds());
        }

        [Test]
        public void ATaskCannotBeBoughtWithoutEnoughCoinsAndRemembersItsStyle()
        {
            var game = NewGame();
            game.State.Coins = game.CurrentTaskCost - 1;
            Assert.IsFalse(game.CanAffordTask);
            Assert.IsNull(game.CompleteTask());
            Assert.AreEqual(0, game.State.TaskIndex);

            game.State.Coins += 1;
            var task = game.CompleteTask("modern");
            Assert.AreEqual("modern", game.State.TaskStyles[task.Id]);
        }

        sealed class FakeClock : IDeviceClock
        {
            public long UtcNowMs { get; set; }
            public long UptimeMs { get; set; }
        }
    }
}
