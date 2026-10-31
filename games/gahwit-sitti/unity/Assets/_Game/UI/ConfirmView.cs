using System;
using Gahwit.Core;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // A yes-or-no question over everything else, such as "delete my account?".
    public sealed class ConfirmView : MonoBehaviour
    {
        RTLTextMeshPro title, body;
        Button yes, no;
        Action onYes;

        public void Init()
        {
            var card = Sheet.Card(transform, 900, 860);
            title = Sheet.Title(card, "", null);
            body = UiKit.Text(card, "Body", "", 30, Palette.Ink);
            body.rectTransform.Stretch(48, 48, 140, 320);
            yes = UiKit.Button(card, "Yes", "", Palette.Hex("#9B2C3F"), Color.white, 32, Yes, 40);
            ((RectTransform)yes.transform).Bottom(184, UiKit.MinTouch, 48, 48);
            no = UiKit.Button(card, "No", "", Palette.Muted, Palette.Ink, 30, () => gameObject.SetActive(false), 40);
            ((RectTransform)no.transform).Bottom(40, UiKit.MinTouch, 48, 48);
            gameObject.SetActive(false);
        }

        public void Show(string heading, string text, string yesLabel, string noLabel, Action confirmed)
        {
            title.text = heading;
            body.text = text;
            yes.Label().text = yesLabel;
            no.Label().text = noLabel;
            onYes = confirmed;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        void Yes()
        {
            gameObject.SetActive(false);
            onYes?.Invoke();
        }
    }
}
