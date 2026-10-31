using System;
using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.Memories;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Find: the 1974 room's objects scattered on the card, with the list to find at the top. A hinted object pulses.
    // Placeholder tiles until the room's art arrives (missing/13).
    public sealed class FindView : MemoryGameView
    {
        readonly Dictionary<string, Button> tiles = new Dictionary<string, Button>();
        FindGame game;
        RTLTMPro.RTLTextMeshPro wanted;
        int leftShown = -1;

        public void Init()
        {
            Build("find.title", 1180);
            wanted = UiKit.Text(Card, "Wanted", "", 28, Palette.Teal, TextAlignmentOptions.Center);
            wanted.rectTransform.Top(96, 80, 40, 40);
            Area.offsetMax = new Vector2(Area.offsetMax.x, -190);
        }

        // objects to find, and decoys that are only part of the room.
        public void Play(FindGame find, IList<string> objects, IList<string> decoys, int seed, Action<int> done)
        {
            game = find;
            leftShown = -1;
            UiKit.Clear(Area);
            tiles.Clear();
            var all = new List<string>(objects);
            all.AddRange(decoys);
            var random = new System.Random(seed);
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            // A loose grid with a little jitter, so nothing overlaps and every tile is a full touch target.
            int columns = 4, rows = Mathf.CeilToInt(all.Count / (float)columns);
            for (int i = 0; i < all.Count; i++)
            {
                string id = all[i];
                var tile = UiKit.Button(Area, id, Loc.T("memobj." + id), Palette.Hex(i % 2 == 0 ? "#E8D9C4" : "#D9C7B0"), Palette.Ink, 24, () => Tap(id), 24);
                var rect = (RectTransform)tile.transform;
                float x = (i % columns + 0.5f) / columns + ((float)random.NextDouble() - 0.5f) * 0.06f;
                float y = 1f - (i / columns + 0.5f) / rows + ((float)random.NextDouble() - 0.5f) * 0.04f;
                rect.Pin(new Vector2(x, y), Vector2.zero, new Vector2(200, UiKit.MinTouch + 10));
                rect.pivot = new Vector2(0.5f, 0.5f);
                tiles[id] = tile;
            }
            Begin(find, done);
        }

        void Tap(string id)
        {
            if (game.Tap(id))
            {
                UiSound.Play("ui_coin");
                var tile = tiles[id];
                tile.interactable = false;
                // Found: a tick and a faded tile, so it doesn't rely on colour.
                UiKit.Icon(tile.transform, "Found", UiSprites.Tick, Palette.SageDark, 48).rectTransform.Pin(new Vector2(0, 1), new Vector2(6, -6), new Vector2(48, 48));
                Tweens.Instance?.PopOf((RectTransform)tile.transform, 0.2f);
            }
            else
            {
                UiSound.Play("ui_error");
                Tweens.Instance?.PopOf((RectTransform)tiles[id].transform, -0.12f);
            }
        }

        protected override void Draw()
        {
            ShowClock(game.SecondsLeft);
            if (game.Left.Count != leftShown)
            {
                leftShown = game.Left.Count;
                var names = new List<string>();
                foreach (var id in game.Left) names.Add(Loc.T("memobj." + id));
                wanted.text = string.Join(" · ", names);
            }
            string hint = game.Hint;
            if (hint != null && tiles.TryGetValue(hint, out var tile))
                tile.transform.localScale = Vector3.one * (1f + (Tweens.Reduced ? 0f : 0.06f * Mathf.Sin(Time.unscaledTime * 6f)));
        }
    }
}
