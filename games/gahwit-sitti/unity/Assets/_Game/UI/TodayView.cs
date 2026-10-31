using System;
using Gahwit.Core;
using Gahwit.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Today (missing/18): the daily gift, the day's three tasks, the season pass, and the events running now. Events
    // need a connection; offline, a gentle line says so.
    public sealed class TodayView : MonoBehaviour
    {
        Game game;
        ILiveEventsUi events;
        Func<string> today;
        Func<long> now;
        Action claimed;       // asks the platform for the gems just earned
        Action<string> play;  // opens a mini board
        RectTransform list;

        public Action OpenPass; // the season pass (PassView)

        public bool IsOpen => gameObject.activeSelf;

        public void Init(Game game, ILiveEventsUi events, Func<string> today, Func<long> now, Action claimed, Action<string> play)
        {
            this.game = game;
            this.events = events;
            this.today = today;
            this.now = now;
            this.claimed = claimed;
            this.play = play;
            var card = Sheet.Card(transform, 980, 1640);
            Sheet.Title(card, Loc.T("today.title"), Close);
            list = Sheet.Column(card, 150, 40, 14);
            game.Changed += () => { if (IsOpen) Build(); };
            events.Changed += () => { if (IsOpen) Build(); };
            gameObject.SetActive(false);
        }

        public void Show()
        {
            UiSound.Play("ui_panel_open");
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Build();
        }

        void Close()
        {
            UiSound.Play("ui_panel_close");
            gameObject.SetActive(false);
        }

        void Build()
        {
            UiKit.Clear(list);
            string day = today();

            Section(Loc.T("today.gift"));
            if (!game.GiftOpen) Line(Loc.T("today.giftLater", game.Config.Unlocks.Level("daily_gift")));
            else if (game.GiftDue(day))
                Row(Loc.T("today.giftDay", game.GiftDay), Describe(game.GiftToday), Loc.T("today.take"), true, () =>
                {
                    game.ClaimGift(day);
                    claimed();
                });
            else Line(Loc.T("today.giftTomorrow", game.GiftDay));

            Section(Loc.T("today.tasks"));
            foreach (var task in game.Tasks(day))
            {
                var def = game.TaskDef(task.Id);
                if (def == null) continue;
                string id = task.Id;
                bool done = game.TaskDone(task);
                Row(Loc.T("task." + Game.TaskKind(id), def.Target), $"{task.Progress}/{def.Target} · " + Loc.T("today.taskReward", game.Config.DailyTasks.RewardEach.Gems),
                    task.Claimed ? Loc.T("today.claimed") : Loc.T("today.claim"), done && !task.Claimed, () =>
                    {
                        if (game.ClaimTask(id, day)) claimed();
                    });
            }

            Section(Loc.T("pass.title"));
            if (!game.PassOpen) Line(Loc.T("today.passLater", game.Config.Unlocks.Level("season_pass")));
            else Row(Loc.T("pass.title"), Loc.T("today.passDetail", game.PassTier, game.Config.Pass.Tiers, game.PassToClaim, game.PassDaysLeft),
                Loc.T("today.open"), OpenPass != null, () => OpenPass?.Invoke());

            Section(Loc.T("today.events"));
            if (events.Offline) Line(Loc.T("today.offline"));
            else if (events.Running.Count == 0) Line(Loc.T("today.noEvents"));
            foreach (var running in events.Running)
            {
                string left = Left(running.EndsMs);
                var info = running;
                if (running.Template == "miniBoard")
                    Row(running.Settings.Value<string>(game.English ? "nameEn" : "name") ?? Loc.T("event.miniBoard"), Loc.T("today.left", left),
                        Loc.T("today.play"), true, () => play(info.Id));
                else if (running.Template == "frenzy")
                    Row(Loc.T("event.frenzy"), Loc.T("event.frenzyText", game.CoinMultiplier) + " · " + Loc.T("today.left", left), null, false, null);
            }
        }

        // What a day's gift holds: "+20 energy · 50 coins".
        public static string Describe(DailyGiftDef gift)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (gift.Energy > 0) parts.Add(Loc.T("shop.energy", gift.Energy));
            if (gift.Coins > 0) parts.Add(Loc.T("today.coins", gift.Coins));
            if (gift.Gems > 0) parts.Add(Loc.T("shop.gems", gift.Gems));
            if (!string.IsNullOrEmpty(gift.Decor)) parts.Add(Loc.T("today.decor"));
            return string.Join(" · ", parts);
        }

        string Left(long endsMs)
        {
            long minutes = Math.Max(0, (endsMs - now()) / 60000);
            return minutes >= 1440 ? Loc.T("today.days", minutes / 1440) : $"{minutes / 60}:{minutes % 60:00}";
        }

        void Section(string title)
        {
            var label = UiKit.Text(list, "Section", title, 28, Palette.InkSoft);
            label.fontStyle = FontStyles.Bold;
            Sheet.Sized(label, 56);
        }

        void Line(string text) => Sheet.Sized(UiKit.Text(list, "Line", text, 26, Palette.InkSoft), 70);

        void Row(string name, string detail, string action, bool enabled, Action act)
        {
            var row = UiKit.Panel(list, name, Color.white, 28);
            Sheet.Sized(row, 170);
            var title = UiKit.Text(row.transform, "Name", name, 30, Palette.Ink);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.Stretch(action != null ? 280 : 28, 28, 20, 90);
            UiKit.Text(row.transform, "Detail", detail, 24, Palette.InkSoft).rectTransform.Stretch(action != null ? 280 : 28, 28, 82, 16);
            if (action == null) return;
            var button = UiKit.Button(row.transform, "Act", action, enabled ? Palette.Rose : Palette.Muted, enabled ? Color.white : Palette.Ink, 28, () => act?.Invoke(), 32);
            button.interactable = enabled;
            ((RectTransform)button.transform).Pin(new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(230, UiKit.MinTouch));
        }
    }
}
