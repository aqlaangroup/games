namespace Gahwit.Core
{
    // Counts frames for the "perf" event (missing/23): average fps, the slowest frame, and the share of frames slower
    // than the weakest phone's target (balance.json ui.graphics.lowFps). No allocations.
    public sealed class FrameSampler
    {
        readonly float slowSeconds;
        int frames, slow;
        double total;
        float worst;

        public FrameSampler(int minFps) => slowSeconds = 1f / minFps;

        public void Add(float seconds)
        {
            frames++;
            total += seconds;
            if (seconds > slowSeconds) slow++;
            if (seconds > worst) worst = seconds;
        }

        public float Seconds => (float)total;
        public float AverageFps => total > 0 ? (float)(frames / total) : 0f;
        public float SlowShare => frames > 0 ? slow / (float)frames : 0f;
        public float WorstMs => worst * 1000f;
    }
}
