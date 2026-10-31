using System;
using System.Collections;
using Aqlaan.CozyKit;
using Aqlaan.CozyKit.Platform;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Save;
using UnityEngine;

namespace Gahwit.Boot
{
    // The one composition root: everything the scenes share, made once by the Boot scene.
    // Scenes take what they need from here instead of building it themselves.
    public sealed class Services
    {
        public static Services Current { get; private set; }

        // The Editor can enter Play mode without reloading code, which keeps statics from the last run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ForgetLastRun() => Current = null;

        public BalanceConfig Config { get; private set; }
        public ChapterContent Content { get; private set; }
        public SaveService Saves { get; private set; }
        public GameState LoadedState { get; private set; } // null when there was no save
        public TimeService Time { get; private set; }
        public PlatformConfig PlatformConfig { get; private set; }
        public PlatformClient Platform { get; private set; }
        public AudioService Audio { get; private set; }
        public PlatformAnalytics Analytics { get; private set; }
        public long AwayMs { get; private set; } // since the save was last written, for the free-space offer

        // A coroutine, because the text tables load in the background (a blocking wait hangs on the phone).
        public static IEnumerator Start()
        {
            if (Current != null) yield break;
            var services = new Services
            {
                Config = RemoteConfig.Load(), // the last good published config over the game's own (missing/18)
                Content = ChapterContent.Load()
            };
            services.Saves = new SaveService(services.Config, services.Content);
            services.LoadedState = services.Saves.Load(out var clock);
            Debug.Log("[Boot] Save loaded.");
            yield return Loc.Load(services.Saves.Data.Settings.Language);
            Debug.Log("[Boot] Text loaded.");
            long lastSeen = clock?.LastSeenMs ?? 0; // read before the time service moves it on
            services.Time = new TimeService(new DeviceClock(), clock);
            services.AwayMs = lastSeen > 0 ? Math.Max(0, services.Time.NowMs - lastSeen) : 0;
            services.PlatformConfig = PlatformConfig.Load();
            services.Platform = new PlatformClient(new UnityPlatformHttp(), new DeviceSecrets(), services.PlatformConfig.ActiveBaseUrl,
                services.PlatformConfig.Slug, SystemInfo.deviceModel, Application.version, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            services.Audio = AudioService.Shared;
            services.Analytics = new PlatformAnalytics(services.Platform, () => services.Time.NowMs, "gahwit-sitti-events");
            services.Analytics.CatchErrors();
            _ = RemoteConfig.Refresh(services.Platform); // for the next launch
            Current = services;
        }

        // Forgets everything, so the Boot scene loads it again: after a new language, a cloud save or a deleted account.
        public static void Reset()
        {
            if (Current == null) return;
            Current.Analytics.StopCatchingErrors();
            Current.Analytics.Keep();
            Current = null;
        }
    }
}
