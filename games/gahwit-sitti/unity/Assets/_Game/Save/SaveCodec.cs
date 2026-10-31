using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aqlaan.CozyKit;
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
                OrdersServed = state.OrdersServed,
                UnlocksShown = new List<string>(state.UnlocksShown)
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
                Character = o.IsCharacter ? o.Customer : null,
                CreatedMs = o.CreatedMs,
                Recipe = o.Recipe
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
            into.Cafe.Cosiness = state.Cosiness;
            into.Cafe.Owned = new List<string>(state.OwnedDecor);
            into.Wardrobe["owned"] = new JArray(state.WardrobeOwned);
            into.Wardrobe["worn"] = JObject.FromObject(state.WardrobeWorn);
            into.Wardrobe["occasions"] = new JObject(state.OccasionHearts.Select(o => new JProperty(o.Key.ToString(), o.Value)));
            into.Recipes["found"] = new JArray(state.RecipesFound);
            into.Recipes["wrongStreak"] = state.RecipeWrongStreak;
            into.Recipes["pan"] = new JArray(state.Pan.Select(p => new JObject { ["chain"] = p.Chain, ["level"] = p.Level }));
            into.Daily["adsDate"] = state.AdsDate;
            into.Daily["loginDay"] = state.LoginDay;
            into.Daily["lastLoginDate"] = state.LastGiftDate;
            into.Daily["tasksDate"] = state.TasksDate;
            into.Daily["tasks"] = new JArray(state.DailyTasks.Select(t => new JObject { ["id"] = t.Id, ["progress"] = t.Progress, ["claimed"] = t.Claimed }));
            into.Daily["passPoints"] = state.PassPoints;
            into.Daily["pendingRewards"] = new JArray(state.PendingRewards);
            foreach (var old in into.Events.Properties().Where(p => p.Name.StartsWith(MiniBoardKey)).ToList()) old.Remove();
            foreach (var mini in state.MiniBoards)
                into.Events[MiniBoardKey + mini.Key] = new JObject
                {
                    ["cells"] = new JArray(mini.Value.Board.Select((p, i) => (p, i)).Where(x => x.p != null)
                        .Select(x => new JObject { ["i"] = x.i, ["level"] = x.p.Level, ["stall"] = x.p.IsGenerator })),
                    ["size"] = mini.Value.Board.Length,
                    ["orders"] = new JArray(mini.Value.Orders),
                    ["tokens"] = mini.Value.Tokens,
                    ["paid"] = new JArray(mini.Value.Paid),
                    ["rng"] = mini.Value.Rng.ToString()
                };
            into.Daily["adsWatched"] = JObject.FromObject(state.AdsWatched);
            into.Purchases["applied"] = new JArray(state.PurchasesApplied);
            into.Purchases["entitlements"] = new JArray(state.Entitlements);
            into.Cafe.Placed = new JArray(state.PlacedDecor.Select(p => new JObject
                { ["id"] = p.Id, ["x"] = p.X, ["z"] = p.Z, ["rot"] = p.Rot * 90 }));
            foreach (var regular in state.Regulars)
            {
                var entry = into.Regulars[regular.Key] as JObject ?? new JObject();
                entry["points"] = regular.Value.Points;
                entry["level"] = regular.Value.Level;
                entry["storiesSeen"] = new JArray(regular.Value.StoriesSeen);
                into.Regulars[regular.Key] = entry;
            }
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
                UnlocksShown = new List<string>(data.Player.UnlocksShown ?? new List<string>()),
                Chapter = data.Story.Chapter,
                TaskIndex = data.Story.Task,
                TaskStyles = new Dictionary<string, string>(data.Cafe.TaskStyles),
                Cosiness = data.Cafe.Cosiness,
                OwnedDecor = new List<string>(data.Cafe.Owned),
                WardrobeOwned = data.Wardrobe["owned"]?.Values<string>().ToList() ?? new List<string>(),
                WardrobeWorn = (data.Wardrobe["worn"] as JObject)?.Properties().Where(p => p.Value.Type == JTokenType.String)
                    .ToDictionary(p => p.Name, p => (string)p.Value) ?? new Dictionary<string, string>(),
                OccasionHearts = (data.Wardrobe["occasions"] as JObject)?.Properties()
                    .ToDictionary(p => int.Parse(p.Name), p => (int)p.Value) ?? new Dictionary<int, int>(),
                RecipesFound = data.Recipes["found"]?.Values<string>().ToList() ?? new List<string>(),
                RecipeWrongStreak = data.Recipes.Value<int?>("wrongStreak") ?? 0,
                AdsDate = data.Daily.Value<string>("adsDate"),
                LoginDay = data.Daily.Value<int?>("loginDay") ?? 0,
                LastGiftDate = data.Daily.Value<string>("lastLoginDate"),
                TasksDate = data.Daily.Value<string>("tasksDate"),
                DailyTasks = (data.Daily["tasks"] as JArray)?.OfType<JObject>().Select(t => new DailyTaskState
                {
                    Id = t.Value<string>("id"), Progress = t.Value<int?>("progress") ?? 0, Claimed = t.Value<bool?>("claimed") ?? false
                }).Where(t => t.Id != null).ToList() ?? new List<DailyTaskState>(),
                PassPoints = data.Daily.Value<int?>("passPoints") ?? 0,
                PendingRewards = data.Daily["pendingRewards"]?.Values<string>().ToList() ?? new List<string>(),
                MiniBoards = data.Events.Properties().Where(p => p.Name.StartsWith(MiniBoardKey) && p.Value is JObject)
                    .ToDictionary(p => p.Name.Substring(MiniBoardKey.Length), p => ReadMiniBoard((JObject)p.Value)),
                AdsWatched = (data.Daily["adsWatched"] as JObject)?.Properties().Where(p => p.Value.Type == JTokenType.Integer)
                    .ToDictionary(p => p.Name, p => (int)p.Value) ?? new Dictionary<string, int>(),
                PurchasesApplied = data.Purchases["applied"]?.Values<string>().ToList() ?? new List<string>(),
                Entitlements = data.Purchases["entitlements"]?.Values<string>().ToList() ?? new List<string>(),
                Pan = (data.Recipes["pan"] as JArray)?.OfType<JObject>()
                    .Where(p => p.Value<string>("chain") != null && config.Chains.ContainsKey(p.Value<string>("chain")))
                    .Select(p => new Item(p.Value<string>("chain"), p.Value<int>("level"))).ToList() ?? new List<Item>(),
                PlacedDecor = data.Cafe.Placed.OfType<JObject>().Select(p => new PlacedDecor
                {
                    Id = p.Value<string>("id"),
                    X = p.Value<int?>("x") ?? 0,
                    Z = p.Value<int?>("z") ?? 0,
                    Rot = ((p.Value<int?>("rot") ?? 0) / 90 % 4 + 4) % 4
                }).ToList(),
                Choices = new Dictionary<string, string>(data.Story.Choices),
                ScenesSeen = new List<string>(data.Story.ScenesSeen),
                LettersFound = new List<string>(data.Story.LettersFound),
                TutorialStep = data.Tutorial.LastStep,
                TutorialDone = data.Tutorial.Done,
                EasyOrdersLeft = data.Tutorial.EasyOrdersLeft,
                Rng = ulong.TryParse(data.Rng, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rng) ? rng : 0
            };

            foreach (var regular in data.Regulars.Properties())
                if (regular.Value is JObject entry)
                    state.Regulars[regular.Name] = new RegularState
                    {
                        Points = entry.Value<int?>("points") ?? 0,
                        Level = entry.Value<int?>("level") ?? 1,
                        StoriesSeen = entry["storiesSeen"]?.Values<int>().ToList() ?? new List<int>()
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
                    Xp = saved.Xp,
                    CreatedMs = saved.CreatedMs,
                    Recipe = saved.Recipe
                });
                state.NextOrderId = Math.Max(state.NextOrderId, id + 1);
            }
            return state;
        }

        // A mini board's progress sits in the events section under "miniBoard:<event id>".
        const string MiniBoardKey = "miniBoard:";

        static MiniBoardState ReadMiniBoard(JObject saved)
        {
            var board = new Item[saved.Value<int?>("size") ?? 30];
            foreach (var cell in (saved["cells"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                int i = cell.Value<int>("i");
                if (i < 0 || i >= board.Length) continue;
                bool stall = cell.Value<bool?>("stall") ?? false;
                board[i] = new Item(MiniBoard.Chain, cell.Value<int?>("level") ?? 1, stall ? MiniBoard.Stall : null);
            }
            return new MiniBoardState
            {
                Board = board,
                Orders = saved["orders"]?.Values<int>().ToList() ?? new List<int>(),
                Tokens = saved.Value<int?>("tokens") ?? 0,
                Paid = saved["paid"]?.Values<int>().ToList() ?? new List<int>(),
                Rng = ulong.TryParse(saved.Value<string>("rng"), out var rng) ? rng : 1
            };
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
