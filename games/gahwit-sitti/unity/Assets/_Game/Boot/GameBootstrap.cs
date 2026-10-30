using System;
using System.Collections.Generic;
using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Story;
using Gahwit.UI;
using UnityEngine;

namespace Gahwit.Boot
{
    // Starts the game: loads config, content and the save, builds the café and the interface, plays story scenes and saves.
    public sealed class GameBootstrap : MonoBehaviour
    {
        public const string SaveSlot = "gahwit-sitti";
        const float SaveEverySeconds = 2f;

        readonly Dictionary<string, StoryPlayer> stories = new Dictionary<string, StoryPlayer>();
        readonly Queue<Action> afterScene = new Queue<Action>();
        Game game;
        GameUi ui;
        CafeRoom cafe;
        bool dirty;
        float nextSave, nextTick;

        static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        void Awake()
        {
            Application.targetFrameRate = 60;
            Loc.Load("ar");
            var config = BalanceConfig.Load();
            var content = ChapterContent.Load();
            var state = SaveStore.Load<GameState>(SaveSlot) ?? Game.NewState(config, content, (ulong)DateTime.UtcNow.Ticks, Now());
            game = new Game(config, content, state);
            game.Tick(Now());

            var camera = Camera.main;
            cafe = new GameObject("Cafe").AddComponent<CafeRoom>();
            cafe.Build(FindSun(), camera);
            cafe.SetTimeOfDay(CafeRoom.TimeFromClock(DateTime.Now));
            cafe.Apply(state.CompletedTasks);
            var cafeCamera = camera.gameObject.AddComponent<CafeCamera>();
            ui = GameUi.Create(game, cafeCamera, Now);

            game.Changed += () => dirty = true;
            game.OrderServed += order => ui.Toast.Show(Loc.T("toast.served", order.Coins, order.Xp));
            game.LeveledUp += level => ui.Toast.Show(Loc.T("toast.levelUp", level, config.PlayerLevels.Rewards.Energy));
            game.TaskCompleted += OnTaskCompleted;
            game.ChapterCompleted += OnChapterCompleted;
        }

        void Start()
        {
            if (!game.State.IntroSeen) PlayScene(game.State.Chapter, game.Content.IntroKnot, () =>
            {
                game.State.IntroSeen = true;
                dirty = true;
            });
        }

        void OnTaskCompleted(TaskDef task)
        {
            // Fires before a finished chapter moves on, so State.Chapter is still the task's chapter.
            cafe.Apply(game.State.CompletedTasks);
            ui.ShowCafe();
            PlayScene(game.State.Chapter, task.Knot);
        }

        void OnChapterCompleted(int chapter)
        {
            var reward = game.Config.Chapter(chapter).Reward;
            string generator = game.Config.GeneratorDef(reward.GeneratorCopy)?.Ar ?? "";
            afterScene.Enqueue(() => ui.Toast.Show(Loc.T("toast.chapterDone", chapter, reward.Energy, generator)));
        }

        void PlayScene(int chapter, string knot, Action done = null)
        {
            var story = StoryFor(chapter);
            if (story == null)
            {
                done?.Invoke();
                return;
            }
            ui.Dialogue.Play(story, knot, () =>
            {
                done?.Invoke();
                while (afterScene.Count > 0) afterScene.Dequeue()();
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
                game.Tick(Now());
            }
            if (dirty && Time.unscaledTime >= nextSave) Save();
        }

        void Save()
        {
            dirty = false;
            nextSave = Time.unscaledTime + SaveEverySeconds;
            SaveStore.Save(SaveSlot, game.State);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();
    }
}
