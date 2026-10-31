using System;
using Gahwit.Core;
using UnityEngine;
using UnityEngine.Profiling;

namespace Gahwit.Boot
{
    // After a couple of minutes of play, reports how smoothly this phone runs (missing/23): one "perf" event and one
    // [Perf] log line. Testers' phones, such as the 3 GB Android, send real numbers without anyone watching.
    public sealed class FrameStats : MonoBehaviour
    {
        FrameSampler sampler;
        float sampleSeconds;
        Action<FrameSampler> report;

        public void Init(GraphicsConfig graphics, Action<FrameSampler> report)
        {
            sampler = new FrameSampler(graphics.LowFps);
            sampleSeconds = graphics.PerfSampleSeconds;
            this.report = report;
        }

        void Update()
        {
            if (sampler == null) return;
            sampler.Add(Time.unscaledDeltaTime);
            if (sampler.Seconds < sampleSeconds) return;
            Debug.Log($"[Perf] {SystemInfo.deviceModel}: {sampler.AverageFps:0.0} fps, {sampler.SlowShare:P1} slow frames, " +
                      $"worst {sampler.WorstMs:0} ms, {Profiler.GetTotalAllocatedMemoryLong() / 1048576} MB allocated of {SystemInfo.systemMemorySize} MB.");
            report(sampler);
            sampler = null;
            enabled = false;
        }
    }
}
