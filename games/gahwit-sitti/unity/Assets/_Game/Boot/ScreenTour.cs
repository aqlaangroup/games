#if GAHWIT_TOUR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
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
            var room = FindFirstObjectByType<Gahwit.Cafe.CafeRoom>();

            // The intro (tutorial step 2), shot at its main moments.
            ui.SetCinematic(true, null);
            StartCoroutine(room.Intro(false, _ => { }, ui.CinematicFade));
            float played = 0f;
            foreach (var (at, shot) in new[] { (1.5f, "00a_intro_dark"), (4.6f, "00b_intro_shutter"), (6.8f, "00c_intro_door"), (9.8f, "00d_intro_room"), (13.5f, "00e_intro_counter") })
            {
                yield return Wait(at - played);
                played = at;
                yield return Shot(shot);
            }
            yield return Wait(Gahwit.Cafe.CafeRoom.IntroSeconds - played + 0.5f);
            ui.SetCinematic(false, null);

            // The wall pieces (missing/07), bought and hung where they go, then taken down again.
            var hung = new List<string> { "old_hebron_plates", "old_family_frames", "modern_wall_clock", "modern_arch_mirror", "gulf_sadu_weaving", "gulf_finjan_shelf" };
            var decorRules = ui.TourDecor;
            if (decorRules != null)
            {
                ui.TourGame.State.Coins += 10_000;
                foreach (var id in hung) decorRules.Buy(id);
                yield return Wait(1.5f);
                yield return Shot("01a_wall_decor");
                foreach (var id in hung) decorRules.Store(id);

                // The newest pieces: the five shop ones, every event reward and the Eid set, as if all were won.
                var state = ui.TourGame.State;
                foreach (var def in decorRules.Catalog.Pieces.Where(p => p.Source == "event"))
                    state.Entitlements.Add("decor:" + def.Id);
                state.Entitlements.Add("decorSet:eid_lanterns");
                var bought = new List<string> { "old_bird_cage", "gulf_majlis_sofa", "modern_bookshelf", "old_brass_tray_table", "gulf_palm_planter" };
                foreach (var id in bought) decorRules.Buy(id);
                ui.TourGame.Touch();
                yield return Wait(1.5f);
                yield return Shot("01aa_new_decor");
                foreach (var def in decorRules.Catalog.Pieces.Where(p => p.Source == "event" || p.Source == "premium" || bought.Contains(p.Id)))
                    decorRules.Store(def.Id);
            }

            // The café's other spaces (missing/07), then back to the floor.
            foreach (var (space, shot) in new[] { ("kitchen", "01b_kitchen_corner"), ("rooftop", "01c_rooftop"), ("backroom", "01d_back_room") })
            {
                room.ShowSpace(space, true);
                yield return Wait(1.2f);
                yield return Shot(shot);
            }
            // The rooftop as it looks once chapter 6 is done: its string of lights and the opening night's bunting.
            var tourGame = ui.TourGame;
            room.Apply(tourGame.Content.Chapters.SelectMany(c => c.Tasks).Select(t => t.Id).ToList(),
                new Dictionary<string, string> { ["sign"] = "old", ["chairs"] = "old", ["rooftop_lights"] = "old" }, tourGame.Content.Styles);
            room.ShowSpace("rooftop", true);
            yield return Wait(1.2f);
            yield return Shot("01e_rooftop_finished");

            // The same spaces on a morning, whatever the clock says now.
            room.SetTimeOfDay(Gahwit.Cafe.TimeOfDay.Morning);
            foreach (var (space, shot) in new[] { ("kitchen", "01f_kitchen_morning"), ("rooftop", "01g_rooftop_morning"), ("backroom", "01h_back_room_morning") })
            {
                room.ShowSpace(space, true);
                yield return Wait(1.2f);
                yield return Shot(shot);
            }
            room.SetTimeOfDay(Gahwit.Cafe.CafeRoom.TimeFromClock(DateTime.Now, tourGame.Config.LevelB.Cafe.TimeOfDayStartHours));
            room.Apply(tourGame.CompletedTaskIds(), tourGame.State.TaskStyles, tourGame.Content.Styles);
            room.ShowSpace(ChapterContent.FloorSpace, true);
            yield return Wait(0.5f);
            ui.ShowKitchen();
            yield return Wait(1.5f);
            yield return Shot("02_kitchen");

            // The kitchen's voices: a served customer's thanks, the next customer's line, a streak, a new notebook page.
            var game = ui.TourGame;
            float line = game.Config.LevelB.Kitchen.BarkSeconds;
            ServeFirst(game);
            yield return Wait(0.8f);
            yield return Shot("02b_kitchen_thanks");
            yield return Wait(line);
            yield return Shot("02c_kitchen_ask");
            yield return Wait(line);
            ServeFirst(game);
            yield return Wait(0.8f);
            yield return Shot("02d_kitchen_streak");
            MakeNewItem(game);
            yield return Wait(0.8f);
            yield return Shot("02e_kitchen_page");
            yield return Wait(line * 2);

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

        // Puts what the first order wants on the board and serves it.
        static void ServeFirst(Game game)
        {
            if (game.Orders.Count == 0) return;
            var order = game.Orders[0];
            foreach (var line in order.Lines)
                for (int n = 0; n < line.Count; n++) Put(game, line.Chain, line.Level);
            game.Serve(order.Id);
        }

        // Merges two of the lowest item whose next level has no notebook page yet, so its page opens.
        static void MakeNewItem(Game game)
        {
            foreach (var chain in game.Config.Chains)
            {
                if (chain.Value.UnlockChapter > game.State.Chapter) continue;
                for (int level = 2; level <= game.MaxLevel(chain.Key); level++)
                {
                    if (game.HasPage(chain.Key, level)) continue;
                    int a = Put(game, chain.Key, level - 1), b = Put(game, chain.Key, level - 1);
                    if (a >= 0 && b >= 0) game.Move(a, b);
                    return;
                }
            }
        }

        static int Put(Game game, string chain, int level)
        {
            int cell = Array.FindIndex(game.State.Board, item => item == null);
            if (cell < 0) return -1;
            game.State.Board[cell] = new Item(chain, level);
            game.Touch();
            return cell;
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
