using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Memories.Tests
{
    public sealed class PourGameTests
    {
        MemoriesConfig memories;

        [SetUp]
        public void SetUp() => memories = BalanceConfig.Load().LevelB.Memories;

        // Holds the button until the cup reaches the given level, then lets go.
        static PourResult PourTo(PourGame game, float level)
        {
            game.Press();
            while (game.Level < level && !game.Finished) game.Tick(0.005f);
            return game.Release();
        }

        float Middle => (memories.Pour.TargetLow + memories.Pour.TargetHigh) / 2;

        [Test]
        public void ReleasingInsideTheLineIsPerfect()
        {
            var game = new PourGame(memories, false);
            Assert.AreEqual(PourResult.Perfect, PourTo(game, Middle));
            Assert.AreEqual(memories.Stars[1], game.Stars);
        }

        [Test]
        public void ReleasingTooEarlyAsksForMoreAndPouringCanContinue()
        {
            var game = new PourGame(memories, false);
            Assert.AreEqual(PourResult.KeepPouring, PourTo(game, memories.Pour.TargetLow / 2));
            Assert.IsFalse(game.Finished);
            Assert.AreEqual(PourResult.Perfect, PourTo(game, Middle));
        }

        [Test]
        public void OverfillingAndSpillingStillFinishWithFewerStars()
        {
            var over = new PourGame(memories, false);
            Assert.AreEqual(PourResult.Overfilled, PourTo(over, (memories.Pour.TargetHigh + 1f) / 2));
            Assert.AreEqual(memories.Stars[0] + 1, over.Stars);

            var spill = new PourGame(memories, false);
            spill.Press();
            for (int i = 0; i < 1000 && !spill.Finished; i++) spill.Tick(0.05f); // never let go
            Assert.AreEqual(PourResult.Spilled, spill.Result);
            Assert.AreEqual(memories.Stars[0], spill.Stars);
        }

        [Test]
        public void TheCupFillsAtTheConfiguredSpeed()
        {
            var game = new PourGame(memories, false);
            game.Press();
            game.Tick(1f);
            Assert.AreEqual(memories.Pour.FillPerSecond, game.Level, 1e-4);
        }

        [Test]
        public void TheEasyAssistWidensTheLine()
        {
            var normal = new PourGame(memories, false);
            var easy = new PourGame(memories, true);
            Assert.Less(easy.TargetLow, normal.TargetLow);
            Assert.Greater(easy.TargetHigh, normal.TargetHigh);
            Assert.AreEqual(PourResult.Perfect, PourTo(easy, normal.TargetHigh + memories.Pour.EasyWiden / 2));
        }
    }
}
