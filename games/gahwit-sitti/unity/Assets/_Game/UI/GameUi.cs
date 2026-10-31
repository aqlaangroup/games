using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Decor;
using Gahwit.Merge;
using Gahwit.Tutorial;
using Gahwit.Wardrobe;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The whole interface, built in code: HUD, kitchen and café screens, tabs, dialogue, the account screens and toasts.
    public sealed class GameUi : MonoBehaviour
    {
        Game game;
        CafeCamera cafeCamera;
        Image background;
        Hud hud;
        KitchenScreen kitchen;
        CafeScreen cafe;
        Button kitchenTab, cafeTab;
        Image cafeBadge;
        bool cafeShown;

        public Toast Toast { get; private set; }
        public DialogueView Dialogue { get; private set; }
        public MemoryView Memory { get; private set; }
        public LetterView Letter { get; private set; }
        public SettingsView Settings { get; private set; }
        public CloudChoiceView CloudChoice { get; private set; }
        public AgeGateView AgeGate { get; private set; }
        public UpdateRequiredView UpdateRequired { get; private set; }
        public TutorialView Tutorial { get; private set; }
        public PromptView Prompt { get; private set; }
        public StorageView Storage { get; private set; }
#if GAHWIT_TOUR
        public Game TourGame => game; // the screen tour plays a few kitchen moves (Boot/ScreenTour.cs)
        public DecorRules TourDecor { get; private set; } // and hangs the wall pieces
#endif
        public Action OutOfEnergy;  // set by Boot: opens the out-of-energy card
        public Action BubbleTapped; // set by Boot: opens the bubble offer
        ItemLook look;
        HelpView help;
        ConfirmView confirm;
        DecorShopView decorShop;
        WardrobeView wardrobe;
        IAccountUi account;
        RectTransform tweens;

        // A panel over the game: the tutorial waits under it.
        public bool ModalOpen => Settings.IsOpen || help.gameObject.activeSelf || confirm.gameObject.activeSelf || CloudChoice.IsOpen
                                 || AgeGate.IsOpen || UpdateRequired.gameObject.activeSelf || Letter.IsOpen || Memory.IsPlaying
                                 || decorShop != null && decorShop.IsOpen || wardrobe != null && wardrobe.IsOpen
                                 || Prompt.IsOpen || Storage != null && Storage.IsOpen
                                 || Pan != null && Pan.IsOpen || RecipeBook != null && RecipeBook.IsOpen || Shop != null && Shop.IsOpen
                                 || Today != null && Today.IsOpen || MiniBoard != null && MiniBoard.IsOpen
                                 || Friends != null && Friends.IsOpen || hostingView != null && hostingView.IsOpen
                                 || wallView != null && wallView.IsOpen || visitBar != null
                                 || Journal != null && Journal.IsOpen;

        public static GameUi Create(Game game, CafeCamera cafeCamera, CafeRoom room, Func<long> now, Action openDrawer, IAccountUi account,
            TutorialFlow tutorial, bool canSkipTutorial)
        {
            EnsureEventSystem();
            var go = new GameObject("GameUI", typeof(RectTransform)) { layer = 5 };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();

            var ui = go.AddComponent<GameUi>();
            ui.Build(game, cafeCamera, room, now, openDrawer, account, tutorial, canSkipTutorial);
            return ui;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        void Build(Game game, CafeCamera cafeCamera, CafeRoom room, Func<long> now, Action openDrawer, IAccountUi account,
            TutorialFlow tutorial, bool canSkipTutorial)
        {
            this.game = game;
            this.cafeCamera = cafeCamera;
            look = new ItemLook(game);

            background = UiKit.Panel(transform, "Background", Palette.Blush, 0);
            background.rectTransform.Stretch();
            background.raycastTarget = false;

            var root = UiKit.Rect(transform, "SafeArea").Stretch();
            root.gameObject.AddComponent<SafeArea>();

            var screens = UiKit.Rect(root, "Screens").Stretch(0, 0, 124, 176);
            kitchen = UiKit.Rect(screens, "Kitchen").Stretch().gameObject.AddComponent<KitchenScreen>();
            cafe = UiKit.Rect(screens, "Cafe").Stretch().gameObject.AddComponent<CafeScreen>();

            hud = UiKit.Rect(root, "Hud").Top(0, 124).gameObject.AddComponent<Hud>();
            hud.Init(game, now);
            BuildTabs(UiKit.Rect(root, "Tabs").Bottom(0, 176));

            var dragLayer = UiKit.Rect(root, "DragLayer").Stretch();
            Memory = UiKit.Rect(root, "Memory").Stretch().gameObject.AddComponent<MemoryView>();
            Memory.Init(game.Config.LevelB.Memories);
            Dialogue = UiKit.Rect(root, "Dialogue").Stretch().gameObject.AddComponent<DialogueView>();
            Dialogue.Init(game);
            Letter = UiKit.Rect(root, "Letter").Stretch().gameObject.AddComponent<LetterView>();
            Letter.Init();
            Toast = UiKit.Rect(root, "Toast").Pin(new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(920, 84)).gameObject.AddComponent<Toast>();
            Toast.Init();
            tweens = UiKit.Rect(root, "Tweens").Stretch();
            tweens.gameObject.AddComponent<Tweens>().Init();

            this.account = account;
            cafe.ReducedMotion = () => account.GetSetting("reducedMotion");
            Tutorial = UiKit.Rect(root, "Tutorial").Stretch().gameObject.AddComponent<TutorialView>();
            Tutorial.Init(tutorial, this, game, canSkipTutorial);
            help = UiKit.Rect(root, "Help").Stretch().gameObject.AddComponent<HelpView>();
            help.Init(account, Toast);
            confirm = UiKit.Rect(root, "Confirm").Stretch().gameObject.AddComponent<ConfirmView>();
            confirm.Init();
            Settings = UiKit.Rect(root, "Settings").Stretch().gameObject.AddComponent<SettingsView>();
            Settings.Init(game, account, Toast, help, confirm);
            Settings.transform.SetSiblingIndex(help.transform.GetSiblingIndex());
            CloudChoice = UiKit.Rect(root, "CloudChoice").Stretch().gameObject.AddComponent<CloudChoiceView>();
            CloudChoice.Init();
            AgeGate = UiKit.Rect(root, "AgeGate").Stretch().gameObject.AddComponent<AgeGateView>();
            AgeGate.Init(account);
            Prompt = UiKit.Rect(root, "Prompt").Stretch().gameObject.AddComponent<PromptView>();
            Prompt.Init();
            UpdateRequired = UiKit.Rect(root, "UpdateRequired").Stretch().gameObject.AddComponent<UpdateRequiredView>();
            UpdateRequired.Init(Toast);
            KeepOnTop();

            kitchen.Init(game, look, this, now, dragLayer);
            cafe.Init(game, this, cafeCamera, room, openDrawer, look);

            game.Changed += Refresh;
            game.OrderServed += CoinsToCounter;
            Wallet.Changed += Refresh;
            ShowCafe(); // the 3D café is the main screen (COMPLETE_SPEC.md section 12)
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.Changed -= Refresh;
                game.OrderServed -= CoinsToCounter;
            }
            Wallet.Changed -= Refresh;
        }

        // Toasts and motion stay above every panel.
        void KeepOnTop()
        {
            Toast.transform.SetAsLastSibling();
            tweens.SetAsLastSibling();
        }

        // Order served (COMPLETE_SPEC.md section 8): coins burst from the order and fly to the counter.
        void CoinsToCounter(Order order)
        {
            if (cafeShown || Tweens.Instance == null) return;
            var card = kitchen.Orders.ServeButton(order.Id);
            Tweens.Instance.Sparkle(card, Palette.Gold, 10);
            Tweens.Instance.CoinsFly(card, hud.CoinsRect);
            Tweens.Instance.PopOf(hud.CoinsRect, 0.15f);
        }

        // Decor placed: a dust puff where the piece landed.
        public void DecorDust(Vector3 world)
        {
            if (!cafeShown || Tweens.Instance == null) return;
            var screen = cafeCamera.GetComponent<Camera>().WorldToScreenPoint(world);
            if (screen.z > 0) Tweens.Instance.Dust(screen);
        }

        // The way into a memory: a sepia wash spreads over the screen from the last touch.
        public void MemoryWipe()
        {
            var touch = UnityEngine.InputSystem.Pointer.current?.position.ReadValue() ?? new Vector2(Screen.width / 2f, Screen.height / 2f);
            Tweens.Instance?.Wipe(touch, Palette.Hex("#B98A5A"), 0.9f);
        }

        void BuildTabs(RectTransform tabs)
        {
            var bar = UiKit.Panel(tabs, "Bar", Palette.Card, 0);
            bar.rectTransform.Stretch();
            var row = UiKit.Row(bar.gameObject, 16);
            row.padding = new RectOffset(24, 24, 18, 26);
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            kitchenTab = UiKit.Button(bar.transform, "KitchenTab", Loc.T("tab.kitchen"), Palette.Rose, Color.white, 32, ShowKitchen, 34);
            cafeTab = UiKit.Button(bar.transform, "CafeTab", Loc.T("tab.cafe"), Palette.Card, Palette.Ink, 32, ShowCafe, 34);
            cafeBadge = UiKit.Icon(cafeTab.transform, "Badge", UiSprites.Circle, Palette.Rose, 28);
            cafeBadge.rectTransform.Pin(new Vector2(1, 1), new Vector2(-18, -10), new Vector2(28, 28));
        }

        // The intro's frame (tutorial step 2): the controls hidden, letterbox bars, a fade from black, and a skip button
        // when this phone has finished the tutorial before.
        GameObject cinematic;
        Image cinematicBlack;
        Button cinematicSkip;
        Action skipCinematic;

        public void SetCinematic(bool on, Action skip)
        {
            var safe = transform.Find("SafeArea");
            var group = safe.GetComponent<CanvasGroup>();
            if (group == null) group = safe.gameObject.AddComponent<CanvasGroup>();
            group.alpha = on ? 0f : 1f;
            group.blocksRaycasts = !on;
            if (cinematic == null)
            {
                var root = UiKit.Rect(transform, "Cinematic").Stretch();
                cinematic = root.gameObject;
                UiKit.Panel(root, "Top", Color.black, 0).rectTransform.Top(0, 240);
                UiKit.Panel(root, "Bottom", Color.black, 0).rectTransform.Bottom(0, 240);
                cinematicBlack = UiKit.Panel(root, "Black", Color.black, 0);
                cinematicBlack.rectTransform.Stretch();
                cinematicBlack.raycastTarget = false;
                cinematicSkip = UiKit.Button(root, "Skip", Loc.T("tutorial.skip"), new Color(1f, 1f, 1f, 0.18f), Color.white, 26,
                    () => skipCinematic?.Invoke(), 30);
                ((RectTransform)cinematicSkip.transform).Pin(new Vector2(0, 0), new Vector2(24, 54), new Vector2(220, UiKit.MinTouch));
            }
            skipCinematic = skip;
            cinematicSkip.gameObject.SetActive(skip != null);
            cinematic.SetActive(on);
            cinematic.transform.SetAsLastSibling();
        }

        public void CinematicFade(float black) => cinematicBlack.color = new Color(0f, 0f, 0f, black);

        public void ShowKitchen() => SetCafeShown(false);

        public void ShowCafe() => SetCafeShown(true);

        void SetCafeShown(bool show)
        {
            cafeShown = show;
            kitchen.gameObject.SetActive(!show);
            cafe.gameObject.SetActive(show);
            background.enabled = !show;
            cafeCamera.SetVisible(show);
            StyleTab(kitchenTab, !show);
            StyleTab(cafeTab, show);
            Refresh();
        }

        static void StyleTab(Button tab, bool active)
        {
            tab.targetGraphic.color = active ? Palette.Rose : Palette.Card;
            tab.Label().color = active ? Color.white : Palette.Ink;
        }

        // Decor comes once its rules exist (Boot.CafeScene): the shop sheet and the café's decor mode.
        public void SetDecor(DecorRules rules)
        {
#if GAHWIT_TOUR
            TourDecor = rules;
#endif
            decorShop = UiKit.Rect(transform.Find("SafeArea"), "DecorShop").Stretch().gameObject.AddComponent<DecorShopView>();
            decorShop.Init(rules, game, Toast);
            KeepOnTop();
            cafe.SetDecor(rules, decorShop);
        }

        // Storage needs gems from the platform to buy slots (Boot.GemShop).
        public void SetGems(Func<System.Threading.Tasks.Task<ActionResult>> buyStorageSlot)
        {
            Storage = UiKit.Rect(transform.Find("SafeArea"), "Storage").Stretch().gameObject.AddComponent<StorageView>();
            Storage.Init(game, look, Toast, buyStorageSlot);
            KeepOnTop();
        }

        // Recipes (missing/10): the pan in the kitchen and Teta's recipe book.
        public void SetRecipes(Gahwit.Recipes.RecipeBook book)
        {
            var bookView = UiKit.Rect(transform.Find("SafeArea"), "RecipeBook").Stretch().gameObject.AddComponent<RecipeBookView>();
            bookView.Init(book, game);
            Pan = UiKit.Rect(transform.Find("SafeArea"), "Pan").Stretch().gameObject.AddComponent<PanView>();
            Pan.Init(book, game, look, bookView);
            RecipeBook = bookView;
            KeepOnTop();
            kitchen.SetRecipes(book, Pan);
        }

        public PanView Pan { get; private set; }
        public ShopView Shop { get; private set; }
        public TodayView Today { get; private set; }
        public FriendsView Friends { get; private set; }
        HostingView hostingView;
        WallView wallView;
        GameObject visitBar;

        // Friends, hosting and the Sitti wall (missing/20).
        public void SetSocial(ISocialUi social, Func<long> now)
        {
            var safe = transform.Find("SafeArea");
            hostingView = UiKit.Rect(safe, "Hosting").Stretch().gameObject.AddComponent<HostingView>();
            hostingView.Init(social, Toast, now);
            wallView = UiKit.Rect(safe, "Wall").Stretch().gameObject.AddComponent<WallView>();
            wallView.Init(social, game, Toast);
            Friends = UiKit.Rect(safe, "Friends").Stretch().gameObject.AddComponent<FriendsView>();
            Friends.Init(social, game, Toast, hostingView, wallView);
            cafe.SetFriends(() => Friends.Show(), () => game.State.Level >= game.Config.Unlocks.Level("friends"));
            KeepOnTop();
        }

        // Visiting a friend's café: the café shows theirs, with only a way to leave a gift and go home.
        public void ShowVisit(Action gift, Action home)
        {
            if (visitBar != null) Destroy(visitBar);
            var bar = UiKit.Panel(transform.Find("SafeArea"), "Visit", Palette.Card, 0);
            bar.rectTransform.Bottom(0, 260);
            visitBar = bar.gameObject;
            UiKit.Text(bar.transform, "Text", Loc.T("visit.title"), 32, Palette.Ink, TMPro.TextAlignmentOptions.Center).rectTransform.Top(16, 60);
            var row = UiKit.Rect(bar.transform, "Buttons").Bottom(24, UiKit.MinTouch, 40, 40);
            var layout = UiKit.Row(row.gameObject, 20);
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            UiKit.Button(row, "Gift", Loc.T("visit.gift"), Palette.Rose, Color.white, 30, () => gift(), 36);
            UiKit.Button(row, "Home", Loc.T("visit.home"), Palette.Muted, Palette.Ink, 30, () => home(), 36);
            hud.gameObject.SetActive(false);
            transform.Find("SafeArea/Tabs").gameObject.SetActive(false);
            cafe.gameObject.SetActive(false);
            // Dragging turns the camera around their café, as at home.
            var pad = UiKit.Rect(transform.Find("SafeArea"), "VisitPad").Stretch(0, 0, 0, 260);
            pad.gameObject.AddComponent<Image>().color = Color.clear;
            pad.gameObject.AddComponent<OrbitPad>().Target = cafeCamera;
            pad.SetSiblingIndex(bar.transform.GetSiblingIndex());
            visitPad = pad.gameObject;
        }

        GameObject visitPad;

        public void HideVisit()
        {
            if (visitBar != null) Destroy(visitBar);
            if (visitPad != null) Destroy(visitPad);
            visitBar = visitPad = null;
            hud.gameObject.SetActive(true);
            transform.Find("SafeArea/Tabs").gameObject.SetActive(true);
            ShowCafe();
        }

        // A postcard (SYSTEMS_B.md section 8): the café as it is, with Salma, in a soft frame with the game's name.
        public void TakePostcard(Action<string> saved) => StartCoroutine(Postcard(saved));

        System.Collections.IEnumerator Postcard(Action<string> saved)
        {
            ShowCafe();
            var safe = transform.Find("SafeArea");
            var hidden = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in safe)
                if (child.gameObject.activeSelf)
                {
                    hidden.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            var frame = UiKit.Rect(safe, "Postcard").Stretch();
            foreach (var (anchorMin, anchorMax, size) in new[]
                     {
                         (new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 300)), (new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 40)),
                         (new Vector2(0, 0), new Vector2(0, 1), new Vector2(40, 0)), (new Vector2(1, 0), new Vector2(1, 1), new Vector2(40, 0))
                     })
            {
                var edge = UiKit.Panel(frame, "Edge", Palette.Cream, 0);
                edge.rectTransform.anchorMin = anchorMin;
                edge.rectTransform.anchorMax = anchorMax;
                edge.rectTransform.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
                edge.rectTransform.sizeDelta = size;
                edge.rectTransform.anchoredPosition = Vector2.zero;
            }
            var name = UiKit.Text(frame, "Name", Loc.T("postcard.name"), 64, Palette.Rose, TMPro.TextAlignmentOptions.Center);
            name.fontStyle = TMPro.FontStyles.Bold;
            name.rectTransform.Bottom(150, 100);
            UiKit.Text(frame, "Line", Loc.T("postcard.line"), 32, Palette.InkSoft, TMPro.TextAlignmentOptions.Center).rectTransform.Bottom(70, 70);
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            Destroy(frame.gameObject);
            foreach (var go in hidden) go.SetActive(true);
            string path = System.IO.Path.Combine(Application.persistentDataPath, "gahwit-postcard.png");
            System.IO.File.WriteAllBytes(path, shot.EncodeToPNG());
            Destroy(shot);
            UiSound.Play("ui_coin");
            saved(path);
        }
        public MiniBoardView MiniBoard { get; private set; }
        public JournalView Journal { get; private set; }

        // The story journal (missing/11), from the café's side buttons.
        public void SetJournal(Func<string, string, string> choiceText, Action<string> replay, Action<string> reread,
            Func<List<string>> clues)
        {
            Journal = UiKit.Rect(transform.Find("SafeArea"), "Journal").Stretch().gameObject.AddComponent<JournalView>();
            Journal.Init(game, choiceText, replay, reread, clues);
            cafe.SetJournal(() => Journal.Show());
            KeepOnTop();
        }

        // Today and the events (missing/18): the daily gift and tasks, and the mini board.
        public void SetLiveOps(ILiveEventsUi events, Func<string> today, Func<long> now, Action claimed, Action<MiniBoard> paid)
        {
            var safe = transform.Find("SafeArea");
            MiniBoard = UiKit.Rect(safe, "MiniBoard").Stretch().gameObject.AddComponent<MiniBoardView>();
            MiniBoard.Init(game, now, paid);
            Today = UiKit.Rect(safe, "Today").Stretch().gameObject.AddComponent<TodayView>();
            Today.Init(game, events, today, now, claimed, id =>
            {
                var board = events.Board(id);
                if (board == null) return;
                var info = events.Running.FirstOrDefault(e => e.Id == id);
                MiniBoard.Show(board, info?.Settings.Value<string>(game.English ? "nameEn" : "name") ?? Loc.T("event.miniBoard"));
            });
            cafe.SetToday(() => Today.Show());
            Pass = UiKit.Rect(safe, "Pass").Stretch().gameObject.AddComponent<PassView>();
            Pass.Init(game, claimed);
            if (shopUi != null) Pass.Shop = shopUi;
            Today.OpenPass = () => Pass.Show();
            KeepOnTop();
        }

        public PassView Pass { get; private set; }
        IShopUi shopUi;
        Button adOffer;
        float adOfferUntil;

        // The shop (missing/17): from the HUD's gems, from the shop's level.
        public void SetShop(IShopUi shop, System.Func<System.Threading.Tasks.Task<ActionResult>> restore)
        {
            Shop = UiKit.Rect(transform.Find("SafeArea"), "Shop").Stretch().gameObject.AddComponent<ShopView>();
            Shop.Init(shop, game, Toast);
            shopUi = shop;
            if (Pass != null) Pass.Shop = shop;
            hud.GemsTapped = () =>
            {
                if (game.AdsOpen) Shop.Show();
                else Toast.Show(Loc.T("shop.later", game.Config.Unlocks.NoShopOrAdsBeforeLevel));
            };
            Settings.Restore = restore;
            KeepOnTop();
        }

        // A short-lived offer over the screen, such as doubling a served order's coins with an ad.
        public void OfferAd(string label, System.Action act, float seconds)
        {
            if (adOffer == null)
            {
                adOffer = UiKit.Button(transform.Find("SafeArea"), "AdOffer", "", Palette.Teal, Color.white, 28, null, 40);
                ((RectTransform)adOffer.transform).Pin(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(560, UiKit.MinTouch));
            }
            adOffer.onClick.RemoveAllListeners();
            adOffer.onClick.AddListener(() => UiSound.Play("ui_tap"));
            adOffer.onClick.AddListener(() =>
            {
                adOffer.gameObject.SetActive(false);
                act();
            });
            adOffer.Label().text = label;
            adOffer.gameObject.SetActive(true);
            adOffer.transform.SetAsLastSibling();
            adOfferUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (adOffer != null && adOffer.gameObject.activeSelf && Time.unscaledTime > adOfferUntil) adOffer.gameObject.SetActive(false);
        }
        public RecipeBookView RecipeBook { get; private set; }

        public void SetWardrobe(WardrobeRules rules)
        {
            wardrobe = UiKit.Rect(transform.Find("SafeArea"), "Wardrobe").Stretch().gameObject.AddComponent<WardrobeView>();
            wardrobe.Init(rules, game, Toast);
            KeepOnTop();
            cafe.SetWardrobe(rules, wardrobe);
        }

        // What the tutorial's pointer shows for a step: one target, or two for a drag (from, to). On the wrong screen
        // it points at the tab that leads to the right one.
        public (RectTransform from, RectTransform to) TutorialTarget(TutorialStep step)
        {
            if (step.Screen == "kitchen" && cafeShown) return (null, (RectTransform)kitchenTab.transform);
            if (step.Screen == "cafe" && !cafeShown) return (null, (RectTransform)cafeTab.transform);
            var board = game.State.Board;
            switch (step.Target)
            {
                case "board.pair":
                    for (int a = 0; a < board.Length; a++)
                    {
                        if (board[a] == null || board[a].IsGenerator) continue;
                        for (int b = a + 1; b < board.Length; b++)
                            if (board[a].SameAs(board[b])) return (kitchen.Board.CellRect(a), kitchen.Board.CellRect(b));
                    }
                    return (null, null);
                case "board.generator":
                    int generator = Array.FindIndex(board, i => i != null && i.IsGenerator);
                    return (null, generator >= 0 ? kitchen.Board.CellRect(generator) : null);
                case "order.serve":
                    foreach (var order in game.Orders)
                        if (game.CanServe(order)) return (null, kitchen.Orders.ServeButton(order.Id));
                    return (null, null);
                case "cafe.restore":
                    return (null, cafe.RestoreButton);
                default:
                    return (null, null);
            }
        }

        public void Refresh()
        {
            hud.ShowEnergy(game.State.TutorialDone || game.State.TutorialStep >= 4);
            hud.Refresh();
            if (cafeShown) cafe.Refresh();
            else kitchen.Refresh();
            cafeBadge.enabled = !cafeShown && game.CanAffordTask;
        }
    }
}
