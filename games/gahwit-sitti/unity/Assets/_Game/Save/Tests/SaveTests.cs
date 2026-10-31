using System.IO;
using System.Linq;
using Aqlaan.CozyKit;
using Gahwit.Core;
using Gahwit.Merge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Gahwit.Save.Tests
{
    public sealed class SaveTests
    {
        const long Now = 1_793_372_400_000;

        BalanceConfig config;
        ChapterContent content;

        sealed class FakeClock : IDeviceClock
        {
            public long UtcNowMs { get; set; } = Now;
            public long UptimeMs { get; set; } = 50_000;
        }

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
        }

        string WriteJson(GameState state, TimeService time)
        {
            var data = new SaveData();
            SaveCodec.Write(state, time.State, config, data);
            return JsonConvert.SerializeObject(data, SaveCodec.Json);
        }

        GameState PlayedState()
        {
            var game = new Game(config, content, Game.NewState(config, content, 99, Now));
            int generator = game.State.Board.ToList().FindIndex(p => p != null && p.Generator == "gen_coffee");
            game.TapGenerator(generator, Now, out _);
            game.State.Coins = 40;
            game.CompleteTask("old");
            game.State.Choices["dallahs"] = "ask";
            game.State.ScenesSeen.Add("intro");
            game.RecordMemory("m1_pour", 3);
            game.FindLetter("letter_1");
            return game.State;
        }

        [Test]
        public void ASaveLoadsBackToTheSameState()
        {
            var time = new TimeService(new FakeClock(), null);
            var state = PlayedState();
            var loaded = SaveService.FromJson(WriteJson(state, time), config, content, out _, out var clock);

            for (int i = 0; i < state.Board.Length; i++)
            {
                var a = state.Board[i];
                var b = loaded.Board[i];
                Assert.AreEqual(a == null, b == null, $"cell {i}");
                if (a != null) Assert.IsTrue(a.SameAs(b), $"cell {i}");
            }
            Assert.AreEqual(state.Coins, loaded.Coins);
            Assert.AreEqual(state.Energy, loaded.Energy);
            Assert.AreEqual(state.EnergyStampMs, loaded.EnergyStampMs);
            Assert.AreEqual(state.Level, loaded.Level);
            Assert.AreEqual(state.Xp, loaded.Xp);
            Assert.AreEqual(state.TaskIndex, loaded.TaskIndex);
            Assert.AreEqual("old", loaded.TaskStyles["sign"]);
            Assert.AreEqual("ask", loaded.Choices["dallahs"]);
            Assert.AreEqual(3, loaded.MemoryStars["m1_pour"]);
            CollectionAssert.Contains(loaded.LettersFound, "letter_1");
            Assert.AreEqual(state.Rng, loaded.Rng);
            Assert.AreEqual(state.Orders.Count, loaded.Orders.Count);
            for (int n = 0; n < state.Orders.Count; n++)
            {
                Assert.AreEqual(state.Orders[n].Id, loaded.Orders[n].Id);
                Assert.AreEqual(state.Orders[n].Coins, loaded.Orders[n].Coins);
                Assert.AreEqual(state.Orders[n].Units, loaded.Orders[n].Units);
                Assert.AreEqual(state.Orders[n].IsCharacter, loaded.Orders[n].IsCharacter);
            }
            Assert.AreEqual(time.State.TrustedMs, clock.TrustedMs);
        }

        [Test]
        public void TheSaveHasEveryFieldOfTheDesignSchemaAndNoGems()
        {
            string schemaPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../design/save_schema.json"));
            var schema = JObject.Parse(File.ReadAllText(schemaPath));
            var save = JObject.Parse(WriteJson(PlayedState(), new TimeService(new FakeClock(), null)));

            foreach (var key in schema.Properties().Select(p => p.Name).Where(k => !k.StartsWith("$")))
                Assert.IsTrue(save.ContainsKey(key), $"missing top-level '{key}'");
            foreach (var section in new[] { "player", "board", "story", "cafe", "tutorial", "settings", "clock" })
            foreach (var key in ((JObject)schema[section]).Properties().Select(p => p.Name))
                Assert.IsTrue(((JObject)save[section]).ContainsKey(key) || key == "lastServerTime", $"missing '{section}.{key}'");

            StringAssert.DoesNotContain("gems", save.ToString().ToLowerInvariant());
            Assert.AreEqual(SaveData.CurrentSchema, save.Value<int>("schemaVersion"));
        }

        [Test]
        public void TheFirstPlayablesSaveMigratesToTheCurrentVersion()
        {
            // A save written by the first playable on 2026-10-30, before the save schema was followed.
            const string old = @"{
              ""Version"": 1,
              ""Board"": [null, {""Chain"":""coffee"",""Level"":2}, null, {""Chain"":""sweets"",""Level"":1}],
              ""Generators"": [{""Id"":""gen_coffee"",""Chain"":""coffee"",""Level"":2}, {""Id"":""gen_sweets"",""Chain"":""sweets"",""Level"":1}],
              ""Energy"": 73, ""EnergyStamp"": 1793372000, ""Coins"": 55, ""Gems"": 9, ""Xp"": 40, ""Level"": 2,
              ""Orders"": [{""Id"":7,""Customer"":""abufadi"",""IsCharacter"":true,""Lines"":[{""Chain"":""coffee"",""Level"":2,""Count"":2}],""Units"":4,""Coins"":6,""Xp"":4}],
              ""NextOrderId"": 8, ""OrdersServed"": 3, ""Chapter"": 1, ""TaskIndex"": 2,
              ""CompletedTasks"": [""sign"", ""floor""], ""IntroSeen"": true, ""Rng"": 12345
            }";

            var state = SaveService.FromJson(old, config, content, out var data, out _);

            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.IsTrue(state.Board[1].Is("coffee", 2));
            Assert.IsTrue(state.Board[3].Is("sweets", 1));
            var coffeeGenerator = state.Board.First(p => p != null && p.Generator == "gen_coffee");
            Assert.AreEqual(2, coffeeGenerator.Level);
            Assert.IsTrue(state.Board.Any(p => p != null && p.Generator == "gen_sweets"));
            Assert.AreEqual(73, state.Energy);
            Assert.AreEqual(1793372000L * 1000, state.EnergyStampMs);
            Assert.AreEqual(55, state.Coins);
            Assert.AreEqual(2, state.Level);
            Assert.AreEqual(3, state.OrdersServed);
            Assert.AreEqual(2, state.TaskIndex);
            CollectionAssert.AreEquivalent(new[] { "intro", "sign", "floor" }, state.ScenesSeen);
            Assert.AreEqual("abu_fadi", state.Orders[0].Customer);
            Assert.AreEqual(2, state.Orders[0].Lines[0].Count);
            Assert.AreEqual(8, state.NextOrderId);
            Assert.AreEqual(12345UL, state.Rng);
        }

        [Test]
        public void AVersionOneSaveGetsTheAccountSectionAndKeepsEverythingElse()
        {
            var time = new TimeService(new FakeClock(), null);
            var v1 = JObject.Parse(WriteJson(PlayedState(), time));
            v1["schemaVersion"] = 1;
            v1.Remove("account");
            var settings = (JObject)v1["settings"];
            settings["notifications"] = false; // v1 had one switch
            settings.Remove("voice");
            settings["music"] = false;

            var state = SaveService.FromJson(v1.ToString(), config, content, out var data, out _);

            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.IsNull(data.Account.BirthYear, "the age gate shows again");
            Assert.IsFalse(data.Account.ParentConsent);
            Assert.IsFalse(data.Settings.Notifications.EnergyFull, "each type starts as the old switch was");
            Assert.IsFalse(data.Settings.Notifications.Away);
            Assert.IsFalse(data.Settings.Voice);
            Assert.IsFalse(data.Settings.Music, "existing settings are kept");
            Assert.AreEqual("old", state.TaskStyles["sign"]);
            Assert.AreEqual(3, state.MemoryStars["m1_pour"]);
        }

        [Test]
        public void AVersionTwoSaveStartsWithNoEasyOrdersLeft()
        {
            var time = new TimeService(new FakeClock(), null);
            var state = PlayedState();
            state.EasyOrdersLeft = 2;
            var v2 = JObject.Parse(WriteJson(state, time));
            Assert.AreEqual(2, (int)v2["tutorial"]["easyOrdersLeft"], "written by the current version");
            v2["schemaVersion"] = 2;
            ((JObject)v2["tutorial"]).Remove("easyOrdersLeft");

            var loaded = SaveService.FromJson(v2.ToString(), config, content, out var data, out _);

            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.AreEqual(0, loaded.EasyOrdersLeft);
            Assert.AreEqual(state.TutorialStep, loaded.TutorialStep);
        }

        [Test]
        public void AVersionSevenSaveStartsWithTheMemoryAssistOff()
        {
            var time = new TimeService(new FakeClock(), null);
            var v7 = JObject.Parse(WriteJson(PlayedState(), time));
            v7["schemaVersion"] = 7;
            var settings = (JObject)v7["settings"];
            settings.Remove("memoryAssist");
            settings["largeText"] = true;

            SaveService.FromJson(v7.ToString(), config, content, out var data, out _);

            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.IsFalse(data.Settings.MemoryAssist);
            Assert.IsTrue(data.Settings.LargeText, "existing settings are kept");
        }

        [Test]
        public void NotebookPagesAndStoryOrdersRoundTripAndAVersionTwelveSaveGetsThem()
        {
            var time = new TimeService(new FakeClock(), null);
            var state = PlayedState();
            state.NotebookPages.Add("coffee_3");
            state.StoryOrdersDone.Add("c1_first");
            state.Orders[0].Story = "c1_second";
            var loaded = SaveService.FromJson(WriteJson(state, time), config, content, out _, out _);
            CollectionAssert.AreEqual(state.NotebookPages, loaded.NotebookPages);
            CollectionAssert.Contains(loaded.NotebookPages, "coffee_3");
            CollectionAssert.AreEqual(new[] { "c1_first" }, loaded.StoryOrdersDone);
            Assert.AreEqual("c1_second", loaded.Orders[0].Story);

            var v12 = JObject.Parse(WriteJson(PlayedState(), time));
            v12["schemaVersion"] = 12;
            v12.Remove("kitchen");
            ((JObject)v12["story"]).Remove("ordersDone");
            SaveService.FromJson(v12.ToString(), config, content, out var data, out _);
            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.AreEqual(0, data.Kitchen.Notebook.Count);
            Assert.AreEqual(0, data.Story.OrdersDone.Count);
        }

        [Test]
        public void ThePassSeasonAndItsClaimsAreSavedAndAVersionThirteenSaveGetsThem()
        {
            var time = new TimeService(new FakeClock(), null);
            var state = PlayedState();
            state.PassPoints = 1200;
            state.PassSeason = 2;
            state.PassClaimed.Add("free:1");
            state.PassClaimed.Add("premium:1");
            state.PassPremiumSeason = 2;
            var loaded = SaveService.FromJson(WriteJson(state, time), config, content, out _, out _);
            Assert.AreEqual(1200, loaded.PassPoints);
            Assert.AreEqual(2, loaded.PassSeason);
            CollectionAssert.AreEqual(new[] { "free:1", "premium:1" }, loaded.PassClaimed);
            Assert.AreEqual(2, loaded.PassPremiumSeason);

            var v13 = JObject.Parse(WriteJson(PlayedState(), time));
            v13["schemaVersion"] = 13;
            var daily = (JObject)v13["daily"];
            daily.Remove("passSeason");
            daily.Remove("passClaimed");
            daily.Remove("passPremiumSeason");
            var old = SaveService.FromJson(v13.ToString(), config, content, out var data, out _);
            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.AreEqual(0, old.PassSeason, "a save from before seasons: its points count toward the season found on the first tick");
            Assert.AreEqual(0, old.PassClaimed.Count);
            Assert.AreEqual(0, old.PassPremiumSeason);
        }

        [Test]
        public void AVersionElevenSaveStartsWithSubtitlesOff()
        {
            var time = new TimeService(new FakeClock(), null);
            var v11 = JObject.Parse(WriteJson(PlayedState(), time));
            v11["schemaVersion"] = 11;
            var settings = (JObject)v11["settings"];
            settings.Remove("subtitles");
            settings["glossary"] = false;

            SaveService.FromJson(v11.ToString(), config, content, out var data, out _);

            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.IsFalse(data.Settings.Subtitles);
            Assert.IsFalse(data.Settings.Glossary, "existing settings are kept");
        }

        [Test]
        public void AVersionEightSaveGetsAnEmptyPanAndRecipesRoundTrip()
        {
            var time = new TimeService(new FakeClock(), null);
            var state = PlayedState();
            state.RecipesFound.Add("coffee_kaak");
            state.RecipeWrongStreak = 2;
            state.Pan.Add(new Item("coffee", 3));
            state.Orders[0].Recipe = "coffee_kaak";
            var loaded = SaveService.FromJson(WriteJson(state, time), config, content, out _, out _);
            CollectionAssert.AreEqual(new[] { "coffee_kaak" }, loaded.RecipesFound);
            Assert.AreEqual(2, loaded.RecipeWrongStreak);
            Assert.IsTrue(loaded.Pan.Single().Is("coffee", 3));
            Assert.AreEqual("coffee_kaak", loaded.Orders[0].Recipe);

            var v8 = JObject.Parse(WriteJson(PlayedState(), time));
            v8["schemaVersion"] = 8;
            v8["recipes"] = new JObject();
            SaveService.FromJson(v8.ToString(), config, content, out var data, out _);
            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.AreEqual(0, ((JArray)data.Recipes["pan"]).Count);
            Assert.AreEqual(0, (int)data.Recipes["wrongStreak"]);
        }

        [Test]
        public void AdCountsAndAppliedPurchasesRoundTripAndAVersionNineSaveGetsThem()
        {
            var time = new TimeService(new FakeClock(), null);
            var state = PlayedState();
            state.AdsDate = "2026-10-31";
            state.AdsWatched["energy"] = 3;
            state.PurchasesApplied.Add("p-1");
            state.Entitlements.Add("passPremium");
            var loaded = SaveService.FromJson(WriteJson(state, time), config, content, out _, out _);
            Assert.AreEqual("2026-10-31", loaded.AdsDate);
            Assert.AreEqual(3, loaded.AdsWatched["energy"]);
            CollectionAssert.AreEqual(new[] { "p-1" }, loaded.PurchasesApplied);
            CollectionAssert.AreEqual(new[] { "passPremium" }, loaded.Entitlements);

            var v9 = JObject.Parse(WriteJson(PlayedState(), time));
            v9["schemaVersion"] = 9;
            v9.Remove("purchases");
            SaveService.FromJson(v9.ToString(), config, content, out var data, out _);
            Assert.AreEqual(SaveData.CurrentSchema, data.SchemaVersion);
            Assert.AreEqual(0, ((JArray)data.Purchases["applied"]).Count);
        }

        [Test]
        public void DailyProgressAndMiniBoardsRoundTrip()
        {
            var time = new TimeService(new FakeClock(), null);
            var state = PlayedState();
            state.LoginDay = 3;
            state.LastGiftDate = "2026-10-31";
            state.TasksDate = "2026-10-31";
            state.DailyTasks.Add(new DailyTaskState { Id = "serve_10", Progress = 4 });
            state.PassPoints = 50;
            var board = new Item[30];
            board[27] = new Item(MiniBoard.Chain, 1, MiniBoard.Stall);
            board[3] = new Item(MiniBoard.Chain, 4);
            state.MiniBoards["e1"] = new MiniBoardState { Board = board, Orders = { 2, 3 }, Tokens = 12, Paid = { 1 }, Rng = 99 };

            var loaded = SaveService.FromJson(WriteJson(state, time), config, content, out _, out _);
            Assert.AreEqual(3, loaded.LoginDay);
            Assert.AreEqual("2026-10-31", loaded.LastGiftDate);
            Assert.AreEqual(4, loaded.DailyTasks.Single().Progress);
            Assert.AreEqual(50, loaded.PassPoints);
            var mini = loaded.MiniBoards["e1"];
            Assert.IsTrue(mini.Board[27].IsGenerator);
            Assert.AreEqual(4, mini.Board[3].Level);
            CollectionAssert.AreEqual(new[] { 2, 3 }, mini.Orders);
            Assert.AreEqual(12, mini.Tokens);
            CollectionAssert.AreEqual(new[] { 1 }, mini.Paid);
        }

        [Test]
        public void SectionsTheGameDoesNotUseYetSurviveASave()
        {
            var time = new TimeService(new FakeClock(), null);
            var data = new SaveData();
            data.Events["pass_2026_11"] = new JObject { ["points"] = 4120 };
            var state = PlayedState();
            SaveCodec.Write(state, time.State, config, data);
            var reloaded = JObject.Parse(JsonConvert.SerializeObject(data, SaveCodec.Json));
            Assert.AreEqual(4120, (int)reloaded["events"]["pass_2026_11"]["points"]);
        }
    }
}
