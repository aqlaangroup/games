using System;
using System.Collections.Generic;
using Gahwit.Core;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Language and birth year, before anything else on the first launch (COMPLETE_SPEC.md section 1, step 1;
    // screens/AgeGate). No year is picked in advance, so the screen doesn't nudge anyone towards an adult age.
    public sealed class AgeGateView : MonoBehaviour
    {
        const int OldestShown = 1930;

        readonly List<Button> languageChips = new List<Button>();
        readonly Dictionary<int, Button> yearRows = new Dictionary<int, Button>();
        Button next;
        string language;
        int? year;
        Action<string, int> done;
        IAccountUi account;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(IAccountUi account)
        {
            this.account = account;
            var card = Sheet.Card(transform, 980, 1640);

            var name = UiKit.Text(card, "Game", Loc.T("age.game"), 56, Palette.Rose, TextAlignmentOptions.Center);
            name.fontStyle = FontStyles.Bold;
            name.rectTransform.Top(44, 80, 40, 40);
            UiKit.Text(card, "Welcome", Loc.T("age.welcome"), 30, Palette.InkSoft, TextAlignmentOptions.Center).rectTransform.Top(128, 48, 40, 40);

            UiKit.Text(card, "LanguageLabel", Loc.T("age.language"), 32, Palette.Ink).rectTransform.Top(200, 48, 56, 56);
            var languages = UiKit.Rect(card, "Languages").Top(254, UiKit.MinTouch, 56, 56);
            UiKit.Row(languages.gameObject, 20).childForceExpandWidth = true;
            foreach (var code in SettingsView.Languages)
            {
                var chip = UiKit.Button(languages, "Language " + code, Loc.T("language." + code), Color.white, Palette.Ink, 32, () => PickLanguage(code), 30);
                chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                chip.gameObject.AddComponent<Outline>();
                chip.name = code;
                languageChips.Add(chip);
            }
            UiKit.Text(card, "LanguageNote", Loc.T("age.languageNote"), 24, Palette.InkSoft).rectTransform.Top(384, 40, 56, 56);

            UiKit.Text(card, "YearLabel", Loc.T("age.birthYear"), 32, Palette.Ink).rectTransform.Top(440, 48, 56, 56);
            var years = Sheet.Column(card, 496, 600, 10);
            years.parent.GetComponent<Image>().color = Palette.Muted;
            years.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 16, 16);
            for (int y = DateTime.UtcNow.Year; y >= OldestShown; y--)
            {
                int pick = y;
                var row = UiKit.Button(years, "Year " + y, y.ToString(), Color.white, Palette.Ink, 32, () => PickYear(pick), 24);
                Sheet.Sized(row, UiKit.MinTouch);
                row.gameObject.AddComponent<Outline>();
                yearRows[y] = row;
            }

            var why = Sheet.Paragraph(card, Loc.T("age.why"), 24, Palette.InkSoft);
            why.rectTransform.Bottom(440, 120, 56, 56);
            UiKit.Text(card, "NoChat", Loc.T("age.noChat"), 24, Palette.InkSoft).rectTransform.Bottom(340, 90, 56, 56);

            next = UiKit.Button(card, "Continue", Loc.T("age.continue"), Palette.Rose, Color.white, 34, Continue, 40);
            ((RectTransform)next.transform).Bottom(170, UiKit.MinTouch, 56, 56);

            var legal = UiKit.Rect(card, "Legal").Bottom(26, UiKit.MinTouch, 56, 56);
            UiKit.Row(legal.gameObject, 12);
            Sheet.Sized(UiKit.Text(legal, "Agree", Loc.T("age.agree"), 24, Palette.InkSoft, TextAlignmentOptions.Center), 60)
                .GetComponent<LayoutElement>().preferredWidth = 330;
            LegalLink(legal, Loc.T("legal.terms"), () => this.account.OpenTerms());
            LegalLink(legal, Loc.T("legal.privacy"), () => this.account.OpenPrivacy());

            gameObject.SetActive(false);
        }

        static void LegalLink(RectTransform row, string label, UnityEngine.Events.UnityAction open)
        {
            var link = UiKit.Button(row, "Link", label, Color.clear, Palette.Teal, 24, open, 0);
            link.Label().fontStyle = FontStyles.Underline;
            var size = link.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 220;
            size.preferredHeight = UiKit.MinTouch;
        }

        public void Show(string currentLanguage, Action<string, int> onDone)
        {
            done = onDone;
            year = null;
            PickLanguage(currentLanguage);
            Restyle();
            gameObject.SetActive(true);
        }

        void PickLanguage(string code)
        {
            language = code;
            Restyle();
        }

        void PickYear(int picked)
        {
            year = picked;
            Restyle();
        }

        // The pick shows as a thick gold ring and bold text, not by colour alone.
        void Restyle()
        {
            foreach (var chip in languageChips) Mark(chip, chip.name == language);
            foreach (var pair in yearRows) Mark(pair.Value, pair.Key == year);
            next.interactable = year != null;
        }

        static void Mark(Button button, bool picked)
        {
            var outline = button.GetComponent<Outline>();
            outline.effectColor = picked ? Palette.Gold : new Color(0.23f, 0.15f, 0.19f, 0.18f);
            outline.effectDistance = picked ? new Vector2(5, -5) : new Vector2(1.5f, -1.5f);
            button.Label().fontStyle = picked ? FontStyles.Bold : FontStyles.Normal;
        }

        void Continue()
        {
            if (year == null) return;
            gameObject.SetActive(false);
            done?.Invoke(language, year.Value);
        }
    }
}
