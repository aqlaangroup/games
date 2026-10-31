using System;
using Gahwit.Core;

namespace Gahwit.Memories
{
    public enum Beat { Waiting, Perfect, Good, Missed }

    // "Rhythm" memory (SYSTEMS_B.md section 5): tap in time with the song to shape ma'amoul. A tap near a beat is
    // perfect or good; a beat with no tap is missed. Accuracy counts perfect as 1 and good as half.
    // Numbers come from balance.json levelB.memories.rhythm; the easy assist widens both windows.
    public sealed class RhythmGame : IMemoryGame
    {
        readonly RhythmConfig config;
        readonly MemoriesConfig memories;
        readonly Beat[] beats;
        readonly float perfect, good, interval;
        int next;

        public RhythmGame(MemoriesConfig memories, bool easyAssist)
        {
            this.memories = memories;
            config = memories.Rhythm;
            beats = new Beat[config.Beats];
            float scale = easyAssist ? config.EasyWindowScale : 1f;
            perfect = config.PerfectWindowMs / 1000f * scale;
            good = config.GoodWindowMs / 1000f * scale;
            interval = 60f / config.BeatsPerMinute;
        }

        public float Clock { get; private set; }
        public int BeatCount => beats.Length;
        public float BeatInterval => interval;

        // The first beat comes one bar (4 beats) after the start, so the player hears the tempo first.
        public float BeatTime(int i) => (i + 4) * interval;
        public Beat this[int i] => beats[i];
        public bool Finished => next >= beats.Length;

        public float Accuracy
        {
            get
            {
                float score = 0;
                foreach (var beat in beats) score += beat == Beat.Perfect ? 1f : beat == Beat.Good ? 0.5f : 0f;
                return score / beats.Length;
            }
        }

        public int Stars => MemoryStars.From(Accuracy, config.StarsAtAccuracy, memories.Stars);

        public void Tick(float seconds)
        {
            Clock += seconds;
            while (next < beats.Length && Clock > BeatTime(next) + good) beats[next++] = Beat.Missed;
        }

        // A tap judges the next beat. Taps far from any beat do nothing.
        public Beat Tap()
        {
            if (Finished) return Beat.Waiting;
            float off = Math.Abs(Clock - BeatTime(next));
            if (off > good) return Beat.Waiting;
            return beats[next++] = off <= perfect ? Beat.Perfect : Beat.Good;
        }
    }
}
