using System;
using Gahwit.Core;

namespace Gahwit.Memories
{
    // "Tune" memory (SYSTEMS_B.md section 5): turn a radio dial to find a song or a voice. Near the station the static
    // fades; holding the dial on it for a moment tunes it in. Frequencies are in MHz. Numbers come from balance.json
    // levelB.memories.tune; the easy assist widens the station.
    public sealed class TuneGame : IMemoryGame
    {
        readonly TuneConfig config;
        readonly MemoriesConfig memories;
        readonly float width;
        float held;

        public TuneGame(MemoriesConfig memories, int seed, bool easyAssist)
        {
            this.memories = memories;
            config = memories.Tune;
            width = config.TargetWidth * (easyAssist ? config.EasyWidthScale : 1f);
            float low = config.DialRange[0], high = config.DialRange[1];
            // The station sits away from the dial's ends and from where the dial starts.
            var random = new Random(seed);
            Dial = low;
            Station = low + config.HintWidth + (float)random.NextDouble() * (high - low - 2 * config.HintWidth);
            Station = (float)Math.Round(Station, 1);
            SecondsLeft = config.Seconds;
        }

        public float Station { get; }
        public float Dial { get; private set; }
        public float SecondsLeft { get; private set; }
        public bool Tuned { get; private set; }
        public bool Finished => Tuned || SecondsLeft <= 0;
        public int Stars => MemoryStars.From(Tuned ? SecondsLeft : 0, config.StarsAtSecondsLeft, memories.Stars);

        // How clear the sound is, from 0 (only static) to 1 (the station), for the view's volumes.
        public float Clarity
        {
            get
            {
                float off = Math.Abs(Dial - Station);
                if (off <= width / 2) return 1f;
                if (off >= config.HintWidth / 2) return 0f;
                return 1f - (off - width / 2) / (config.HintWidth / 2 - width / 2);
            }
        }

        public void Turn(float frequency)
        {
            if (Finished) return;
            Dial = Math.Max(config.DialRange[0], Math.Min(config.DialRange[1], frequency));
        }

        public void Tick(float seconds)
        {
            if (Finished) return;
            SecondsLeft = Math.Max(0, SecondsLeft - seconds);
            held = Math.Abs(Dial - Station) <= width / 2 ? held + seconds : 0;
            if (held >= config.HoldSeconds) Tuned = true;
        }
    }
}
