using Gahwit.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Help and contact (COMPLETE_SPEC.md section 16): a short FAQ, then a form. The platform adds the player id,
    // game version and phone model by itself, so the player only writes the message.
    public sealed class HelpView : MonoBehaviour
    {
        public const int Questions = 4;

        IAccountUi account;
        Toast toast;
        TMP_InputField message, email;
        Button send;
        GameObject offline;

        public void Init(IAccountUi account, Toast toast)
        {
            this.account = account;
            this.toast = toast;
            var card = Sheet.Card(transform, 980, 1720);
            Sheet.Title(card, Loc.T("help.title"), () =>
            {
                UiSound.Play("ui_panel_close");
                gameObject.SetActive(false);
            });

            var column = Sheet.Column(card, 130, 40, 18);
            for (int n = 1; n <= Questions; n++)
            {
                var question = Sheet.Paragraph(column, Loc.T($"help.q{n}"), 30, Palette.Ink);
                question.fontStyle = FontStyles.Bold;
                Sheet.Paragraph(column, Loc.T($"help.a{n}"), 26, Palette.InkSoft);
            }

            var contact = Sheet.Paragraph(column, Loc.T("help.contact"), 32, Palette.Rose);
            contact.fontStyle = FontStyles.Bold;
            // A small line, only while the platform can't be reached (COMPLETE_SPEC.md section 10).
            offline = Sheet.Paragraph(column, Loc.T("help.offlineBanner"), 24, Palette.InkSoft).gameObject;
            message = Sheet.Input(column, "Message", Loc.T("help.messageHint"), 260, true);
            email = Sheet.Input(column, "Email", Loc.T("help.emailHint"), 124, false);
            email.contentType = TMP_InputField.ContentType.EmailAddress;
            Sheet.Paragraph(column, Loc.T("help.sentWith"), 22, Palette.InkSoft);
            send = UiKit.Button(column, "Send", Loc.T("help.send"), Palette.Rose, Color.white, 32, Send, 40);
            Sheet.Sized(send, 124);

            gameObject.SetActive(false);
        }

        public void Show()
        {
            offline.SetActive(account.Cloud == CloudStatus.Offline);
            UiSound.Play("ui_panel_open");
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        async void Send()
        {
            if (string.IsNullOrWhiteSpace(message.text))
            {
                toast.Show(Loc.T("help.empty"));
                return;
            }
            send.interactable = false;
            var result = await account.SendSupport(message.text.Trim(), email.text);
            send.interactable = true;
            toast.Show(Loc.T("help." + result.ToString().ToLowerInvariant()));
            if (result != ActionResult.Done) return;
            message.text = email.text = "";
            gameObject.SetActive(false);
        }
    }
}
