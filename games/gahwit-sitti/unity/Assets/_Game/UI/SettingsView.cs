using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Settings (COMPLETE_SPEC.md section 4; screens/Settings): the cloud save line, the switches, and the
    // account and help rows. Switches show their state by the knob's side as well as the colour.
    public sealed class SettingsView : MonoBehaviour
    {
        public static readonly string[] Switches = { "sound", "music", "haptics", "notifications", "glossary", "voice" };
        public static readonly string[] WithSubtitle = { "notifications", "glossary", "voice" };
        public static readonly string[] Languages = { "ar", "en" };

        readonly Dictionary<string, (Image track, RectTransform knob)> switches = new Dictionary<string, (Image, RectTransform)>();
        readonly List<Button> languageChips = new List<Button>();
        Game game;
        IAccountUi account;
        Toast toast;
        HelpView help;
        ConfirmView confirm;
        RTLTextMeshPro cafe, cloud, linkValue;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(Game game, IAccountUi account, Toast toast, HelpView help, ConfirmView confirm)
        {
            this.game = game;
            this.account = account;
            this.toast = toast;
            this.help = help;
            this.confirm = confirm;

            var card = Sheet.Card(transform, 980, 1720);
            Sheet.Title(card, Loc.T("settings.title"), Close);

            var avatar = UiKit.Icon(card, "Avatar", UiSprites.Circle, Palette.RoseSoft, 112);
            avatar.rectTransform.Pin(new Vector2(1, 1), new Vector2(-48, -128), new Vector2(112, 112));
            UiKit.Text(avatar.transform, "Letter", Loc.T("settings.avatarLetter"), 52, Color.white, TextAlignmentOptions.Center).rectTransform.Stretch();
            cafe = UiKit.Text(card, "Cafe", "", 32, Palette.Ink);
            cafe.fontStyle = FontStyles.Bold;
            cafe.rectTransform.Top(136, 48, 48, 184);
            cloud = UiKit.Text(card, "Cloud", "", 26, Palette.SageDark);
            cloud.rectTransform.Top(186, 40, 48, 184);

            var column = Sheet.Column(card, 268, 110, 14);
            Section(column, Loc.T("settings.play"));
            foreach (var id in Switches) SwitchRow(column, id);

            Section(column, Loc.T("settings.accountHelp"));
            LanguageRow(column);
            linkValue = LinkRow(column, Loc.T("settings.link"), Palette.Ink, LinkAccount);
            LinkRow(column, Loc.T("settings.help"), Palette.Ink, help.Show);
            LegalRow(column);
            LinkRow(column, Loc.T("settings.delete"), Palette.Hex("#9B2C3F"), AskDelete);

            UiKit.Text(card, "Version", Loc.T("settings.version", Application.version), 22, Palette.InkSoft, TextAlignmentOptions.Center)
                .rectTransform.Bottom(36, 40, 48, 48);

            account.Changed += Refresh;
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (account != null) account.Changed -= Refresh;
        }

        public void Show()
        {
            UiSound.Play("ui_panel_open");
            gameObject.SetActive(true);
            Refresh();
        }

        void Close()
        {
            UiSound.Play("ui_panel_close");
            gameObject.SetActive(false);
        }

        void Refresh()
        {
            if (!IsOpen) return;
            cafe.text = Loc.T("settings.cafe", game.State.Level);
            cloud.text = Loc.T("cloud." + account.Cloud.ToString().ToLowerInvariant());
            cloud.color = account.Cloud == CloudStatus.Saved ? Palette.SageDark : Palette.InkSoft;
            linkValue.text = account.LinkedProvider != null ? Loc.T("settings.linked." + account.LinkedProvider) : Loc.T("settings.linkHint");
            foreach (var pair in switches) Paint(pair.Value, account.GetSetting(pair.Key));
            foreach (var chip in languageChips)
            {
                bool current = chip.name == account.Language;
                chip.Label().fontStyle = current ? FontStyles.Bold : FontStyles.Normal;
                chip.GetComponent<Outline>().effectDistance = current ? new Vector2(4, -4) : new Vector2(1, -1);
            }
        }

        static void Section(RectTransform column, string title)
        {
            var label = UiKit.Text(column, "Section", title, 26, Palette.InkSoft);
            label.fontStyle = FontStyles.Bold;
            Sheet.Sized(label, 56);
        }

        static Image Row(RectTransform column, string name, float height)
        {
            var row = UiKit.Panel(column, name, Color.white, 28);
            Sheet.Sized(row, height);
            return row;
        }

        void SwitchRow(RectTransform column, string id)
        {
            var row = Row(column, "Switch " + id, 124);
            bool explained = WithSubtitle.Contains(id);
            var name = UiKit.Text(row.transform, "Name", Loc.T($"settings.{id}"), 30, Palette.Ink);
            name.rectTransform.Stretch(180, 28, explained ? 18 : 38, explained ? 62 : 38);
            if (explained)
                UiKit.Text(row.transform, "Sub", Loc.T($"settings.{id}.sub"), 22, Palette.InkSoft).rectTransform.Stretch(180, 28, 64, 14);

            var track = UiKit.Panel(row.transform, "Track", Palette.Muted, 30);
            track.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(110, 60));
            var knob = UiKit.Icon(track.transform, "Knob", UiSprites.Circle, Color.white, 50).rectTransform;
            switches[id] = (track, knob);

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            button.onClick.AddListener(() =>
            {
                account.SetSetting(id, !account.GetSetting(id));
                Refresh();
            });
        }

        // On: teal with the knob to the left (the end side in right-to-left). Off: beige with the knob to the right.
        static void Paint((Image track, RectTransform knob) toggle, bool on)
        {
            toggle.track.color = on ? Palette.Teal : Palette.Hex("#D8CBBE");
            toggle.knob.Pin(new Vector2(on ? 0 : 1, 0.5f), new Vector2(on ? 5 : -5, 0), new Vector2(50, 50));
        }

        RTLTextMeshPro LinkRow(RectTransform column, string label, Color color, UnityEngine.Events.UnityAction open)
        {
            var row = Row(column, "Link " + label, 124);
            UiKit.Text(row.transform, "Name", label, 30, color).rectTransform.Stretch(330, 28, 20, 20);
            var value = UiKit.Text(row.transform, "Value", "", 24, Palette.InkSoft, TextAlignmentOptions.Left);
            value.rectTransform.Stretch(28, 330, 20, 20);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            button.onClick.AddListener(open);
            return value;
        }

        void LanguageRow(RectTransform column)
        {
            var row = Row(column, "Language", 140);
            UiKit.Text(row.transform, "Name", Loc.T("settings.language"), 30, Palette.Ink).rectTransform.Stretch(420, 28, 20, 20);
            var chips = UiKit.Rect(row.transform, "Chips").Stretch(10, 600, 10, 10);
            UiKit.Row(chips.gameObject, 12).childForceExpandWidth = true;
            foreach (var code in Languages)
            {
                var chip = UiKit.Button(chips, code, Loc.T("language." + code), Palette.Cream, Palette.Ink, 26, () =>
                {
                    if (code != account.Language) account.SetLanguage(code);
                }, 24);
                chip.name = code;
                chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                chip.gameObject.AddComponent<Outline>().effectColor = Palette.Gold;
                languageChips.Add(chip);
            }
        }

        void LegalRow(RectTransform column)
        {
            var row = Row(column, "Legal", 140);
            UiKit.Text(row.transform, "Name", Loc.T("settings.legal"), 30, Palette.Ink).rectTransform.Stretch(420, 28, 20, 20);
            var chips = UiKit.Rect(row.transform, "Chips").Stretch(10, 560, 10, 10);
            UiKit.Row(chips.gameObject, 12).childForceExpandWidth = true;
            foreach (var (label, open) in new (string, UnityEngine.Events.UnityAction)[]
                     { (Loc.T("legal.privacy"), account.OpenPrivacy), (Loc.T("legal.terms"), account.OpenTerms) })
            {
                var chip = UiKit.Button(chips, label, label, Palette.Cream, Palette.Teal, 24, open, 24);
                chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
        }

        async void LinkAccount()
        {
            if (account.LinkedProvider != null)
            {
                toast.Show(Loc.T("settings.linked." + account.LinkedProvider));
                return;
            }
            var result = await account.LinkApple();
            if (result != ActionResult.Cancelled) toast.Show(Loc.T("link." + result.ToString().ToLowerInvariant()));
            Refresh();
        }

        void AskDelete()
        {
            confirm.Show(Loc.T("delete.title"), Loc.T("delete.body"), Loc.T("delete.confirm"), Loc.T("common.cancel"), async () =>
            {
                var result = await account.DeleteAccount();
                toast.Show(Loc.T("delete." + result.ToString().ToLowerInvariant()));
            });
        }
    }
}
