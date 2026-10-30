using System.Collections.Generic;
using TMPro;
using RTLTMPro;
using UnityEngine;

namespace Gahwit.UI
{
    // A short message at the top of the screen; several in a row play one after another.
    public sealed class Toast : MonoBehaviour
    {
        const float Duration = 1.8f;

        readonly Queue<string> queue = new Queue<string>();
        CanvasGroup group;
        RTLTextMeshPro label;
        float shownAt = -100f;

        public void Init()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            var background = UiKit.Panel(transform, "Background", Palette.ToastBg, 40);
            background.raycastTarget = false;
            background.rectTransform.Stretch();
            label = UiKit.Text(background.transform, "Text", "", 30, Color.white, TextAlignmentOptions.Center);
            label.rectTransform.Stretch(24, 24, 6, 6);
        }

        public void Show(string text)
        {
            queue.Enqueue(text);
            if (Time.unscaledTime - shownAt >= Duration) Next();
        }

        void Next()
        {
            if (queue.Count == 0) return;
            label.text = queue.Dequeue();
            shownAt = Time.unscaledTime;
        }

        void Update()
        {
            float age = Time.unscaledTime - shownAt;
            group.alpha = age < Duration ? Mathf.Clamp01(Mathf.Min(age / 0.15f, (Duration - age) / 0.3f)) : 0;
            if (age >= Duration && queue.Count > 0) Next();
        }
    }
}
