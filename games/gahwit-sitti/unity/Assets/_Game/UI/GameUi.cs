using System;
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
        HelpView help;
        ConfirmView confirm;
        DecorShopView decorShop;
        WardrobeView wardrobe;
        IAccountUi account;

        // A panel over the game: the tutorial waits under it.
        public bool ModalOpen => Settings.IsOpen || help.gameObject.activeSelf || confirm.gameObject.activeSelf || CloudChoice.IsOpen
                                 || AgeGate.IsOpen || UpdateRequired.gameObject.activeSelf || Letter.IsOpen || Memory.IsPlaying
                                 || decorShop != null && decorShop.IsOpen || wardrobe != null && wardrobe.IsOpen;

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
            var look = new ItemLook(game);

            background = UiKit.Panel(transform, "Background", Palette.Blush, 0);
            background.rectTransform.Stretch();
            background.raycastTarget = false;

            var root = UiKit.Rect(transform, "SafeArea").Stretch();
            root.gameObject.AddComponent<SafeArea>();

            var screens = UiKit.Rect(root, "Screens").Stretch(0, 0, 124, 140);
            kitchen = UiKit.Rect(screens, "Kitchen").Stretch().gameObject.AddComponent<KitchenScreen>();
            cafe = UiKit.Rect(screens, "Cafe").Stretch().gameObject.AddComponent<CafeScreen>();

            hud = UiKit.Rect(root, "Hud").Top(0, 124).gameObject.AddComponent<Hud>();
            hud.Init(game, now);
            BuildTabs(UiKit.Rect(root, "Tabs").Bottom(0, 140));

            var dragLayer = UiKit.Rect(root, "DragLayer").Stretch();
            Memory = UiKit.Rect(root, "Memory").Stretch().gameObject.AddComponent<MemoryView>();
            Memory.Init();
            Dialogue = UiKit.Rect(root, "Dialogue").Stretch().gameObject.AddComponent<DialogueView>();
            Dialogue.Init(game);
            Letter = UiKit.Rect(root, "Letter").Stretch().gameObject.AddComponent<LetterView>();
            Letter.Init();
            Toast = UiKit.Rect(root, "Toast").Pin(new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(920, 84)).gameObject.AddComponent<Toast>();
            Toast.Init();

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
            UpdateRequired = UiKit.Rect(root, "UpdateRequired").Stretch().gameObject.AddComponent<UpdateRequiredView>();
            UpdateRequired.Init(Toast);
            Toast.transform.SetAsLastSibling();

            kitchen.Init(game, look, this, now, dragLayer);
            cafe.Init(game, this, cafeCamera, room, openDrawer, look);

            game.Changed += Refresh;
            Wallet.Changed += Refresh;
            ShowCafe(); // the 3D café is the main screen (COMPLETE_SPEC.md section 12)
        }

        void OnDestroy()
        {
            if (game != null) game.Changed -= Refresh;
            Wallet.Changed -= Refresh;
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
            decorShop = UiKit.Rect(transform.Find("SafeArea"), "DecorShop").Stretch().gameObject.AddComponent<DecorShopView>();
            decorShop.Init(rules, game, Toast);
            Toast.transform.SetAsLastSibling();
            cafe.SetDecor(rules, decorShop);
        }

        public void SetWardrobe(WardrobeRules rules)
        {
            wardrobe = UiKit.Rect(transform.Find("SafeArea"), "Wardrobe").Stretch().gameObject.AddComponent<WardrobeView>();
            wardrobe.Init(rules, game, Toast);
            Toast.transform.SetAsLastSibling();
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
