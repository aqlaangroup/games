using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Tutorial;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Draws the hands-on tutorial steps (screens/Tutorial): a banner with "step N of 5" and the instruction, a ring
    // on the thing to touch, and a finger that shows the gesture. It never blocks input; only the skip button
    // takes taps. The rules are in Tutorial.TutorialFlow.
    public sealed class TutorialView : MonoBehaviour
    {
        const float FingerSeconds = 1.2f;

        TutorialFlow flow;
        GameUi ui;
        Game game;
        RectTransform root, banner;
        RTLTextMeshPro counter, text;
        Button skip;
        Image ring, finger;
        TutorialStep shownStep;
        int shownCoins = -1;

        public void Init(TutorialFlow flow, GameUi ui, Game game, bool canSkip)
        {
            this.flow = flow;
            this.ui = ui;
            this.game = game;
            root = (RectTransform)transform;
            var group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = true;

            ring = UiKit.Icon(transform, "Ring", UiSprites.Circle, new Color(0.72f, 0.29f, 0.38f, 0.28f), 180);
            finger = UiKit.Icon(transform, "Finger", UiSprites.Circle, Palette.Rose, 56);
            var fingerOutline = finger.gameObject.AddComponent<Outline>();
            fingerOutline.effectColor = Color.white;
            fingerOutline.effectDistance = new Vector2(4, -4);

            var bannerImage = UiKit.Panel(transform, "Banner", Palette.Card, 36);
            bannerImage.raycastTarget = false;
            banner = bannerImage.rectTransform;
            banner.Pin(new Vector2(0.5f, 1), new Vector2(0, -424), new Vector2(1000, 170)); // under the orders and the café's side buttons
            counter = UiKit.Text(banner, "Step", "", 24, Palette.Rose);
            counter.fontStyle = FontStyles.Bold;
            counter.rectTransform.Top(16, 36, 32, canSkip ? 200 : 32);
            text = UiKit.Text(banner, "Text", "", 30, Palette.Ink);
            text.rectTransform.Stretch(canSkip ? 200 : 32, 32, 56, 16);
            skip = UiKit.Button(banner, "Skip", Loc.T("tutorial.skip"), Palette.Muted, Palette.Ink, 26, flow.Skip, 30);
            ((RectTransform)skip.transform).Pin(new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(160, 124));
            skip.gameObject.SetActive(canSkip);
            SetShown(false);
        }

        void SetShown(bool show)
        {
            if (banner.gameObject.activeSelf == show) return;
            banner.gameObject.SetActive(show);
            ring.gameObject.SetActive(show);
            finger.gameObject.SetActive(show);
        }

        void Update()
        {
            var step = flow.Current;
            bool show = step != null && step.HandsOn && !ui.Dialogue.IsOpen && !ui.ModalOpen;
            SetShown(show);
            if (!show) return;

            if (step != shownStep || (step.Until.Kind == "afford" && game.State.Coins != shownCoins))
            {
                shownStep = step;
                shownCoins = game.State.Coins;
                counter.text = Loc.T("tutorial.stepOf", flow.HandsOnNumber(step), flow.HandsOnCount);
                text.text = step.Until.Kind == "afford"
                    ? Loc.T(step.Text, game.State.Coins, game.CurrentTaskCost)
                    : Loc.T(step.Text);
            }

            var (from, to) = ui.TutorialTarget(step);
            bool pointing = to != null;
            ring.enabled = finger.enabled = pointing;
            if (!pointing) return;

            var end = Local(to);
            ring.rectTransform.anchoredPosition = end;
            ring.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 4f));
            float t = Mathf.Repeat(Time.unscaledTime, FingerSeconds) / FingerSeconds;
            // A drag from one item to the other, or a gentle tap on a single target.
            finger.rectTransform.anchoredPosition = from != null
                ? Vector2.Lerp(Local(from), end, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 1.4f)))
                : end + new Vector2(30, -30) * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f));
        }

        Vector2 Local(RectTransform target)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
            return local;
        }
    }
}
