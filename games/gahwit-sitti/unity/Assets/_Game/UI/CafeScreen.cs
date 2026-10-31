using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Decor;
using Gahwit.Merge;
using Gahwit.Wardrobe;
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
        GameUi ui;
        CafeRoom room;
        Camera view;
        RTLTextMeshPro chapter, progress, title, description, need;
        Image progressFill;
        Button restore, toKitchen, drawer;
        RectTransform styleRow;
        string styleTask, chosenStyle;

        public RectTransform RestoreButton => restore != null && restore.gameObject.activeInHierarchy ? (RectTransform)restore.transform : null;

        // Each seated customer shows what they ordered over their head (SYSTEMS_B.md section 3).
        readonly System.Collections.Generic.List<(int orderId, ItemTile tile)> bubbles = new System.Collections.Generic.List<(int, ItemTile)>();
        RectTransform bubbleLayer;
        ItemLook look;
        int bubblesFor = -1;

        // Decor mode (missing/07): the task card gives way to a bar with cosiness and the piece tools.
        DecorRules decor;
        DecorShopView shop;
        bool decorMode;
        string selected;
        OrbitPad pad;
        GameObject taskCard, decorBar;
        Button decorButton, turn, store, wardrobeButton;
        WardrobeRules wardrobe;
        RTLTextMeshPro cosiness;
        CafeCamera cafeCamera;
        int cosinessShown = -1;
        float nextCosinessTick;

        // The café's spaces (missing/07): chips over the task card to move between the floor, the kitchen corner and,
        // from chapter 6, the rooftop.
        RectTransform spaceRow;
        string spacesShown, spaceShownFor;
        int spacesOpen = -1;

        // With reduced motion the camera cuts instead of gliding (COMPLETE_SPEC.md section 5).
        public System.Func<bool> ReducedMotion = () => false;

        public void Init(Game game, GameUi ui, CafeCamera cafeCamera, CafeRoom room, System.Action openDrawer, ItemLook look)
        {
            this.look = look;
            this.game = game;
            this.ui = ui;
            this.room = room;
            view = cafeCamera.GetComponent<Camera>();
            this.cafeCamera = cafeCamera;

            var padRect = UiKit.Rect(transform, "OrbitPad").Stretch();
            padRect.gameObject.AddComponent<Image>().color = Color.clear;
            pad = padRect.gameObject.AddComponent<OrbitPad>();
            pad.Target = cafeCamera;
            pad.DoubleTap = Focus;
            bubbleLayer = UiKit.Rect(transform, "Bubbles").Stretch();

            UiKit.Text(transform, "Hint", Loc.T("cafe.hintOrbit"), 24, Palette.InkSoft, TextAlignmentOptions.Center)
                .rectTransform.Top(16, 36);

            var settings = UiKit.Button(transform, "Settings", "", Palette.Card, Palette.Ink, 24, () => ui.Settings.Show(), 60);
            ((RectTransform)settings.transform).Pin(new Vector2(0, 1), new Vector2(20, -12), new Vector2(UiKit.MinTouch, UiKit.MinTouch));
            UiKit.Icon(settings.transform, "Gear", UiSprites.Gear, Palette.InkSoft, 76).rectTransform.Pin(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 76));

            decorButton = UiKit.Button(transform, "Decor", Loc.T("decor.button"), Palette.Card, Palette.Ink, 26, EnterDecor, 60);

            Fit(decorButton);
            ((RectTransform)decorButton.transform).Pin(new Vector2(0, 1), new Vector2(20, -156), new Vector2(UiKit.MinTouch, UiKit.MinTouch));
            decorButton.gameObject.SetActive(false);
            wardrobeButton = UiKit.Button(transform, "Wardrobe", Loc.T("wardrobe.button"), Palette.Card, Palette.Ink, 24, null, 60);
            Fit(wardrobeButton);
            ((RectTransform)wardrobeButton.transform).Pin(new Vector2(0, 1), new Vector2(20, -300), new Vector2(UiKit.MinTouch, UiKit.MinTouch));
            wardrobeButton.gameObject.SetActive(false);
            BuildDecorBar();

            var card = UiKit.Panel(transform, "TaskCard", Palette.Card, 40);
            taskCard = card.gameObject;
            card.rectTransform.Bottom(16, 520, 24, 24);
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

            styleRow = UiKit.Rect(card.transform, "Styles").Top(214, UiKit.MinTouch, 32, 32);
            UiKit.Row(styleRow.gameObject, 16).childForceExpandWidth = true;

            restore = UiKit.Button(card.transform, "Restore", "", Palette.Rose, Color.white, 32,
                () => game.CompleteTask(game.CurrentTask?.Styles != null ? chosenStyle : null), 34);
            ((RectTransform)restore.transform).Bottom(24, UiKit.MinTouch, 32, 32);
            toKitchen = UiKit.Button(card.transform, "ToKitchen", Loc.T("cafe.toKitchen"), Palette.SageDark, Color.white, 30, ui.ShowKitchen, 34);
            ((RectTransform)toKitchen.transform).Bottom(24, UiKit.MinTouch, 32, 32);

            spaceRow = UiKit.Rect(transform, "Spaces").Bottom(552, UiKit.MinTouch + 8, 40, 40);
            var spaceLayout = UiKit.Row(spaceRow.gameObject, 14);
            spaceLayout.childForceExpandWidth = false;

            // Floats over the counter's drawer while a letter waits in it.
            drawer = UiKit.Button(transform, "OpenDrawer", Loc.T("drawer.open"), Palette.Rose, Color.white, 30, () => openDrawer(), 40);
            ((RectTransform)drawer.transform).Pin(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, UiKit.MinTouch));
            drawer.gameObject.SetActive(false);
        }

        void Update()
        {
            TickCosiness();
            // Order bubbles and the drawer button sit over the floor, so they show only there.
            bool onFloor = room.Space == ChapterContent.FloorSpace;
            if (bubbleLayer.gameObject.activeSelf != onFloor) bubbleLayer.gameObject.SetActive(onFloor);
            if (onFloor) PlaceBubbles();
            if (spaceShownFor != room.Space) RefreshSpaces(); // a memory moved the camera
            bool waiting = game.LetterWaiting() != null;
            room.SetDrawerGlow(waiting);
            // Hidden while a scene or the letter itself is on screen, so it can't open twice.
            bool show = waiting && onFloor && !ui.Dialogue.IsOpen && !ui.Letter.IsOpen;
            drawer.gameObject.SetActive(show);
            if (!show) return;

            var rect = (RectTransform)transform;
            var screen = RectTransformUtility.WorldToScreenPoint(view, room.DrawerPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out var local);
            var button = (RectTransform)drawer.transform;
            button.anchoredPosition = local + new Vector2(0, 90);
            button.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3f));
        }

        // Tiles are rebuilt only when the orders change; each frame only moves them over the figures.
        void PlaceBubbles()
        {
            int signature = 17;
            foreach (var order in game.Orders) signature = signature * 31 + order.Id;
            if (signature != bubblesFor)
            {
                bubblesFor = signature;
                UiKit.Clear(bubbleLayer);
                bubbles.Clear();
                foreach (var order in game.Orders)
                {
                    var bubble = UiKit.Panel(bubbleLayer, "Bubble", Color.white, 40);
                    bubble.raycastTarget = false;
                    bubble.rectTransform.sizeDelta = new Vector2(96, 96);
                    var tile = new ItemTile(bubble.transform, "Wanted", 30, 0);
                    tile.Root.Stretch(8, 8, 8, 8);
                    var line = order.Lines[0];
                    tile.Show(new Item(line.Chain, line.Level), look);
                    bubbles.Add((order.Id, tile));
                }
            }
            var rect = (RectTransform)bubbleLayer;
            foreach (var (orderId, tile) in bubbles)
            {
                var bubble = (RectTransform)tile.Root.parent;
                bool shown = room.Customers.TryHead(orderId, out var head);
                if (bubble.gameObject.activeSelf != shown) bubble.gameObject.SetActive(shown);
                if (!shown) continue;
                var screen = RectTransformUtility.WorldToScreenPoint(view, head);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out var local);
                bubble.anchoredPosition = local;
            }
        }

        public void SetDecor(DecorRules rules, DecorShopView shopView)
        {
            decor = rules;
            shop = shopView;
            rules.Changed += RefreshDecor;
            Refresh();
        }

        public void SetWardrobe(WardrobeRules rules, WardrobeView view)
        {
            wardrobe = rules;
            wardrobeButton.onClick.AddListener(view.Show);
            Refresh();
        }

        void BuildDecorBar()
        {
            var bar = UiKit.Panel(transform, "DecorBar", Palette.Card, 40);
            bar.rectTransform.Bottom(16, 310, 24, 24);
            decorBar = bar.gameObject;
            cosiness = UiKit.Text(bar.transform, "Cosiness", "", 30, Palette.Ink);
            cosiness.fontStyle = FontStyles.Bold;
            cosiness.rectTransform.Top(24, 48, 32, 32);
            UiKit.Text(bar.transform, "Hint", Loc.T("decor.hint"), 24, Palette.InkSoft).rectTransform.Top(76, 40, 32, 32);
            var row = UiKit.Rect(bar.transform, "Tools").Bottom(24, UiKit.MinTouch, 24, 24);
            UiKit.Row(row.gameObject, 12).childForceExpandWidth = true;
            Tool(row, Loc.T("decor.shop"), () => shop.Show());
            turn = Tool(row, Loc.T("decor.rotate"), () => { if (selected != null && !decor.Rotate(selected)) UiSound.Play("ui_error"); });
            store = Tool(row, Loc.T("decor.store"), () =>
            {
                if (selected == null) return;
                decor.Store(selected);
                Select(null);
            });
            Tool(row, Loc.T("decor.done"), ExitDecor).targetGraphic.color = Palette.Rose;
            decorBar.SetActive(false);
        }

        static Button Tool(RectTransform row, string label, UnityEngine.Events.UnityAction act)
        {
            var button = UiKit.Button(row, label, label, Palette.Muted, Palette.Ink, 26, act, 30);
            button.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            return button;
        }

        void EnterDecor()
        {
            ShowSpace(ChapterContent.FloorSpace); // decor stands on the floor
            decorMode = true;
            pad.PressPiece = PressPiece;
            pad.DragPiece = DragPiece;
            pad.Tap = _ => Select(null);
            Refresh();
        }

        void ExitDecor()
        {
            decorMode = false;
            pad.PressPiece = null;
            pad.DragPiece = null;
            pad.Tap = null;
            Select(null);
            Refresh();
        }

        void Select(string id)
        {
            selected = id;
            room.Decor.Select(id);
            RefreshDecor();
        }

        bool PressPiece(Vector2 screen)
        {
            string id = room.Decor.PieceAt(view.ScreenPointToRay(screen));
            if (id == null) return false;
            if (id != selected) Select(id);
            return true;
        }

        void DragPiece(Vector2 screen)
        {
            if (selected != null && FloorPoint(screen, out var point)) decor.MoveTo(selected, point.x, point.z);
        }

        // Double tap: look closer at that spot, or back at the whole room.
        void Focus(Vector2 screen)
        {
            if (cafeCamera.Focused) cafeCamera.Unfocus(ReducedMotion());
            else if (FloorPoint(screen, out var point)) cafeCamera.Focus(point, ReducedMotion());
        }

        bool FloorPoint(Vector2 screen, out Vector3 point)
        {
            var ray = view.ScreenPointToRay(screen);
            point = default;
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return false;
            float distance = -ray.origin.y / ray.direction.y;
            if (distance <= 0) return false;
            point = ray.origin + ray.direction * distance;
            return true;
        }

        void RefreshDecor()
        {
            if (decor == null) return;
            // Cosiness ticks up to its new total when a piece goes down (COMPLETE_SPEC.md section 8).
            if (cosinessShown < 0 || cosinessShown > game.State.Cosiness || ReducedMotion()) cosinessShown = game.State.Cosiness;
            WriteCosiness();
            store.interactable = selected != null;
            turn.interactable = selected != null && !decor.IsWall(selected); // wall pieces always face the room
        }

        void WriteCosiness()
        {
            int next = decor.NextTableAt;
            cosiness.text = next < 0 ? Loc.T("decor.cosinessMax", cosinessShown) : Loc.T("decor.cosinessNext", cosinessShown, next);
        }

        void TickCosiness()
        {
            if (decor == null || cosinessShown < 0 || cosinessShown >= game.State.Cosiness || Time.unscaledTime < nextCosinessTick) return;
            cosinessShown++;
            nextCosinessTick = Time.unscaledTime + 0.06f;
            WriteCosiness();
            Tweens.Instance?.PopOf(cosiness.rectTransform, 0.12f);
        }

        Button todayButton, friendsButton, journalButton;

        // A round side button keeps its word on one line and shrinks it to fit ("Wardrobe" in English).
        static void Fit(Button button) => button.Label().OneLine();
        System.Func<bool> friendsOpen = () => false;

        // Friends (missing/20), under Today, once their level is reached.
        public void SetFriends(System.Action open, System.Func<bool> isOpen)
        {
            friendsOpen = isOpen;
            friendsButton = UiKit.Button(transform, "Friends", Loc.T("friends.button"), Palette.Card, Palette.Ink, 24, () => open(), 60);
            Fit(friendsButton);
            ((RectTransform)friendsButton.transform).Pin(new Vector2(0, 1), new Vector2(20, -588), new Vector2(UiKit.MinTouch, UiKit.MinTouch));
        }

        // The story journal (missing/11), under Friends.
        public void SetJournal(System.Action open)
        {
            journalButton = UiKit.Button(transform, "Journal", Loc.T("journal.button"), Palette.Card, Palette.Ink, 24, () => open(), 60);
            Fit(journalButton);
            ((RectTransform)journalButton.transform).Pin(new Vector2(0, 1), new Vector2(20, -732), new Vector2(UiKit.MinTouch, UiKit.MinTouch));
        }

        // Today's gift, tasks and events (missing/18), under the side buttons.
        public void SetToday(System.Action open)
        {
            todayButton = UiKit.Button(transform, "Today", Loc.T("today.button"), Palette.Gold, Palette.Ink, 24, () => open(), 60);
            Fit(todayButton);
            ((RectTransform)todayButton.transform).Pin(new Vector2(0, 1), new Vector2(20, -444), new Vector2(UiKit.MinTouch, UiKit.MinTouch));
        }

        void ShowSpace(string id)
        {
            if (room.Space == id) return;
            room.ShowSpace(id, ReducedMotion());
            RefreshSpaces();
        }

        // One chip per open space; the one shown is filled, bold and underlined, so it doesn't rely on colour. A newly
        // opened space (the rooftop) is announced once.
        void RefreshSpaces()
        {
            spaceShownFor = room.Space;
            var open = game.Content.OpenSpaces(game.CompletedTaskIds());
            spaceRow.gameObject.SetActive(!decorMode && open.Count > 1);
            if (spacesOpen >= 0 && open.Count > spacesOpen)
                ui.Toast.Show(Loc.T("space.opened", Loc.T("space." + open[open.Count - 1])));
            spacesOpen = open.Count;
            string key = string.Join(",", open) + "|" + room.Space;
            if (key == spacesShown) return;
            spacesShown = key;
            UiKit.Clear(spaceRow);
            foreach (var id in open)
            {
                bool shown = id == room.Space;
                string space = id;
                var chip = UiKit.Button(spaceRow, id, Loc.T("space." + id), shown ? Palette.Rose : Palette.Card, shown ? Color.white : Palette.Ink,
                    26, () => ShowSpace(space), 32);
                var label = chip.Label().OneLine();
                if (shown) label.fontStyle = FontStyles.Bold | FontStyles.Underline;
                var size = chip.gameObject.AddComponent<LayoutElement>();
                size.preferredWidth = 230;
                size.preferredHeight = UiKit.MinTouch;
            }
        }

        public void Refresh()
        {
            RefreshSpaces();
            if (todayButton != null) todayButton.gameObject.SetActive(!decorMode);
            if (friendsButton != null) friendsButton.gameObject.SetActive(!decorMode && friendsOpen());
            if (journalButton != null) journalButton.gameObject.SetActive(!decorMode);
            taskCard.SetActive(!decorMode);
            decorBar.SetActive(decorMode);
            decorButton.gameObject.SetActive(decor != null && decor.ShopOpen && !decorMode);
            wardrobeButton.gameObject.SetActive(wardrobe != null && wardrobe.Open && !decorMode);
            if (decorMode) RefreshDecor();
            var config = game.Config.Chapter(game.State.Chapter);
            chapter.text = config != null ? Loc.T("cafe.chapter", game.State.Chapter, game.English ? config.En : config.Ar) : "";

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

            bool choosing = afford && task.Styles != null && task.Styles.Count > 0;
            styleRow.gameObject.SetActive(choosing);
            if (choosing && styleTask != task.Id)
            {
                styleTask = task.Id;
                chosenStyle = task.Styles[0];
            }
            if (choosing) ShowStyleChips(task.Id, task.Styles);
        }

        readonly System.Collections.Generic.List<Button> styleChips = new System.Collections.Generic.List<Button>();
        string chipsFor;

        // One chip per style, painted like the sign would look; the chosen one has a thick outline. Built once per
        // task; later refreshes only move the outline.
        void ShowStyleChips(string taskId, System.Collections.Generic.List<string> styles)
        {
            if (chipsFor != taskId)
            {
                chipsFor = taskId;
                UiKit.Clear(styleRow);
                styleChips.Clear();
                foreach (var id in styles)
                {
                    string style = id;
                    var def = game.Content.Styles[style];
                    var chip = UiKit.Button(styleRow, style, Loc.T("style." + style),
                        Palette.Hex(def.SignBoard), Palette.Hex(def.SignText), 26, () => { chosenStyle = style; Refresh(); }, 30);
                    chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                    chip.gameObject.AddComponent<Outline>();
                    styleChips.Add(chip);
                }
            }
            foreach (var chip in styleChips)
            {
                bool chosen = chip.name == chosenStyle;
                var outline = chip.GetComponent<Outline>();
                outline.effectColor = chosen ? Palette.Rose : new Color(0.23f, 0.15f, 0.19f, 0.25f);
                outline.effectDistance = chosen ? new Vector2(5, -5) : new Vector2(1.5f, -1.5f);
                chip.Label().fontStyle = chosen ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            }
        }
    }
}
