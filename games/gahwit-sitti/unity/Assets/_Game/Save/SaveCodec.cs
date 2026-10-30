using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Gahwit.Save
{
    // Maps the runtime GameState to the save file and back. Only the sections the game owns are written;
    // the rest of the SaveData passes through untouched.
    public static class SaveCodec
    {
        public static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false } },
            NullValueHandling = NullValueHandling.Ignore
        };

        public static JsonSerializer Serializer => JsonSerializer.Create(Json);

        public static void Write(GameState state, TimeService.Anchor clock, BalanceConfig config, SaveData into)
        {
            into.SchemaVersion = SaveData.CurrentSchema;
            into.SavedAt = Iso(clock.LastSeenMs);
            into.Rng = state.Rng.ToString(CultureInfo.InvariantCulture);
            into.Clock = new ClockSave { TrustedMs = clock.TrustedMs, UptimeMs = clock.UptimeMs, LastSeenMs = clock.LastSeenMs, FromServer = clock.FromServer };
            into.Tutorial = new TutorialSave { LastStep = state.TutorialStep, Done = state.TutorialDone, EasyOrdersLeft = state.EasyOrdersLeft };
            into.Player = new PlayerSave
            {
                Level = state.Level,
                Xp = state.Xp,
                Coins = state.Coins,
                Energy = state.Energy,
                EnergyStampMs = state.EnergyStampMs,
                LastServerTime = clock.FromServer ? Iso(clock.TrustedMs) : null,
                OrdersServed = state.OrdersServed
            };

            var board = new BoardSave { Cols = config.Board.Cols, Rows = config.Board.Rows, StorageSlots = state.StorageSlots };
            for (int i = 0; i < state.Board.Length; i++)
            {
                var piece = state.Board[i];
                if (piece == null) continue;
                board.Cells.Add(piece.IsGenerator
                    ? new CellSave { I = i, Gen = piece.Generator, GenLevel = piece.Level }
                    : new CellSave { I = i, Chain = piece.Chain, Level = piece.Level });
            }
            board.Storage = state.Storage.Select(p => p.IsGenerator
                ? new PieceSave { Gen = p.Generator, GenLevel = p.Level }
                : new PieceSave { Chain = p.Chain, Level = p.Level }).ToList();
            into.Board = board;

            into.Orders = state.Orders.Select(o => new OrderSave
            {
                Id = "o_" + o.Id,
                Items = o.Lines.SelectMany(l => Enumerable.Repeat(new OrderItemSave { Chain = l.Chain, Level = l.Level }, l.Count)).ToList(),
                Coins = o.Coins,
                Xp = o.Xp,
                Character = o.IsCharacter ? o.Customer : null
            }).ToList();

            into.Story = new StorySave
            {
                Chapter = state.Chapter,
                Task = state.TaskIndex,
                Choices = new Dictionary<string, string>(state.Choices),
                ScenesSeen = new List<string>(state.ScenesSeen),
                LettersFound = new List<string>(state.LettersFound)
            };
            into.Cafe.TaskStyles = new Dictionary<string, string>(state.TaskStyles);
            foreach (var memory in state.MemoryStars)
            {
                var entry = into.Memories[memory.Key] as JObject ?? new JObject();
                entry["stars"] = memory.Value;
                into.Memories[memory.Key] = entry;
            }
        }

        public static GameState Read(SaveData data, BalanceConfig config, ChapterContent content)
        {
            var state = new GameState
            {
                Board = new Item[config.Board.Cols * config.Board.Rows],
                StorageSlots = data.Board.StorageSlots,
                Energy = data.Player.Energy,
                EnergyStampMs = data.Player.EnergyStampMs,
                Coins = data.Player.Coins,
                Xp = data.Player.Xp,
                Level = data.Player.Level,
                OrdersServed = data.Player.OrdersServed,
                Chapter = data.Story.Chapter,
                TaskIndex = data.Story.Task,
                TaskStyles = new Dictionary<string, string>(data.Cafe.TaskStyles),
                Choices = new Dictionary<string, string>(data.Story.Choices),
                ScenesSeen = new List<string>(data.Story.ScenesSeen),
                LettersFound = new List<string>(data.Story.LettersFound),
                TutorialStep = data.Tutorial.LastStep,
                TutorialDone = data.Tutorial.Done,
                EasyOrdersLeft = data.Tutorial.EasyOrdersLeft,
                Rng = ulong.TryParse(data.Rng, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rng) ? rng : 0
            };

            foreach (var memory in data.Memories.Properties())
                if (memory.Value is JObject entry && entry["stars"] != null) state.MemoryStars[memory.Name] = (int)entry["stars"];

            foreach (var cell in data.Board.Cells)
            {
                if (cell.I < 0 || cell.I >= state.Board.Length) continue;
                state.Board[cell.I] = Piece(cell.Chain, cell.Level, cell.Gen, cell.GenLevel, config);
            }
            foreach (var piece in data.Board.Storage.Where(p => p != null))
                state.Storage.Add(Piece(piece.Chain, piece.Level, piece.Gen, piece.GenLevel, config));

            foreach (var saved in data.Orders)
            {
                int id = int.TryParse(saved.Id?.Replace("o_", ""), out var n) ? n : 0;
                var lines = saved.Items.GroupBy(i => (i.Chain, i.Level))
                    .Select(g => new OrderLine { Chain = g.Key.Chain, Level = g.Key.Level, Count = g.Count() }).ToList();
                state.Orders.Add(new Order
                {
                    Id = id,
                    Customer = saved.Character ?? content.WalkIns[Math.Abs(id) % content.WalkIns.Count],
                    IsCharacter = saved.Character != null,
                    Lines = lines,
                    Units = lines.Sum(l => l.Count * config.ItemUnits(l.Level)),
                    Coins = saved.Coins,
                    Xp = saved.Xp
                });
                state.NextOrderId = Math.Max(state.NextOrderId, id + 1);
            }
            return state;
        }

        public static TimeService.Anchor ReadClock(SaveData data) =>
            data.Clock == null || data.Clock.UptimeMs == 0 && data.Clock.TrustedMs == 0
                ? null
                : new TimeService.Anchor { TrustedMs = data.Clock.TrustedMs, UptimeMs = data.Clock.UptimeMs, LastSeenMs = data.Clock.LastSeenMs, FromServer = data.Clock.FromServer };

        static Item Piece(string chain, int? level, string gen, int? genLevel, BalanceConfig config) =>
            gen != null
                ? new Item(config.GeneratorDef(gen)?.Chain, genLevel ?? 1, gen)
                : new Item(chain, level ?? 1);

        public static string Iso(long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
