using System;
using System.Collections.Generic;
using Gahwit.Core;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // One card for the game's short moments: level up, chapter complete, out of energy, a bubble, the free-space
    // offer and unlock cards. A title, an optional big line, a body and up to three buttons; a petal burst for good
    // news. Only one shows at a time; the next waits.
    public sealed class PromptView : MonoBehaviour
    {
        const int Petals = 18, CountSteps = 16;
        const float PetalSeconds = 1.4f, CountSeconds = 0.8f;

        public struct Choice
        {
            public string Label;
            public Color Color;
            public Action Act;
            public bool Enabled;
            public bool KeepOpen; // the action closes the card itself, or it stays (a purchase waiting on the platform)
        }

        readonly Queue<Action> waiting = new Queue<Action>();
        readonly List<Image> petals = new List<Image>();
        readonly Vector2[] petalDirections = new Vector2[Petals];
        RTLTextMeshPro title, big, body;
        RectTransform buttons, card;
        float burstAt = -10, countAt = -10;
        int countShown;
        Func<float, string> countUp;

        public bool IsOpen => gameObject.activeSelf;

        public void Init()
        {
            card = Sheet.Card(transform, 920, 1040);
            title = Sheet.Title(card, "", null);
            big = UiKit.Text(card, "Big", "", 96, Palette.Rose, TextAlignmentOptions.Center);
            big.fontStyle = FontStyles.Bold;
            big.rectTransform.Top(130, 140, 48, 48);
            body = UiKit.Text(card, "Body", "", 30, Palette.Ink, TextAlignmentOptions.Center);
            body.rectTransform.Stretch(56, 56, 280, 440);
            buttons = UiKit.Rect(card, "Buttons").Bottom(40, 430, 56, 56);
            var column = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 14;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.LowerCenter;
            for (int i = 0; i < Petals; i++)
            {
                var petal = UiKit.Icon(transform, "Petal", UiSprites.Circle, i % 2 == 0 ? Palette.RoseSoft : Palette.Lilac, 26);
                petals.Add(petal);
                float angle = i * Mathf.PI * 2f / Petals;
                petalDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (420 + 90 * (i % 3));
                petal.gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        // Shows now, or after the card on screen closes. countUp, when given, writes the body for a fraction from 0 to 1,
        // so rewards count up to their totals.
        public void Show(string heading, string bigLine, string text, IList<Choice> choices, bool celebrate = false,
            Func<float, string> countUp = null)
        {
            void Open()
            {
                title.text = heading;
                big.text = bigLine ?? "";
                body.rectTransform.offsetMax = new Vector2(-56, string.IsNullOrEmpty(bigLine) ? -140 : -280);
                body.text = text ?? "";
                UiKit.Clear(buttons);
                foreach (var choice in choices)
                {
                    var c = choice;
                    var button = UiKit.Button(buttons, c.Label, c.Label, c.Color, Palette.TextOn(c.Color), 30, () =>
                    {
                        if (!c.KeepOpen) Close();
                        c.Act?.Invoke();
                    }, 40);
                    button.interactable = c.Enabled;
                    Sheet.Sized(button, UiKit.MinTouch);
                }
                if (celebrate && !Tweens.Reduced) burstAt = Time.unscaledTime;
                this.countUp = Tweens.Reduced ? null : countUp;
                countAt = Time.unscaledTime;
                countShown = -1;
                transform.SetAsLastSibling();
                gameObject.SetActive(true);
                UiSound.Play("ui_panel_open");
            }
            if (IsOpen) waiting.Enqueue(Open);
            else Open();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            if (waiting.Count > 0) waiting.Dequeue()();
        }

        public static Choice Primary(string label, Action act, bool enabled = true, bool keepOpen = false) =>
            new Choice { Label = label, Color = Palette.Rose, Act = act, Enabled = enabled, KeepOpen = keepOpen };

        public static Choice Quiet(string label, Action act = null) =>
            new Choice { Label = label, Color = Palette.Muted, Act = act, Enabled = true };

        void Update()
        {
            if (countUp != null)
            {
                // A new string only when the shown step changes: sixteen in all.
                int step = Mathf.Min(CountSteps, Mathf.FloorToInt((Time.unscaledTime - countAt) / CountSeconds * CountSteps));
                if (step != countShown)
                {
                    countShown = step;
                    body.text = countUp(step / (float)CountSteps);
                }
                if (step >= CountSteps) countUp = null;
            }

            float t = (Time.unscaledTime - burstAt) / PetalSeconds;
            bool show = t >= 0 && t < 1;
            for (int i = 0; i < Petals; i++)
            {
                var petal = petals[i];
                if (petal.gameObject.activeSelf != show) petal.gameObject.SetActive(show);
                if (!show) continue;
                float eased = 1 - (1 - t) * (1 - t);
                petal.rectTransform.anchoredPosition = petalDirections[i] * eased + new Vector2(0, 200 - 260 * t * t);
                petal.color = new Color(petal.color.r, petal.color.g, petal.color.b, 1 - t);
            }
        }
    }
}
