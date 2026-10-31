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
        public FavouriteItem Favourite;    // a regular's favourite: +3 friendship points (SYSTEMS_B.md section 3)
        public bool FriendshipFromStart;   // Abu Fadi: friendship counts before level 6 unlocks it for others
        public bool Female;                // for Arabic text that agrees with her (وصلت, not وصل)
        public bool Plural;                // a couple, such as Sara and Noor (وصلوا)
        // A friendship scene that waits for the story: level → chapter. Faris's 6 and 10 wait for chapters 8 and 9
        // (STORY_BIBLE.md section 9).
        public Dictionary<int, int> StoryFromChapter;
    }

    public sealed class FavouriteItem
    {
        public string Chain;
        public int Level;
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
        public List<TaskProp> Props; // what the finished task adds to the café (chapters 2–10)
    }

    // Something a finished task adds to the café, made of placeholder shapes until the 3D art replaces it (missing/13).
    public sealed class TaskProp
    {
        public List<DecorPart> Parts = new List<DecorPart>();
        public float[] At;   // metres from the room's centre: x (right), y (up), z (towards the camera); the back wall is z = -4
        public float Rot;    // degrees around y
        public string Style; // only when the player chose this style for the task, if set
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
        public string GameKnot;   // choose: the conversation; reassemble: the torn letter's strips, one per line
        public List<string> Objects; // find: what to find (memobj.<id>)
        public List<string> Decoys;  // find: the rest of the room
        public List<List<float>> Line; // trace: the seam, as [x, y] in centimetres
        public bool Present;      // played in today's café instead of 1974 (the season's last memory)
    }

    public sealed class LetterDef
    {
        public string Story;
        public string Knot;       // the letter's lines
        public string VoiceKnot;  // Teta's voice note
        public string VoiceClip;  // its recording under Resources; a draft until the actor's take (tools/voice_drafts.json)
        public string NotifyKnot; // the message that arrives afterwards
    }
}
