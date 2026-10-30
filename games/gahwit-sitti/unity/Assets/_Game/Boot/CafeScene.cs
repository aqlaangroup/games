using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Memories;
using Gahwit.Merge;
using Gahwit.Save;
using Gahwit.Story;
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
            cafe.SetTimeOfDay(CafeRoom.TimeFromClock(DateTime.Now, config.LevelB.Cafe.TimeOfDayStartHours));
            cafe.Apply(game.CompletedTaskIds(), game.State.TaskStyles, game.Content.Styles);
            var cafeCamera = camera.gameObject.AddComponent<CafeCamera>();

            account = new AccountService(services.Platform, services.PlatformConfig, saves, time, game, Save, Reload, ReloadFresh);
            ui = GameUi.Create(game, cafeCamera, cafe, () => time.NowMs, OpenDrawer, account);
            account.Ui = ui;
            ui.Dialogue.ShowGlosses = saves.Data.Settings.Glossary;

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
                if (!reloading) PlayIntro();
            });
            else PlayIntro();
        }

        void PlayIntro()
        {
            string intro = game.Content.IntroKnot;
            if (!game.State.ScenesSeen.Contains(intro)) PlayScene(StoryFor(game.State.Chapter), intro, ResumeMemories);
            else ResumeMemories();
        }

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
                // Teta ran it in the old Levantine style.
                cafe.Apply(game.Content.Chapters.SelectMany(c => c.Tasks).Select(t => t.Id).ToList(),
                    new Dictionary<string, string> { ["sign"] = "old", ["chairs"] = "old" }, game.Content.Styles);
                ui.Memory.ShowBanner(true);
                // The easy assist (COMPLETE_SPEC.md section 5) needs its own settings field; off until then.
                PlayScene(story, def.IntroKnot, () => ui.Memory.PlayPour(new PourGame(config, easyAssist: false), stars =>
                {
                    game.RecordMemory(memoryId, stars);
                    ui.Toast.Show(Loc.T("memory.stars", stars, config.Stars[1]));
                    PlayScene(story, stars >= config.Stars[1] ? def.PerfectKnot : def.OkKnot, () =>
                        PlayScene(story, def.OutroKnot, () =>
                        {
                            cafe.SetPast(false);
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
            });
        }

        void OnChapterCompleted(int chapter)
        {
            var reward = game.Config.Chapter(chapter).Reward;
            string generator = game.Config.GeneratorDef(reward.GeneratorCopy)?.Ar ?? "";
            afterScene.Enqueue(() => ui.Toast.Show(Loc.T("toast.chapterDone", chapter, reward.Energy, generator)));
        }

        void PlayScene(StoryPlayer story, string knot, Action then)
        {
            if (story == null)
            {
                then?.Invoke();
                return;
            }
            ui.Dialogue.Play(story, knot,
                () =>
                {
                    if (!game.State.ScenesSeen.Contains(knot)) game.State.ScenesSeen.Add(knot);
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
                game.Tick(time.NowMs);
            }
            if (dirty && Time.unscaledTime >= nextSave) Save();
            if (Time.unscaledTime >= nextSync)
            {
                nextSync = Time.unscaledTime + game.Config.Saves.CloudEverySeconds; // COMPLETE_SPEC.md section 9
                Run(() => account.Sync(false));
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
            if (!paused || account == null) return;
            Save();
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

        void OnApplicationQuit() => Save();
    }
}
