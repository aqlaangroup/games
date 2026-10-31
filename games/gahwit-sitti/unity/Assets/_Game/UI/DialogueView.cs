using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Memories;
using Gahwit.Merge;
using Gahwit.Story;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Story scenes: one line at a time, tap to continue, choices as buttons. Tap-to-explain words are listed under the
    // line, and tapping one in the line shows its meaning. Neutral-Arabic subtitles sit under the dialect line when
    // they're on, and Skip runs the scene to its next choice or its end (missing/11).
    public sealed class DialogueView : MonoBehaviour, IPointerClickHandler
    {
        Game game;
        StoryPlayer player;
        StoryLine shown;
        Action onDone;
        Action<string> onChoice;
        RTLTextMeshPro speaker, line, subtitle, glosses, meaningText;
        Image meaning;
        RectTransform choices;
        Button next, skip;
        bool waitingForChoice;
        ChooseGame timed;            // a timed-choice memory, while one plays
        List<string> choiceTags;
        Image timer;

        public bool ShowGlosses { get; set; } = true; // the tap-to-explain setting

        // Whether the player skipped the scene that just ended (scene_skipped instead of scene_viewed).
        public bool Skipped { get; private set; }

        public void Init(Game game)
        {
            this.game = game;
            gameObject.AddComponent<Image>().color = Palette.Shade;

            var card = UiKit.Panel(transform, "Card", Palette.Cream, 44);
            card.rectTransform.Bottom(40, 640, 28, 28);
            card.gameObject.AddComponent<SpringIn>();
            speaker = UiKit.Text(card.transform, "Speaker", "", 36, Palette.Rose);
            speaker.fontStyle = FontStyles.Bold;
            speaker.rectTransform.Top(28, 50, 40, 40);
            line = UiKit.Text(card.transform, "Line", "", 40, Palette.Ink);
            line.rectTransform.Top(86, 230, 40, 40);
            subtitle = UiKit.Text(card.transform, "Subtitle", "", 28, Palette.InkSoft);
            subtitle.rectTransform.Top(320, 60, 40, 40);
            glosses = UiKit.Text(card.transform, "Glosses", "", 26, Palette.Teal);
            glosses.rectTransform.Top(384, 44, 40, 40);
            next = UiKit.Button(card.transform, "Next", Loc.T("dialogue.next"), Palette.Rose, Color.white, 30, Advance, 32);
            ((RectTransform)next.transform).Pin(Vector2.zero, new Vector2(32, 28), new Vector2(240, UiKit.MinTouch));

            // A tapped word's meaning, over the card until the next line.
            meaning = UiKit.Panel(transform, "Meaning", Palette.Teal, 32);
            meaning.rectTransform.Bottom(700, 150, 60, 60);
            meaningText = UiKit.Text(meaning.transform, "Text", "", 32, Color.white, TextAlignmentOptions.Center);
            meaningText.rectTransform.Stretch(28, 28, 12, 12);
            meaning.gameObject.SetActive(false);

            skip = UiKit.Button(transform, "Skip", Loc.T("dialogue.skip"), Palette.Card, Palette.Ink, 28, Skip, 30);
            ((RectTransform)skip.transform).Pin(new Vector2(1, 1), new Vector2(-32, -150), new Vector2(240, UiKit.MinTouch)); // under the HUD

            // The time left for a timed choice: a bar that shrinks, over the choices.
            timer = UiKit.Panel(transform, "Timer", Palette.Rose, 8);
            timer.rectTransform.Bottom(1160, 16, 80, 80);
            timer.gameObject.SetActive(false);

            choices = UiKit.Rect(transform, "Choices").Bottom(700, 440, 40, 40);
            var column = choices.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 16;
            column.childAlignment = TextAnchor.LowerCenter;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            gameObject.SetActive(false);
        }

        public bool IsOpen => gameObject.activeSelf;

        // A "choose" memory (missing/12): the conversation plays like a scene, but each choice has a timer.
        public void PlayTimed(StoryPlayer story, string knot, ChooseGame game, Action done)
        {
            timed = game;
            game.TimedOut += TimedOut;
            Play(story, knot, () =>
            {
                game.TimedOut -= TimedOut;
                game.End();
                timed = null;
                timer.gameObject.SetActive(false);
                done?.Invoke();
            });
        }

        public void Play(StoryPlayer story, string knot, Action done, Action<string> choiceMade = null)
        {
            player = story;
            onDone = done;
            onChoice = choiceMade;
            Skipped = false;
            gameObject.SetActive(true);
            player.Play(knot);
            Advance();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (waitingForChoice || TapWord(eventData)) return;
            Advance();
        }

        // A tap on a highlighted word shows its meaning instead of moving on. Each word is a link whose id is its place
        // in the line's glosses (StoryPlayer.Parse).
        bool TapWord(PointerEventData eventData)
        {
            if (!ShowGlosses || shown == null || shown.Glosses.Count == 0) return false;
            int link = TMP_TextUtilities.FindIntersectingLink(line, eventData.position, eventData.pressEventCamera);
            if (link < 0 || !int.TryParse(line.textInfo.linkInfo[link].GetLinkID(), out int index) || index >= shown.Glosses.Count)
                return false;
            var gloss = shown.Glosses[index];
            meaningText.text = gloss.Key + ": " + gloss.Value;
            meaning.gameObject.SetActive(true);
            UiSound.Play("ui_tap");
            return true;
        }

        void Skip()
        {
            Skipped = true;
            Advance();
        }

        void Advance()
        {
            UiKit.Clear(choices);
            waitingForChoice = false;
            meaning.gameObject.SetActive(false);
            if (Skipped) while (player.TryNext(out _)) { } // a skipped scene runs to its next choice or its end

            if (player.TryNext(out var storyLine))
            {
                Show(storyLine);
                next.gameObject.SetActive(true);
                skip.gameObject.SetActive(timed == null);
                return;
            }

            if (player.HasChoices)
            {
                waitingForChoice = true;
                next.gameObject.SetActive(false);
                skip.gameObject.SetActive(false);
                if (timed != null)
                {
                    choiceTags = player.ChoiceTags;
                    timed.Ask();
                    timer.gameObject.SetActive(true);
                }
                var options = player.Choices;
                for (int i = 0; i < options.Count; i++)
                {
                    int index = i;
                    var button = UiKit.Button(choices, "Choice", options[i], Palette.Card, Palette.Ink, 32, () => Choose(index), 30);
                    button.gameObject.AddComponent<LayoutElement>().preferredHeight = UiKit.MinTouch;
                }
                return;
            }

            gameObject.SetActive(false);
            var done = onDone;
            onDone = null;
            done?.Invoke();
        }

        void Choose(int index)
        {
            if (timed != null)
            {
                timed.Answer(choiceTags[index] == "good");
                timer.gameObject.SetActive(false);
            }
            string id = player.Choose(index);
            onChoice?.Invoke(id);
            Advance();
        }

        // The time ran out: the talk moves on with a weak answer.
        void TimedOut()
        {
            timer.gameObject.SetActive(false);
            int weak = Math.Max(0, choiceTags.FindIndex(tag => tag != "good"));
            onChoice?.Invoke(player.Choose(weak));
            Advance();
        }

        void Update()
        {
            if (timed == null || !waitingForChoice) return;
            timed.Tick(Time.unscaledDeltaTime);
            if (timed != null && timed.Waiting)
                timer.rectTransform.localScale = new Vector3(Mathf.Clamp01(timed.SecondsLeft / timed.SecondsPerChoice), 1, 1);
        }

        void Show(StoryLine storyLine)
        {
            shown = storyLine;
            bool narrator = storyLine.Speaker == "sys";
            speaker.text = narrator ? "" : Loc.T("char." + storyLine.Speaker);
            speaker.color = game.Content.Characters.TryGetValue(storyLine.Speaker, out var character) ? Palette.Hex(character.Color) : Palette.Rose;
            line.text = storyLine.Text;
            line.color = narrator ? Palette.InkSoft : Palette.Ink;
            subtitle.text = storyLine.Subtitle ?? "";
            glosses.text = ShowGlosses ? string.Join("   ", storyLine.Glosses.Select(g => $"{g.Key}: {g.Value}")) : "";
        }
    }
}
