using System;
using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.Story;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Teta's letter line by line, then her voice note with subtitles, then a message that arrives on the phone.
    // Matches the feeling demo (feel-demo/index.html). The voice note plays its recording when there is one,
    // with the subtitles timed to it; without one, the subtitles carry it alone.
    public sealed class LetterView : MonoBehaviour
    {
        const float LineEvery = 1.3f, FirstLineAfter = 0.7f, FadeSeconds = 0.8f;
        const float VoiceSeconds = 9f, AfterVoiceSeconds = 1.6f, NotificationSeconds = 6f;
        const int WaveBars = 28;

        GameObject paper, voice, notification;
        RectTransform lines;
        Button more, play;
        RTLTextMeshPro subtitle, duration, notifyFrom, notifyText;
        readonly List<CanvasGroup> lineGroups = new List<CanvasGroup>();
        readonly List<Image> bars = new List<Image>();
        Image shade;
        AudioSource voicePlayer;
        float voiceSeconds;
        string[] voiceWords;
        float openedAt, playedAt = -1, notifiedAt = -1;
        Action onDone;

        public void Init()
        {
            shade = gameObject.AddComponent<Image>();
            shade.color = new Color(0.12f, 0.08f, 0.1f, 0.62f);
            voicePlayer = gameObject.AddComponent<AudioSource>();
            voicePlayer.playOnAwake = false;

            var paperImage = UiKit.Panel(transform, "Letter", Palette.Hex("#FBF3E4"), 30);
            paperImage.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(940, 1240));
            paper = paperImage.gameObject;
            lines = UiKit.Rect(paperImage.transform, "Lines").Stretch(70, 70, 90, 200);
            var column = lines.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 34;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            more = UiKit.Button(paperImage.transform, "More", Loc.T("letter.more"), Palette.Rose, Color.white, 30, ShowVoice, 34);
            ((RectTransform)more.transform).Bottom(60, 90, 160, 160);

            var voiceImage = UiKit.Panel(transform, "VoiceNote", Palette.Cream, 44);
            voiceImage.rectTransform.Bottom(60, 640, 28, 28);
            voice = voiceImage.gameObject;
            UiKit.Text(voiceImage.transform, "Title", Loc.T("voice.title"), 36, Palette.Ink).rectTransform.Top(36, 52, 48, 48);
            UiKit.Text(voiceImage.transform, "Sub", Loc.T("voice.subtitle"), 24, Palette.InkSoft).rectTransform.Top(92, 36, 48, 48);
            var wave = UiKit.Rect(voiceImage.transform, "Wave").Top(160, 90, 220, 48);
            var waveRow = UiKit.Row(wave.gameObject, 6);
            waveRow.childControlWidth = waveRow.childControlHeight = false;
            for (int k = 0; k < WaveBars; k++)
            {
                var bar = UiKit.Panel(wave, "Bar", Palette.Muted, 4);
                bar.raycastTarget = false;
                bar.rectTransform.sizeDelta = new Vector2(12, 18 + Mathf.Abs(Mathf.Sin(k * 1.7f)) * 56 + k % 3 * 6);
                bars.Add(bar);
            }
            play = UiKit.Button(voiceImage.transform, "Play", Loc.T("voice.play"), Palette.Rose, Color.white, 28, PlayVoice, 60);
            ((RectTransform)play.transform).Pin(new Vector2(0, 1), new Vector2(48, -165), new Vector2(150, 80));
            duration = UiKit.Text(voiceImage.transform, "Duration", "", 24, Palette.InkSoft, TextAlignmentOptions.Left);
            duration.rectTransform.Pin(new Vector2(0, 1), new Vector2(48, -258), new Vector2(150, 36));
            subtitle = UiKit.Text(voiceImage.transform, "Subtitle", "", 34, Palette.Ink);
            subtitle.rectTransform.Stretch(48, 48, 310, 40);

            var note = UiKit.Panel(transform, "Notification", new Color(1f, 1f, 1f, 0.97f), 36);
            note.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(980, 200));
            notification = note.gameObject;
            var app = UiKit.Text(note.transform, "App", Loc.T("notify.app") + "  ·  " + Loc.T("notify.now"), 22, Palette.InkSoft);
            app.rectTransform.Top(18, 32, 36, 36);
            notifyFrom = UiKit.Text(note.transform, "From", "", 30, Palette.Ink);
            notifyFrom.fontStyle = FontStyles.Bold;
            notifyFrom.rectTransform.Top(54, 42, 36, 36);
            notifyText = UiKit.Text(note.transform, "Message", "", 26, Palette.Ink);
            notifyText.rectTransform.Top(98, 90, 36, 36);
            note.gameObject.AddComponent<Button>().onClick.AddListener(Finish);

            gameObject.SetActive(false);
        }

        public bool IsOpen => gameObject.activeSelf;

        public void Play(List<StoryLine> letter, StoryLine voiceLine, AudioClip recording, StoryLine message, string messageFrom, Action done)
        {
            voicePlayer.clip = recording;
            voiceSeconds = recording != null ? recording.length : VoiceSeconds;
            onDone = done;
            gameObject.SetActive(true);
            shade.enabled = true;
            paper.SetActive(true);
            voice.SetActive(false);
            notification.SetActive(false);
            more.gameObject.SetActive(false);
            playedAt = notifiedAt = -1;
            UiSound.Play("letter_unfold");

            UiKit.Clear(lines);
            lineGroups.Clear();
            foreach (var line in letter)
            {
                string style = line.Speaker;
                var text = UiKit.Text(lines, "Line", line.Text, style == "letter_signature" ? 34 : 38,
                    style == "letter_faded" ? new Color(0.48f, 0.4f, 0.44f, 0.6f) : Palette.Ink,
                    style == "letter_signature" ? TextAlignmentOptions.Left : TextAlignmentOptions.Right);
                var group = text.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0;
                lineGroups.Add(group);
            }

            voiceWords = voiceLine.Text.Split(' ');
            notifyFrom.text = messageFrom;
            notifyText.text = message.Text;
            openedAt = Time.unscaledTime;
            foreach (var bar in bars) bar.color = Palette.Muted;
            subtitle.text = "";
        }

        void ShowVoice()
        {
            paper.SetActive(false);
            voice.SetActive(true);
            play.interactable = true;
            duration.text = $"0:{Mathf.CeilToInt(voiceSeconds):00}";
        }

        void PlayVoice()
        {
            if (playedAt >= 0) return;
            playedAt = Time.unscaledTime;
            play.interactable = false;
            if (voicePlayer.clip != null)
            {
                UiSound.Duck(true);
                voicePlayer.Play();
            }
        }

        void Update()
        {
            if (paper.activeSelf)
            {
                float age = Time.unscaledTime - openedAt;
                for (int i = 0; i < lineGroups.Count; i++)
                    lineGroups[i].alpha = Mathf.Clamp01((age - FirstLineAfter - i * LineEvery) / FadeSeconds);
                more.gameObject.SetActive(age >= FirstLineAfter + lineGroups.Count * LineEvery);
            }

            if (voice.activeSelf && playedAt >= 0)
            {
                float progress = Mathf.Clamp01((Time.unscaledTime - playedAt) / voiceSeconds);
                for (int k = 0; k < bars.Count; k++) bars[k].color = (float)k / bars.Count <= progress ? Palette.RoseSoft : Palette.Muted;
                duration.text = $"0:{Mathf.Max(0, Mathf.CeilToInt(voiceSeconds * (1 - progress))):00}";
                int words = Mathf.CeilToInt(progress * voiceWords.Length);
                subtitle.text = string.Join(" ", voiceWords, 0, words);
                if (Time.unscaledTime - playedAt >= voiceSeconds + AfterVoiceSeconds)
                {
                    voice.SetActive(false);
                    UiSound.Duck(false);
                    shade.enabled = false;
                    notification.SetActive(true);
                    notifiedAt = Time.unscaledTime;
                }
            }

            if (notification.activeSelf && Time.unscaledTime - notifiedAt >= NotificationSeconds) Finish();
        }

        void Finish()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            var done = onDone;
            onDone = null;
            done?.Invoke();
        }
    }
}
