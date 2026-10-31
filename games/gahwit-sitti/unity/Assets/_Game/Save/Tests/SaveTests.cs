using System.IO;
using System.Linq;
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
