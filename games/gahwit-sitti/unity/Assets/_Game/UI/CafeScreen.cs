using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Merge;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The 3D café with the chapter's current task: spend coins to restore the next part of the room.
    public sealed class CafeScreen : MonoBehaviour
    {
        Game game;
        RTLTextMeshPro chapter, progress, title, description, need;
        Image progressFill;
        Button restore, toKitchen;

        public void Init(Game game, GameUi ui, CafeCamera cafeCamera)
        {
            this.game = game;

            var pad = UiKit.Rect(transform, "OrbitPad").Stretch();
            pad.gameObject.AddComponent<Image>().color = Color.clear;
            pad.gameObject.AddComponent<OrbitPad>().Target = cafeCamera;

            UiKit.Text(transform, "Hint", Loc.T("cafe.hintOrbit"), 24, Palette.InkSoft, TextAlignmentOptions.Center)
                .rectTransform.Top(16, 36);

            var card = UiKit.Panel(transform, "TaskCard", Palette.Card, 40);
            card.rectTransform.Bottom(16, 390, 24, 24);
            chapter = UiKit.Text(card.transform, "Chapter", "", 28, Palette.InkSoft);
            chapter.rectTransform.Top(24, 40, 32, 32);

            var track = UiKit.Panel(card.transform, "Progress", Palette.Track, 8);
            track.rectTransform.Top(78, 14, 150, 32); // the bar on the right, its "2 of 6" label on the left
            progressFill = UiKit.Panel(track.transform, "Fill", Palette.Rose, 8);
            progress = UiKit.Text(card.transform, "ProgressText", "", 24, Palette.InkSoft, TextAlignmentOptions.Left);
            progress.rectTransform.Pin(new Vector2(0, 1), new Vector2(32, -66), new Vector2(110, 38));

            title = UiKit.Text(card.transform, "Title", "", 42, Palette.Ink);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.Top(108, 58, 32, 32);
            description = UiKit.Text(card.transform, "Description", "", 28, Palette.InkSoft);
            description.rectTransform.Top(170, 44, 32, 32);
            need = UiKit.Text(card.transform, "Need", "", 26, Palette.Rose);
            need.rectTransform.Top(218, 40, 32, 32);

            restore = UiKit.Button(card.transform, "Restore", "", Palette.Rose, Color.white, 32, () => game.CompleteTask(), 34);
            ((RectTransform)restore.transform).Bottom(24, 84, 32, 32);
            toKitchen = UiKit.Button(card.transform, "ToKitchen", Loc.T("cafe.toKitchen"), Palette.SageDark, Color.white, 30, ui.ShowKitchen, 34);
            ((RectTransform)toKitchen.transform).Bottom(24, 84, 32, 32);
        }

        public void Refresh()
        {
            var config = game.Config.Chapter(game.State.Chapter);
            chapter.text = config != null ? Loc.T("cafe.chapter", game.State.Chapter, config.Ar) : "";

            var task = game.CurrentTask;
            if (task == null)
            {
                progressFill.SetFill(1);
                progress.text = "";
                title.text = Loc.T("cafe.soon");
                description.text = need.text = "";
                restore.gameObject.SetActive(false);
                toKitchen.gameObject.SetActive(true);
                return;
            }

            int done = game.State.TaskIndex, total = game.ChapterTasks.Tasks.Count;
            progressFill.SetFill((float)done / total);
            progress.text = Loc.T("cafe.progress", done, total);
            title.text = Loc.T($"task.{task.Id}.title");
            description.text = Loc.T($"task.{task.Id}.desc");

            bool afford = game.CanAffordTask;
            restore.gameObject.SetActive(afford);
            toKitchen.gameObject.SetActive(!afford);
            restore.Label().text = Loc.T("cafe.restore", game.CurrentTaskCost);
            need.text = afford ? "" : Loc.T("cafe.need", game.CurrentTaskCost - game.State.Coins);
        }
    }
}
