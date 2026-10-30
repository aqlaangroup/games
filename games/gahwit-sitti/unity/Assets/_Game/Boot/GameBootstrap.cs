using System;
using System.Collections.Generic;
using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Save;
using Gahwit.Story;
using Gahwit.UI;
using UnityEngine;

namespace Gahwit.Boot
{
    // Starts the game: loads config, content and the save, builds the café and the interface, plays story scenes and saves.
    public sealed class GameBootstrap : MonoBehaviour
    {
        const float SaveEverySeconds = 2f; // COMPLETE_SPEC.md section 9

        readonly Dictionary<string, StoryPlayer> stories = new Dictionary<string, StoryPlayer>();
        readonly Queue<Action> afterScene = new Queue<Action>();
        TimeService time;
        SaveService saves;
        Game game;
        GameUi ui;
        CafeRoom cafe;
        bool dirty;
        float nextSave, nextTick;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Loc.Load("ar");
            var config = BalanceConfig.Load();
            var content = ChapterContent.Load();

            saves = new SaveService(config, content);
            var state = saves.Load(out var clock);
            time = new TimeService(new DeviceClock(), clock);
            if (state == null) state = Game.NewState(config, content, (ulong)DateTime.UtcNow.Ticks, time.NowMs);
            game = new Game(config, content, state);
            game.Tick(time.NowMs);

            var camera = Camera.main;
            cafe = new GameObject("Cafe").AddComponent<CafeRoom>();
            cafe.Build(FindSun(), camera);
            cafe.SetTimeOfDay(CafeRoom.TimeFromClock(DateTime.Now));
            cafe.Apply(game.CompletedTaskIds());
            var cafeCamera = camera.gameObject.AddComponent<CafeCamera>();
            ui = GameUi.Create(game, cafeCamera, () => time.NowMs);

            game.Changed += () => dirty = true;
            game.OrderServed += order => ui.Toast.Show(Loc.T("toast.served", order.Coins, order.Xp));
            game.LeveledUp += level => ui.Toast.Show(Loc.T("toast.levelUp", level, config.PlayerLevels.Rewards.Energy));
            game.TaskCompleted += OnTaskCompleted;
            game.ChapterCompleted += OnChapterCompleted;
        }

        void Start()
        {
            string intro = game.Content.IntroKnot;
            if (!game.State.ScenesSeen.Contains(intro)) PlayScene(game.State.Chapter, intro);
        }

        void OnTaskCompleted(TaskDef task)
        {
            // Fires before a finished chapter moves on, so State.Chapter is still the task's chapter.
            cafe.Apply(game.CompletedTaskIds());
            ui.ShowCafe();
            PlayScene(game.State.Chapter, task.Knot);
        }

        void OnChapterCompleted(int chapter)
        {
            var reward = game.Config.Chapter(chapter).Reward;
            string generator = game.Config.GeneratorDef(reward.GeneratorCopy)?.Ar ?? "";
            afterScene.Enqueue(() => ui.Toast.Show(Loc.T("toast.chapterDone", chapter, reward.Energy, generator)));
        }

        void PlayScene(int chapter, string knot)
        {
            var story = StoryFor(chapter);
            if (story == null) return;
            ui.Dialogue.Play(story, knot,
                () =>
                {
                    if (!game.State.ScenesSeen.Contains(knot)) game.State.ScenesSeen.Add(knot);
                    dirty = true;
                    while (afterScene.Count > 0) afterScene.Dequeue()();
                },
                choice =>
                {
                    game.State.Choices[knot] = choice;
                    dirty = true;
                });
        }

        StoryPlayer StoryFor(int chapter)
        {
            var path = game.Content.ForChapter(chapter)?.Story;
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
            if (Time.unscaledTime >= nextTick)
            {
                nextTick = Time.unscaledTime + 1f;
                game.Tick(time.NowMs);
            }
            if (dirty && Time.unscaledTime >= nextSave) Save();
        }

        void Save()
        {
            dirty = false;
            nextSave = Time.unscaledTime + SaveEverySeconds;
            saves.Save(game.State, time);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();
    }
}
