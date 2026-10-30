using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;

namespace Gahwit.Merge
{
    public enum TapResult { Spawned, NoEnergy, BoardFull }

    public enum MoveResult { None, Moved, Swapped, Merged }

    // The rules of the merge kitchen, orders, player levels and café tasks (GAME_SYSTEMS.md sections 1–9).
    // Plain C# with no Unity objects; the views call it and listen to its events.
    public sealed class Game
    {
        // The "FromLevel5" in balance.json's orders.twoItemOrderChanceFromLevel5.
        const int TwoLineOrdersFromLevel = 5;

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

        public static GameState NewState(BalanceConfig config, ChapterContent content, ulong seed, long nowUnix)
        {
            var state = new GameState
            {
                Energy = config.Energy.StartingEnergy,
                EnergyStamp = nowUnix,
                Rng = seed == 0 ? 1 : seed,
                Board = new Item[config.Board.Cols * config.Board.Rows]
            };
            foreach (var start in content.StartingBoard)
                state.Board[start.Row * config.Board.Cols + start.Col] = new Item(start.Chain, start.Level);
            return state;
        }

        public int Cols => Config.Board.Cols;
        public int Rows => Config.Board.Rows;
        public int CellCount => Cols * Rows;
        public int EnergyCap => Config.Energy.Cap;
        public int XpToNext => Config.XpToNext(State.Level);
        public IReadOnlyList<Generator> Generators => State.Generators;
        public IReadOnlyList<Order> Orders => State.Orders;

        public Item ItemAt(int cell) => State.Board[cell];
        public int MaxLevel(string chain) => Config.MaxItemLevel(chain);
        public string ItemName(string chain, int level) => Config.Chains[chain].Items[level - 1][0];
        public int EmptyCells => State.Board.Count(item => item == null);

        // ---------- Energy ----------

        public void Tick(long nowUnix)
        {
            if (State.Energy >= EnergyCap)
            {
                State.EnergyStamp = nowUnix;
                return;
            }
            int regen = Config.Energy.RegenSeconds;
            long gained = (nowUnix - State.EnergyStamp) / regen;
            if (gained <= 0) return;
            State.Energy = (int)Math.Min(EnergyCap, State.Energy + gained);
            State.EnergyStamp += gained * regen;
            if (State.Energy >= EnergyCap) State.EnergyStamp = nowUnix;
            Changed?.Invoke();
        }

        public int SecondsToNextEnergy(long nowUnix) =>
            State.Energy >= EnergyCap ? 0 : (int)Math.Max(0, Config.Energy.RegenSeconds - (nowUnix - State.EnergyStamp));

        void SpendEnergy(int amount, long nowUnix)
        {
            // Refill starts counting the moment energy drops below the cap.
            if (State.Energy >= EnergyCap && State.Energy - amount < EnergyCap) State.EnergyStamp = nowUnix;
            State.Energy -= amount;
        }

        // ---------- Generators ----------

        public TapResult TapGenerator(int index, long nowUnix, out int cell)
        {
            cell = -1;
            var generator = State.Generators[index];
            int cost = Config.Generators.TapEnergyCost;
            if (State.Energy < cost) return TapResult.NoEnergy;
            cell = NearestEmptyCell();
            if (cell < 0) return TapResult.BoardFull;

            SpendEnergy(cost, nowUnix);
            int level = Math.Min(RollGeneratorOutput(generator.Level), MaxLevel(generator.Chain));
            State.Board[cell] = new Item(generator.Chain, level);
            ItemSpawned?.Invoke(cell);
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

        // The empty cell closest to the bottom middle of the board, next to the generators.
        public int NearestEmptyCell()
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            float centreX = (Cols - 1) / 2f, bottomY = Rows - 1;
            for (int i = 0; i < CellCount; i++)
            {
                if (State.Board[i] != null) continue;
                float dx = i % Cols - centreX, dy = i / Cols - bottomY;
                float distance = dx * dx + dy * dy;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        void UnlockGenerators()
        {
            foreach (var def in Config.Generators.List)
            {
                if (def.UnlockChapter > State.Chapter || State.Generators.Any(g => g.Id == def.Id)) continue;
                State.Generators.Add(new Generator { Id = def.Id, Chain = def.Chain, Level = 1 });
            }
        }

        // A chapter's generator copy is merged straight into the matching generator.
        void UpgradeGenerator(string id)
        {
            var generator = State.Generators.Find(g => g.Id == id);
            if (generator == null) return;
            if (Config.Generators.LevelOutput.ContainsKey((generator.Level + 1).ToString())) generator.Level++;
        }

        // ---------- Board ----------

        public bool CanMerge(int a, int b)
        {
            var x = State.Board[a];
            return a != b && x != null && x.SameAs(State.Board[b]) && x.Level < MaxLevel(x.Chain);
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
                State.Board[to] = new Item(moving.Chain, moving.Level + 1);
                State.Board[from] = null;
                ItemsMerged?.Invoke(to);
                Changed?.Invoke();
                return MoveResult.Merged;
            }

            State.Board[to] = moving;
            State.Board[from] = target;
            Changed?.Invoke();
            return MoveResult.Swapped;
        }

        public int SellValue(Item item) => item.Level * Config.Formulas.SellCoinsPerLevel;

        public int Sell(int cell)
        {
            var item = State.Board[cell];
            if (item == null) return 0;
            int coins = SellValue(item);
            State.Board[cell] = null;
            State.Coins += coins;
            Changed?.Invoke();
            return coins;
        }

        // ---------- Orders ----------

        public int CountOnBoard(string chain, int level) => State.Board.Count(item => item != null && item.Is(chain, level));

        public bool CanServe(Order order) => order.Lines.All(line => CountOnBoard(line.Chain, line.Level) >= line.Count);

        public bool IsWanted(Item item) =>
            item != null && State.Orders.Any(o => o.Lines.Any(line => line.Chain == item.Chain && line.Level == item.Level));

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
            var pool = State.Generators.Select(g => g.Chain).Distinct().ToList();
            float size = Config.OrderSizeUnits(State.Level);
            bool twoLines = State.Level >= TwoLineOrdersFromLevel && pool.Count > 1 &&
                            rng.NextDouble() < Config.Orders.TwoItemOrderChanceFromLevel5;
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
            return MakeOrder(Content.WalkIns[rng.Range(0, Content.WalkIns.Count)], false, lines);
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

        Order MakeOrder(string customer, bool isCharacter, List<OrderLine> lines)
        {
            int units = lines.Sum(line => line.Count * Config.ItemUnits(line.Level));
            return new Order
            {
                Id = State.NextOrderId++,
                Customer = customer,
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

        public TaskDef CompleteTask()
        {
            var task = CurrentTask;
            if (task == null || State.Coins < CurrentTaskCost) return null;

            State.Coins -= CurrentTaskCost;
            State.CompletedTasks.Add(task.Id);
            State.TaskIndex++;
            AddXp(Config.TaskXp(State.Chapter));
            TaskCompleted?.Invoke(task);
            if (State.TaskIndex >= ChapterTasks.Tasks.Count) FinishChapter();
            Changed?.Invoke();
            return task;
        }

        // The chapter's gem reward is granted by the platform, like every gem.
        void FinishChapter()
        {
            var chapter = Config.Chapter(State.Chapter);
            State.Energy += chapter.Reward.Energy;
            UpgradeGenerator(chapter.Reward.GeneratorCopy);
            int finished = State.Chapter;
            State.Chapter++;
            State.TaskIndex = 0;
            UnlockGenerators();
            ChapterCompleted?.Invoke(finished);
        }
    }
}
