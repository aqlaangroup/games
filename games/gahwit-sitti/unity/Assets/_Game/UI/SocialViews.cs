using System;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using Newtonsoft.Json.Linq;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Friends (missing/20): the player's code, adding a friend by code, gifts, visits, and the ways to hosting, the
    // Sitti wall and postcards. There is no chat. Players under 13 without a parent's consent see a line instead.
    public sealed class FriendsView : MonoBehaviour
    {
        ISocialUi social;
        Game game;
        Toast toast;
        HostingView hosting;
        WallView wall;
        RectTransform list;
        TMP_InputField code;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(ISocialUi social, Game game, Toast toast, HostingView hosting, WallView wall)
        {
            this.social = social;
            this.game = game;
            this.toast = toast;
            this.hosting = hosting;
            this.wall = wall;
            var card = Sheet.Card(transform, 980, 1700);
            Sheet.Title(card, Loc.T("friends.title"), Close);
            list = Sheet.Column(card, 140, 40, 14);
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

        async void Build()
        {
            UiKit.Clear(list);
            if (!social.Allowed)
            {
                Social.Line(list, Loc.T("friends.parent"));
                return;
            }
            var places = UiKit.Rect(list, "Places");
            Sheet.Sized(places, UiKit.MinTouch);
            var row = UiKit.Row(places.gameObject, 12);
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            if (game.State.Level >= game.Config.Unlocks.Level("hosting")) Social.Chip(places, Loc.T("hosting.title"), () => hosting.Show());
            if (game.State.Level >= game.Config.Unlocks.Level("sitti_wall")) Social.Chip(places, Loc.T("wall.title"), () => wall.Show());
            if (game.State.Level >= game.Config.Unlocks.Level("postcards")) Social.Chip(places, Loc.T("postcard.make"), () =>
            {
                gameObject.SetActive(false);
                social.Postcard();
            });

            var reply = await social.Summary();
            if (!IsOpen) return;
            if (!reply.Ok)
            {
                Social.Line(list, Loc.T(reply.Result == ActionResult.Offline ? "friends.offline" : "friends.failed"));
                return;
            }
            var data = reply.Data;
            Social.Section(list, Loc.T("friends.yourCode"));
            var mine = Social.Row(list, 150);
            var codeText = UiKit.Text(mine.transform, "Code", (string)data["friendCode"], 48, Palette.Rose, TextAlignmentOptions.Center);
            codeText.rectTransform.Stretch(280, 28, 20, 20);
            Social.Button(mine.transform, Loc.T("friends.copy"), Palette.Card, () =>
            {
                GUIUtility.systemCopyBuffer = (string)data["friendCode"];
                toast.Show(Loc.T("friends.copied"));
            });

            Social.Section(list, Loc.T("friends.add"));
            var add = Social.Row(list, 150);
            code = Sheet.Input(add.transform, "Code", Loc.T("friends.codeHint"), UiKit.MinTouch, false);
            ((RectTransform)code.transform).Stretch(280, 28, 9, 9);
            Social.Button(add.transform, Loc.T("friends.addButton"), Palette.Rose, async () =>
            {
                var added = await social.AddFriend(code.text.Trim());
                toast.Show(added.Ok ? Loc.T("friends.added") : Loc.T("friends.error." + (added.Error ?? "failed")));
                if (added.Ok) Build();
            });

            int waiting = data.Value<int>("giftsWaiting");
            if (waiting > 0)
            {
                var gifts = Social.Row(list, 150);
                UiKit.Text(gifts.transform, "Gifts", Loc.T("friends.giftsWaiting", waiting), 30, Palette.Ink).rectTransform.Stretch(280, 28, 20, 20);
                Social.Button(gifts.transform, Loc.T("friends.take"), Palette.Rose, async () =>
                {
                    var claimed = await social.ClaimGifts();
                    if (claimed.Ok) toast.Show(Loc.T("friends.gotEnergy", claimed.Data.Value<int>("energy")));
                    Build();
                });
            }

            var friends = (JArray)data["friends"];
            Social.Section(list, Loc.T("friends.list", friends.Count, data.Value<int>("giftsLeftToday")));
            if (friends.Count == 0) Social.Line(list, Loc.T("friends.none"));
            foreach (var friend in friends.OfType<JObject>())
            {
                string id = (string)friend["playerId"];
                var entry = Social.Row(list, 190);
                UiKit.Text(entry.transform, "Name", (string)friend["code"], 32, Palette.Ink).rectTransform.Stretch(420, 28, 20, 100);
                UiKit.Text(entry.transform, "Points", Loc.T("friends.points", friend.Value<int>("points")), 24, Palette.InkSoft)
                    .rectTransform.Stretch(420, 28, 90, 20);
                var buttons = UiKit.Rect(entry.transform, "Buttons").Stretch(20, 600, 28, 28);
                var line = UiKit.Row(buttons.gameObject, 10);
                line.childForceExpandWidth = line.childForceExpandHeight = true;
                bool sent = friend.Value<bool>("giftSentToday");
                Social.Chip(buttons, sent ? Loc.T("friends.giftSent") : Loc.T("friends.gift"), async () =>
                {
                    var gift = await social.Gift(id);
                    toast.Show(gift.Ok ? Loc.T("friends.giftDone") : Loc.T("friends.error." + (gift.Error ?? "failed")));
                    if (gift.Ok) Build();
                }, !sent);
                if (game.State.Level >= game.Config.Unlocks.Level("cafe_visits")) Social.Chip(buttons, Loc.T("friends.visit"), () =>
                {
                    gameObject.SetActive(false);
                    social.Visit(id);
                });
            }
        }
    }

    // Hosting (SYSTEMS_B.md section 7): a big order friends fill together within its time, with preset messages.
    public sealed class HostingView : MonoBehaviour
    {
        public const int Presets = 12;

        ISocialUi social;
        Toast toast;
        Func<long> now;
        RectTransform list;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(ISocialUi social, Toast toast, Func<long> now)
        {
            this.social = social;
            this.toast = toast;
            this.now = now;
            var card = Sheet.Card(transform, 980, 1700);
            Sheet.Title(card, Loc.T("hosting.title"), Close);
            list = Sheet.Column(card, 140, 40, 14);
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

        async void Build()
        {
            UiKit.Clear(list);
            var reply = await social.Hosting();
            var friendsReply = reply.Ok ? await social.Summary() : null;
            if (!IsOpen) return;
            if (!reply.Ok)
            {
                Social.Line(list, Loc.T(reply.Result == ActionResult.Offline ? "friends.offline" : "friends.failed"));
                return;
            }
            Social.Line(list, Loc.T("hosting.how"));
            var mine = reply.Data["mine"] as JObject;
            if (mine != null && (string)mine["status"] == "open") Order(mine, true, friendsReply?.Data?["friends"] as JArray);
            else
            {
                if (mine != null) Social.Line(list, Loc.T((string)mine["status"] == "done" ? "hosting.lastDone" : "hosting.lastExpired"));
                bool canStart = reply.Data.Value<bool>("canStart");
                var start = Social.Row(list, 150);
                UiKit.Text(start.transform, "Text", Loc.T(canStart ? "hosting.startText" : "hosting.waitText"), 26, Palette.Ink).rectTransform.Stretch(280, 28, 20, 20);
                if (canStart) Social.Button(start.transform, Loc.T("hosting.start"), Palette.Rose, async () =>
                {
                    var started = await social.StartHosting();
                    if (!started.Ok) toast.Show(Loc.T("friends.error." + (started.Error ?? "failed")));
                    Build();
                });
            }
            foreach (var helping in ((JArray)reply.Data["helping"]).OfType<JObject>()) Order(helping, false, null);
        }

        void Order(JObject order, bool mine, JArray friends)
        {
            string id = (string)order["id"];
            int needed = order.Value<int>("needed"), progress = order.Value<int>("progress"), bots = order.Value<int>("bots");
            Social.Section(list, Loc.T(mine ? "hosting.yours" : "hosting.friends"));
            var card = Social.Row(list, 230);
            long left = Math.Max(0, ((DateTimeOffset)order.Value<DateTime>("endsAt")).ToUnixTimeMilliseconds() - now()) / 60000;
            UiKit.Text(card.transform, "Progress", Loc.T("hosting.progress", progress, needed), 34, Palette.Ink).rectTransform.Stretch(280, 28, 18, 150);
            UiKit.Text(card.transform, "Left", Loc.T("hosting.left", left / 60, left % 60) + (bots > 0 ? " · " + Loc.T("hosting.bots", bots) : ""),
                24, Palette.InkSoft).rectTransform.Stretch(280, 28, 80, 100);
            var track = UiKit.Panel(card.transform, "Track", Palette.Track, 10);
            track.rectTransform.Stretch(280, 28, 150, 50);
            var fill = UiKit.Panel(track.transform, "Fill", Palette.SageDark, 10);
            fill.SetFill(needed > 0 ? progress / (float)needed : 0);
            Social.Button(card.transform, Loc.T("hosting.give"), Palette.Rose, async () =>
            {
                var given = await social.Give(id);
                toast.Show(given.Ok ? Loc.T("hosting.given") : Loc.T("friends.error." + (given.Error ?? "failed")));
                Build();
            });

            if (mine && friends != null)
            {
                var invited = ((JArray)order["helpers"]).Select(h => (string)h["playerId"]).ToList();
                foreach (var friend in friends.OfType<JObject>().Where(f => !invited.Contains((string)f["playerId"])))
                {
                    var invite = Social.Row(list, 150);
                    UiKit.Text(invite.transform, "Code", (string)friend["code"], 28, Palette.Ink).rectTransform.Stretch(280, 28, 20, 20);
                    string friendId = (string)friend["playerId"];
                    Social.Button(invite.transform, Loc.T("hosting.invite"), Palette.Card, async () =>
                    {
                        var r = await social.Invite(id, friendId);
                        toast.Show(r.Ok ? Loc.T("hosting.invited") : Loc.T("friends.error." + (r.Error ?? "failed")));
                        Build();
                    });
                }
            }

            foreach (var message in ((JArray)order["messages"]).OfType<JObject>().Reverse().Take(3))
                Social.Line(list, "«" + Loc.T("hosting.preset" + message.Value<int>("preset")) + "»");
            var presets = UiKit.Rect(list, "Presets");
            Sheet.Sized(presets, 4 * (UiKit.MinTouch + 10));
            var grid = presets.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(285, UiKit.MinTouch);
            grid.spacing = new Vector2(10, 10);
            for (int n = 1; n <= Presets; n++)
            {
                int preset = n;
                var chip = UiKit.Button(presets, "Preset" + n, Loc.T("hosting.preset" + n), Palette.Cream, Palette.Ink, 22, async () =>
                {
                    var sent = await social.Message(id, preset);
                    if (sent.Ok) Build();
                }, 20);
                chip.Label().enableAutoSizing = true;
                chip.Label().fontSizeMin = UiKit.FontSize(16);
                chip.Label().fontSizeMax = UiKit.FontSize(22);
            }
        }
    }

    // The Sitti wall (SYSTEMS_B.md section 8): grandmothers' sayings and recipes, without names. A person reads every
    // post before anyone sees it. Players can like, report and block, but not reply.
    public sealed class WallView : MonoBehaviour
    {
        ISocialUi social;
        Game game;
        Toast toast;
        RectTransform list;
        TMP_InputField input;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(ISocialUi social, Game game, Toast toast)
        {
            this.social = social;
            this.game = game;
            this.toast = toast;
            var card = Sheet.Card(transform, 980, 1700, Palette.Hex("#FBF3E6"));
            Sheet.Title(card, Loc.T("wall.title"), Close);
            list = Sheet.Column(card, 140, 40, 14);
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

        async void Build()
        {
            UiKit.Clear(list);
            Social.Line(list, Loc.T("wall.how", game.Config.LevelB.SittiWall.MaxChars));
            var compose = Social.Row(list, 300);
            input = Sheet.Input(compose.transform, "Saying", Loc.T("wall.hint"), 200, true);
            ((RectTransform)input.transform).Stretch(28, 28, 20, 80);
            input.characterLimit = game.Config.LevelB.SittiWall.MaxChars;
            var post = UiKit.Button(compose.transform, "Post", Loc.T("wall.post"), Palette.Rose, Color.white, 28, async () =>
            {
                var posted = await social.Post(input.text.Trim());
                toast.Show(posted.Ok ? Loc.T("wall.sent") : Loc.T("wall.error." + (posted.Error ?? "failed")));
                if (posted.Ok) Build();
            }, 32);
            ((RectTransform)post.transform).Pin(new Vector2(0, 0), new Vector2(28, 10), new Vector2(240, UiKit.MinTouch));

            var reply = await social.Wall();
            if (!IsOpen) return;
            if (!reply.Ok)
            {
                Social.Line(list, Loc.T(reply.Result == ActionResult.Offline ? "friends.offline" : "friends.failed"));
                return;
            }
            foreach (var mine in ((JArray)reply.Data["mine"]).OfType<JObject>().Where(m => (string)m["status"] != "approved").Take(3))
                Social.Line(list, Loc.T("wall.yours." + (string)mine["status"]) + ": " + (string)mine["text"]);
            var posts = ((JArray)reply.Data["posts"]).OfType<JObject>().ToList();
            if (posts.Count == 0) Social.Line(list, Loc.T("wall.empty"));
            foreach (var p in posts)
            {
                string id = (string)p["id"];
                var row = Social.Row(list, 250);
                UiKit.Text(row.transform, "Text", (string)p["text"], 30, Palette.Ink).rectTransform.Stretch(28, 28, 20, 120);
                var buttons = UiKit.Rect(row.transform, "Buttons").Bottom(10, UiKit.MinTouch, 28, 28);
                var line = UiKit.Row(buttons.gameObject, 10);
                line.childForceExpandWidth = line.childForceExpandHeight = true;
                bool liked = p.Value<bool>("liked"), own = p.Value<bool>("mine");
                Social.Chip(buttons, (liked ? "♥ " : "♡ ") + p.Value<int>("likes"), async () => { await social.WallAction(id, "like"); Build(); }, !own);
                if (own) continue;
                Social.Chip(buttons, Loc.T("wall.report"), async () =>
                {
                    await social.WallAction(id, "report");
                    toast.Show(Loc.T("wall.reported"));
                    Build();
                });
                Social.Chip(buttons, Loc.T("wall.block"), async () =>
                {
                    await social.WallAction(id, "block");
                    toast.Show(Loc.T("wall.blocked"));
                    Build();
                });
            }
        }
    }

    // Small builders the social screens share.
    static class Social
    {
        public static void Section(RectTransform list, string title)
        {
            var label = UiKit.Text(list, "Section", title, 28, Palette.InkSoft);
            label.fontStyle = FontStyles.Bold;
            Sheet.Sized(label, 56);
        }

        public static void Line(RectTransform list, string text) => Sheet.Sized(UiKit.Text(list, "Line", text, 26, Palette.InkSoft), 80);

        public static Image Row(RectTransform list, float height)
        {
            var row = UiKit.Panel(list, "Row", Color.white, 28);
            Sheet.Sized(row, height);
            return row;
        }

        public static void Button(Transform row, string label, Color color, UnityEngine.Events.UnityAction act)
        {
            var button = UiKit.Button(row, label, label, color, Palette.TextOn(color), 28, act, 32);
            ((RectTransform)button.transform).Pin(new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(230, UiKit.MinTouch));
        }

        public static void Chip(RectTransform row, string label, UnityEngine.Events.UnityAction act, bool enabled = true)
        {
            var chip = UiKit.Button(row, label, label, enabled ? Palette.Cream : Palette.Muted, Palette.Ink, 24, act, 24);
            chip.interactable = enabled;
            chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }
    }
}
