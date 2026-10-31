using System;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace Aqlaan.CozyKit
{
    // The phone's clock and its uptime counter. The uptime counter keeps running while the phone sleeps
    // (Android elapsedRealtime, iOS CLOCK_MONOTONIC), so energy still refills overnight.
    public sealed class DeviceClock : IDeviceClock
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        readonly AndroidJavaClass systemClock = new AndroidJavaClass("android.os.SystemClock");
#endif

        public long UtcNowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public long UptimeMs
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return systemClock.CallStatic<long>("elapsedRealtime");
#elif UNITY_IOS && !UNITY_EDITOR
                return CozyKitUptimeMs();
#else
                return System.Diagnostics.Stopwatch.GetTimestamp() * 1000 / System.Diagnostics.Stopwatch.Frequency;
#endif
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern long CozyKitUptimeMs();
#endif
    }
}
