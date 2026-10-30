using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Merge;
using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Tutorial
{
    public sealed class TutorialContent
    {
        public const string ResourcePath = "Content/tutorial";
        public List<TutorialStep> Steps = new List<TutorialStep>();

        public static TutorialContent Load() =>
            JsonConvert.DeserializeObject<TutorialContent>(Resources.Load<TextAsset>(ResourcePath).text);
    }

    public sealed class TutorialStep
    {
        public int N;
        public string Id;
        public Until Until;
        public string Target; // what the pointer shows: board.pair, board.generator, order.serve, cafe.restore
        public string Screen; // kitchen or cafe: where the step happens
        public string Text;   // UI string table key

        public bool HandsOn => Text != null;
    }

    public sealed class Until
    {
        public string Kind; // ageGate, scene, merge, tap, serve, afford, task, memory, letter, free
        public string Knot;
        public string Task;
        public string Memory;
        public string Letter;
    }

    // The first-time steps (COMPLETE_SPEC.md section 1). Each finishes when the game's state shows it, so closing
    // the app resumes at the same step. Plain C#: the view only draws the pointer and the banner.
    public sealed class TutorialFlow
    {
        readonly Game game;
        readonly TutorialContent content;
        readonly Func<bool> ageAnswered;
        bool merged, tapped;

        public event Action<TutorialStep> StepFinished;
        public event Action Skipped;

        public TutorialFlow(Game game, TutorialContent content, Func<bool> ageAnswered)
        {
            this.game = game;
            this.content = content;
            this.ageAnswered = ageAnswered;
            game.ItemsMerged += _ => merged = true;
            game.ItemSpawned += _ => tapped = true;
            game.Changed += Check;
        }

        public bool Done => game.State.TutorialDone;
        public TutorialStep Current => Done ? null : content.Steps.FirstOrDefault(s => s.N == game.State.TutorialStep + 1);

        // The hands-on steps are numbered 1 to 5 on the banner (screens/Tutorial).
        public int HandsOnCount => content.Steps.Count(s => s.HandsOn);
        public int HandsOnNumber(TutorialStep step) => content.Steps.Where(s => s.HandsOn).TakeWhile(s => s != step).Count() + 1;

        public void Check()
        {
            for (var step = Current; step != null && Finished(step); step = Current) Finish(step);
        }

        bool Finished(TutorialStep step)
        {
            var s = game.State;
            bool pastKitchen = s.OrdersServed > 0;
            switch (step.Until.Kind)
            {
                case "ageGate": return ageAnswered();
                case "scene": return s.ScenesSeen.Contains(step.Until.Knot) || pastKitchen;
                case "merge": return merged || pastKitchen || s.Board.Any(i => i != null && !i.IsGenerator && i.Level > 1);
                case "tap": return tapped || pastKitchen;
                case "serve": return pastKitchen;
                case "afford": return game.CanAffordTask || s.TaskIndex > 0 || s.Chapter > 1;
                case "task": return s.Chapter > 1 || game.CompletedTaskIds().Contains(step.Until.Task);
                case "memory": return s.MemoryStars.ContainsKey(step.Until.Memory);
                case "letter": return s.LettersFound.Contains(step.Until.Letter);
                default: return true; // free play
            }
        }

        void Finish(TutorialStep step)
        {
            game.State.TutorialStep = step.N;
            if (step.N == content.Steps.Max(s => s.N)) End();
            StepFinished?.Invoke(step);
        }

        // Stops guiding; the story still plays as tasks and memories happen. Allowed once this phone has finished
        // the tutorial before (the decision for "skippable after the first time", DEVLOG 2026-10-31).
        public void Skip()
        {
            if (Done) return;
            game.State.TutorialStep = Math.Max(game.State.TutorialStep, content.Steps.Where(s => s.HandsOn).Max(s => s.N));
            End();
            Skipped?.Invoke();
        }

        // Free play starts with a few easy orders (balance.json tutorial.easyOrdersAfter).
        void End()
        {
            game.State.TutorialDone = true;
            game.State.EasyOrdersLeft = game.Config.Tutorial.EasyOrdersAfter;
        }
    }
}
