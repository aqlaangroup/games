using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Customers;
using Gahwit.Decor;
using Gahwit.Memories;
using Gahwit.Merge;
using Gahwit.Platform;
using Gahwit.Save;
using Gahwit.Story;
using Gahwit.Tutorial;
using Gahwit.Wardrobe;
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
        GemShop gems;
        readonly NotificationService notifications = new NotificationService();
        Unlocks unlocks;
        float nextUnlockCheck;
        CustomerRules customers;
        DecorRules decor;
        WardrobeRules wardrobe;
        readonly List<CafeDecor.Placement> placements = new List<CafeDecor.Placement>();
        readonly List<CafeCustomers.Info> present = new List<CafeCustomers.Info>();
        bool customersShown;
        const string TutorialSeenKey = "gahwit.tutorial.finishedOnThisPhone";
        PlatformAnalytics analytics;
        float sessionStartedAt;
        string cafeMusic;
        TimeOfDay timeOfDay;
        bool inMemory;
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
            game = new Game(config, content, state) { English = saves.Data.Settings.Language == "en" };
            game.Tick(time.NowMs);

            var camera = Camera.main;
            cafe = new GameObject("Cafe").AddComponent<CafeRoom>();
            cafe.Build(FindSun(), camera);
            timeOfDay = CafeRoom.TimeFromClock(DateTime.Now, config.LevelB.Cafe.TimeOfDayStartHours);
            cafe.SetTimeOfDay(timeOfDay);
            cafe.SetWeather(Weather.For(DateTime.Now, config.Weather));
            cafe.Apply(game.CompletedTaskIds(), game.State.TaskStyles, game.Content.Styles);
            var cafeCamera = camera.gameObject.AddComponent<CafeCamera>();

            account = new AccountService(services.Platform, services.PlatformConfig, saves, time, game, Save, Reload, ReloadFresh);
            tutorial = new TutorialFlow(game, TutorialContent.Load(), () => saves.Data.Account.BirthYear != null);
            // Sizes and sides for the code-built interface (COMPLETE_SPEC.md sections 4 and 5).
            UiKit.MinTouch = UiRules.MinTouchUnits(config.Ui);
            UiKit.TextScale = UiRules.TextScale(config.Ui, saves.Data.Settings.LargeText);
            UiKit.MinText = UiRules.MinTextUnits(config.Ui, saves.Data.Settings.LargeText);
            UiKit.LeftToRight = account.Language == "en";
            SpringIn.Seconds = config.Ui.PanelSlideSeconds;
            ui = GameUi.Create(game, cafeCamera, cafe, () => time.NowMs, OpenDrawer, account, tutorial,
                PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1);
            account.Ui = ui;
            ui.Dialogue.ShowGlosses = saves.Data.Settings.Glossary;

            analytics = services.Analytics;
            TrackGame();
            notifications.Clear();
            WireDecor();
            WireWardrobe();
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
            ApplyLookSettings();
            account.SettingChanged += ApplySoundSettings;
            account.SettingChanged += ApplyLookSettings;
            gameObject.AddComponent<FrameStats>().Init(game.Config.Ui.Graphics, sample => Track("perf",
                "fps", Mathf.Round(sample.AverageFps * 10) / 10, "slowShare", Mathf.Round(sample.SlowShare * 1000) / 1000,
                "worstMs", Mathf.RoundToInt(sample.WorstMs), "memoryMb", SystemInfo.systemMemorySize,
                "allocatedMb", (int)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576),
                "device", SystemInfo.deviceModel, "highGraphics", UiRules.HighGraphics(account.Quality, PhoneSettings.MemoryMb, game.Config.Ui.Graphics)));
            cafe.Decor.Settled += world => ui.DecorDust(world);
            WireSounds();
            cafeMusic = CafeMusic(timeOfDay);
            audio.PlayMusic(cafeMusic);

            game.Changed += () => dirty = true;
            game.OrderServed += order => ui.Toast.Show(Loc.T("toast.served", order.Coins, order.Xp));
            game.LeveledUp += ShowLevelUp;
            game.TaskCompleted += OnTaskCompleted;
            game.ChapterCompleted += OnChapterCompleted;
            // A level-up or a finished chapter earns gems; upload soon so the platform can pay them.
            game.LeveledUp += _ => SyncSoon();
            game.ChapterCompleted += _ => SyncSoon();
            Debug.Log("[Boot] Café and interface built.");

            nextSync = Time.unscaledTime + game.Config.Saves.CloudEverySeconds; // COMPLETE_SPEC.md section 9
            gems = new GemShop(services.Platform, game);
            ui.SetGems(() => gems.Buy("storageSlot:" + game.NextStorageSlot));
            ui.OutOfEnergy = ShowOutOfEnergy;
            ui.BubbleTapped = ShowBubble;
            unlocks = new Unlocks(game, Newtonsoft.Json.Linq.JObject.Parse(Resources.Load<TextAsset>("Content/unlock_cards").text)["cards"]
                .Select(c => (string)c));
            Run(async () =>
            {
                await account.Start();
                await gems.Replay();
            });
            if (services.AwayMs >= config.Board.FreeSpaceOfferAfterHoursAway * 3_600_000L) afterScene.Enqueue(OfferFreeSpace);
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

        // Decor (missing/07): the 15 pieces, placed on the café floor's grid, and the cosiness they add up to.
        void WireDecor()
        {
            var keepClear = CafeRoom.KeepClear().Select(a => new DecorLayout.Area(a.minX, a.minZ, a.maxX, a.maxZ));
            var layout = new DecorLayout(CafeRoom.Width, CafeRoom.Depth, game.Config.LevelB.Cafe.GridCellMeters, keepClear);
            decor = new DecorRules(game, DecorCatalog.Load(), layout);
            decor.Changed += ShowDecor;
            ui.SetDecor(decor);
            ShowDecor();
        }

        // Salma's wardrobe (missing/09): she wears her outfit in the café.
        void WireWardrobe()
        {
            wardrobe = new WardrobeRules(game, WardrobeContent.Load());
            wardrobe.Changed += DressSalma;
            ui.SetWardrobe(wardrobe);
            DressSalma();
        }

        // Her own clothes show where no wardrobe piece is worn.
        static readonly Color BaseTop = Palette.Hex("#F7EFE6"), BaseBottom = Palette.Hex("#6B5B73");

        void DressSalma()
        {
            Color? Worn(string slot) => wardrobe.Worn(slot) is string id ? Palette.Hex(wardrobe.Content.Get(id).Color) : (Color?)null;
            cafe.Salma.Dress(new CafeSalma.Outfit
            {
                Top = Worn("top") ?? BaseTop,
                Bottom = Worn("bottom_or_dress") ?? BaseBottom,
                Outer = Worn("outer"),
                Hijab = Worn("hijab_optional"),
                Dress = wardrobe.Worn("bottom_or_dress") is string bottom && wardrobe.Content.Get(bottom).Dress
            });
        }

        void ShowDecor()
        {
            placements.Clear();
            foreach (var piece in game.State.PlacedDecor)
            {
                var (x, z) = decor.Centre(piece);
                placements.Add(new CafeDecor.Placement { Id = piece.Id, X = x, Z = z, Degrees = piece.Rot * 90 });
            }
            cafe.Decor.Show(placements, decor.Catalog, game.Config.LevelB.Cafe.GridCellMeters);
            cafe.Apply(game.CompletedTaskIds(), game.State.TaskStyles, game.Content.Styles); // a style may have changed
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

        // The light and music follow the clock while the app stays open (missing/07).
        void ChangeTimeOfDay(TimeOfDay now)
        {
            timeOfDay = now;
            cafe.SetTimeOfDay(now);
            cafeMusic = CafeMusic(now);
            if (!inMemory) audio.PlayMusic(cafeMusic);
        }

        // The café's music follows the time of day (missing/14); the afternoon shares the morning's track for now.
        static string CafeMusic(TimeOfDay timeOfDay) => timeOfDay switch
        {
            TimeOfDay.Evening => "music_cafe_evening",
            TimeOfDay.Night => "music_cafe_night",
            _ => "music_cafe_morning"
        };

        // Reduced motion (the player's switch or the phone's own) and graphics quality take effect at once.
        void ApplyLookSettings()
        {
            bool calm = account.GetSetting("reducedMotion");
            Tweens.Reduced = calm;
            cafe.Decor.Calm = calm;
            var graphics = game.Config.Ui.Graphics;
            bool high = UiRules.HighGraphics(account.Quality, PhoneSettings.MemoryMb, graphics);
            cafe.SetQuality(high);
            Application.targetFrameRate = UiRules.FrameRate(high, graphics);
        }

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
        void PlayMemory(string memoryId) => PlayMemory(memoryId, game.Content.Memories[memoryId], true);

        // One of the practice memories, one per game type (Content/memory_practice.json), for trying the games before
        // their chapters are written (missing/12). Nothing is saved. Editor menu: Gahwit Sitti > Play memory game.
        public void PracticeMemory(string type)
        {
            var all = Newtonsoft.Json.Linq.JObject.Parse(Resources.Load<TextAsset>("Content/memory_practice").text)["memories"];
            var def = all["practice_" + type]?.ToObject<MemoryDef>();
            if (def == null) Debug.LogWarning($"[Memory] No practice memory for '{type}'.");
            else if (!ui.Dialogue.IsOpen && !ui.Memory.IsPlaying) PlayMemory("practice_" + type, def, false);
        }

        void PlayMemory(string memoryId, MemoryDef def, bool record)
        {
            var config = game.Config.LevelB.Memories;
            var story = Story(def.Story);
            PlayScene(story, def.LeadKnot, () =>
            {
                // 1974: the café as Teta ran it, in faded film colours. A sepia wash spreads from the last touch.
                ui.MemoryWipe();
                cafe.SetPast(true);
                inMemory = true;
                audio.PlayMusic("music_memory");
                // Teta ran it in the old Levantine style.
                cafe.Apply(game.Content.Chapters.SelectMany(c => c.Tasks).Select(t => t.Id).ToList(),
                    new Dictionary<string, string> { ["sign"] = "old", ["chairs"] = "old" }, game.Content.Styles);
                ui.Memory.ShowBanner(true);
                PlayScene(story, def.IntroKnot, () => PlayMemoryGame(memoryId, def, story, played => AfterMemoryGame(memoryId, def, story, record, played, stars =>
                {
                    PlayScene(story, stars >= config.Stars[1] ? def.PerfectKnot : def.OkKnot, () =>
                        PlayScene(story, def.OutroKnot, () =>
                        {
                            cafe.SetPast(false);
                            inMemory = false;
                            audio.PlayMusic(cafeMusic);
                            cafe.Apply(game.CompletedTaskIds(), game.State.TaskStyles, game.Content.Styles);
                            ui.Memory.ShowBanner(false);
                            PlayScene(story, def.ReturnKnot, null);
                        }));
                })));
            });
        }

        // Records the stars, then offers a retry for more while it's under 3 (SYSTEMS_B.md section 9): paid in gems
        // through the platform (memoryRetry:<id>, balance.json levelB.memories.skipRetryGems), never required.
        void AfterMemoryGame(string memoryId, MemoryDef def, StoryPlayer story, bool record, int stars, Action<int> then)
        {
            var config = game.Config.LevelB.Memories;
            if (record)
            {
                game.RecordMemory(memoryId, stars);
                notifications.AskOnce(); // after the first memory, never at first launch
            }
            Track(record ? "memory_played" : "memory_practice", "memory", memoryId, "type", def.Type, "stars", stars);
            int best = record ? game.State.MemoryStars[memoryId] : stars;
            if (!record || best >= config.Stars[1])
            {
                ui.Toast.Show(Loc.T("memory.stars", stars, config.Stars[1]));
                then(best);
                return;
            }
            ui.Prompt.Show(Loc.T("memory.retryTitle"), Loc.T("memory.stars", best, config.Stars[1]), Loc.T("memory.retryText"), new[]
            {
                PromptView.Primary(Loc.T("memory.retry", config.SkipRetryGems), () => Run(async () =>
                {
                    var result = await gems.Buy("memoryRetry:" + memoryId);
                    if (result == ActionResult.Done)
                        PlayMemoryGame(memoryId, def, story, again => AfterMemoryGame(memoryId, def, story, true, again, then));
                    else
                    {
                        ui.Toast.Show(Loc.T("gems." + result.ToString().ToLowerInvariant()));
                        then(best);
                    }
                })),
                PromptView.Quiet(Loc.T("memory.carryOn"), () => then(best))
            });
        }

        // The game itself, by type (SYSTEMS_B.md section 5). The easy assist (COMPLETE_SPEC.md section 5) widens the
        // timing games' targets. The seed comes from the memory's id, so a memory looks the same every time.
        void PlayMemoryGame(string memoryId, MemoryDef def, StoryPlayer story, Action<int> done)
        {
            var config = game.Config.LevelB.Memories;
            bool easy = saves.Data.Settings.MemoryAssist;
            int seed = 0;
            foreach (char c in memoryId) seed = seed * 31 + c;
            switch (def.Type)
            {
                case "pour":
                    ui.Memory.PlayPour(new PourGame(config, easy), done);
                    break;
                case "rhythm":
                    ui.Memory.Rhythm.Play(new RhythmGame(config, easy), done);
                    break;
                case "find":
                    ui.Memory.Find.Play(new FindGame(config, def.Objects), def.Objects, def.Decoys ?? new List<string>(), seed, done);
                    break;
                case "restore":
                    ui.Memory.Restore.Play(new RestoreGame(config, seed), done);
                    break;
                case "trace":
                    var line = def.Line.Select(p => (p[0], p[1])).ToList();
                    ui.Memory.Trace.Play(new TraceGame(config, line, easy), done);
                    break;
                case "tune":
                    ui.Memory.Tune.Play(new TuneGame(config, seed, easy), done);
                    break;
                case "choose":
                    var choose = new ChooseGame(config, easy);
                    ui.Dialogue.PlayTimed(story, def.GameKnot, choose, () => done(choose.Stars));
                    break;
                case "reassemble":
                    var strips = story.ReadAll(def.GameKnot).Select(l => l.Text).ToList();
                    ui.Memory.Reassemble.Play(new ReassembleGame(config, strips.Count, seed), strips, done);
                    break;
                default:
                    Debug.LogWarning($"[Memory] Unknown memory type '{def.Type}' in {memoryId}; the story goes on.");
                    done(config.Stars[0]);
                    break;
            }
        }

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
            afterScene.Enqueue(() =>
            {
                cafe.LightsOn();
                string title = game.Config.Chapter(chapter)?.Ar ?? "";
                ui.Prompt.Show(Loc.T("chapterDone.title", chapter), title,
                    Loc.T("chapterDone.rewards", reward.Gems, reward.Energy, generator),
                    new[] { PromptView.Primary(Loc.T("chapterDone.continue"), OfferFreeSpace) }, celebrate: true);
            });
            StartCoroutine(PlayTheme());
        }

        // The level-up card (screens/LevelUp): the new level, its energy and gems, and a petal burst.
        void ShowLevelUp(int level)
        {
            int energy = game.Config.PlayerLevels.Rewards.Energy, gems = game.Config.LevelUpGems(level);
            ui.Prompt.Show(Loc.T("levelUp.title"), level.ToString(), Loc.T("levelUp.rewards", energy, gems),
                new[] { PromptView.Primary(Loc.T("levelUp.continue"), MaybeShowUnlock) }, celebrate: true,
                countUp: t => Loc.T("levelUp.rewards", Mathf.RoundToInt(energy * t), Mathf.RoundToInt(gems * t)));
        }

        // Out of energy (screens/OutOfEnergy): the wait, a gem refill through the platform, and a tip.
        void ShowOutOfEnergy()
        {
            var refill = game.Config.Energy.GemRefill;
            int price = refill.GemCostsByPurchaseOfDay[Mathf.Min(RefillsToday(), refill.GemCostsByPurchaseOfDay.Count - 1)];
            int seconds = game.SecondsToFullEnergy(time.NowMs);
            ui.Prompt.Show(Loc.T("energy.title"), $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}", Loc.T("energy.tip"), new[]
            {
                PromptView.Primary(Loc.T("energy.refill", refill.Amount, price), () => Run(async () =>
                {
                    var result = await gems.Buy("energyRefill");
                    if (result == ActionResult.Done) CountRefill();
                    ui.Toast.Show(Loc.T("gems." + result.ToString().ToLowerInvariant()));
                })),
                new PromptView.Choice { Label = Loc.T("ads.soon"), Color = Palette.Muted, Enabled = false },
                PromptView.Quiet(Loc.T("common.close"))
            });
        }

        // Refill prices rise through the day (energy.gemRefill); the platform has the final say, this is the label.
        static string RefillKey => "gahwit.refills." + DateTime.UtcNow.ToString("yyyy-MM-dd");
        static int RefillsToday() => PlayerPrefs.GetInt(RefillKey, 0);
        static void CountRefill() => PlayerPrefs.SetInt(RefillKey, RefillsToday() + 1);

        // A bubble offer (screens/Bubble): its piece for gems through the platform, before it floats away.
        void ShowBubble()
        {
            var offer = game.Bubble;
            if (offer == null) return;
            int price = game.BubbleGems(offer.Level);
            ui.Prompt.Show(Loc.T("bubble.title"), game.ItemName(offer.Chain, offer.Level), Loc.T("bubble.text"), new[]
            {
                PromptView.Primary(Loc.T("bubble.buy", price), () => Run(async () =>
                {
                    var result = await gems.Buy($"bubble:{offer.Level}:{offer.Chain}");
                    ui.Toast.Show(Loc.T("gems." + result.ToString().ToLowerInvariant()));
                })),
                new PromptView.Choice { Label = Loc.T("ads.soon"), Color = Palette.Muted, Enabled = false },
                PromptView.Quiet(Loc.T("bubble.no"), game.LetBubbleGo)
            });
        }

        // The free-space offer (GAME_SYSTEMS.md section 2): after a chapter, or after a long time away.
        void OfferFreeSpace()
        {
            if (!game.NeedsSpace) return;
            var plan = game.SpacePlan();
            if (plan.Count == 0) return;
            int coins = plan.Sum(cell => game.SellValue(game.ItemAt(cell)));
            var choices = new List<PromptView.Choice> { PromptView.Primary(Loc.T("space.sell", plan.Count, coins), () => game.ClearSpace(false)) };
            if (game.StorageCapacity - game.State.Storage.Count > 0) choices.Add(PromptView.Quiet(Loc.T("space.store"), () => game.ClearSpace(true)));
            choices.Add(PromptView.Quiet(Loc.T("space.later")));
            ui.Prompt.Show(Loc.T("space.title"), null, Loc.T("space.text"), choices);
        }

        // One unlock card at a time, never during a scene, a panel or the hands-on tutorial.
        void MaybeShowUnlock()
        {
            if (ui.Dialogue.IsOpen || ui.ModalOpen || !tutorial.Done && tutorial.Current != null && tutorial.Current.HandsOn) return;
            string feature = unlocks.NextCard();
            if (feature == null) return;
            unlocks.Shown(feature);
            ui.Prompt.Show(Loc.T($"unlock.{feature}.title"), null, Loc.T($"unlock.{feature}.text"),
                new[] { PromptView.Primary(Loc.T("unlock.ok"), null) });
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
            if (story == null || string.IsNullOrEmpty(knot))
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
            if (Time.unscaledTime >= nextUnlockCheck)
            {
                nextUnlockCheck = Time.unscaledTime + 5f;
                MaybeShowUnlock();
            }
            if (Time.unscaledTime >= nextTick)
            {
                nextTick = Time.unscaledTime + 1f;
                var now = CafeRoom.TimeFromClock(DateTime.Now, game.Config.LevelB.Cafe.TimeOfDayStartHours);
                if (now != timeOfDay) ChangeTimeOfDay(now);
                game.TimeOfDay = now.ToString().ToLowerInvariant();
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
                notifications.Clear();
                return;
            }
            Track("session_end", "seconds", (int)(Time.realtimeSinceStartup - sessionStartedAt));
            notifications.Schedule(game, saves.Data.Settings.Notifications, time.NowMs);
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
