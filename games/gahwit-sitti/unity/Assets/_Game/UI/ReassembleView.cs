using System;
using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.Memories;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Reassemble: the torn letter's strips, one per row. Tap a strip, then another, to swap them. The chosen strip has
    // a thick outline, so it doesn't rely on colour.
    public sealed class ReassembleView : MemoryGameView
    {
        ReassembleGame game;
        IList<string> lines;
        readonly List<Button> rows = new List<Button>();
        int picked = -1;

        public void Init() => Build("reassemble.title", 1300);

        // lines: the letter's strips in their right order (from the memory's Ink story).
        public void Play(ReassembleGame reassemble, IList<string> letterLines, Action<int> done)
        {
            game = reassemble;
            lines = letterLines;
            picked = -1;
            UiKit.Clear(Area);
            rows.Clear();
            var column = Area.gameObject.GetComponent<VerticalLayoutGroup>();
            if (column == null) column = Area.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 12;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = column.childForceExpandHeight = true;
            for (int slot = 0; slot < game.Count; slot++)
            {
                int at = slot;
                var row = UiKit.Button(Area, "Strip" + slot, "", Palette.Hex("#FBF6EC"), Palette.Ink, 28, () => Pick(at), 6);
                row.Label().alignment = UiKit.Side(TextAlignmentOptions.Right);
                row.Label().fontStyle = FontStyles.Italic;
                row.gameObject.AddComponent<Outline>().effectColor = Palette.Rose;
                rows.Add(row);
            }
            Hint.text = Loc.T("reassemble.hint");
            Begin(reassemble, done);
            Refresh();
        }

        void Pick(int slot)
        {
            if (game.Finished) return;
            if (picked < 0)
            {
                picked = slot;
            }
            else
            {
                game.Swap(picked, slot);
                UiSound.Play("page_turn");
                picked = -1;
            }
            Refresh();
        }

        void Refresh()
        {
            for (int slot = 0; slot < rows.Count; slot++)
            {
                rows[slot].Label().text = lines[game.StripAt(slot)];
                var outline = rows[slot].GetComponent<Outline>();
                outline.effectDistance = slot == picked ? new Vector2(5, -5) : Vector2.zero;
                outline.enabled = slot == picked;
            }
        }

        protected override void Draw() => ShowClock(game.SecondsLeft);
    }
}
