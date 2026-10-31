using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gahwit.Story
{
    public sealed class StoryLine
    {
        public string Speaker;
        public string Expression;                            // the second tag ("#salma #smile"); portraits come with the art (missing/13)
        public string Text;                                  // tap-to-explain words marked up as links
        public string Subtitle;                              // the neutral-Arabic line, while subtitles are on
        public List<KeyValuePair<string, string>> Glosses;   // word, meaning
    }

    // Plays one knot of a compiled Ink story line by line.
    // The speaker is the line's first tag ("text #abu_fadi") and the expression its second. [[word]] marks a
    // tap-to-explain word whose meaning is in the Glossary table; [[word:meaning]] gives the meaning in place.
    // A choice's first tag is its id, written inside the brackets ("* [text #ask]": after them it would tag the next
    // line). The save records it under story.choices, and Ink reads it back with chose("knot") (STORY_BIBLE.md 8).
    // Each language has its own Ink file with the same knots, lines and choices (missing/11): chapter1 (dialect),
    // chapter1_en and chapter1_ar (neutral-Arabic subtitles, played in step with the dialect).
    public sealed class StoryPlayer
    {
        static readonly Regex GlossPattern = new Regex(@"\[\[([^:\]]+)(?::([^\]]+))?\]\]");

        readonly Ink.Runtime.Story story;
        StoryPlayer subtitles;

        // The saved choice for a knot ("" before it's made), for Ink's chose(); set by the game.
        public Func<string, string> Chosen { get; set; }

        // A word's meaning in the interface language (the Glossary table), for [[word]] without one.
        public Func<string, string> Glossary { get; set; }

        public StoryPlayer(string compiledJson)
        {
            story = new Ink.Runtime.Story(compiledJson);
            story.BindExternalFunction("chose", (string knot) => (object)(Chosen?.Invoke(knot) ?? ""), true);
        }

        // Ink compiles each .ink file on import into an InkFile asset holding the story JSON.
        public static StoryPlayer FromResources(string path)
        {
            var file = UnityEngine.Resources.Load<Ink.UnityIntegration.InkFile>(path);
            if (file == null || !file.isCompiled)
                throw new InvalidOperationException($"Ink story Resources/{path} is missing or did not compile.");
            return new StoryPlayer(file.storyJson);
        }

        public static bool Exists(string path) => UnityEngine.Resources.Load<Ink.UnityIntegration.InkFile>(path) != null;

        // The file for a language: "chapter1_en" in English, the dialect file otherwise.
        public static string PathFor(string path, string language) => language == "en" ? path + "_en" : path;

        // The neutral-Arabic file played under the dialect lines.
        public static string SubtitlesPath(string path) => path + "_ar";

        // Plays its lines and choices in step with this story; the files have the same structure (the parity test).
        public StoryPlayer Subtitles
        {
            get => subtitles;
            set
            {
                subtitles = value;
                if (value != null) value.Chosen = knot => Chosen?.Invoke(knot);
            }
        }

        // Every knot in the file ("global decl" holds the VAR lines, not a knot).
        public IEnumerable<string> Knots => story.mainContentContainer.namedContent.Keys.Where(name => name != "global decl");

        // The story's state lives in the save, not in Ink, so every scene starts fresh: a replayed scene offers the
        // same choices as the first time.
        public void Play(string knot)
        {
            story.ResetState();
            story.ChoosePathString(knot);
            subtitles?.Play(knot);
        }

        // Every line of a knot without choices, for screens that show them all (the letter).
        public List<StoryLine> ReadAll(string knot)
        {
            Play(knot);
            var lines = new List<StoryLine>();
            while (TryNext(out var line)) lines.Add(line);
            return lines;
        }

        public bool HasChoices => !story.canContinue && story.currentChoices.Count > 0;

        public List<string> Choices
        {
            get
            {
                var texts = new List<string>();
                foreach (var choice in story.currentChoices) texts.Add(choice.text);
                return texts;
            }
        }

        // Each current choice's first tag ("" without one), such as "good" in a timed-choice memory.
        public List<string> ChoiceTags
        {
            get
            {
                var tags = new List<string>();
                foreach (var choice in story.currentChoices)
                    tags.Add(choice.tags != null && choice.tags.Count > 0 ? choice.tags[0].Trim() : "");
                return tags;
            }
        }

        // Returns the chosen option's id, or its position when the choice has no tag.
        public string Choose(int index)
        {
            var choice = story.currentChoices[index];
            story.ChooseChoiceIndex(index);
            if (subtitles != null && index < subtitles.story.currentChoices.Count) subtitles.story.ChooseChoiceIndex(index);
            return choice.tags != null && choice.tags.Count > 0 ? choice.tags[0].Trim() : index.ToString();
        }

        // The text of a saved choice, for the journal: plays the knot to its first choices.
        public string ChoiceText(string knot, string id)
        {
            int index = ChoiceIndex(knot, id);
            return index >= 0 ? Choices[index] : null;
        }

        // A choice's id. Saves from before the ids worked (2026-10-31) hold the option's position instead.
        public string ChoiceId(string knot, string saved)
        {
            int index = ChoiceIndex(knot, saved);
            return index >= 0 && ChoiceTags[index].Length > 0 ? ChoiceTags[index] : saved;
        }

        int ChoiceIndex(string knot, string saved)
        {
            Play(knot);
            while (TryNext(out _)) { }
            var tags = ChoiceTags;
            int index = tags.IndexOf(saved);
            if (index < 0 && int.TryParse(saved, out int position)) index = position;
            return index >= 0 && index < tags.Count ? index : -1;
        }

        public bool TryNext(out StoryLine line)
        {
            while (story.canContinue)
            {
                string raw = story.Continue().Trim();
                var tags = story.currentTags;
                string subtitle = NextSubtitle();
                if (raw.Length == 0) continue;
                line = Parse(raw, tags.Count > 0 ? tags[0].Trim() : "sys", Glossary);
                line.Expression = tags.Count > 1 ? tags[1].Trim() : null;
                line.Subtitle = subtitle;
                return true;
            }
            line = null;
            return false;
        }

        // The subtitle file's line at the same step, without its markup.
        string NextSubtitle()
        {
            if (subtitles == null || !subtitles.story.canContinue) return null;
            string raw = subtitles.story.Continue().Trim();
            return raw.Length == 0 ? null : GlossPattern.Replace(raw, "$1");
        }

        // Each word with a meaning becomes a link ("0", "1", …, its place in Glosses) that the dialogue card can tap.
        public static StoryLine Parse(string raw, string speaker, Func<string, string> glossary = null, string highlightColor = "#1D6C67")
        {
            var glosses = new List<KeyValuePair<string, string>>();
            string text = GlossPattern.Replace(raw, match =>
            {
                string word = match.Groups[1].Value;
                string meaning = match.Groups[2].Success ? match.Groups[2].Value : glossary?.Invoke(word);
                if (string.IsNullOrEmpty(meaning)) return word;
                glosses.Add(new KeyValuePair<string, string>(word, meaning));
                return $"<link=\"{glosses.Count - 1}\"><color={highlightColor}>{word}</color></link>";
            });
            return new StoryLine { Speaker = speaker, Text = text, Glosses = glosses };
        }
    }
}
