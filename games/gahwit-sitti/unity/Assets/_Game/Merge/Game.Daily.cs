using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;

namespace Gahwit.Merge
{
    // Daily systems (GAME_SYSTEMS.md section 14; missing/18). A day starts at the player's local midnight by trusted
    // time (decided 2026-10-31), passed in as "today" ("2026-10-31"). Gems from the gift and the tasks come through
    // the platform ("daily:<date>", "task:<date>:<id>"), which pays each once; everything else is applied here.
    public sealed partial class Game
    {
        public int CoinMultiplier { get; set; } = 1; // 2 during a double weekend
        public int LastServedCoins { get; private set; }

        // ---------- Daily gift: login days 1-7 ----------

        public bool GiftOpen => State.Level >= Config.Unlocks.Level("daily_gift");
        public bool GiftDue(string today) => GiftOpen && State.LastGiftDate != today;
        public DailyGiftDef GiftToday => Config.DailyLogin[State.LoginDay % Config.DailyLogin.Count];
        public int GiftDay => State.LoginDay % Config.DailyLogin.Count + 1;

        // Applies today's gift (not its gems) once a day; null when it's not due.
        public DailyGiftDef ClaimGift(string today)
        {
            if (!GiftDue(today)) return null;
            var gift = GiftToday;
            State.Energy += gift.Energy;
            State.Coins += gift.Coins;
            if (!string.IsNullOrEmpty(gift.Decor) && !State.Entitlements.Contains("decor:" + gift.Decor)) State.Entitlements.Add("decor:" + gift.Decor);
            State.LoginDay++;
            State.LastGiftDate = today;
            if (gift.Gems > 0) State.PendingRewards.Add("daily:" + today);
            Changed?.Invoke();
            return gift;
        }

        // ---------- Daily tasks ----------

        public IReadOnlyList<DailyTaskState> Tasks(string today)
        {
            RefreshTasks(today);
            return State.DailyTasks;
        }

        public DailyTaskDef TaskDef(string id) => Config.DailyTasks.List.Find(t => t.Id == id);

        // What a task counts: its id without the target ("serve_10" counts "serve").
        public static string TaskKind(string id)
        {
            int cut = id.LastIndexOf('_');
            return cut > 0 && int.TryParse(id.Substring(cut + 1), out _) ? id.Substring(0, cut) : id;
        }

        // A new day picks perDay tasks the player can do at their level, the same ones for the whole day.
        void RefreshTasks(string today)
        {
            if (State.TasksDate == today) return;
            var open = Config.DailyTasks.List.Where(t => t.FromLevel <= State.Level).ToList();
            uint seed = 2166136261;
            foreach (char c in today) seed = (seed ^ c) * 16777619;
            var picker = new Random((int)seed);
            State.DailyTasks = open.OrderBy(_ => picker.Next()).Take(Config.DailyTasks.PerDay)
                .Select(t => new DailyTaskState { Id = t.Id }).ToList();
            State.TasksDate = today;
        }

        // Called where play happens: serving, merging, tapping, selling, and by decor and the wardrobe.
        public void Progress(string kind, int amount)
        {
            foreach (var task in State.DailyTasks)
            {
                if (task.Claimed || TaskKind(task.Id) != kind) continue;
                var def = TaskDef(task.Id);
                if (def != null) task.Progress = Math.Min(def.Target, task.Progress + amount);
            }
        }

        public bool TaskDone(DailyTaskState task) => TaskDef(task.Id) is DailyTaskDef def && task.Progress >= def.Target;

        // Marks a finished task claimed and adds its pass points; its gems come from the platform.
        public bool ClaimTask(string id, string today)
        {
            RefreshTasks(today);
            var task = State.DailyTasks.Find(t => t.Id == id);
            if (task == null || task.Claimed || !TaskDone(task)) return false;
            task.Claimed = true;
            State.PassPoints += Config.DailyTasks.RewardEach.PassPoints;
            State.PendingRewards.Add($"task:{today}:{id}");
            Changed?.Invoke();
            return true;
        }
    }
}
