using Sentry.Unity;
using UnityEngine;

namespace Gahwit.Boot
{
    // What the crash reporter (the open-source Sentry SDK, sending to our own GlitchTip at errors.aqlaan.com;
    // missing/16) sets when the game starts: the release and environment a report belongs to. Nothing personal is
    // sent. The rest of its settings are in Resources/Sentry/SentryOptions (Gahwit Sitti > Set up crash reporting).
    public sealed class CrashReportingOptions : SentryOptionsConfiguration
    {
        public override void Configure(SentryUnityOptions options)
        {
            options.Release = "gahwit-sitti@" + Application.version;
#if GAHWIT_TOUR
            options.Environment = "tour"; // the simulator's screen tour (Boot/ScreenTour.cs)
#else
            options.Environment = Debug.isDebugBuild ? "development" : "production";
#endif
            options.SendDefaultPii = false;
            options.IsEnvironmentUser = false;
        }
    }
}
