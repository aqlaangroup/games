using System;
using Gahwit.Core;

namespace Gahwit.Memories
{
    // "Choose" memory (SYSTEMS_B.md section 5): a conversation with timed choices, such as haggling at the souk.
    // The lines and choices are in the memory's Ink story; a choice tagged "good" is the good answer. Letting the
    // time run out counts as a weak answer and the talk moves on. Numbers come from balance.json
    // levelB.memories.choose; the easy assist gives more time.
    public sealed class ChooseGame : IMemoryGame
    {
        readonly ChooseConfig config;
        readonly MemoriesConfig memories;
        readonly float seconds;
        bool waiting;

        public ChooseGame(MemoriesConfig memories, bool easyAssist)
        {
            this.memories = memories;
            config = memories.Choose;
            seconds = config.SecondsPerChoice * (easyAssist ? config.EasyTimeScale : 1f);
        }

        public int Answered { get; private set; }
        public int Good { get; private set; }
        public float SecondsLeft { get; private set; }
        public float SecondsPerChoice => seconds;
        public bool Waiting => waiting;
        public bool Finished { get; private set; }
        public int Stars => MemoryStars.From(Good, config.StarsAtGoodChoices, memories.Stars);

        // The time ran out on the current choice; the view picks a weak answer to move the talk on.
        public event Action TimedOut;

        // A choice is on screen: its timer starts.
        public void Ask()
        {
            if (Finished) return;
            waiting = true;
            SecondsLeft = seconds;
        }

        public void Answer(bool good)
        {
            if (!waiting) return;
            waiting = false;
            Answered++;
            if (good) Good++;
        }

        // The conversation is over.
        public void End() => Finished = true;

        public void Tick(float elapsed)
        {
            if (!waiting) return;
            SecondsLeft -= elapsed;
            if (SecondsLeft > 0) return;
            Answer(false);
            TimedOut?.Invoke();
        }
    }
}
