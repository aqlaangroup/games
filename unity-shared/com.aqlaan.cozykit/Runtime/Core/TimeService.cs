using System;

namespace Aqlaan.CozyKit
{
    public interface IDeviceClock
    {
        long UtcNowMs { get; }
        long UptimeMs { get; } // since the phone started, including sleep
    }

    // Trusted time for energy and events (COMPLETE_SPEC.md section 11): server time when the platform has given it,
    // moved forward by the phone's uptime counter. Changing the phone's clock does not move it.
    // After a restart the uptime counter starts again, so the anchor falls back to the phone clock, but time never goes back.
    public sealed class TimeService
    {
        public sealed class Anchor
        {
            public long TrustedMs;
            public long UptimeMs;
            public long LastSeenMs;
            public bool FromServer;
        }

        readonly IDeviceClock device;

        public Anchor State { get; private set; }

        public TimeService(IDeviceClock device, Anchor saved)
        {
            this.device = device;
            State = saved;
            if (State == null) State = NewAnchor(device.UtcNowMs, false);
        }

        public long NowMs
        {
            get
            {
                long uptime = device.UptimeMs;
                if (uptime < State.UptimeMs) State = NewAnchor(Math.Max(State.LastSeenMs, device.UtcNowMs), false);
                long now = State.TrustedMs + (uptime - State.UptimeMs);
                if (now > State.LastSeenMs) State.LastSeenMs = now;
                return now;
            }
        }

        public long LastServerTimeMs => State.FromServer ? State.TrustedMs : 0;

        public void ApplyServerTime(long serverMs) => State = NewAnchor(serverMs, true);

        Anchor NewAnchor(long trustedMs, bool fromServer) => new Anchor
        {
            TrustedMs = trustedMs,
            UptimeMs = device.UptimeMs,
            LastSeenMs = trustedMs,
            FromServer = fromServer
        };
    }
}
