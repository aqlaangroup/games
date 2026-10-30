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
            [0] = FromFirstPlayable
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
            var data = new SaveData();
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
