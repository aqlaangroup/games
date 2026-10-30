using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Gahwit.Story
{
    public sealed class StoryLine
    {
        public string Speaker;
        public string Text;                                  // with tap-to-explain words already marked up
        public List<KeyValuePair<string, string>> Glosses;   // word, meaning
    }

    // Plays one knot of a compiled Ink story line by line.
    // The speaker is the line's first tag ("text #abufadi"); [[word:meaning]] marks a tap-to-explain word.
    public sealed class StoryPlayer
    {
        static readonly Regex GlossPattern = new Regex(@"\[\[([^:\]]+):([^\]]+)\]\]");

        readonly Ink.Runtime.Story story;

        public StoryPlayer(string compiledJson)
        {
            story = new Ink.Runtime.Story(compiledJson);
        }

        // Ink compiles each .ink file on import into an InkFile asset holding the story JSON.
        public static StoryPlayer FromResources(string path)
        {
            var file = UnityEngine.Resources.Load<Ink.UnityIntegration.InkFile>(path);
            if (file == null || !file.isCompiled)
                throw new System.InvalidOperationException($"Ink story Resources/{path} is missing or did not compile.");
            return new StoryPlayer(file.storyJson);
        }

        public void Play(string knot) => story.ChoosePathString(knot);

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

        public void Choose(int index) => story.ChooseChoiceIndex(index);

        public bool TryNext(out StoryLine line)
        {
            while (story.canContinue)
            {
                string raw = story.Continue().Trim();
                if (raw.Length == 0) continue;
                line = Parse(raw, story.currentTags.Count > 0 ? story.currentTags[0].Trim() : "sys");
                return true;
            }
            line = null;
            return false;
        }

        public static StoryLine Parse(string raw, string speaker, string highlightColor = "#1D6C67")
        {
            var glosses = new List<KeyValuePair<string, string>>();
            string text = GlossPattern.Replace(raw, match =>
            {
                glosses.Add(new KeyValuePair<string, string>(match.Groups[1].Value, match.Groups[2].Value));
                return $"<color={highlightColor}>{match.Groups[1].Value}</color>";
            });
            return new StoryLine { Speaker = speaker, Text = text, Glosses = glosses };
        }
    }
}
