using System;
using Gahwit.Core;

namespace Gahwit.Memories
{
    public enum PourResult { Pouring, KeepPouring, Perfect, Overfilled, Spilled }

    // "Pour" memory (SYSTEMS_B.md section 5): hold to pour, release inside the line.
    // Releasing too early just asks for more; it never fails. Numbers come from balance.json levelB.memories.pour.
    public sealed class PourGame
    {
        readonly PourConfig config;
        readonly int minStars, maxStars;

        public PourGame(MemoriesConfig memories, bool easyAssist)
        {
            config = memories.Pour;
            minStars = memories.Stars[0];
            maxStars = memories.Stars[1];
            float widen = easyAssist ? config.EasyWiden : 0;
            TargetLow = config.TargetLow - widen;
            TargetHigh = Math.Min(1f, config.TargetHigh + widen);
        }

        public float Level { get; private set; }
        public float TargetLow { get; }
        public float TargetHigh { get; }
        public bool Holding { get; private set; }
        public PourResult Result { get; private set; } = PourResult.Pouring;
        public bool Finished => Result == PourResult.Perfect || Result == PourResult.Overfilled || Result == PourResult.Spilled;

        public int Stars => Result == PourResult.Perfect ? maxStars
            : Result == PourResult.Overfilled ? Math.Min(maxStars, minStars + 1)
            : Result == PourResult.Spilled ? minStars
            : 0;

        public void Press()
        {
            if (!Finished) Holding = true;
        }

        public void Tick(float seconds)
        {
            if (!Holding || Finished) return;
            Level = Math.Min(1f, Level + config.FillPerSecond * seconds);
            if (Level >= 1f) Release();
        }

        public PourResult Release()
        {
            if (!Holding || Finished) return Result;
            Holding = false;
            if (Level >= 1f) return Result = PourResult.Spilled;
            if (Level < TargetLow) return Result = PourResult.KeepPouring;
            return Result = Level <= TargetHigh ? PourResult.Perfect : PourResult.Overfilled;
        }
    }
}
