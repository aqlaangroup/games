using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using NUnit.Framework;

namespace Gahwit.Customers.Tests
{
    public sealed class CustomerRulesTests
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
            var game = new Game(config, content, Game.NewState(config, content, 11, Now));
            game.Tick(Now);
            return game;
        }

        // A walk-in customer with a small order, so tests don't depend on what the seed rolled.
        static Order WalkIn(Game game)
        {
            var order = game.MakeOrder(null, false, new System.Collections.Generic.List<OrderLine>
                { new OrderLine { Chain = "coffee", Level = 1, Count = 1 } });
            game.State.Orders.Add(order);
            return order;
        }

        // Puts the items an order wants on the board, so it can be served.
        static void Prepare(Game game, Order order)
        {
            foreach (var line in order.Lines)
                for (int n = 0; n < line.Count; n++)
                {
                    int free = System.Array.FindIndex(game.State.Board, i => i == null);
                    game.State.Board[free] = new Item(line.Chain, line.Level);
                }
        }

        [Test]
        public void AWalkInWhoWaitsTooLongLeavesAndANewCustomerComes()
        {
            var game = NewGame();
            var rules = new CustomerRules(game);
            var walkIn = WalkIn(game);
            Order left = null;
            rules.Left += o => left = o;

            rules.Tick(Now + config.LevelB.WalkIns.LeaveAfterMinutes * 60_000L - 1);
            Assert.IsNull(left);
            rules.Tick(Now + config.LevelB.WalkIns.LeaveAfterMinutes * 60_000L + 1);

            Assert.AreEqual(walkIn.Id, left.Id);
            Assert.IsFalse(game.State.Orders.Any(o => o.Id == walkIn.Id));
            Assert.IsTrue(game.State.Orders.Any(o => o.Customer == "abu_fadi"), "Abu Fadi waits");
            Assert.GreaterOrEqual(game.State.Orders.Count, config.OrderSlots(game.State.Level), "the slots fill again");
        }

        [Test]
        public void AServedWalkInTipsAndCosinessRaisesTheTip()
        {
            var game = NewGame();
            var rules = new CustomerRules(game);
            int tip = -1;
            rules.Tipped += (_, coins) => tip = coins;
            var walkIn = WalkIn(game);
            Prepare(game, walkIn);
            int before = game.State.Coins;

            Assert.IsTrue(game.Serve(walkIn.Id));

            var range = config.LevelB.WalkIns.TipCoins;
            Assert.That(tip, Is.InRange(range[0], range[1]));
            Assert.AreEqual(before + walkIn.Coins + tip, game.State.Coins);
            game.State.Cosiness = 1000;
            Assert.AreEqual(config.LevelB.Cafe.TipBonusMax, rules.TipBonus);
        }

        [Test]
        public void AStoryOrderBringsMoreFriendshipThanAPlainOne()
        {
            var game = NewGame();
            var rules = new CustomerRules(game);
            var lines = new System.Collections.Generic.List<OrderLine> { new OrderLine { Chain = "coffee", Level = 1, Count = 1 } };

            var plain = game.MakeOrder("abu_fadi", true, lines);
            game.State.Orders.Add(plain);
            Prepare(game, plain);
            game.Serve(plain.Id);
            Assert.AreEqual(config.LevelB.Regulars.PointsOther, rules.Regular("abu_fadi").Points);

            var story = game.MakeOrder("abu_fadi", true, lines);
            story.Story = "test_story";
            game.State.Orders.Add(story);
            Prepare(game, story);
            game.Serve(story.Id);
            Assert.AreEqual(config.LevelB.Regulars.PointsOther + config.LevelB.Kitchen.StoryOrderFriendshipPoints, rules.Regular("abu_fadi").Points);
        }

        [Test]
        public void AbuFadiGainsFriendshipAndReachesHisFirstStory()
        {
            var game = NewGame();
            var rules = new CustomerRules(game);
            int reached = 0;
            rules.FriendshipLevelUp += (who, level) => reached = level;
            var favourite = content.Characters["abu_fadi"].Favourite;

            // Each favourite cup gives 3 points; level 1 needs 10 and level 2 needs 20.
            for (int n = 0; n < 10; n++)
            {
                var order = game.MakeOrder("abu_fadi", true, new System.Collections.Generic.List<OrderLine>
                    { new OrderLine { Chain = favourite.Chain, Level = favourite.Level, Count = 1 } });
                game.State.Orders.Add(order);
                Prepare(game, order);
                game.Serve(order.Id);
            }

            Assert.AreEqual(3, reached);
            Assert.AreEqual(3, rules.Regular("abu_fadi").Level);
            Assert.AreEqual("abu_fadi_3", rules.StoryDue("abu_fadi"));
            rules.StorySeen("abu_fadi", "abu_fadi_3");
            Assert.IsNull(rules.StoryDue("abu_fadi"));
        }

        [Test]
        public void AFriendshipSceneWaitsForItsChapter()
        {
            var game = NewGame();
            var rules = new CustomerRules(game);
            content.Characters["abu_fadi"].StoryFromChapter = new System.Collections.Generic.Dictionary<int, int> { [3] = 2 };
            rules.Regular("abu_fadi").Level = 6;

            Assert.IsNull(rules.StoryDue("abu_fadi"), "level 3's scene waits for chapter 2, and level 6's waits behind it");
            game.State.Chapter = 2;
            Assert.AreEqual("abu_fadi_3", rules.StoryDue("abu_fadi"));
            rules.StorySeen("abu_fadi", "abu_fadi_3");
            Assert.AreEqual("abu_fadi_6", rules.StoryDue("abu_fadi"));
        }

        [Test]
        public void SeatsGrowWithCosinessOnceTheChairsAreBack()
        {
            var game = NewGame();
            var rules = new CustomerRules(game);
            Assert.AreEqual(0, rules.Seats(false));
            Assert.AreEqual(config.LevelB.Cafe.WalkInTables.Min, rules.Seats(true));
            game.State.Cosiness = 10_000;
            Assert.AreEqual(config.LevelB.Cafe.WalkInTables.Max, rules.Seats(true));
        }

        [Test]
        public void RegularsOnlyOrderOnTheirSchedule()
        {
            var game = NewGame();
            game.State.OrdersServed = 1; // past the tutorial's first order

            int AbuFadiOrdersAt(string timeOfDay)
            {
                game.TimeOfDay = timeOfDay;
                int seen = 0;
                for (int n = 0; n < 60; n++)
                {
                    int newest = game.State.Orders.Max(o => o.Id);
                    game.DropOrder(game.State.Orders[0].Id);
                    seen += game.State.Orders.Count(o => o.Customer == "abu_fadi" && o.Id > newest); // only the new arrival
                }
                return seen;
            }

            Assert.AreEqual(0, AbuFadiOrdersAt("night"), "Abu Fadi visits mornings and afternoons");
            Assert.Greater(AbuFadiOrdersAt("morning"), 0);
            Assert.AreEqual(6, new CustomerRules(game).FriendshipUnlockLevel);
        }
    }
}
