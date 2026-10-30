using System;
using Gahwit.Core;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // "Keep this phone or the other one?" with the level and chapter of each (COMPLETE_SPEC.md section 9).
    // Also used when an Apple id that already has a café is linked on a phone with its own progress.
    public sealed class CloudChoiceView : MonoBehaviour
    {
        public struct Side
        {
            public int Level;
            public int Chapter;
            public string Device; // null for this phone
        }

        RTLTextMeshPro hereDetails, otherDetails;
        Action<bool> chosen;

        public bool IsOpen => gameObject.activeSelf;

        public void Init()
        {
            var card = Sheet.Card(transform, 960, 1240);
            Sheet.Title(card, Loc.T("cloud.choiceTitle"), null);
            UiKit.Text(card, "Body", Loc.T("cloud.choiceBody"), 28, Palette.InkSoft).rectTransform.Top(116, 96, 48, 48);
            hereDetails = Option(card, 240, Loc.T("cloud.thisPhone"), Loc.T("cloud.keepThis"), Palette.Rose, true);
            otherDetails = Option(card, 666, Loc.T("cloud.otherPhone"), Loc.T("cloud.keepOther"), Palette.SageDark, false);
            gameObject.SetActive(false);
        }

        RTLTextMeshPro Option(RectTransform card, float top, string heading, string action, Color color, bool keepThis)
        {
            var box = UiKit.Panel(card, heading, Color.white, 36);
            box.rectTransform.Top(top, 390, 48, 48);
            var title = UiKit.Text(box.transform, "Heading", heading, 34, Palette.Ink);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.Top(28, 52, 36, 36);
            var details = UiKit.Text(box.transform, "Details", "", 28, Palette.InkSoft);
            details.rectTransform.Top(86, 110, 36, 36);
            var button = UiKit.Button(box.transform, "Choose", action, color, Color.white, 30, () => Choose(keepThis), 36);
            ((RectTransform)button.transform).Bottom(28, 124, 36, 36);
            return details;
        }

        public void Show(Side here, Side other, Action<bool> keepThisPhone)
        {
            hereDetails.text = Loc.T("cloud.progress", here.Level, here.Chapter);
            otherDetails.text = Loc.T("cloud.progress", other.Level, other.Chapter) +
                                (string.IsNullOrEmpty(other.Device) ? "" : "\n" + Loc.T("cloud.device", other.Device));
            chosen = keepThisPhone;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        void Choose(bool keepThis)
        {
            gameObject.SetActive(false);
            chosen?.Invoke(keepThis);
        }
    }
}
