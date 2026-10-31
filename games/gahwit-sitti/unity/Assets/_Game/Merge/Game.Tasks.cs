using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;

namespace Gahwit.Merge
{
    // Gahwit Sitti's café tasks, memories and letters (GAME_SYSTEMS.md section 9). These stay in the game
    // when the merge rules move to the shared package (missing/24).
    public sealed partial class Game
    {
        // ---------- Café tasks ----------

        public ChapterTasks ChapterTasks => Content.ForChapter(State.Chapter);

        public TaskDef CurrentTask
        {
            get
            {
                var chapter = ChapterTasks;
                return chapter != null && State.TaskIndex < chapter.Tasks.Count ? chapter.Tasks[State.TaskIndex] : null;
            }
        }

        public int CurrentTaskCost => CurrentTask == null ? 0 : Config.Chapter(State.Chapter).TaskCosts[State.TaskIndex];

        public bool CanAffordTask => CurrentTask != null && State.Coins >= CurrentTaskCost;

        // Every task finished so far, worked out from the chapter and task position.
        public List<string> CompletedTaskIds() => CompletedTaskIds(State.Chapter, State.TaskIndex);

        // The tasks done at a chapter and task, such as a friend's (missing/20).
        public List<string> CompletedTaskIds(int atChapter, int atTask)
        {
            var done = new List<string>();
            foreach (var chapter in Content.Chapters)
            {
                if (chapter.N < atChapter) done.AddRange(chapter.Tasks.Select(t => t.Id));
                else if (chapter.N == atChapter) done.AddRange(chapter.Tasks.Take(atTask).Select(t => t.Id));
            }
            return done;
        }

        public TaskDef CompleteTask(string style = null)
        {
            var task = CurrentTask;
            if (task == null || State.Coins < CurrentTaskCost) return null;

            State.Coins -= CurrentTaskCost;
            if (style != null) State.TaskStyles[task.Id] = style;
            State.TaskIndex++;
            AddXp(Config.TaskXp(State.Chapter));
            TaskCompleted?.Invoke(task);
            if (State.TaskIndex >= ChapterTasks.Tasks.Count) FinishChapter();
            Changed?.Invoke();
            return task;
        }

        // ---------- Memories and letters ----------

        // Keeps the best result; stars never block the story (SYSTEMS_B.md section 5).
        public void RecordMemory(string memoryId, int stars)
        {
            if (!State.MemoryStars.TryGetValue(memoryId, out int best) || stars > best) State.MemoryStars[memoryId] = stars;
            Changed?.Invoke();
        }

        // A letter is waiting in the counter drawer once its memory has been played and until it is read.
        public string LetterWaiting()
        {
            if (Content.Memories == null) return null;
            foreach (var memory in Content.Memories)
            {
                string letter = memory.Value.Letter;
                if (letter != null && State.MemoryStars.ContainsKey(memory.Key) && !State.LettersFound.Contains(letter)) return letter;
            }
            return null;
        }

        public void FindLetter(string letterId)
        {
            if (!State.LettersFound.Contains(letterId)) State.LettersFound.Add(letterId);
            Changed?.Invoke();
        }
    }
}
