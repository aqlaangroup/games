using System;
using Gahwit.Cafe;
using Gahwit.Core;
using Gahwit.Merge;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Gahwit.UI
{
    // The whole interface, built in code: HUD, kitchen and café screens, tabs, dialogue and toasts.
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

        public static GameUi Create(Game game, CafeCamera cafeCamera, Func<long> now)
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
            ui.Build(game, cafeCamera, now);
            return ui;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        void Build(Game game, CafeCamera cafeCamera, Func<long> now)
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
            Dialogue = UiKit.Rect(root, "Dialogue").Stretch().gameObject.AddComponent<DialogueView>();
            Dialogue.Init(game);
            Toast = UiKit.Rect(root, "Toast").Pin(new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(920, 84)).gameObject.AddComponent<Toast>();
            Toast.Init();

            kitchen.Init(game, look, this, now, dragLayer);
            cafe.Init(game, this, cafeCamera);

            game.Changed += Refresh;
            ShowCafe(); // the 3D café is the main screen (COMPLETE_SPEC.md section 12)
        }

        void OnDestroy()
        {
            if (game != null) game.Changed -= Refresh;
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

        public void Refresh()
        {
            hud.Refresh();
            if (cafeShown) cafe.Refresh();
            else kitchen.Refresh();
            cafeBadge.enabled = !cafeShown && game.CanAffordTask;
        }
    }
}
