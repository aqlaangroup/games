using System.Linq;
using Gahwit.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Gahwit.Merge.Tests
{
    public sealed class LiveOpsTests
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

        Game NewGame(int level = 10)
        {
            var game = new Game(config, content, Game.NewState(config, content, 4, Now));
            game.State.Level = level;
            return game;
        }

        [Test]
        public void TheDailyGiftGoesRoundItsSevenDaysOnceADay()
        {
            var game = NewGame();
            int energy = game.State.Energy;
            Assert.IsTrue(game.GiftDue(Today));
            var first = game.ClaimGift(Today);
            Assert.AreEqual(config.DailyLogin[0].Energy, first.Energy);
            Assert.AreEqual(energy + first.Energy, game.State.Energy);
            Assert.IsNull(game.ClaimGift(Today), "once a day");
            Assert.AreEqual(2, game.GiftDay);
            for (int day = 2; day <= config.DailyLogin.Count; day++) game.ClaimGift("2026-11-" + day.ToString("00"));
            Assert.AreEqual(1, game.GiftDay, "after day 7 it starts again");
            CollectionAssert.Contains(game.State.Entitlements, "decor:" + config.DailyLogin.Last().Decor);
            Assert.IsFalse(NewGame(level: 1).GiftDue(Today), "from its unlock level");
        }

        [Test]
        public void TodaysTasksStayTheSameAllDayAndCountPlay()
        {
            var game = NewGame();
            var tasks = game.Tasks(Today).Select(t => t.Id).ToList();
            Assert.AreEqual(config.DailyTasks.PerDay, tasks.Count);
            CollectionAssert.AreEqual(tasks, game.Tasks(Today).Select(t => t.Id).ToList());
            Assert.IsTrue(tasks.All(id => config.DailyTasks.List.Find(t => t.Id == id).FromLevel <= game.State.Level));

            var task = game.Tasks(Today)[0];
            var def = game.TaskDef(task.Id);
            Assert.IsFalse(game.ClaimTask(task.Id, Today), "not done yet");
            game.Progress(Game.TaskKind(task.Id), def.Target + 5);
            Assert.AreEqual(def.Target, task.Progress, "progress stops at the target");
            Assert.IsTrue(game.ClaimTask(task.Id, Today));
            Assert.IsFalse(game.ClaimTask(task.Id, Today), "once");
            Assert.AreEqual(config.DailyTasks.RewardEach.PassPoints, game.State.PassPoints);

            game.Tasks(Tomorrow);
            Assert.IsTrue(game.State.DailyTasks.All(t => t.Progress == 0 && !t.Claimed), "a new day starts fresh");
        }

        [Test]
        public void ServingCountsTowardServeTasksAndADoubleWeekendPaysTwice()
        {
            var game = NewGame();
            game.State.DailyTasks = new System.Collections.Generic.List<DailyTaskState> { new DailyTaskState { Id = "serve_10" } };
            game.State.TasksDate = Today;
            var order = game.State.Orders[0];
            foreach (var line in order.Lines)
                for (int n = 0; n < line.Count; n++)
                    game.State.Board[System.Array.FindIndex(game.State.Board, i => i == null)] = new Item(line.Chain, line.Level);
            game.CoinMultiplier = 2;
            int coins = game.State.Coins;
            Assert.IsTrue(game.Serve(order.Id));
            Assert.AreEqual(coins + order.Coins * 2, game.State.Coins);
            Assert.AreEqual(1, game.State.DailyTasks[0].Progress);
        }

        [Test]
        public void TheMiniBoardMergesServesAndPaysMilestonesOnce()
        {
            var game = NewGame();
            var board = new MiniBoard(game, "e1", MiniBoard.Settings(config, null));
            Assert.AreEqual(30, board.State.Board.Length);
            int stall = System.Array.FindIndex(board.State.Board, i => i != null && i.IsGenerator);
            int energy = game.State.Energy;
            Assert.IsTrue(board.Tap(stall, Now));
            Assert.IsTrue(board.Tap(stall, Now));
            Assert.AreEqual(energy - 2, game.State.Energy, "the stall uses the main energy");
            var ones = Enumerable.Range(0, board.State.Board.Length).Where(i => board.State.Board[i] is Item item && !item.IsGenerator).ToList();
            Assert.AreEqual(MoveResult.Merged, board.Move(ones[0], ones[1]));
            Assert.AreEqual(2, board.State.Board[ones[1]].Level);

            // Serve until the first milestone.
            while (board.State.Tokens < board.MilestoneTokens[0])
            {
                board.State.Board[System.Array.FindIndex(board.State.Board, i => i == null)] = new Item(MiniBoard.Chain, board.State.Orders[0]);
                Assert.IsTrue(board.Serve(0));
            }
            CollectionAssert.AreEqual(new[] { 1 }, board.Due());
            int coins = game.State.Coins;
            var reward = board.Pay(1);
            Assert.AreEqual(coins + (reward.Value<int?>("coins") ?? 0), game.State.Coins);
            Assert.IsNull(board.Pay(1), "once");
            CollectionAssert.IsEmpty(board.Due());

            var again = new MiniBoard(game, "e1", MiniBoard.Settings(config, null));
            Assert.AreSame(board.State, again.State, "the same event keeps its board");
        }

        [Test]
        public void AnEventsOwnSettingsReplaceTheTemplates()
        {
            var own = JObject.Parse("{ \"chain\": [[\"أ\", \"A\"], [\"ب\", \"B\"], [\"ج\", \"C\"]], \"milestoneTokens\": [4] }");
            var board = new MiniBoard(NewGame(), "e2", MiniBoard.Settings(config, own));
            Assert.AreEqual(3, board.MaxLevel);
            CollectionAssert.AreEqual(new[] { 4 }, board.MilestoneTokens);
            Assert.AreEqual((int)config.Events.Templates["miniBoard"]["tokensPerOrder"], board.TokensPerOrder, "the rest comes from the template");
        }
    }
}
