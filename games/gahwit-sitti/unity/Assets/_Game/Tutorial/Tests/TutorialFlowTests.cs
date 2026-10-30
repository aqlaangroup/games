using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using NUnit.Framework;

namespace Gahwit.Tutorial.Tests
{
    public sealed class TutorialFlowTests
    {
        const long Now = 1_000_000_000;

        BalanceConfig config;
        ChapterContent content;
        TutorialContent steps;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
            steps = TutorialContent.Load();
        }

        Game NewGame() => new Game(config, content, Game.NewState(config, content, 7, Now));

        static int Find(Game game, System.Func<Item, bool> match) => game.State.Board.ToList().FindIndex(i => i != null && match(i));

        [Test]
        public void TheElevenStepsFollowTheGameFromTheAgeGateToFreePlay()
        {
            var game = NewGame();
            bool age = false;
            var flow = new TutorialFlow(game, steps, () => age);
            var finished = new List<int>();
            flow.StepFinished += s => finished.Add(s.N);

            flow.Check();
            Assert.AreEqual("age_gate", flow.Current.Id);
            age = true;
            flow.Check();
            Assert.AreEqual("intro", flow.Current.Id);
            game.State.ScenesSeen.Add("intro");
            game.State.ScenesSeen.Add("abu_fadi_arrives");
            flow.Check();
            Assert.AreEqual("merge", flow.Current.Id);

            int a = Find(game, i => i.Is("coffee", 1));
            int b = game.State.Board.ToList().FindIndex(a + 1, i => i != null && i.Is("coffee", 1));
            game.Move(a, b);
            Assert.AreEqual("generator", flow.Current.Id);

            game.TapGenerator(Find(game, i => i.Generator == "gen_coffee"), Now, out _);
            Assert.AreEqual("serve", flow.Current.Id);

            Assert.IsTrue(game.Serve(game.State.Orders.First(o => o.Customer == "abu_fadi").Id));
            Assert.AreEqual("coins", flow.Current.Id);
            game.State.Coins = 100;
            flow.Check();
            Assert.AreEqual("sign", flow.Current.Id);

            game.CompleteTask("old");
            Assert.AreEqual("memory", flow.Current.Id);
            game.RecordMemory("m1_pour", 3);
            Assert.AreEqual("letter", flow.Current.Id);
            game.FindLetter("letter_1");

            Assert.IsTrue(flow.Done);
            CollectionAssert.AreEqual(Enumerable.Range(1, 11), finished);
            Assert.AreEqual(config.Tutorial.EasyOrdersAfter, game.State.EasyOrdersLeft);
        }

        [Test]
        public void ARestartResumesAtTheStepThatWasNotFinished()
        {
            var game = NewGame();
            game.State.TutorialStep = 4; // the merge was done before closing the app
            var flow = new TutorialFlow(new Game(config, content, game.State), steps, () => true);
            flow.Check();
            Assert.AreEqual("generator", flow.Current.Id);
            Assert.AreEqual(2, flow.HandsOnNumber(flow.Current));
            Assert.AreEqual(5, flow.HandsOnCount);
        }

        [Test]
        public void FreePlayStartsWithEasyOrders()
        {
            var game = NewGame();
            game.State.EasyOrdersLeft = 3;
            game.State.Orders.Clear();
            game.State.OrdersServed = 1;
            game = new Game(config, content, game.State); // fills the order slots
            var easy = game.State.Orders.Take(3).ToList();
            Assert.IsTrue(easy.All(o => o.Lines.Count == 1 && o.Lines[0].Count == 1 && o.Lines[0].Level <= config.Tutorial.EasyOrder.MaxItemLevel));
            Assert.AreEqual(0, game.State.EasyOrdersLeft);
        }

        [Test]
        public void SkippingStopsTheGuidanceAndStartsFreePlay()
        {
            var game = NewGame();
            var flow = new TutorialFlow(game, steps, () => true);
            flow.Skip();
            Assert.IsTrue(flow.Done);
            Assert.IsNull(flow.Current);
            Assert.AreEqual(config.Tutorial.EasyOrdersAfter, game.State.EasyOrdersLeft);
        }
    }
}
