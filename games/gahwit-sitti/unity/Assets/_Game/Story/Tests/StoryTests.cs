using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Gahwit.Story.Tests
{
    public sealed class StoryTests
    {
        const string StoryFolder = "Assets/Content/Resources/Story";
        // Knot names go through constants: the audio test reads a quoted name passed to Play as a sound cue.
        const string Knot = "scene", Dallahs = "dallahs", Haggling = "practice_choose";

        const string Scene = @"
EXTERNAL chose(knot)
=== scene ===
Hello there. #salma #smile
{
- chose(""scene"") == ""ask"":
    You asked before. #abu_fadi
}
* [Ask #ask]
    An answer. #abu_fadi
* [Wait #wait]
    Later. #abu_fadi
- -> END
";

        static StoryPlayer Compile(string source) => new StoryPlayer(new Ink.Compiler(source).Compile().ToJson());

        [Test]
        public void AWordWithAMeaningBecomesALinkTheCardCanTap()
        {
            var line = StoryPlayer.Parse("Sit [[here:in this place]] and [[wait]] for [[me]].", "salma",
                word => word == "wait" ? "stay a while" : null);
            Assert.AreEqual(2, line.Glosses.Count);
            Assert.AreEqual("in this place", line.Glosses[0].Value);
            Assert.AreEqual("stay a while", line.Glosses[1].Value);
            StringAssert.Contains("<link=\"0\">", line.Text);
            StringAssert.Contains("<link=\"1\">", line.Text);
            StringAssert.EndsWith("for me.", line.Text); // no meaning anywhere: plain text
        }

        [Test]
        public void TheSecondTagIsTheExpression()
        {
            var story = Compile(Scene);
            story.Play(Knot);
            Assert.IsTrue(story.TryNext(out var line));
            Assert.AreEqual("salma", line.Speaker);
            Assert.AreEqual("smile", line.Expression);
        }

        [Test]
        public void InkReadsASavedChoiceBack()
        {
            var story = Compile(Scene);
            story.Chosen = knot => knot == Knot ? "ask" : "";
            var lines = story.ReadAll(Knot);
            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("You asked before.", lines[1].Text);

            story.Chosen = null;
            Assert.AreEqual(1, story.ReadAll(Knot).Count);
        }

        [Test]
        public void AReplayedSceneOffersTheSameChoices()
        {
            var story = Compile(Scene);
            story.Play(Knot);
            while (story.TryNext(out _)) { }
            Assert.AreEqual("ask", story.Choose(0));

            story.Play(Knot);
            while (story.TryNext(out _)) { }
            CollectionAssert.AreEqual(new[] { "ask", "wait" }, story.ChoiceTags);
        }

        [Test]
        public void TheJournalFindsTheTextOfASavedChoice()
        {
            var story = Compile(Scene);
            Assert.AreEqual("Wait", story.ChoiceText(Knot, "wait"));
            Assert.AreEqual("Ask", story.ChoiceText(Knot, "0")); // a choice saved by its position
            Assert.IsNull(story.ChoiceText(Knot, "gone"));
        }

        // A tag belongs to a choice only inside its brackets ("* [text #id]"); after them it tags the next line.
        [Test]
        public void TheStoryFilesGiveTheirChoicesIds()
        {
            var chapter = StoryPlayer.FromResources("Story/chapter1");
            chapter.Play(Dallahs);
            while (chapter.TryNext(out _)) { }
            CollectionAssert.AreEqual(new[] { "ask", "tomorrow" }, chapter.ChoiceTags);
            StringAssert.DoesNotContain("#", chapter.Choices[0]);

            var practice = StoryPlayer.FromResources("Story/memory_practice");
            practice.Play(Haggling);
            while (practice.TryNext(out _)) { }
            CollectionAssert.AreEqual(new[] { "good", "weak" }, practice.ChoiceTags);
        }

        [Test]
        public void AChoiceSavedByItsPositionFindsItsId()
        {
            var story = Compile(Scene);
            Assert.AreEqual("wait", story.ChoiceId(Knot, "1"));
            Assert.AreEqual("ask", story.ChoiceId(Knot, "ask"));
            Assert.AreEqual("7", story.ChoiceId(Knot, "7"));
        }

        [Test]
        public void SubtitlesPlayInStepWithTheDialect()
        {
            var dialect = StoryPlayer.FromResources("Story/chapter1");
            dialect.Subtitles = StoryPlayer.FromResources(StoryPlayer.SubtitlesPath("Story/chapter1"));
            dialect.Play(Dallahs);
            int lines = 0;
            while (dialect.TryNext(out var line))
            {
                Assert.IsFalse(string.IsNullOrEmpty(line.Subtitle), line.Text);
                lines++;
            }
            Assert.AreEqual(2, lines);
            dialect.Choose(1);
            Assert.IsTrue(dialect.TryNext(out var answer));
            Assert.IsFalse(string.IsNullOrEmpty(answer.Subtitle));
            StringAssert.DoesNotContain("[[", answer.Subtitle);
        }

        [Test]
        public void TheCounterSceneRemembersTheDallahsChoice()
        {
            var story = StoryPlayer.FromResources("Story/chapter1");
            story.Chosen = knot => knot == "dallahs" ? "ask" : "";
            var asked = story.ReadAll("counter");
            story.Chosen = knot => knot == "dallahs" ? "tomorrow" : "";
            var waited = story.ReadAll("counter");
            Assert.AreEqual(asked.Count, waited.Count);
            Assert.AreNotEqual(asked[2].Text, waited[2].Text);
        }

        // Every Ink file has an English file and a neutral-Arabic subtitle file with the same knots, and every path
        // through each knot has the same speakers, expressions and choice ids (missing/11).
        [Test]
        public void EveryLanguageFollowsTheDialectFileLineForLine()
        {
            var sources = Directory.GetFiles(StoryFolder, "*.ink").Select(Path.GetFileNameWithoutExtension)
                .Where(name => !name.EndsWith("_en") && !name.EndsWith("_ar")).ToList();
            Assert.IsNotEmpty(sources);
            foreach (var name in sources)
            {
                string path = "Story/" + name;
                var dialect = StoryPlayer.FromResources(path);
                var answers = new List<string> { "" };
                answers.AddRange(dialect.Knots.SelectMany(knot => Paths(dialect, knot)).SelectMany(p => p)
                    .Where(step => step.StartsWith("choice ")).SelectMany(step => step.Substring(7).Split(',')).Distinct());
                foreach (string other in new[] { StoryPlayer.PathFor(path, "en"), StoryPlayer.SubtitlesPath(path) })
                {
                    Assert.IsTrue(StoryPlayer.Exists(other), $"{other} is missing");
                    var translation = StoryPlayer.FromResources(other);
                    CollectionAssert.AreEquivalent(dialect.Knots.ToList(), translation.Knots.ToList(), other);
                    foreach (string answer in answers)
                    {
                        dialect.Chosen = translation.Chosen = _ => answer;
                        foreach (string knot in dialect.Knots)
                            CollectionAssert.AreEqual(Paths(dialect, knot), Paths(translation, knot), $"{other}, {knot}, chose \"{answer}\"");
                    }
                }
            }
        }

        // Every way through a knot, as one list of steps per way.
        static List<List<string>> Paths(StoryPlayer story, string knot)
        {
            var paths = new List<List<string>>();
            var pending = new Queue<List<int>>();
            pending.Enqueue(new List<int>());
            while (pending.Count > 0)
            {
                var picks = pending.Dequeue();
                var steps = Walk(story, knot, picks, out int open);
                if (open == 0) paths.Add(steps);
                for (int i = 0; i < open; i++) pending.Enqueue(new List<int>(picks) { i });
            }
            return paths;
        }

        // Follows the picks; stops at the first choice point past them, and says how many options it has.
        static List<string> Walk(StoryPlayer story, string knot, List<int> picks, out int open)
        {
            var steps = new List<string>();
            story.Play(knot);
            open = 0;
            for (int step = 0; ; step++)
            {
                while (story.TryNext(out var line)) steps.Add($"line {line.Speaker} {line.Expression}");
                if (!story.HasChoices) return steps;
                steps.Add("choice " + string.Join(",", story.ChoiceTags));
                if (step == picks.Count)
                {
                    open = story.ChoiceTags.Count;
                    return steps;
                }
                story.Choose(picks[step]);
            }
        }
    }
}
