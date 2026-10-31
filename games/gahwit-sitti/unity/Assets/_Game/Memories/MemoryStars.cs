using System;
using System.Collections.Generic;

namespace Gahwit.Memories
{
    // Stars from a score and three thresholds (balance.json levelB.memories.<type>.starsAt...): one star per threshold
    // met, never fewer than the minimum, because stars never block the story (SYSTEMS_B.md section 5).
    public static class MemoryStars
    {
        public static int From(float score, IList<float> thresholds, IList<int> range)
        {
            int stars = 0;
            foreach (float threshold in thresholds)
                if (score >= threshold) stars++;
            return Math.Max(range[0], Math.Min(range[1], stars));
        }
    }

    // What every memory game shares, so the café can play any of them the same way.
    public interface IMemoryGame
    {
        bool Finished { get; }
        int Stars { get; }
        void Tick(float seconds);
    }
}
