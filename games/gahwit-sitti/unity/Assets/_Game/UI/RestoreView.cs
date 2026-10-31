using System;
using Gahwit.Core;
using Gahwit.Memories;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // Restore: the torn photo's pieces lie under its frame; drag each one into place. The photo is drawn in code
    // until the memory's real photo exists (missing/13).
    public sealed class RestoreView : MemoryGameView
    {
        const float PhotoWidth = 720, PhotoHeight = 540;

        RestoreGame game;
        RectTransform photo;
        RawImage[] pieces;
        Texture2D picture;

        public void Init()
        {
            Build("restore.title", 1360);
            var frame = UiKit.Panel(Area, "Frame", Palette.Hex("#EADFCF"), 12);
            frame.rectTransform.Pin(new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(PhotoWidth + 24, PhotoHeight + 24));
            photo = UiKit.Rect(Area, "Photo").Pin(new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(PhotoWidth, PhotoHeight));
            picture = SepiaPicture();
        }

        public void Play(RestoreGame restore, Action<int> done)
        {
            game = restore;
            UiKit.Clear(photo);
            pieces = new RawImage[game.Count];
            var size = new Vector2(PhotoWidth / game.Columns, PhotoHeight / game.Rows);
            for (int i = 0; i < game.Count; i++)
            {
                var piece = UiKit.Rect(photo, "Piece" + i).gameObject.AddComponent<RawImage>();
                piece.texture = picture;
                var home = game.Home(i);
                piece.uvRect = new Rect(home.x - 0.5f / game.Columns, 1f - home.y - 0.5f / game.Rows, 1f / game.Columns, 1f / game.Rows);
                piece.rectTransform.anchorMin = piece.rectTransform.anchorMax = new Vector2(0, 1);
                piece.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                piece.rectTransform.sizeDelta = Vector2.Max(size, new Vector2(UiKit.MinTouch, UiKit.MinTouch));
                piece.gameObject.AddComponent<Outline>().effectColor = new Color(0.36f, 0.25f, 0.17f, 0.6f);
                var drag = piece.gameObject.AddComponent<PieceDrag>();
                drag.Index = i;
                drag.View = this;
                pieces[i] = piece;
                Place(i);
            }
            Begin(restore, done);
        }

        void Place(int i)
        {
            var at = game.At(i);
            pieces[i].rectTransform.anchoredPosition = new Vector2(at.x * PhotoWidth, -at.y * PhotoHeight);
            // A placed piece is its true size; a loose one is lifted a little.
            var size = new Vector2(PhotoWidth / game.Columns, PhotoHeight / game.Rows);
            pieces[i].rectTransform.sizeDelta = game.Placed(i) ? size : Vector2.Max(size, new Vector2(UiKit.MinTouch, UiKit.MinTouch));
        }

        internal void Dragged(int i, PointerEventData eventData)
        {
            if (game == null || game.Placed(i)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(photo, eventData.position, eventData.pressEventCamera, out var local);
            pieces[i].rectTransform.anchoredPosition = local + new Vector2(PhotoWidth / 2, -PhotoHeight / 2);
            pieces[i].transform.SetAsLastSibling();
        }

        internal void Dropped(int i)
        {
            if (game == null || game.Placed(i)) return;
            var p = pieces[i].rectTransform.anchoredPosition;
            bool snapped = game.Drop(i, p.x / PhotoWidth, -p.y / PhotoHeight);
            Place(i);
            if (snapped)
            {
                pieces[i].transform.SetAsFirstSibling();
                UiSound.Play("ui_tap");
                Tweens.Instance?.Flash(pieces[i].rectTransform, Color.white);
            }
        }

        protected override void Draw() => ShowClock(game.SecondsLeft);

        // A faded family photo: a sky, a shop front and three figures, in sepia.
        static Texture2D SepiaPicture()
        {
            const int w = 240, h = 180;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "TornPhoto", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = y > h * 0.55f ? 0.85f - (y - h * 0.55f) / h * 0.4f : 0.55f;
                if (y < h * 0.55f && y > h * 0.12f && x > w * 0.1f && x < w * 0.9f) v = 0.45f + 0.05f * Mathf.Sin(x * 0.3f); // the shop front
                for (int f = 0; f < 3; f++)
                {
                    float cx = w * (0.3f + f * 0.2f), cy = h * (0.36f + f * 0.02f);
                    if (Mathf.Abs(x - cx) < 9 && y < cy && y > h * 0.08f) v = 0.25f;           // body
                    if ((x - cx) * (x - cx) + (y - cy - 10) * (y - cy - 10) < 70) v = 0.3f;   // head
                }
                pixels[y * w + x] = new Color32((byte)(255 * v), (byte)(220 * v), (byte)(170 * v), 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        sealed class PieceDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public int Index;
            public RestoreView View;
            public void OnBeginDrag(PointerEventData eventData) => View.Dragged(Index, eventData);
            public void OnDrag(PointerEventData eventData) => View.Dragged(Index, eventData);
            public void OnEndDrag(PointerEventData eventData) => View.Dropped(Index);
        }
    }
}
