using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gahwit.Core;
using Newtonsoft.Json.Linq;

namespace Gahwit.Save
{
    // Upgrades an old save one schema version at a time. Every change to the save format adds a step here, with a test.
    public static class SaveMigrations
    {
        static readonly Dictionary<int, Func<JObject, BalanceConfig, JObject>> Steps = new Dictionary<int, Func<JObject, BalanceConfig, JObject>>
        {
            [0] = FromFirstPlayable,
            [1] = AddAccount,
            [2] = AddEasyOrders,
            [3] = AddArrivalTimes,
            [4] = AddOccasions,
            [5] = AddUnlockCards,
            [6] = SplitNotifications,
            [7] = AddMemoryAssist,
            [8] = AddRecipes,
            [9] = AddPurchases,
            [10] = AddDailyAndEvents,
            [11] = AddSubtitles,
            [12] = AddKitchen
        };

        public static JObject Upgrade(JObject raw, BalanceConfig config)
        {
            int version = raw.Value<int?>("schemaVersion") ?? 0;
            while (version < SaveData.CurrentSchema)
            {
                if (!Steps.TryGetValue(version, out var step))
                    throw new InvalidOperationException($"No save migration from schema version {version}.");
                raw = step(raw, config);
                version = raw.Value<int>("schemaVersion");
            }
            return raw;
        }

        // Version 0 is the first playable's save (2026-10-30): the runtime state written as-is, in PascalCase,
        // with generators beside the board instead of on it and the energy stamp in seconds.
        static JObject FromFirstPlayable(JObject old, BalanceConfig config)
        {
            int cols = config.Board.Cols, rows = config.Board.Rows;
            var data = new SaveData { SchemaVersion = 1 }; // later steps run after this one
            data.Board.Cols = cols;
            data.Board.Rows = rows;

            var taken = new HashSet<int>();
            if (old["Board"] is JArray board)
            {
                for (int i = 0; i < board.Count && i < cols * rows; i++)
                {
                    if (!(board[i] is JObject item)) continue;
                    data.Board.Cells.Add(new CellSave { I = i, Chain = item.Value<string>("Chain"), Level = item.Value<int>("Level") });
                    taken.Add(i);
                }
            }

            // Generators move onto the board, filling the bottom rows from the middle out; any that don't fit wait in storage.
            foreach (var generator in (old["Generators"] as JArray ?? new JArray()).OfType<JObject>())
            {
                int cell = FreeCellFromBottom(taken, cols, rows);
                string id = generator.Value<string>("Id");
                int level = generator.Value<int?>("Level") ?? 1;
                if (cell < 0) data.Board.Storage.Add(new PieceSave { Gen = id, GenLevel = level });
                else
                {
                    data.Board.Cells.Add(new CellSave { I = cell, Gen = id, GenLevel = level });
                    taken.Add(cell);
                }
            }

            data.Player = new PlayerSave
            {
                Level = old.Value<int?>("Level") ?? 1,
                Xp = old.Value<int?>("Xp") ?? 0,
                Coins = old.Value<int?>("Coins") ?? 0,
                Energy = old.Value<int?>("Energy") ?? 0,
                EnergyStampMs = (old.Value<long?>("EnergyStamp") ?? 0) * 1000,
                OrdersServed = old.Value<int?>("OrdersServed") ?? 0
            };

            foreach (var order in (old["Orders"] as JArray ?? new JArray()).OfType<JObject>())
            {
                var saved = new OrderSave
                {
                    Id = "o_" + order.Value<int>("Id"),
                    Coins = order.Value<int>("Coins"),
                    Xp = order.Value<int>("Xp"),
                    Character = order.Value<bool?>("IsCharacter") == true ? Rename(order.Value<string>("Customer")) : null
                };
                foreach (var line in (order["Lines"] as JArray ?? new JArray()).OfType<JObject>())
                    for (int n = 0; n < line.Value<int>("Count"); n++)
                        saved.Items.Add(new OrderItemSave { Chain = line.Value<string>("Chain"), Level = line.Value<int>("Level") });
                data.Orders.Add(saved);
            }

            data.Story.Chapter = old.Value<int?>("Chapter") ?? 1;
            data.Story.Task = old.Value<int?>("TaskIndex") ?? 0;
            if (old.Value<bool?>("IntroSeen") == true) data.Story.ScenesSeen.Add("intro");
            foreach (var task in (old["CompletedTasks"] as JArray ?? new JArray()).Values<string>())
                data.Story.ScenesSeen.Add(task); // each chapter 1 task's scene has the task's id as its knot
            data.Tutorial.Done = data.Player.OrdersServed > 0;
            data.Rng = (old.Value<ulong?>("Rng") ?? 0).ToString(CultureInfo.InvariantCulture);

            return JObject.FromObject(data, SaveCodec.Serializer);
        }

        // Version 2 (2026-10-31) adds the age gate's answer and the notification and voice switches. A save from
        // before has no birth year, so the age gate shows once on the next launch.
        static JObject AddAccount(JObject save, BalanceConfig config)
        {
            if (!(save["account"] is JObject)) save["account"] = new JObject { ["parentConsent"] = false };
            var settings = save["settings"] as JObject ?? new JObject();
            if (settings["notifications"] == null) settings["notifications"] = true;
            if (settings["voice"] == null) settings["voice"] = false;
            save["settings"] = settings;
            save["schemaVersion"] = 2;
            return save;
        }

        // Version 3 (2026-10-31) counts the easy orders that start free play after the tutorial.
        static JObject AddEasyOrders(JObject save, BalanceConfig config)
        {
            var tutorial = save["tutorial"] as JObject ?? new JObject();
            if (tutorial["easyOrdersLeft"] == null) tutorial["easyOrdersLeft"] = 0;
            save["tutorial"] = tutorial;
            save["schemaVersion"] = 3;
            return save;
        }

        // Version 4 (2026-10-31) saves when each customer walked in. Older orders get the time at the first tick.
        static JObject AddArrivalTimes(JObject save, BalanceConfig config)
        {
            foreach (var order in (save["orders"] as JArray ?? new JArray()).OfType<JObject>())
                if (order["createdMs"] == null) order["createdMs"] = 0;
            save["schemaVersion"] = 4;
            return save;
        }

        // Version 5 (2026-10-31) remembers the hearts won for each chapter's occasion, so each pays once.
        static JObject AddOccasions(JObject save, BalanceConfig config)
        {
            var wardrobe = save["wardrobe"] as JObject ?? new JObject();
            if (wardrobe["occasions"] == null) wardrobe["occasions"] = new JObject();
            save["wardrobe"] = wardrobe;
            save["schemaVersion"] = 5;
            return save;
        }

        // Version 6 (2026-10-31) remembers which unlock cards were shown, so each shows once.
        static JObject AddUnlockCards(JObject save, BalanceConfig config)
        {
            var player = save["player"] as JObject ?? new JObject();
            if (player["unlocksShown"] == null) player["unlocksShown"] = new JArray();
            save["player"] = player;
            save["schemaVersion"] = 6;
            return save;
        }

        // Version 7 (2026-10-31) has one switch per notification type; each starts as the old single switch was.
        static JObject SplitNotifications(JObject save, BalanceConfig config)
        {
            var settings = save["settings"] as JObject ?? new JObject();
            if (settings["notifications"] is JValue single)
            {
                bool on = single.Type != JTokenType.Boolean || (bool)single;
                settings["notifications"] = new JObject
                {
                    ["energyFull"] = on, ["regular"] = on, ["task"] = on, ["event"] = on, ["away"] = on, ["hosting"] = on
                };
            }
            save["settings"] = settings;
            save["schemaVersion"] = 7;
            return save;
        }

        // Version 8 (2026-10-31) has the memory games' easy assist switch, off to begin with.
        static JObject AddMemoryAssist(JObject save, BalanceConfig config)
        {
            var settings = save["settings"] as JObject ?? new JObject();
            if (settings["memoryAssist"] == null) settings["memoryAssist"] = false;
            save["settings"] = settings;
            save["schemaVersion"] = 8;
            return save;
        }

        // Version 9 (2026-10-31) uses the recipes section: found recipes, the wrong-pair streak and the pan's items.
        // Orders may ask for a recipe by name (orders[].recipe), which older saves never have.
        static JObject AddRecipes(JObject save, BalanceConfig config)
        {
            var recipes = save["recipes"] as JObject ?? new JObject();
            if (recipes["found"] == null) recipes["found"] = new JArray();
            if (recipes["wrongStreak"] == null) recipes["wrongStreak"] = 0;
            if (recipes["pan"] == null) recipes["pan"] = new JArray();
            save["recipes"] = recipes;
            save["schemaVersion"] = 9;
            return save;
        }

        // Version 10 (2026-10-31) keeps the purchases whose items are in the save, bought switches, and the day the
        // rewarded-ad counts are for.
        static JObject AddPurchases(JObject save, BalanceConfig config)
        {
            var purchases = save["purchases"] as JObject ?? new JObject();
            if (purchases["applied"] == null) purchases["applied"] = new JArray();
            if (purchases["entitlements"] == null) purchases["entitlements"] = new JArray();
            save["purchases"] = purchases;
            var daily = save["daily"] as JObject ?? new JObject();
            if (daily["adsWatched"] == null) daily["adsWatched"] = new JObject();
            save["daily"] = daily;
            save["schemaVersion"] = 10;
            return save;
        }

        // Version 11 (2026-10-31) uses the daily section (the gift's day, today's tasks, pass points) and keeps each
        // mini board under events."miniBoard:<event id>". Older saves had nothing there to keep.
        static JObject AddDailyAndEvents(JObject save, BalanceConfig config)
        {
            var daily = save["daily"] as JObject ?? new JObject();
            if (!(daily["tasks"] is JArray)) daily["tasks"] = new JArray();
            if (daily["loginDay"] == null) daily["loginDay"] = 0;
            save["daily"] = daily;
            if (!(save["events"] is JObject)) save["events"] = new JObject();
            save["schemaVersion"] = 11;
            return save;
        }

        // Version 12 (2026-10-31) adds settings.subtitles: neutral-Arabic subtitles under the dialect lines, off at
        // first (missing/11).
        static JObject AddSubtitles(JObject save, BalanceConfig config)
        {
            var settings = save["settings"] as JObject ?? new JObject();
            if (settings["subtitles"] == null) settings["subtitles"] = false;
            save["settings"] = settings;
            save["schemaVersion"] = 12;
            return save;
        }

        // Version 13 (2026-10-31) adds the kitchen's feeling: kitchen.notebook (Teta's notebook pages found) and
        // story.ordersDone (story orders served). Orders may carry a story order's id (orders[].story).
        static JObject AddKitchen(JObject save, BalanceConfig config)
        {
            var kitchen = save["kitchen"] as JObject ?? new JObject();
            if (!(kitchen["notebook"] is JArray)) kitchen["notebook"] = new JArray();
            save["kitchen"] = kitchen;
            var story = save["story"] as JObject ?? new JObject();
            if (!(story["ordersDone"] is JArray)) story["ordersDone"] = new JArray();
            save["story"] = story;
            save["schemaVersion"] = 13;
            return save;
        }

        static string Rename(string character) => character == "abufadi" ? "abu_fadi" : character;

        static int FreeCellFromBottom(HashSet<int> taken, int cols, int rows)
        {
            for (int row = rows - 1; row >= 0; row--)
            for (int offset = 0; offset < cols; offset++)
            {
                int col = cols / 2 + (offset % 2 == 0 ? offset / 2 : -(offset / 2 + 1));
                if (col < 0 || col >= cols) continue;
                int cell = row * cols + col;
                if (!taken.Contains(cell)) return cell;
            }
            return -1;
        }
    }
}
