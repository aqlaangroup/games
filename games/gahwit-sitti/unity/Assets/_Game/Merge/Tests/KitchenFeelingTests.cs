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
        public void AStoryOrderComesOnCuePaysMoreAndNeverReturns()
        {
            content.StoryOrders = new List<StoryOrderDef>
            {
                new StoryOrderDef { Id = "test_story", Chapter = 1, AtTask = 1, Character = "abu_fadi",
                    Lines = { new StoryOrderLine { Chain = "coffee", Level = 2, Count = 1 } } }
            };
            var game = NewGame();
            Assert.IsFalse(game.State.Orders.Any(o => o.Story != null), "not before its task");

            game.State.Coins = 10_000;
            game.CompleteTask();
            var story = game.State.Orders.Single(o => o.Story == "test_story");
            Assert.AreEqual("abu_fadi", story.Customer);
            var plain = game.MakeOrder("abu_fadi", true, new List<OrderLine> { new OrderLine { Chain = "coffee", Level = 2, Count = 1 } });
            Assert.AreEqual((int)System.Math.Round(plain.Coins * config.LevelB.Kitchen.StoryOrderCoinMultiplier), story.Coins);

            Order served = null;
            game.StoryOrderServed += o => served = o;
            game.State.Board[Empty(game)] = new Item("coffee", 2);
            Assert.IsTrue(game.Serve(story.Id));
            Assert.AreSame(story, served);
            CollectionAssert.Contains(game.State.StoryOrdersDone, "test_story");
            Assert.IsFalse(game.State.Orders.Any(o => o.Story == "test_story"), "served once, gone for good");
        }
    }
}
