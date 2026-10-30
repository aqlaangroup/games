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
    }
}
