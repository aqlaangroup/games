using System;
using System.Collections;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Platform;
using Gahwit.Save;
using UnityEngine;

namespace Gahwit.Boot
{
    // The one composition root: everything the scenes share, made once by the Boot scene.
    // Scenes take what they need from here instead of building it themselves.
    public sealed class Services
    {
        public static Services Current { get; private set; }

        public BalanceConfig Config { get; private set; }
        public ChapterContent Content { get; private set; }
        public SaveService Saves { get; private set; }
        public GameState LoadedState { get; private set; } // null when there was no save
        public TimeService Time { get; private set; }
        public PlatformConfig PlatformConfig { get; private set; }
        public PlatformClient Platform { get; private set; }
        public AudioService Audio { get; private set; }

        // A coroutine, because the text tables load in the background (a blocking wait hangs on the phone).
        public static IEnumerator Start()
        {
            if (Current != null) yield break;
            var services = new Services
            {
                Config = BalanceConfig.Load(),
                Content = ChapterContent.Load()
            };
            services.Saves = new SaveService(services.Config, services.Content);
            services.LoadedState = services.Saves.Load(out var clock);
            Debug.Log("[Boot] Save loaded.");
            yield return Loc.Load(services.Saves.Data.Settings.Language);
            Debug.Log("[Boot] Text loaded.");
            services.Time = new TimeService(new DeviceClock(), clock);
            services.PlatformConfig = PlatformConfig.Load();
            services.Platform = new PlatformClient(new UnityPlatformHttp(), new DeviceSecrets(), services.PlatformConfig.ActiveBaseUrl,
                services.PlatformConfig.Slug, SystemInfo.deviceModel, Application.version, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            services.Audio = AudioService.Shared;
            Current = services;
        }

        // Forgets everything, so the Boot scene loads it again: after a new language, a cloud save or a deleted account.
        public static void Reset() => Current = null;
    }
}
