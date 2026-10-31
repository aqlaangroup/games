using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Merge.Tests
{
    // Teta's notebook, serving streaks and story orders (DEVLOG 2026-10-31).
    public sealed class KitchenFeelingTests
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

        Game NewGame()
        {
            var game = new Game(config, content, Game.NewState(config, content, 5, Now));
            game.State.TutorialDone = true;
            game.Tick(Now);
            return game;
        }

        static int Empty(Game game) => System.Array.FindIndex(game.State.Board, i => i == null);

        // A small walk-in order with its item already on the board.
        static Order Ready(Game game, string chain = "coffee", int level = 1)
        {
            var order = game.MakeOrder(null, false, new List<OrderLine> { new OrderLine { Chain = chain, Level = level, Count = 1 } });
            game.State.Orders.Add(order);
            game.State.Board[Empty(game)] = new Item(chain, level);
            return order;
        }

        [Test]
        public void TheFirstTimeAnItemIsMadeItsPageOpensOnce()
        {
            var game = NewGame();
            var found = new List<string>();
            game.PageFound += (chain, level) => found.Add(Game.PageId(chain, level));
            int coins = game.State.Coins;

            int a = Empty(game);
            game.State.Board[a] = new Item("sweets", 3);
            int b = Empty(game);
            game.State.Board[b] = new Item("sweets", 3);
            Assert.AreEqual(MoveResult.Merged, game.Move(a, b));
            CollectionAssert.AreEqual(new[] { "sweets_4" }, found);
            Assert.AreEqual(coins + config.LevelB.Kitchen.NotebookCoinsPerPage, game.State.Coins);

            game.State.Board[a] = new Item("sweets", 3);
            int c = Empty(game);
            game.State.Board[c] = new Item("sweets", 3);
            game.Move(a, c);
            Assert.AreEqual(1, found.Count, "a page opens only once");
        }

        [Test]
        public void FindingEveryPageOfAChainGivesEnergy()
        {
            var game = NewGame();
            int top = game.MaxLevel("tea");
            for (int level = 1; level < top; level++) game.State.NotebookPages.Add(Game.PageId("tea", level));
            string done = null;
            game.NotebookChainDone += chain => done = chain;
            int energy = game.State.Energy;

            int a = Empty(game);
            game.State.Board[a] = new Item("tea", top - 1);
            int b = Empty(game);
            game.State.Board[b] = new Item("tea", top - 1);
            game.Move(a, b);

            Assert.AreEqual("tea", done);
            Assert.AreEqual(energy + config.LevelB.Kitchen.NotebookChainEnergy, game.State.Energy);
        }

        [Test]
        public void OrdersServedCloseTogetherBuildAStreak()
        {
            var kitchen = config.LevelB.Kitchen;
            var game = NewGame();

            Assert.IsTrue(game.Serve(Ready(game).Id));
            Assert.AreEqual(1, game.Streak);
            Assert.AreEqual(0, game.LastStreakBonus, "the first serve of a streak earns no bonus");

            game.Tick(Now + (kitchen.StreakWindowSeconds - 1) * 1000L);
            int coins = game.State.Coins;
            var second = Ready(game);
            Assert.IsTrue(game.Serve(second.Id));
            Assert.AreEqual(2, game.Streak);
            int bonus = (int)System.Math.Round(game.LastServedCoins * kitchen.StreakBonusPercent[1] / 100.0);
            Assert.AreEqual(bonus, game.LastStreakBonus);
            Assert.AreEqual(coins + game.LastServedCoins + bonus, game.State.Coins);

            game.Tick(Now + (kitchen.StreakWindowSeconds * 3) * 1000L);
            Assert.IsTrue(game.Serve(Ready(game).Id));
            Assert.AreEqual(1, game.Streak, "too long after the last one: a new streak");
        }

        [Test]
        public void EveryStoryOrderInTheSeasonCanBeMadeWhenItComes()
        {
            Assert.IsNotEmpty(content.StoryOrders);
            var ids = new HashSet<string>();
            foreach (var def in content.StoryOrders)
            {
                Assert.IsTrue(ids.Add(def.Id), $"{def.Id}: the id is used twice");
                Assert.That(def.Chapter, Is.InRange(1, content.Chapters.Count), def.Id);
                Assert.That(def.AtTask, Is.InRange(1, content.Chapters[def.Chapter - 1].Tasks.Count), $"{def.Id}: no such task");
                Assert.IsTrue(content.Characters.TryGetValue(def.Character, out var who), $"{def.Id}: no character {def.Character}");
                Assert.IsTrue(who.PlacesOrders, $"{def.Id}: {def.Character} doesn't place orders");
                Assert.LessOrEqual(who.FromChapter, def.Chapter, $"{def.Id}: {def.Character} isn't in the café yet");
                Assert.IsNotEmpty(def.Lines, def.Id);
                foreach (var line in def.Lines)
                {
                    Assert.IsTrue(config.Chains.TryGetValue(line.Chain, out var chain), $"{def.Id}: no chain {line.Chain}");
                    Assert.LessOrEqual(chain.UnlockChapter, def.Chapter, $"{def.Id}: {line.Chain} isn't open yet");
                    Assert.That(line.Level, Is.InRange(1, chain.Items.Count), $"{def.Id}: {line.Chain} has no level {line.Level}");
                    Assert.GreaterOrEqual(line.Count, 1, def.Id);
                }
            }
        }

        [Test]
        public void AStoryOrderTakesTheNextFreeSeatAndNeverReturns()
        {
            content.StoryOrders = new List<StoryOrderDef>
            {
                new StoryOrderDef { Id = "test_story", Chapter = 1, AtTask = 1, Character = "abu_fadi",
                    Lines = { new StoryOrderLine { Chain = "coffee", Level = 2, Count = 1 } } },
                new StoryOrderDef { Id = "test_story_2", Chapter = 1, AtTask = 1, Character = "abu_fadi",
                    Lines = { new StoryOrderLine { Chain = "coffee", Level = 1, Count = 1 } } }
            };
            var game = NewGame();
            int seats = config.OrderSlots(game.State.Level);
            WalkInsInEverySeat(game);

            game.State.Coins = 10_000;
            game.CompleteTask();
            Assert.IsFalse(game.State.Orders.Any(o => o.Story != null), "every seat is taken: the story's customer waits");

            // A seat frees up: the story's customer takes it before anyone else, and the café stays at its seats.
            ServeFirst(game);
            var story = game.State.Orders.Single(o => o.Story != null);
            Assert.AreEqual("test_story", story.Story);
            Assert.AreEqual("abu_fadi", story.Customer);
            Assert.AreEqual(seats, game.State.Orders.Count);
            var plain = game.MakeOrder("abu_fadi", true, new List<OrderLine> { new OrderLine { Chain = "coffee", Level = 2, Count = 1 } });
            Assert.AreEqual(plain.Coins, story.Coins, "it pays like any order; its reward is friendship");

            ServeFirst(game);
            Assert.IsFalse(game.State.Orders.Any(o => o.Story == "test_story_2"), "Abu Fadi has one order at a time");

            Order served = null;
            game.StoryOrderServed += o => served = o;
            game.State.Board[Empty(game)] = new Item("coffee", 2);
            Assert.IsTrue(game.Serve(story.Id));
            Assert.AreSame(story, served);
            CollectionAssert.Contains(game.State.StoryOrdersDone, "test_story");
            Assert.IsFalse(game.State.Orders.Any(o => o.Story == "test_story"), "served once, gone for good");
            Assert.IsTrue(game.State.Orders.Any(o => o.Story == "test_story_2"), "his next one takes the seat he left");
        }

        [Test]
        public void AStoryOrderThatMissedItsChapterComesInTheNextOnly()
        {
            content.StoryOrders = new List<StoryOrderDef>
            {
                new StoryOrderDef { Id = "older", Chapter = 1, AtTask = 1, Character = "samira",
                    Lines = { new StoryOrderLine { Chain = "coffee", Level = 1, Count = 1 } } },
                new StoryOrderDef { Id = "missed", Chapter = 2, AtTask = 6, Character = "abu_fadi",
                    Lines = { new StoryOrderLine { Chain = "coffee", Level = 1, Count = 1 } } }
            };
            var game = NewGame();
            game.State.Chapter = 3;
            game.State.TaskIndex = 0;
            WalkInsInEverySeat(game);

            ServeFirst(game);
            Assert.IsTrue(game.State.Orders.Any(o => o.Story == "missed"), "the last chapter's comes at the first free seat");
            Assert.IsFalse(game.State.Orders.Any(o => o.Story == "older"), "two chapters back: gone");
        }

        [Test]
        public void NoCustomerEverHasTwoOrdersWaiting()
        {
            var game = NewGame();
            for (int n = 0; n < 300; n++)
            {
                var waiting = game.State.Orders.Where(o => o.IsCharacter).Select(o => o.Customer).ToList();
                CollectionAssert.AllItemsAreUnique(waiting, $"after {n} served");
                ServeFirst(game);
            }
        }

        // Walk-ins in every seat, so a test doesn't depend on who the seed sent.
        static void WalkInsInEverySeat(Game game)
        {
            game.State.Orders.Clear();
            while (game.State.Orders.Count < game.Config.OrderSlots(game.State.Level))
                game.State.Orders.Add(game.MakeOrder(null, false, new List<OrderLine> { new OrderLine { Chain = "sweets", Level = 1, Count = 1 } }));
        }

        // Puts what the first order wants on the board and serves it.
        static void ServeFirst(Game game)
        {
            var order = game.State.Orders[0];
            foreach (var line in order.Lines)
                for (int n = 0; n < line.Count; n++) game.State.Board[Empty(game)] = new Item(line.Chain, line.Level);
            Assert.IsTrue(game.Serve(order.Id));
        }
    }
}
