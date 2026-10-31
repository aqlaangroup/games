using System.Linq;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Memories.Tests
{
    public sealed class MemoryGamesTests
    {
        MemoriesConfig memories;

        [SetUp]
        public void SetUp() => memories = BalanceConfig.Load().LevelB.Memories;

        [Test]
        public void StarsCountThresholdsAndNeverDropBelowOne()
        {
            var thresholds = new[] { 0.5f, 0.75f, 0.9f };
            Assert.AreEqual(1, MemoryStars.From(0f, thresholds, memories.Stars), "stars never block the story");
            Assert.AreEqual(2, MemoryStars.From(0.8f, thresholds, memories.Stars));
            Assert.AreEqual(3, MemoryStars.From(1f, thresholds, memories.Stars));
        }

        // ---------- Rhythm ----------

        static void PlayRhythm(RhythmGame game, float offsetSeconds)
        {
            for (int i = 0; i < game.BeatCount; i++)
            {
                game.Tick(game.BeatTime(i) + offsetSeconds - game.Clock);
                game.Tap();
            }
            game.Tick(5f);
        }

        [Test]
        public void TappingOnEveryBeatIsThreeStars()
        {
            var game = new RhythmGame(memories, false);
            PlayRhythm(game, 0f);
            Assert.IsTrue(game.Finished);
            Assert.AreEqual(1f, game.Accuracy);
            Assert.AreEqual(3, game.Stars);
        }

        [Test]
        public void SlightlyLateTapsAreGoodAndNoTapsAreMissed()
        {
            var game = new RhythmGame(memories, false);
            float late = (memories.Rhythm.PerfectWindowMs + memories.Rhythm.GoodWindowMs) / 2000f;
            PlayRhythm(game, late);
            Assert.AreEqual(0.5f, game.Accuracy, 0.001f);

            var idle = new RhythmGame(memories, false);
            idle.Tick(idle.BeatTime(idle.BeatCount - 1) + 1f);
            Assert.IsTrue(idle.Finished);
            Assert.AreEqual(0f, idle.Accuracy);
            Assert.AreEqual(memories.Stars[0], idle.Stars);
        }

        [Test]
        public void TheEasyAssistTurnsAGoodTapIntoAPerfectOne()
        {
            float off = memories.Rhythm.PerfectWindowMs * 1.2f / 1000f;
            var normal = new RhythmGame(memories, false);
            normal.Tick(normal.BeatTime(0) + off);
            Assert.AreEqual(Beat.Good, normal.Tap());
            var easy = new RhythmGame(memories, true);
            easy.Tick(easy.BeatTime(0) + off);
            Assert.AreEqual(Beat.Perfect, easy.Tap());
        }

        // ---------- Find ----------

        [Test]
        public void FindingEverythingQuicklyIsThreeStarsAndAHintComesAfterAWhile()
        {
            var objects = Enumerable.Range(0, memories.Find.Objects).Select(i => "o" + i).ToList();
            var game = new FindGame(memories, objects);
            Assert.IsNull(game.Hint);
            game.Tick(memories.Find.HintAfterSeconds);
            Assert.AreEqual("o0", game.Hint);
            Assert.IsFalse(game.Tap("not_here"));
            foreach (var id in objects) Assert.IsTrue(game.Tap(id));
            Assert.IsTrue(game.Finished);
            Assert.IsNull(game.Hint);
            Assert.AreEqual(3, game.Stars);
        }

        [Test]
        public void RunningOutOfTimeInFindStillGivesAStar()
        {
            var game = new FindGame(memories, new[] { "a", "b" });
            game.Tick(memories.Find.Seconds + 1);
            Assert.IsTrue(game.Finished);
            Assert.AreEqual(memories.Stars[0], game.Stars);
        }

        // ---------- Restore ----------

        [Test]
        public void PiecesSnapNearTheirPlaceAndTheWholePhotoFinishes()
        {
            var game = new RestoreGame(memories, 7);
            Assert.AreEqual(memories.Restore.Pieces, game.Count);
            var home = game.Home(0);
            Assert.IsFalse(game.Drop(0, home.x + 0.3f, home.y), "too far");
            Assert.IsTrue(game.Drop(0, home.x + memories.Restore.SnapDistance * 0.5f, home.y));
            for (int i = 1; i < game.Count; i++) game.Drop(i, game.Home(i).x, game.Home(i).y);
            Assert.IsTrue(game.Finished);
            Assert.AreEqual(3, game.Stars);
        }

        // ---------- Trace ----------

        static readonly (float, float)[] Line = { (0f, 0f), (10f, 0f), (10f, 5f) };

        [Test]
        public void FollowingTheLineStitchesItAll()
        {
            var game = new TraceGame(memories, Line, false);
            Assert.AreEqual(memories.Trace.PathPoints, game.Count);
            for (int i = 0; i < game.Count; i++) game.Touch(game.Point(i).x, game.Point(i).y + memories.Trace.ToleranceCm * 0.5f);
            Assert.IsTrue(game.Finished);
            Assert.AreEqual(1f, game.Accuracy);
            Assert.AreEqual(3, game.Stars);
        }

        [Test]
        public void CuttingACornerLeavesPointsUnstitchedAndTheEasyAssistIsWider()
        {
            var game = new TraceGame(memories, Line, false);
            for (int i = 0; i < game.Count; i++)
            {
                var p = game.Point(i);
                // Wander off the line for the middle third.
                float wobble = i > game.Count / 3 && i < 2 * game.Count / 3 ? 3f : 0f;
                game.Touch(p.x, p.y + wobble);
            }
            game.Lift();
            Assert.IsTrue(game.Finished);
            Assert.Less(game.Accuracy, 0.75f);
            Assert.Greater(new TraceGame(memories, Line, true).ToleranceCm, game.ToleranceCm);
        }

        // ---------- Tune ----------

        [Test]
        public void HoldingTheDialOnTheStationTunesItIn()
        {
            var game = new TuneGame(memories, 3, false);
            Assert.AreEqual(0f, game.Clarity, "the dial starts on static");
            game.Turn(game.Station + memories.Tune.HintWidth / 4);
            Assert.That(game.Clarity, Is.GreaterThan(0f).And.LessThan(1f));
            game.Turn(game.Station);
            Assert.AreEqual(1f, game.Clarity);
            game.Tick(memories.Tune.HoldSeconds / 2);
            Assert.IsFalse(game.Tuned);
            game.Tick(memories.Tune.HoldSeconds);
            Assert.IsTrue(game.Tuned);
            Assert.AreEqual(3, game.Stars);
        }

        // ---------- Choose ----------

        [Test]
        public void GoodChoicesMakeStarsAndTheTimerMovesOn()
        {
            var game = new ChooseGame(memories, false);
            int timeouts = 0;
            game.TimedOut += () => timeouts++;
            int rounds = memories.Choose.Choices;
            for (int i = 0; i < rounds - 1; i++)
            {
                game.Ask();
                game.Answer(true);
            }
            game.Ask();
            game.Tick(memories.Choose.SecondsPerChoice + 0.1f); // the last one runs out
            game.End();
            Assert.IsTrue(game.Finished);
            Assert.AreEqual(1, timeouts);
            Assert.AreEqual(rounds, game.Answered);
            Assert.AreEqual(rounds - 1, game.Good);
            Assert.AreEqual(MemoryStars.From(rounds - 1, memories.Choose.StarsAtGoodChoices, memories.Stars), game.Stars);
            Assert.Greater(new ChooseGame(memories, true).SecondsPerChoice, game.SecondsPerChoice);
        }

        // ---------- Reassemble ----------

        [Test]
        public void TheLetterStartsMixedAndSwappingItBackSolvesIt()
        {
            var game = new ReassembleGame(memories, memories.Reassemble.Strips, 11);
            for (int i = 0; i < game.Count; i++) Assert.AreNotEqual(i, game.StripAt(i), "every strip starts out of place");
            for (int slot = 0; slot < game.Count; slot++)
            {
                int from = slot;
                while (game.StripAt(from) != slot) from++;
                game.Swap(slot, from);
            }
            Assert.IsTrue(game.Solved);
            Assert.AreEqual(3, game.Stars);
        }
    }
}
