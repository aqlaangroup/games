using Gahwit.Core;
using TMPro;
using UnityEngine;

namespace Gahwit.UI
{
    // "Update the game": shown over everything when the platform says this version is too old
    // (COMPLETE_SPEC.md section 10). It can't be closed; the game stays as it was underneath.
    public sealed class UpdateRequiredView : MonoBehaviour
    {
        Toast toast;
        string storeUrl;

        public void Init(Toast toast)
        {
            this.toast = toast;
            var card = Sheet.Card(transform, 900, 700);
            Sheet.Title(card, Loc.T("update.title"), null);
            UiKit.Text(card, "Body", Loc.T("update.body"), 30, Palette.Ink).rectTransform.Stretch(48, 48, 140, 220);
            var button = UiKit.Button(card, "Update", Loc.T("update.button"), Palette.Rose, Color.white, 32, Open, 40);
            ((RectTransform)button.transform).Bottom(48, UiKit.MinTouch, 48, 48);
            gameObject.SetActive(false);
        }

        public void Show(string url)
        {
            storeUrl = url;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        void Open()
        {
            if (!string.IsNullOrEmpty(storeUrl) && !storeUrl.StartsWith("[")) Application.OpenURL(storeUrl);
            else toast.Show(Loc.T("legal.soon"));
        }
    }
}
