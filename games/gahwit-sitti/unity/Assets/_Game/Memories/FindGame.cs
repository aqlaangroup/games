using System;
using System.Collections.Generic;
using Gahwit.Core;

namespace Gahwit.Memories
{
    // "Find" memory (SYSTEMS_B.md section 5): find the listed objects in a 1974 room before time runs out. After a
    // while with nothing found, one object is hinted. Numbers come from balance.json levelB.memories.find.
    public sealed class FindGame : IMemoryGame
    {
        readonly FindConfig config;
        readonly MemoriesConfig memories;
        readonly List<string> left;
        float sinceFound;

        public FindGame(MemoriesConfig memories, IEnumerable<string> objects)
        {
            this.memories = memories;
            config = memories.Find;
            left = new List<string>(objects);
            if (left.Count == 0) throw new ArgumentException("A find memory needs objects.");
            SecondsLeft = config.Seconds;
        }

        public float SecondsLeft { get; private set; }
        public IReadOnlyList<string> Left => left;
        public bool Finished => left.Count == 0 || SecondsLeft <= 0;

        // Shown after hintAfterSeconds without a find, until something is found.
        public string Hint => !Finished && sinceFound >= config.HintAfterSeconds ? left[0] : null;

        public int Stars => MemoryStars.From(left.Count == 0 ? SecondsLeft : 0, config.StarsAtSecondsLeft, memories.Stars);

        public void Tick(float seconds)
        {
            if (Finished) return;
            SecondsLeft = Math.Max(0, SecondsLeft - seconds);
            sinceFound += seconds;
        }

        public bool Tap(string id)
        {
            if (Finished || !left.Remove(id)) return false;
            sinceFound = 0;
            return true;
        }
    }
}
