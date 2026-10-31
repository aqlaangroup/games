using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Customers;
using Gahwit.Memories;
using Gahwit.Merge;
using Gahwit.Platform;
using Gahwit.Save;
using Gahwit.Story;
using Gahwit.Tutorial;
using Gahwit.UI;
using UnityEngine;

namespace Gahwit.Boot
{
    // The café scene: builds the 3D café and the interface from the shared services (Services, made by the Boot
    // scene), plays story scenes, saves on the phone and keeps the cloud save up to date.
    public sealed class CafeScene : MonoBehaviour
    {
        readonly Dictionary<string, StoryPlayer> stories = new Dictionary<string, StoryPlayer>();
        readonly Queue<Action> afterScene = new Queue<Action>();
        TimeService time;
        SaveService saves;
        Game game;
        GameUi ui;
        CafeRoom cafe;
        AccountService account;
        AudioService audio;
        TutorialFlow tutorial;
        CustomerRules customers;
        readonly List<CafeCustomers.Info> present = new List<CafeCustomers.Info>();
        bool customersShown;
        const string TutorialSeenKey = "gahwit.tutorial.finishedOnThisPhone";
        PlatformAnalytics analytics;
        float sessionStartedAt;
        string cafeMusic;
        long gemsHeard;
        readonly Dictionary<string, string> generatorCues = new Dictionary<string, string>();
        bool dirty, reloading;
        float nextSave, nextTick, nextSync;

        IEnumerator Start()
        {
            // Played straight from the Editor, the café makes the services itself.
            if (Services.Current == null) yield return Services.Start();
            var services = Services.Current;
            var config = services.Config;
            var content = services.Content;
            saves = services.Saves;
            time = services.Time;
            var state = services.LoadedState;
            if (state == null) state = Game.NewState(config, content, (ulong)DateTime.UtcNow.Ticks, time.NowMs);
            game = new Game(config, content, state);
            game.Tick(time.NowMs);

            var camera = Camera.main;
            cafe = new GameObject("Cafe").AddComponent<CafeRoom>();
            cafe.Build(FindSun(), camera);
            var timeOfDay = CafeRoom.TimeFromClock(DateTime.Now, config.LevelB.Cafe.TimeOfDayStartHours);
            cafe.SetTimeOfDay(timeOfDay);
            cafe.Apply(game.CompletedTaskIds(), game.State.TaskStyles, game.Content.Styles);
            var cafeCamera = camera.gameObject.AddComponent<CafeCamera>();

            account = new AccountService(services.Platform, services.PlatformConfig, saves, time, game, Save, Reload, ReloadFresh);
            tutorial = new TutorialFlow(game, TutorialContent.Load(), () => saves.Data.Account.BirthYear != null);
            ui = GameUi.Create(game, cafeCamera, cafe, () => time.NowMs, OpenDrawer, account, tutorial,
                PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1);
            account.Ui = ui;
            ui.Dialogue.ShowGlosses = saves.Data.Settings.Glossary;

            analytics = services.Analytics;
            TrackGame();
            WireCustomers();
            tutorial.StepFinished += OnTutorialStep;
            tutorial.Skipped += () =>
            {
                Track("tutorial_skipped", "afterStep", game.State.TutorialStep);
                dirty = true;
                ui.Refresh();
            };
            tutorial.Check();
            audio = services.Audio;
            ApplySoundSettings();
            account.SettingChanged += ApplySoundSettings;
            WireSounds();
            cafeMusic = CafeMusic(timeOfDay);
            audio.PlayMusic(cafeMusic);

            game.Changed += () => dirty = true;
            game.OrderServed += order => ui.Toast.Show(Loc.T("toast.served", order.Coins, order.Xp));
            game.LeveledUp += level => ui.Toast.Show(Loc.T("toast.levelUp", level, config.PlayerLevels.Rewards.Energy));
            game.TaskCompleted += OnTaskCompleted;
            game.ChapterCompleted += OnChapterCompleted;
            // A level-up or a finished chapter earns gems; upload soon so the platform can pay them.
            game.LeveledUp += _ => SyncSoon();
            game.ChapterCompleted += _ => SyncSoon();
            Debug.Log("[Boot] Café and interface built.");

            nextSync = Time.unscaledTime + game.Config.Saves.CloudEverySeconds; // COMPLETE_SPEC.md section 9
            Run(account.Start);
            if (account.NeedsAgeGate) ui.AgeGate.Show(account.Language, (language, birthYear) =>
            {
                account.SetAge(language, birthYear);
                tutorial.Check();
                if (!reloading) PlayIntro();
            });
            else PlayIntro();
        }

        void PlayIntro()
        {
            string intro = game.Content.IntroKnot;
            if (game.State.ScenesSeen.Contains(intro))
            {
                Arrival();
                return;
            }
            audio.PlayMusic("music_main_theme");
            PlayScene(StoryFor(game.State.Chapter), intro, () =>
            {
                audio.PlayMusic(cafeMusic);
                Arrival();
            });
        }

        // Tutorial step 3: the door bell, and Abu Fadi walks in with the first order. Then the kitchen opens.
        void Arrival()
        {
            const string knot = "abu_fadi_arrives";
            if (game.State.ScenesSeen.Contains(knot) || game.State.OrdersServed > 0)
            {
                ResumeMemories();
                return;
            }
            audio.Play("door_bell");
            PlayScene(StoryFor(game.State.Chapter), knot, () =>
            {
                ui.ShowKitchen();
                ResumeMemories();
            });
        }

        // Every order is a customer in the café (DEVLOG 2026-10-31): they walk in, sit, sip when served and leave.
        void WireCustomers()
        {
            customers = new CustomerRules(game);
            customers.Tipped += (_, coins) => ui.Toast.Show(Loc.T("toast.tip", coins));
            customers.FriendshipLevelUp += (who, level) =>
            {
                ui.Toast.Show(Loc.T("toast.friendship", Loc.T("char." + who), level));
                Track("friendship_level", "regular", who, "level", level);
                PlayRegularStory(who);
            };
            game.OrderServed += order => cafe.Customers.Serve(order.Id);
            cafe.Customers.Arrived += _ =>
            {
                if (customersShown) audio.Play("door_bell"); // not for the ones already there at launch
            };
            game.Changed += ShowCustomers;
            ShowCustomers();
            customersShown = true;
            foreach (var regular in game.State.Regulars.Keys.ToList()) PlayRegularStory(regular);
        }

        void ShowCustomers()
        {
            present.Clear();
            foreach (var order in game.Orders)
                present.Add(new CafeCustomers.Info { OrderId = order.Id, Color = CustomerColor(order) });
            cafe.Customers.Sync(present, customers.Seats(game.CompletedTaskIds().Contains("chairs")));
        }

        // Regulars wear their own colour; walk-ins one of a few soft colours, picked by order id.
        static readonly Color[] WalkInColors = { Palette.Lilac, Palette.RoseSoft, Palette.Hex("#A9C7B2"), Palette.Hex("#E7C9A0"), Palette.Hex("#9FB7D6") };

        Color CustomerColor(Order order) =>
            order.IsCharacter && game.Content.Characters.TryGetValue(order.Customer, out var character)
                ? Palette.Hex(character.Color)
                : WalkInColors[order.Id % WalkInColors.Length];

        // A regular's story scene at friendship levels 3, 6 and 10 (SYSTEMS_B.md section 3), after any scene playing.
        void PlayRegularStory(string who)
        {
            string knot = customers.StoryDue(who);
            if (knot == null) return;
            void Play() => PlayScene(StoryFor(game.State.Chapter), knot, () =>
            {
                customers.StorySeen(who, knot);
                dirty = true;
            });
            if (ui.Dialogue.IsOpen) afterScene.Enqueue(Play);
            else Play();
        }

        void OnTutorialStep(TutorialStep step)
        {
            Track("tutorial_step", "step", step.N, "id", step.Id);
            dirty = true;
            if (tutorial.Done)
            {
                PlayerPrefs.SetInt(TutorialSeenKey, 1); // later accounts on this phone may skip it
                PlayerPrefs.Save();
            }
            ui.Refresh();
        }

        // The events in GAME_SYSTEMS.md section 15 and missing/16 that the game has so far. Ads, the shop and live
        // events add theirs when they're built; the tutorial adds tutorial_step.
        void TrackGame()
        {
            sessionStartedAt = Time.realtimeSinceStartup;
            Track("session_start", "energy", game.State.Energy, "level", game.State.Level);
            game.ItemSpawned += cell =>
            {
                var item = game.ItemAt(cell);
                Track("generator_tap", "chain", item?.Chain, "level", item?.Level, "energyLeft", game.State.Energy);
                if (game.State.Energy < game.Config.Generators.TapEnergyCost)
                    Track("energy_empty", "secondsIntoSession", (int)(Time.realtimeSinceStartup - sessionStartedAt));
                if (game.EmptyCells == 0) Track("board_full");
            };
            game.ItemsMerged += cell => Track("merge", "chain", game.ItemAt(cell)?.Chain, "level", game.ItemAt(cell)?.Level);
            game.OrderServed += order => Track("order_served", "coins", order.Coins, "xp", order.Xp,
                "character", order.IsCharacter ? order.Customer : null, "lines", order.Lines.Count);
            game.TaskCompleted += task => Track("task_complete", "chapter", game.State.Chapter, "task", task.Id,
                "style", game.State.TaskStyles.TryGetValue(task.Id, out var style) ? style : null);
            game.LeveledUp += level => Track("level_up", "level", level);
        }

        void Track(string name, params object[] pairs)
        {
            var fields = new Dictionary<string, object>();
            for (int i = 0; i + 1 < pairs.Length; i += 2) fields[(string)pairs[i]] = pairs[i + 1];
            analytics.Track(name, fields);
        }

        // The café's music follows the time of day (missing/14); the afternoon shares the morning's track for now.
        static string CafeMusic(TimeOfDay timeOfDay) => timeOfDay switch
        {
            TimeOfDay.Evening => "music_cafe_evening",
            TimeOfDay.Night => "music_cafe_night",
            _ => "music_cafe_morning"
        };

        void ApplySoundSettings()
        {
            var settings = saves.Data.Settings;
            audio.SoundOn = settings.Sound;
            audio.MusicOn = settings.Music;
            audio.HapticsOn = settings.Haptics;
        }

        // Each kitchen and café action has its cue (missing/14). Merges rise in pitch with the new item's level.
        void WireSounds()
        {
            game.ItemSpawned += cell => audio.Play(GeneratorCue(game.ItemAt(cell)?.Chain));
            game.ItemsMerged += cell =>
            {
                var item = game.ItemAt(cell);
                if (item == null) return;
                bool top = !item.IsGenerator && item.Level >= game.Config.MaxItemLevel(item.Chain);
                audio.Play(top ? "merge_top" : "merge", 1f + 0.06f * (item.Level - 2));
                audio.Buzz(Haptic.Light);
            };
            game.OrderServed += _ =>
            {
                audio.Play("order_served");
                audio.Play("ui_coin");
                audio.Buzz(Haptic.Medium);
            };
            game.LeveledUp += _ =>
            {
                audio.Play("ui_level_up");
                audio.Buzz(Haptic.Success);
            };
            game.TaskCompleted += _ => audio.Play("decor_place");
            gemsHeard = Wallet.Gems;
            Wallet.Changed += OnGemsChanged;
        }

        void OnGemsChanged()
        {
            if (Wallet.Gems > gemsHeard) audio.Play("ui_gem");
            gemsHeard = Wallet.Gems;
        }

        // Chains without their own tap sound yet use the coffee one.
        string GeneratorCue(string chain)
        {
            if (chain == null) return "gen_tap_coffee";
            if (!generatorCues.TryGetValue(chain, out var cue))
                generatorCues[chain] = cue = audio.Has("gen_tap_" + chain) ? "gen_tap_" + chain : "gen_tap_coffee";
            return cue;
        }

        void OnDestroy() => Wallet.Changed -= OnGemsChanged;

        // A memory cut short by closing the app plays again on the next launch.
        void ResumeMemories()
        {
            var done = game.CompletedTaskIds();
            foreach (var task in game.Content.Chapters.SelectMany(c => c.Tasks))
            {
                if (task.Memory == null || !done.Contains(task.Id) || game.State.MemoryStars.ContainsKey(task.Memory)) continue;
                PlayMemory(task.Memory);
                return;
            }
        }

        void OnTaskCompleted(TaskDef task)
        {
            // Fires before a finished chapter moves on, so State.Chapter is still the task's chapter.
            cafe.Apply(game.CompletedTaskIds(), game.State.TaskStyles, game.Content.Styles);
            ui.ShowCafe();
            PlayScene(StoryFor(game.State.Chapter), task.Knot, () =>
            {
                if (task.Memory != null) PlayMemory(task.Memory);
            });
        }

        // A playable 1974 memory (SYSTEMS_B.md section 5), then back to today.
        void PlayMemory(string memoryId)
        {
            var def = game.Content.Memories[memoryId];
            var config = game.Config.LevelB.Memories;
            var story = Story(def.Story);
            if (def.Type != "pour")
            {
                Debug.LogWarning($"[Memory] '{def.Type}' memories aren't built yet; skipping {memoryId}.");
                return;
            }

            PlayScene(story, def.LeadKnot, () =>
            {
                // 1974: the café as Teta ran it, in faded film colours.
                cafe.SetPast(true);
                audio.PlayMusic("music_memory");
                // Teta ran it in the old Levantine style.
                cafe.Apply(game.Content.Chapters.SelectMany(c => c.Tasks).Select(t => t.Id).ToList(),
                    new Dictionary<string, string> { ["sign"] = "old", ["chairs"] = "old" }, game.Content.Styles);
                ui.Memory.ShowBanner(true);
                // The easy assist (COMPLETE_SPEC.md section 5) needs its own settings field; off until then.
                PlayScene(story, def.IntroKnot, () => ui.Memory.PlayPour(new PourGame(config, easyAssist: false), stars =>
                {
                    game.RecordMemory(memoryId, stars);
                    Track("memory_played", "memory", memoryId, "stars", stars);
                    ui.Toast.Show(Loc.T("memory.stars", stars, config.Stars[1]));
                    PlayScene(story, stars >= config.Stars[1] ? def.PerfectKnot : def.OkKnot, () =>
                        PlayScene(story, def.OutroKnot, () =>
                        {
                            cafe.SetPast(false);
                            audio.PlayMusic(cafeMusic);
                            cafe.Apply(game.CompletedTaskIds(), game.State.TaskStyles, game.Content.Styles);
                            ui.Memory.ShowBanner(false);
                            PlayScene(story, def.ReturnKnot, null);
                        }));
                }));
            });
        }

        // The letter in the counter drawer, Teta's voice note, then the message that arrives (COMPLETE_SPEC.md section 1, step 10).
        void OpenDrawer()
        {
            string letterId = game.LetterWaiting();
            if (letterId == null || ui.Dialogue.IsOpen) return;
            audio.Play("drawer_open");
            var def = game.Content.Letters[letterId];
            var story = Story(def.Story);
            var letter = story.ReadAll(def.Knot);
            var voice = story.ReadAll(def.VoiceKnot)[0];
            var message = story.ReadAll(def.NotifyKnot)[0];
            var recording = def.VoiceClip != null ? Resources.Load<AudioClip>(def.VoiceClip) : null;
            ui.Letter.Play(letter, voice, recording, message, Loc.T("char." + message.Speaker), () =>
            {
                foreach (var knot in new[] { def.Knot, def.VoiceKnot, def.NotifyKnot })
                    if (!game.State.ScenesSeen.Contains(knot)) game.State.ScenesSeen.Add(knot);
                game.FindLetter(letterId);
                Track("letter_found", "letter", letterId);
            });
        }

        void OnChapterCompleted(int chapter)
        {
            var reward = game.Config.Chapter(chapter).Reward;
            string generator = game.Config.GeneratorDef(reward.GeneratorCopy)?.Ar ?? "";
            afterScene.Enqueue(() => ui.Toast.Show(Loc.T("toast.chapterDone", chapter, reward.Energy, generator)));
            StartCoroutine(PlayTheme());
        }

        // The main theme marks a finished chapter, then the café's music comes back.
        IEnumerator PlayTheme()
        {
            audio.PlayMusic("music_main_theme");
            yield return new WaitForSecondsRealtime(30f);
            audio.PlayMusic(cafeMusic);
        }

        void PlayScene(StoryPlayer story, string knot, Action then)
        {
            if (story == null)
            {
                then?.Invoke();
                return;
            }
            float startedAt = Time.realtimeSinceStartup;
            ui.Dialogue.Play(story, knot,
                () =>
                {
                    Track("scene_viewed", "scene", knot, "seconds", (int)(Time.realtimeSinceStartup - startedAt));
                    if (!game.State.ScenesSeen.Contains(knot)) game.State.ScenesSeen.Add(knot);
                    tutorial.Check();
                    dirty = true;
                    while (afterScene.Count > 0) afterScene.Dequeue()();
                    then?.Invoke();
                },
                choice =>
                {
                    game.State.Choices[knot] = choice;
                    dirty = true;
                });
        }

        StoryPlayer StoryFor(int chapter) => Story(game.Content.ForChapter(chapter)?.Story);

        StoryPlayer Story(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!stories.TryGetValue(path, out var story)) stories[path] = story = StoryPlayer.FromResources(path);
            return story;
        }

        static Light FindSun()
        {
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) return light;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            return sun;
        }

        void Update()
        {
            if (account == null) return; // still starting
            if (Time.unscaledTime >= nextTick)
            {
                nextTick = Time.unscaledTime + 1f;
                game.TimeOfDay = CafeRoom.TimeFromClock(DateTime.Now, game.Config.LevelB.Cafe.TimeOfDayStartHours).ToString().ToLowerInvariant();
                game.Tick(time.NowMs);
                customers.Tick(time.NowMs);
            }
            if (dirty && Time.unscaledTime >= nextSave) Save();
            if (Time.unscaledTime >= nextSync)
            {
                nextSync = Time.unscaledTime + game.Config.Saves.CloudEverySeconds; // COMPLETE_SPEC.md section 9
                Run(() => account.Sync(false));
                Run(analytics.Flush);
            }
        }

        void SyncSoon() => nextSync = Mathf.Min(nextSync, Time.unscaledTime + 3f);

        void Save()
        {
            if (reloading || game == null) return;
            dirty = false;
            nextSave = Time.unscaledTime + game.Config.Saves.LocalEverySeconds;
            saves.Save(game.State, time);
        }

        void OnApplicationPause(bool paused)
        {
            if (account == null) return;
            if (!paused)
            {
                sessionStartedAt = Time.realtimeSinceStartup;
                Track("session_start", "energy", game.State.Energy, "level", game.State.Level);
                return;
            }
            Track("session_end", "seconds", (int)(Time.realtimeSinceStartup - sessionStartedAt));
            Save();
            Run(analytics.Flush);
            Run(() => account.Sync(false)); // also sent when the app goes to the background
        }

        // Starts again from the Boot scene: after a cloud save replaced this one, or a new language was chosen.
        void Reload(string saveText)
        {
            if (saveText != null) SaveStore.SaveText(SaveService.Slot, saveText);
            else Save();
            reloading = true;
            BootScene.Restart();
        }

        // After the account was deleted: nothing of it stays on this phone.
        void ReloadFresh()
        {
            reloading = true;
            SaveStore.Delete(SaveService.Slot);
            BootScene.Restart();
        }

        static async void Run(Func<System.Threading.Tasks.Task> work)
        {
            try { await work(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        void OnApplicationQuit()
        {
            if (analytics != null)
            {
                Track("session_end", "seconds", (int)(Time.realtimeSinceStartup - sessionStartedAt));
                analytics.Keep(); // sent on the next launch
            }
            Save();
        }
    }
}
