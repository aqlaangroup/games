using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Core
{
    // Story-side content: chapter tasks, cast, the opening board. Numbers (costs, rewards) stay in BalanceConfig.
    public sealed class ChapterContent
    {
        public const string ResourcePath = "Content/chapters";

        public string IntroKnot;
        public List<StartingItem> StartingBoard;
        public FirstOrderDef FirstOrder;
        public Dictionary<string, CharacterDef> Characters;
        public List<string> WalkIns;
        public Dictionary<string, List<string>> ChainColors; // [lowest level, top level]
        public List<ChapterTasks> Chapters;
        public Dictionary<string, MemoryDef> Memories;
        public Dictionary<string, LetterDef> Letters;
        public Dictionary<string, StyleDef> Styles; // old, modern, gulf (SYSTEMS_B.md section 2)

        public static ChapterContent Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException($"Missing Resources/{ResourcePath}.json");
            return JsonConvert.DeserializeObject<ChapterContent>(asset.text);
        }

        public ChapterTasks ForChapter(int n) => Chapters.Find(c => c.N == n);
    }

    public sealed class StartingItem
    {
        public string Chain;
        public string Generator; // set for a generator piece
        public int Level;
        public int Col;
        public int Row;
    }

    public sealed class FirstOrderDef
    {
        public string Customer;
        public string Chain;
        public int Level;
        public int Count;
    }

    public sealed class CharacterDef
    {
        public string Color;
        public bool PlacesOrders;
        public int FromChapter = 1;
    }

    public sealed class ChapterTasks
    {
        public int N;
        public string Story; // Resources path of the compiled Ink story
        public List<TaskDef> Tasks;
    }

    public sealed class TaskDef
    {
        public string Id;
        public string Knot;
        public string Memory; // a memory that plays after this task's scene, if any
        public List<string> Styles; // style choices offered when restoring, if any
    }

    public sealed class StyleDef
    {
        public string SignBoard;
        public string SignText;
        public string Wood;
        public string Accent;
    }

    // A playable 1974 memory (SYSTEMS_B.md section 5): scenes around one memory game.
    public sealed class MemoryDef
    {
        public string Type;       // pour, rhythm, find, ...
        public string Story;      // Resources path of the Ink story
        public string LeadKnot;   // in the present, before the sepia transition
        public string IntroKnot;  // in 1974, before the game
        public string PerfectKnot;
        public string OkKnot;
        public string OutroKnot;  // in 1974, after the game
        public string ReturnKnot; // back in the present
        public string Letter;     // a letter the memory leads to, found in the counter drawer
    }

    public sealed class LetterDef
    {
        public string Story;
        public string Knot;       // the letter's lines
        public string VoiceKnot;  // Teta's voice note
        public string NotifyKnot; // the message that arrives afterwards
    }
}
