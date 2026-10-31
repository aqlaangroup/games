using System;
using System.Linq;

namespace Gahwit.Merge
{
    // The merge board (GAME_SYSTEMS.md sections 2–4): generator taps, placing pieces, merging and selling.
    public sealed partial class Game
    {
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
            Progress("tap_generators", 1);
            Progress("spend_energy", cost);
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
                Progress("merge", 1);
                if (!moving.IsGenerator && moving.Level + 1 == 5) Progress("make_level5", 1);
                MaybeBubble(State.Board[to]);
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
            Progress("sell", 1);
            Progress("earn_coins", coins);
            Changed?.Invoke();
            return coins;
        }
    }
}
