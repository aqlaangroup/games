#if GAHWIT_TOUR
using System;
using System.Collections;
using System.IO;
using Gahwit.Core;
using Gahwit.Story;
using Gahwit.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gahwit.Boot
{
    // Simulator builds only (BuildTools.BuildIosSimulator defines GAHWIT_TOUR): once the café is up, opens each screen
    // in turn and saves a screenshot to persistentDataPath/tour, so every page can be checked by eye. Never in a store
    // build.
    public sealed class ScreenTour : MonoBehaviour
    {
        string folder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            var tour = new GameObject("ScreenTour").AddComponent<ScreenTour>();
            DontDestroyOnLoad(tour.gameObject);
        }

        IEnumerator Start()
        {
            folder = Path.Combine(Application.persistentDataPath, "tour");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);

            GameUi ui = null;
            while (ui == null)
            {
                ui = FindFirstObjectByType<GameUi>();
                yield return null;
            }
            yield return Wait(6f); // customers walk in and the panels settle
            yield return Shot("00_daily_gift");
            ui.Prompt.Close();
            yield return Wait(1f);
            string language = UiKit.LeftToRight ? "en" : "ar";

            yield return Shot("01_cafe");
            ui.ShowKitchen();
            yield return Wait(1.5f);
            yield return Shot("02_kitchen");
            yield return Panel<StorageView>("03_storage", view => view.Show());
            yield return Panel<PanView>("04_pan", view => view.Show());
            yield return Panel<RecipeBookView>("05_recipe_book", view => view.Show());
            ui.ShowCafe();
            yield return Wait(1.5f);

            yield return Panel<SettingsView>("06_settings", view => view.Show());
            yield return Panel<JournalView>("07_journal", view => view.Show());
            yield return Panel<TodayView>("08_today", view => view.Show());
            yield return Panel<ShopView>("09_shop", view => view.Show());
            yield return Panel<DecorShopView>("10_decor_shop", view => view.Show());
            yield return Panel<WardrobeView>("11_wardrobe", view => view.Show());
            yield return Panel<FriendsView>("12_friends", view => view.Show());
            yield return Panel<HostingView>("13_hosting", view => view.Show());
            yield return Panel<WallView>("14_sitti_wall", view => view.Show());
            yield return Panel<HelpView>("15_help", view => view.Show());

            // A scene: its first line, then its choices.
            var story = StoryPlayer.FromResources(StoryPlayer.PathFor("Story/chapter1", language));
            story.Glossary = Loc.Gloss;
            ui.Dialogue.Play(story, "dallahs", null);
            yield return Wait(1f);
            yield return Shot("16_dialogue");
            Next(ui);
            Next(ui);
            yield return Wait(1f);
            yield return Shot("17_dialogue_choices");
            ui.Dialogue.gameObject.SetActive(false);

            // The same kind of scene with neutral-Arabic subtitles and a tap-to-explain word.
            if (language == "ar")
            {
                var subtitled = StoryPlayer.FromResources("Story/chapter1");
                subtitled.Glossary = Loc.Gloss;
                subtitled.Subtitles = StoryPlayer.FromResources(StoryPlayer.SubtitlesPath("Story/chapter1"));
                ui.Dialogue.Play(subtitled, "sign", null);
                yield return Wait(1f);
                yield return Shot("18_dialogue_subtitles");
                ui.Dialogue.gameObject.SetActive(false);
            }

            var letters = StoryPlayer.FromResources(StoryPlayer.PathFor("Story/chapter1", language));
            var message = letters.ReadAll("lana_notify")[0];
            ui.Letter.Play(letters.ReadAll("letter_1"), letters.ReadAll("voice_1")[0], null, message, Loc.T("char." + message.Speaker), null);
            yield return Wait(7f); // the letter's lines appear one by one
            yield return Shot("19_letter");
            ui.Letter.gameObject.SetActive(false);

            ui.AgeGate.Show(language, (_, __) => { });
            yield return Wait(1f);
            yield return Shot("20_age_gate");
            ui.AgeGate.gameObject.SetActive(false);

            File.WriteAllText(Path.Combine(folder, "done.txt"), DateTime.UtcNow.ToString("o"));
            Debug.Log("[Tour] Done: " + folder);

            // A test message and a test exception for the crash reporter (missing/16), environment "tour" in GlitchTip.
            Sentry.Unity.SentrySdk.CaptureMessage("Screen tour: crash reporter test message");
            throw new InvalidOperationException("Screen tour: crash reporter test exception");
        }

        IEnumerator Panel<T>(string name, Action<T> show) where T : MonoBehaviour
        {
            var view = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.Log($"[Tour] No {typeof(T).Name}.");
                yield break;
            }
            show(view);
            yield return Wait(1.2f);
            yield return Shot(name);
            view.gameObject.SetActive(false);
            yield return Wait(0.3f);
        }

        static void Next(GameUi ui)
        {
            var next = ui.Dialogue.transform.Find("Card/Next");
            if (next != null && next.gameObject.activeInHierarchy) next.GetComponent<Button>().onClick.Invoke();
        }

        static IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), shot.EncodeToPNG());
            Destroy(shot);
            Debug.Log("[Tour] " + name);
        }
    }
}
#endif
