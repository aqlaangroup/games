using NUnit.Framework;

namespace Gahwit.Core.Tests
{
    public sealed class TimeServiceTests
    {
        sealed class FakeClock : IDeviceClock
        {
            public long UtcNowMs { get; set; }
            public long UptimeMs { get; set; }
        }

        const long Day = 24 * 60 * 60 * 1000L;

        [Test]
        public void ChangingThePhoneClockDoesNotMoveTime()
        {
            var phone = new FakeClock { UtcNowMs = 1_000_000_000, UptimeMs = 5_000 };
            var time = new TimeService(phone, null);
            long start = time.NowMs;

            phone.UtcNowMs += Day;
            Assert.AreEqual(start, time.NowMs);
        }

        [Test]
        public void TheUptimeCounterMovesTimeForward()
        {
            var phone = new FakeClock { UtcNowMs = 1_000_000_000, UptimeMs = 5_000 };
            var time = new TimeService(phone, null);
            long start = time.NowMs;

            phone.UptimeMs += 60_000;
            Assert.AreEqual(start + 60_000, time.NowMs);
        }

        [Test]
        public void AfterARestartTimeFallsBackToThePhoneClockButNeverGoesBack()
        {
            var phone = new FakeClock { UtcNowMs = 1_000_000_000, UptimeMs = 900_000 };
            var time = new TimeService(phone, null);
            phone.UptimeMs += 100_000;
            long seen = time.NowMs;

            // The phone restarts and its clock was wound back.
            phone.UptimeMs = 1_000;
            phone.UtcNowMs = 500_000_000;
            Assert.AreEqual(seen, time.NowMs);

            // A later clock than anything seen is accepted after a restart.
            phone.UptimeMs = 500;
            phone.UtcNowMs = seen + Day;
            Assert.AreEqual(seen + Day, time.NowMs);
        }

        [Test]
        public void ServerTimeReplacesTheAnchor()
        {
            var phone = new FakeClock { UtcNowMs = 1_000_000_000, UptimeMs = 5_000 };
            var time = new TimeService(phone, null);
            time.ApplyServerTime(2_000_000_000);
            phone.UptimeMs += 1_000;
            Assert.AreEqual(2_000_001_000, time.NowMs);
            Assert.AreEqual(2_000_000_000, time.LastServerTimeMs);
        }

        [Test]
        public void ASavedAnchorContinuesInTheSameSession()
        {
            var phone = new FakeClock { UtcNowMs = 1_000_000_000, UptimeMs = 5_000 };
            var first = new TimeService(phone, null);
            phone.UptimeMs += 10_000;
            long before = first.NowMs;

            var resumed = new TimeService(phone, first.State);
            phone.UtcNowMs += Day; // clock changed while the game was closed
            Assert.AreEqual(before, resumed.NowMs);
        }
    }
}
