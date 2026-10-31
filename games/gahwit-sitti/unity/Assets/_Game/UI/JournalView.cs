using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The story journal (screens/Journal, missing/11): each chapter's scenes, played again with a tap, the regulars'
    // scenes, the letters found and the choices made. Locked rows say so in words, not only in colour.
    public sealed class JournalView : MonoBehaviour
    {
        Game game;
        Func<string, string, string> choiceText; // knot, saved id → the option's text
        Action<string> replay;                   // plays a seen scene again
        Action<string> reread;                   // opens a found letter again
        Func<List<string>> clues;                // the found recipes that carry a note from Teta
        RectTransform list;
        int open;

        public bool IsOpen => gameObject.activeSelf;

        public void Init(Game game, Func<string, string, string> choiceText, Action<string> replay, Action<string> reread,
            Func<List<string>> clues)
        {
            this.game = game;
            this.clues = clues;
            this.choiceText = choiceText;
            this.replay = replay;
            this.reread = reread;
            var card = Sheet.Card(transform, 980, 1640);
            Sheet.Title(card, Loc.T("journal.title"), Close);
            list = Sheet.Column(card, 150, 40, 14);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            UiSound.Play("ui_panel_open");
            transform.SetAsLastSibling();
            open = game.State.Chapter;
            gameObject.SetActive(true);
            Build();
        }

        void Close()
        {
            UiSound.Play("ui_panel_close");
            gameObject.SetActive(false);
        }

        void Build()
        {
            UiKit.Clear(list);
            var chapters = game.Config.Chapters;
            int finished = chapters.Count(c => c.N < game.State.Chapter);
            Line(Loc.T("journal.progress", finished, chapters.Count));

            foreach (var chapter in chapters)
            {
                int n = chapter.N;
                var tasks = game.Content.ForChapter(n)?.Tasks;
                bool locked = n > game.State.Chapter || tasks == null;
                string name = game.English ? chapter.En : chapter.Ar;
                int seen = tasks?.Count(t => game.State.ScenesSeen.Contains(t.Knot)) ?? 0;
                string state = locked ? Loc.T(n > game.State.Chapter ? "journal.locked" : "journal.soon")
                    : Loc.T("journal.scenes", seen, tasks.Count);
                Row(Loc.T("journal.chapter", n, locked && n > game.State.Chapter ? Loc.T("journal.lockedName") : name), state,
                    locked ? null : Loc.T(open == n ? "journal.hide" : "journal.show"), () =>
                    {
                        open = open == n ? 0 : n;
                        Build();
                    });
                if (locked || open != n) continue;
                foreach (var task in tasks)
                {
                    string knot = task.Knot;
                    bool watched = game.State.ScenesSeen.Contains(knot);
                    Row(watched ? Loc.T("task." + task.Id + ".title") : Loc.T("journal.sceneLocked"), "",
                        watched ? Loc.T("journal.replay") : null, () => replay(knot), true, true);
                }
            }

            // The regulars' own scenes: "<regular>_<level>" (Customers.CustomerRules.StoryDue).
            var regulars = game.State.ScenesSeen.Select(RegularScene).Where(r => r.HasValue).Select(r => r.Value).ToList();
            if (regulars.Count > 0) Section(Loc.T("journal.regulars"));
            foreach (var (knot, who, level) in regulars)
                Row(Loc.T("char." + who), Loc.T("journal.friendship", level), Loc.T("journal.replay"), () => replay(knot), false, true);

            if (game.State.LettersFound.Count > 0) Section(Loc.T("journal.letters"));
            for (int i = 0; i < game.State.LettersFound.Count; i++)
            {
                string letter = game.State.LettersFound[i];
                Row(Loc.T("journal.letter", i + 1), "", Loc.T("journal.reread"), () => reread(letter), false, true);
            }

            // Teta's notes in the margins of the recipes found so far (missing/10).
            var notes = clues();
            if (notes.Count > 0) Section(Loc.T("journal.clues"));
            foreach (string recipe in notes)
                Row(Loc.T("recipe." + recipe), Loc.T("recipe." + recipe + ".clue"), null, null);

            // Teta's kitchen notebook (DEVLOG 2026-10-31): every page found so far, chain by chain, with her note.
            var chains = game.Config.Chains.Keys.Where(c => game.PagesFound(c) > 0).ToList();
            if (chains.Count > 0) Section(Loc.T("journal.notebook"));
            foreach (string chain in chains)
            {
                Row(Loc.T("chain." + chain), Loc.T("journal.pages", game.PagesFound(chain), game.MaxLevel(chain)), null, null);
                for (int level = 1; level <= game.MaxLevel(chain); level++)
                    if (game.HasPage(chain, level))
                        Row(game.ItemName(chain, level), Loc.T("notebook." + Game.PageId(chain, level)), null, null, true);
            }

            // Copied first: finding a choice's text can load its story, which updates old saved choices.
            var choices = game.State.Choices.ToList()
                .Select(c => (knot: c.Key, text: choiceText(c.Key, c.Value))).Where(c => c.text != null).ToList();
            if (choices.Count > 0) Section(Loc.T("journal.choices"));
            foreach (var (knot, text) in choices)
                Row(SceneName(knot), text, null, null);
        }

        (string knot, string who, int level)? RegularScene(string knot)
        {
            int cut = knot.LastIndexOf('_');
            if (cut <= 0 || !int.TryParse(knot.Substring(cut + 1), out int level)) return null;
            string who = knot.Substring(0, cut);
            if (!game.Content.Characters.TryGetValue(who, out var character) || !character.PlacesOrders) return null;
            return (knot, who, level);
        }

        // A task's title for its scene, or the chapter's for anything else.
        string SceneName(string knot)
        {
            foreach (var chapter in game.Content.Chapters)
            {
                var task = chapter.Tasks.FirstOrDefault(t => t.Knot == knot);
                if (task != null) return Loc.T("task." + task.Id + ".title");
            }
            return Loc.T("journal.scene");
        }

        void Section(string title)
        {
            var label = UiKit.Text(list, "Section", title, 28, Palette.InkSoft);
            label.fontStyle = FontStyles.Bold;
            Sheet.Sized(label, 56);
        }

        void Line(string text) => Sheet.Sized(UiKit.Text(list, "Line", text, 26, Palette.InkSoft), 70);

        // A quiet action (play again, read again) is a light button, so the rows' names stay the strongest thing.
        void Row(string name, string detail, string action, Action act, bool indented = false, bool quiet = false)
        {
            var row = UiKit.Panel(list, name, indented ? Palette.Card : Color.white, 28);
            Sheet.Sized(row, string.IsNullOrEmpty(detail) ? 150 : 170);
            float start = indented ? 60 : 28;
            var title = UiKit.Text(row.transform, "Name", name, 30, Palette.Ink);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.Stretch(action != null ? 280 : 28, start, 20, string.IsNullOrEmpty(detail) ? 20 : 90);
            if (!string.IsNullOrEmpty(detail))
                UiKit.Text(row.transform, "Detail", detail, 24, Palette.InkSoft).rectTransform.Stretch(action != null ? 280 : 28, start, 82, 16);
            if (action == null) return;
            var button = UiKit.Button(row.transform, "Act", action, quiet ? Palette.Muted : Palette.Rose, quiet ? Palette.Rose : Color.white, 28,
                () => act?.Invoke(), 32);
            ((RectTransform)button.transform).Pin(new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(230, UiKit.MinTouch));
        }
    }
}
