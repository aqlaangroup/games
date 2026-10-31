using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;

namespace Gahwit.Notifications
{
    public sealed class PlannedNotification
    {
        public string Type;     // energy_full, regular, task, away
        public DateTime At;     // local time
        public string TextKey;  // UI table key
        public string Arg;      // a name for the text, or null
    }

    // What to schedule when the app goes to the background (COMPLETE_SPEC.md section 3). Nothing before the first
    // memory; nothing in the quiet hours (moved to their end); at most a few a day; each type has its own switch.
    public static class NotificationPlan
    {
        public static List<PlannedNotification> For(Game game, NotificationSwitches on, long nowMs, DateTime nowLocal)
        {
            var plan = new List<PlannedNotification>();
            var state = game.State;
            var config = game.Config.Notifications;
            if (state.MemoryStars.Count == 0) return plan;

            if (on.EnergyFull && state.Energy < game.EnergyCap)
                plan.Add(Note("energy_full", nowLocal.AddSeconds(game.SecondsToFullEnergy(nowMs)), "notify.energyFull", null));

            if (on.Task && game.CanAffordTask)
                plan.Add(Note("task", nowLocal.AddHours(config.TaskReminderHours), "notify.task", $"task.{game.CurrentTask.Id}.title"));

            if (on.Regular)
                foreach (var regular in game.Config.LevelB.Regulars.Schedule)
                {
                    if (!game.Content.Characters.TryGetValue(regular.Key, out var character) || !character.PlacesOrders || character.FromChapter > state.Chapter) continue;
                    var at = NextVisit(regular.Value, game.Config.LevelB.Cafe.TimeOfDayStartHours, nowLocal);
                    if (at != null) plan.Add(Note("regular", at.Value, character.Plural ? "notify.regularP" : character.Female ? "notify.regularF" : "notify.regular", "char." + regular.Key));
                }

            if (on.Away) plan.Add(Note("away", nowLocal.AddDays(config.AwayDays), "notify.away", null));

            // Quiet hours, then the daily cap: the earliest ones win.
            foreach (var note in plan) note.At = OutOfQuietHours(note.At, config.QuietHoursStart, config.QuietHoursEnd);
            return plan.OrderBy(n => n.At)
                .GroupBy(n => n.At.Date)
                .SelectMany(day => day.Take(config.MaxPerDay))
                .ToList();
        }

        static PlannedNotification Note(string type, DateTime at, string key, string arg) =>
            new PlannedNotification { Type = type, At = at, TextKey = key, Arg = arg };

        // The next start of one of the regular's times of day, after now (not while they could be in already).
        static DateTime? NextVisit(List<string> times, Dictionary<string, int> startHours, DateTime nowLocal)
        {
            DateTime? best = null;
            foreach (var time in times)
            {
                if (!startHours.TryGetValue(time, out int hour)) continue;
                var at = nowLocal.Date.AddHours(hour);
                if (at <= nowLocal) at = at.AddDays(1);
                if (best == null || at < best) best = at;
            }
            return best;
        }

        // A time inside the quiet hours moves to when they end (23:00 to 08:00 by default).
        public static DateTime OutOfQuietHours(DateTime at, int quietStart, int quietEnd)
        {
            if (at.Hour >= quietStart) return at.Date.AddDays(1).AddHours(quietEnd);
            if (at.Hour < quietEnd) return at.Date.AddHours(quietEnd);
            return at;
        }
    }
}
