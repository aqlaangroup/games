using System;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Notifications;
using Unity.Notifications;
using UnityEngine;

namespace Gahwit.Boot
{
    // Local notifications (COMPLETE_SPEC.md section 3) through Unity's Mobile Notifications package. The plan is
    // NotificationPlan's; this only asks permission once (after the first memory) and hands the plan to the phone.
    public sealed class NotificationService
    {
        const string AskedKey = "gahwit.notifications.asked";

        bool started;
        NotificationsPermissionRequest permission;

        static bool OnPhone => Application.isMobilePlatform && !Application.isEditor;

        void Start()
        {
            if (started || !OnPhone) return;
            started = true;
            var args = NotificationCenterArgs.Default;
            args.AndroidChannelId = "gahwit_cafe";
            args.AndroidChannelName = Loc.T("notify.channel");
            args.AndroidChannelDescription = Loc.T("notify.channelText");
            NotificationCenter.Initialize(args);
            // Once asked, asking again only reads the answer; it doesn't show the prompt again.
            if (PlayerPrefs.GetInt(AskedKey, 0) == 1) permission = NotificationCenter.RequestPermission();
        }

        // Asks the first time a memory has been played, never at first launch.
        public void AskOnce()
        {
            Start();
            if (!OnPhone || PlayerPrefs.GetInt(AskedKey, 0) == 1) return;
            PlayerPrefs.SetInt(AskedKey, 1);
            PlayerPrefs.Save();
            permission = NotificationCenter.RequestPermission();
        }

        // When the app goes to the background: the plan replaces whatever was scheduled.
        public void Schedule(Game game, NotificationSwitches switches, long nowMs)
        {
            Start();
            if (!OnPhone || permission == null || permission.Status != NotificationsPermissionStatus.Granted) return;
            NotificationCenter.CancelAllScheduledNotifications();
            foreach (var note in NotificationPlan.For(game, switches, nowMs, DateTime.Now))
            {
                var notification = new Notification
                {
                    Title = Loc.T("notify.title"),
                    Text = note.Arg != null ? Loc.T(note.TextKey, Loc.T(note.Arg)) : Loc.T(note.TextKey)
                };
                NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(note.At));
            }
        }

        // Back in the game: nothing waits, and nothing old stays in the tray.
        public void Clear()
        {
            Start();
            if (!OnPhone) return;
            NotificationCenter.CancelAllScheduledNotifications();
            NotificationCenter.CancelAllDeliveredNotifications();
            NotificationCenter.ClearBadge();
        }
    }
}
