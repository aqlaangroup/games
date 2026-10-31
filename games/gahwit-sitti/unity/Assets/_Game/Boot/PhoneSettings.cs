using System.Runtime.InteropServices;
using UnityEngine;

namespace Gahwit.Boot
{
    // The phone's own settings that the game follows (COMPLETE_SPEC.md section 5).
    public static class PhoneSettings
    {
        // iPhone: Settings > Accessibility > Motion > Reduce Motion. Android: animations turned off in the developer
        // or accessibility settings, which sets the animator scale to 0.
        public static bool ReduceMotion
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return GahwitReduceMotion() != 0;
#elif UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
                    using var global = new AndroidJavaClass("android.provider.Settings$Global");
                    return global.CallStatic<float>("getFloat", resolver, "animator_duration_scale", 1f) == 0f;
                }
                catch (AndroidJavaException)
                {
                    return false;
                }
#else
                return false;
#endif
            }
        }

        // Megabytes of memory, for the 'auto' graphics choice.
        public static int MemoryMb => SystemInfo.systemMemorySize;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int GahwitReduceMotion();
#endif
    }
}
