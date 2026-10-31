using NUnit.Framework;

namespace Gahwit.Core.Tests
{
    public sealed class FrameSamplerTests
    {
        [Test]
        public void SixtyEvenFramesAreSixtyFpsWithNoneSlow()
        {
            var sampler = new FrameSampler(30);
            for (int i = 0; i < 60; i++) sampler.Add(1f / 60);
            Assert.AreEqual(60f, sampler.AverageFps, 0.01f);
            Assert.AreEqual(0f, sampler.SlowShare);
            Assert.AreEqual(1f, sampler.Seconds, 0.001f);
        }

        [Test]
        public void AFrameSlowerThanTheWeakestTargetCountsAsSlow()
        {
            var sampler = new FrameSampler(30);
            for (int i = 0; i < 9; i++) sampler.Add(1f / 60);
            sampler.Add(0.05f);
            Assert.AreEqual(0.1f, sampler.SlowShare, 0.0001f);
            Assert.AreEqual(50f, sampler.WorstMs, 0.01f);
        }
    }
}
