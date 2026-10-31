using System;
using Gahwit.Core;

namespace Gahwit.Memories
{
    // "Reassemble" memory (SYSTEMS_B.md section 5): put the strips of a torn letter back in order to read it. Tap two
    // strips to swap them. Numbers come from balance.json levelB.memories.reassemble.
    public sealed class ReassembleGame : IMemoryGame
    {
        readonly ReassembleConfig config;
        readonly MemoriesConfig memories;
        readonly int[] order; // order[slot] = strip shown in that slot

        // strips: how many the letter has (balance.json levelB.memories.reassemble.strips for a full letter).
        public ReassembleGame(MemoriesConfig memories, int strips, int seed)
        {
            this.memories = memories;
            config = memories.Reassemble;
            order = new int[strips];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            // Shuffle until nothing is in its place, so every strip needs a move.
            var random = new Random(seed);
            if (order.Length > 1) do
            {
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
            } while (AnyInPlace());
            SecondsLeft = config.Seconds;
        }

        public int Count => order.Length;
        public int StripAt(int slot) => order[slot];
        public float SecondsLeft { get; private set; }
        public bool Solved
        {
            get
            {
                for (int i = 0; i < order.Length; i++) if (order[i] != i) return false;
                return true;
            }
        }
        public bool Finished => Solved || SecondsLeft <= 0;
        public int Stars => MemoryStars.From(Solved ? SecondsLeft : 0, config.StarsAtSecondsLeft, memories.Stars);

        public void Tick(float seconds)
        {
            if (!Finished) SecondsLeft = Math.Max(0, SecondsLeft - seconds);
        }

        public void Swap(int a, int b)
        {
            if (Finished || a == b) return;
            (order[a], order[b]) = (order[b], order[a]);
        }

        bool AnyInPlace()
        {
            for (int i = 0; i < order.Length; i++) if (order[i] == i) return true;
            return false;
        }
    }
}
