using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;

namespace Gahwit.Merge
{
    public enum TapResult { Spawned, NoEnergy, BoardFull, NotAGenerator }

    public enum MoveResult { None, Moved, Swapped, Merged }

    // The rules of the merge kitchen, orders, player levels and café tasks (GAME_SYSTEMS.md sections 1–9).
    // Plain C# with no Unity objects; the views call it and listen to its events.
    // Times are trusted milliseconds from TimeService, never the phone clock.
    public sealed class Game
    {

        public readonly BalanceConfig Config;
        public readonly ChapterContent Content;
        public readonly GameState State;
        readonly GameRandom rng;

        public event Action<int> ItemSpawned;
        public event Action<int> ItemsMerged;
        public event Action<Order> OrderServed;
        public event Action<int> LeveledUp;
        public event Action<TaskDef> TaskCompleted;
        public event Action<int> ChapterCompleted;
        public event Action Changed;

        public Game(BalanceConfig config, ChapterContent content, GameState state)
        {
            Config = config;
            Content = content;
            State = state;
            rng = new GameRandom(state);
            if (State.Board == null || State.Board.Length != CellCount) State.Board = new Item[CellCount];
            UnlockGenerators();
            FillOrders();
        }

        public static GameState NewState(BalanceConfig config, ChapterContent content, ulong seed, long nowMs)
        {
            var state = new GameState
            {
                Energy = config.Energy.StartingEnergy,
                EnergyStampMs = nowMs,
                Rng = seed == 0 ? 1 : seed,
                Board = new Item[config.Board.Cols * config.Board.Rows]
            };
            foreach (var start in content.StartingBoard)
            {
                int cell = start.Row * config.Board.Cols + start.Col;
                state.Board[cell] = start.Generator != null
                    ? new Item(config.GeneratorDef(start.Generator).Chain, start.Level, start.Generator)
                    : new Item(start.Chain, start.Level);
            }
            return state;
        }

        public int Cols => Config.Board.Cols;
        public int Rows => Config.Board.Rows;
        public int CellCount => Cols * Rows;
        public int EnergyCap => Config.Energy.Cap;
        public int XpToNext => Config.XpToNext(State.Level);
        public IReadOnlyList<Order> Orders => State.Orders;
        long RegenMs => Config.Energy.RegenSeconds * 1000L;

        public Item ItemAt(int cell) => State.Board[cell];
        public int MaxLevel(string chain) => Config.MaxItemLevel(chain);
        public string ItemName(string chain, int level) => Config.Chains[chain].Items[level - 1][0];
        public string GeneratorName(string id) => Config.GeneratorDef(id).Ar;
        public int EmptyCells => State.Board.Count(item => item == null);

        IEnumerable<Item> AllPieces => State.Board.Where(i => i != null).Concat(State.Storage);

        // ---------- Energy ----------

        public void Tick(long nowMs)
        {
            if (State.Energy >= EnergyCap)
            {
                State.EnergyStampMs = nowMs;
                return;
            }
            long gained = (nowMs - State.EnergyStampMs) / RegenMs;
            if (gained <= 0) return;
            State.Energy = (int)Math.Min(EnergyCap, State.Energy + gained);
            State.EnergyStampMs += gained * RegenMs;
            if (State.Energy >= EnergyCap) State.EnergyStampMs = nowMs;
            Changed?.Invoke();
        }

        public int SecondsToNextEnergy(long nowMs) =>
            State.Energy >= EnergyCap ? 0 : (int)Math.Max(0, (RegenMs - (nowMs - State.EnergyStampMs) + 999) / 1000);

        void SpendEnergy(int amount, long nowMs)
        {
            // Refill starts counting the moment energy drops below the cap.
            if (State.Energy >= EnergyCap && State.Energy - amount < EnergyCap) State.EnergyStampMs = nowMs;
            State.Energy -= amount;
        }

        // ---------- Generators ----------

        public TapResult TapGenerator(int cell, long nowMs, out int spawnedAt)
        {
            spawnedAt = -1;
            var generator = State.Board[cell];
            if (generator == null || !generator.IsGenerator) return TapResult.NotAGenerator;
            int cost = Config.Generators.TapEnergyCost;
            if (State.Energy < cost) return TapResult.NoEnergy;
            spawnedAt = NearestEmptyCell(cell);
            if (spawnedAt < 0) return TapResult.BoardFull;

            SpendEnergy(cost, nowMs);
            int level = Math.Min(RollGeneratorOutput(generator.Level), MaxLevel(generator.Chain));
            State.Board[spawnedAt] = new Item(generator.Chain, level);
            ItemSpawned?.Invoke(spawnedAt);
            Changed?.Invoke();
            return TapResult.Spawned;
        }

        int RollGeneratorOutput(int generatorLevel)
        {
            var output = Config.Generators.LevelOutput[generatorLevel.ToString()];
            double roll = rng.NextDouble(), total = 0;
            int level = 1;
            foreach (var kv in output.ItemLevels.OrderBy(kv => int.Parse(kv.Key)))
            {
                level = int.Parse(kv.Key);
                total += kv.Value;
                if (roll < total) return level;
            }
            return level;
        }

        bool GeneratorCanLevelUp(int level) => Config.Generators.LevelOutput.ContainsKey((level + 1).ToString());

        // A generator unlocked by the current chapter appears on the board (or in storage if the board is full).
        void UnlockGenerators()
        {
            foreach (var def in Config.Generators.List)
            {
                if (def.UnlockChapter > State.Chapter || AllPieces.Any(p => p.Generator == def.Id)) continue;
                Place(new Item(def.Chain, 1, def.Id));
            }
        }

        void Place(Item piece)
        {
            int cell = NearestEmptyCell(CellCount - 1 - Cols / 2);
            if (cell >= 0) State.Board[cell] = piece;
            else State.Storage.Add(piece);
        }

        // Pieces waiting in storage come back as soon as there is room.
        void Settle()
        {
            while (State.Storage.Count > 0)
            {
                int cell = NearestEmptyCell(CellCount - 1 - Cols / 2);
                if (cell < 0) return;
                State.Board[cell] = State.Storage[0];
                State.Storage.RemoveAt(0);
            }
        }

        public int NearestEmptyCell(int near)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            float nx = near % Cols, ny = near / Cols;
            for (int i = 0; i < CellCount; i++)
            {
                if (State.Board[i] != null) continue;
                float dx = i % Cols - nx, dy = i / Cols - ny;
                float distance = dx * dx + dy * dy;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        // ---------- Board ----------

        public bool CanMerge(int a, int b)
        {
            var x = State.Board[a];
            if (a == b || x == null || !x.SameAs(State.Board[b])) return false;
            return x.IsGenerator ? GeneratorCanLevelUp(x.Level) : x.Level < MaxLevel(x.Chain);
        }

        public MoveResult Move(int from, int to)
        {
            var moving = State.Board[from];
            if (from == to || moving == null) return MoveResult.None;
            var target = State.Board[to];

            if (target == null)
            {
                State.Board[to] = moving;
                State.Board[from] = null;
                Changed?.Invoke();
                return MoveResult.Moved;
            }

            if (CanMerge(from, to))
            {
                State.Board[to] = new Item(moving.Chain, moving.Level + 1, moving.Generator);
                State.Board[from] = null;
                Settle();
                ItemsMerged?.Invoke(to);
                Changed?.Invoke();
                return MoveResult.Merged;
            }

            State.Board[to] = moving;
            State.Board[from] = target;
            Changed?.Invoke();
            return MoveResult.Swapped;
        }

        public bool CanSell(Item item) => item != null && !item.IsGenerator;

        public int SellValue(Item item) => item.Level * Config.Formulas.SellCoinsPerLevel;

        public int Sell(int cell)
        {
            var item = State.Board[cell];
            if (!CanSell(item)) return 0;
            int coins = SellValue(item);
            State.Board[cell] = null;
            State.Coins += coins;
            Settle();
            Changed?.Invoke();
            return coins;
        }

        // ---------- Orders ----------

        public int CountOnBoard(string chain, int level) => State.Board.Count(item => item != null && item.Is(chain, level));

        public bool CanServe(Order order) => order.Lines.All(line => CountOnBoard(line.Chain, line.Level) >= line.Count);

        public bool IsWanted(Item item) =>
            item != null && !item.IsGenerator &&
            State.Orders.Any(o => o.Lines.Any(line => line.Chain == item.Chain && line.Level == item.Level));

        public bool Serve(int orderId)
        {
            var order = State.Orders.Find(o => o.Id == orderId);
            if (order == null || !CanServe(order)) return false;

            foreach (var line in order.Lines)
            {
                int left = line.Count;
                for (int i = 0; i < CellCount && left > 0; i++)
                {
                    if (State.Board[i] == null || !State.Board[i].Is(line.Chain, line.Level)) continue;
                    State.Board[i] = null;
                    left--;
                }
            }

            State.Orders.Remove(order);
            State.OrdersServed++;
            State.Coins += order.Coins;
            Settle();
            OrderServed?.Invoke(order);
            AddXp(order.Xp);
            FillOrders();
            Changed?.Invoke();
            return true;
        }

        void FillOrders()
        {
            var first = Content.FirstOrder;
            if (State.OrdersServed == 0 && State.Orders.Count == 0 && first != null)
            {
                var line = new OrderLine { Chain = first.Chain, Level = first.Level, Count = first.Count };
                State.Orders.Add(MakeOrder(first.Customer, true, new List<OrderLine> { line }));
            }

            int slots = Config.OrderSlots(State.Level);
            while (State.Orders.Count < slots) State.Orders.Add(NewOrder());
        }

        Order NewOrder()
        {
            // Fairness (GAME_SYSTEMS.md section 6): orders only use chains whose generator the player has.
            var pool = AllPieces.Where(p => p.IsGenerator).Select(p => p.Chain).Distinct().OrderBy(c => c).ToList();
            float size = Config.OrderSizeUnits(State.Level);
            bool twoLines = State.Level >= Config.Orders.TwoItemOrderFromLevel && pool.Count > 1 &&
                            rng.NextDouble() < Config.Orders.TwoItemOrderChance;
            int lineCount = twoLines ? 2 : 1;

            var lines = new List<OrderLine>();
            for (int n = 0; n < lineCount; n++)
            {
                string chain = pool[rng.Range(0, pool.Count)];
                pool.Remove(chain);
                lines.Add(MakeLine(chain, size / lineCount));
            }

            var named = Content.Characters
                .Where(kv => kv.Value.PlacesOrders && kv.Value.FromChapter <= State.Chapter)
                .Select(kv => kv.Key).OrderBy(id => id).ToList();
            if (named.Count > 0 && rng.NextDouble() < Config.Orders.CharacterOrderChance)
                return MakeOrder(named[rng.Range(0, named.Count)], true, lines);
            return MakeOrder(null, false, lines);
        }

        OrderLine MakeLine(string chain, float targetUnits)
        {
            var pick = Config.Formulas.OrderLevelPick;
            int cap = Math.Min(Config.MaxOrderItemLevel(State.Level), MaxLevel(chain));
            int level = rng.NextDouble() < pick.TopLevelChance ? cap : rng.Range(Math.Max(1, cap - pick.Spread), cap + 1);
            int count = (int)Math.Round(targetUnits / Config.ItemUnits(level));
            count = Math.Max(1, Math.Min(Config.Formulas.OrderMaxItemsPerLine, count));
            return new OrderLine { Chain = chain, Level = level, Count = count };
        }

        // Walk-ins have no character; their shown name is picked from the order id, so it needs no save field.
        public Order MakeOrder(string character, bool isCharacter, List<OrderLine> lines, int id = 0)
        {
            int units = lines.Sum(line => line.Count * Config.ItemUnits(line.Level));
            if (id == 0) id = State.NextOrderId++;
            return new Order
            {
                Id = id,
                Customer = isCharacter ? character : Content.WalkIns[id % Content.WalkIns.Count],
                IsCharacter = isCharacter,
                Lines = lines,
                Units = units,
                Coins = Math.Max(Config.Formulas.OrderMinCoins, (int)Math.Round(units * Config.Orders.CoinsPerUnit)),
                Xp = Math.Max(1, (int)Math.Round(units * Config.Orders.XpPerUnit))
            };
        }

        // ---------- Levels ----------

        // Level-up gems are granted by the platform, never here; listeners report the level-up to it.
        void AddXp(int xp)
        {
            State.Xp += xp;
            while (State.Xp >= Config.XpToNext(State.Level))
            {
                State.Xp -= Config.XpToNext(State.Level);
                State.Level++;
                State.Energy += Config.PlayerLevels.Rewards.Energy;
                LeveledUp?.Invoke(State.Level);
            }
        }

        // ---------- Café tasks ----------

        public ChapterTasks ChapterTasks => Content.ForChapter(State.Chapter);

        public TaskDef CurrentTask
        {
            get
            {
                var chapter = ChapterTasks;
                return chapter != null && State.TaskIndex < chapter.Tasks.Count ? chapter.Tasks[State.TaskIndex] : null;
            }
        }

        public int CurrentTaskCost => CurrentTask == null ? 0 : Config.Chapter(State.Chapter).TaskCosts[State.TaskIndex];

        public bool CanAffordTask => CurrentTask != null && State.Coins >= CurrentTaskCost;

        // Every task finished so far, worked out from the chapter and task position.
        public List<string> CompletedTaskIds()
        {
            var done = new List<string>();
            foreach (var chapter in Content.Chapters)
            {
                if (chapter.N < State.Chapter) done.AddRange(chapter.Tasks.Select(t => t.Id));
                else if (chapter.N == State.Chapter) done.AddRange(chapter.Tasks.Take(State.TaskIndex).Select(t => t.Id));
            }
            return done;
        }

        public TaskDef CompleteTask(string style = null)
        {
            var task = CurrentTask;
            if (task == null || State.Coins < CurrentTaskCost) return null;

            State.Coins -= CurrentTaskCost;
            if (style != null) State.TaskStyles[task.Id] = style;
            State.TaskIndex++;
            AddXp(Config.TaskXp(State.Chapter));
            TaskCompleted?.Invoke(task);
            if (State.TaskIndex >= ChapterTasks.Tasks.Count) FinishChapter();
            Changed?.Invoke();
            return task;
        }

        // ---------- Memories and letters ----------

        // Keeps the best result; stars never block the story (SYSTEMS_B.md section 5).
        public void RecordMemory(string memoryId, int stars)
        {
            if (!State.MemoryStars.TryGetValue(memoryId, out int best) || stars > best) State.MemoryStars[memoryId] = stars;
            Changed?.Invoke();
        }

        // A letter is waiting in the counter drawer once its memory has been played and until it is read.
        public string LetterWaiting()
        {
            if (Content.Memories == null) return null;
            foreach (var memory in Content.Memories)
            {
                string letter = memory.Value.Letter;
                if (letter != null && State.MemoryStars.ContainsKey(memory.Key) && !State.LettersFound.Contains(letter)) return letter;
            }
            return null;
        }

        public void FindLetter(string letterId)
        {
            if (!State.LettersFound.Contains(letterId)) State.LettersFound.Add(letterId);
            Changed?.Invoke();
        }

        // The chapter's gem reward is granted by the platform, like every gem. Its generator copy lands on the board
        // to be merged with the matching generator.
        void FinishChapter()
        {
            var chapter = Config.Chapter(State.Chapter);
            State.Energy += chapter.Reward.Energy;
            var def = Config.GeneratorDef(chapter.Reward.GeneratorCopy);
            if (def != null) Place(new Item(def.Chain, 1, def.Id));
            int finished = State.Chapter;
            State.Chapter++;
            State.TaskIndex = 0;
            UnlockGenerators();
            ChapterCompleted?.Invoke(finished);
        }
    }
}
