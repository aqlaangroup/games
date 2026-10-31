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
    // This file holds the state, events and energy; the board, orders, progression and tasks have their own files.
    public sealed partial class Game
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
        public event Action<Order> OrderDropped;

        // The café's time of day ("morning", "afternoon", "evening", "night"); regulars only order on their schedule.
        public string TimeOfDay { get; set; } = "morning";

        // The trusted time of the last Tick, which stamps when each customer walked in.
        public long ClockMs { get; private set; }

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
        public bool English { get; set; } // names in English instead of Arabic (the settings' language)

        public string ItemName(string chain, int level)
        {
            var names = Config.Chains[chain].Items[level - 1];
            return English && names.Count > 1 ? names[1] : names[0];
        }

        public string GeneratorName(string id) => English ? Config.GeneratorDef(id).En : Config.GeneratorDef(id).Ar;
        public int EmptyCells => State.Board.Count(item => item == null);

        IEnumerable<Item> AllPieces => State.Board.Where(i => i != null).Concat(State.Storage);

        // For rule classes outside Game (decor, customers) that change the state: tells everyone it changed.
        public void Touch() => Changed?.Invoke();

        // ---------- Energy ----------

        public void Tick(long nowMs)
        {
            ClockMs = nowMs;
            if (Bubble != null && nowMs >= Bubble.ExpiresMs) Bubble = null;
            foreach (var order in State.Orders)
                if (order.CreatedMs == 0) order.CreatedMs = nowMs; // orders from before arrival times were saved
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

        public void SpendEnergy(int amount, long nowMs)
        {
            // Refill starts counting the moment energy drops below the cap.
            if (State.Energy >= EnergyCap && State.Energy - amount < EnergyCap) State.EnergyStampMs = nowMs;
            State.Energy -= amount;
            AddPassPoints(amount * Config.Pass.PointsPerEnergySpent);
        }
    }
}
