using System;
using System.Collections.Generic;
using System.Linq;

namespace Gahwit.Merge
{
    // A floating copy after a big merge (GAME_SYSTEMS.md section 7); bought with gems through the platform.
    public sealed class Bubble
    {
        public string Chain;
        public int Level;
        public long ExpiresMs;
    }

    // The kitchen's extras around the board: storage, the free-space offer, bubbles, and what purchases add.
    public sealed partial class Game
    {
        public event Action<Bubble> BubbleShown;

        // ---------- Storage (GAME_SYSTEMS.md section 2) ----------

        // Opens at level 2 with board.inventorySlots slots; slots bought with gems add to them (State.StorageSlots).
        // Rewards that don't fit the board wait here too, even past the limit; only storing by hand is limited.
        public bool StorageOpen => State.Level >= Config.Unlocks.Level("storage");
        public int StorageCapacity => StorageOpen ? Config.Board.InventorySlots + State.StorageSlots : 0;

        public bool CanStore(int cell) => StorageOpen && cell >= 0 && cell < CellCount && State.Board[cell] != null && State.Storage.Count < StorageCapacity;

        public bool Store(int cell)
        {
            if (!CanStore(cell)) return false;
            State.Storage.Add(State.Board[cell]);
            State.Board[cell] = null;
            FillOrders();
            Changed?.Invoke();
            return true;
        }

        public bool TakeOut(int index)
        {
            if (index < 0 || index >= State.Storage.Count) return false;
            int cell = NearestEmptyCell(CellCount - 1 - Cols / 2);
            if (cell < 0) return false;
            State.Board[cell] = State.Storage[index];
            State.Storage.RemoveAt(index);
            Changed?.Invoke();
            return true;
        }

        // Slot n (1 = the first one bought) after the platform took its gems.
        public void AddStorageSlot(int slot)
        {
            State.StorageSlots = Math.Max(State.StorageSlots, slot);
            Changed?.Invoke();
        }

        public int NextStorageSlot => State.StorageSlots < Config.Board.InventorySlotGemCosts.Count ? State.StorageSlots + 1 : -1;

        // ---------- Free space (GAME_SYSTEMS.md section 2) ----------

        public bool NeedsSpace => EmptyCells * 100 < CellCount * Config.Board.FreeSpaceTargetPct;

        // The lowest items no order wants, until a quarter of the board would be free.
        public List<int> SpacePlan()
        {
            int needed = (CellCount * Config.Board.FreeSpaceTargetPct + 99) / 100 - EmptyCells;
            return Enumerable.Range(0, CellCount)
                .Where(i => State.Board[i] != null && !State.Board[i].IsGenerator && !IsWanted(State.Board[i]))
                .OrderBy(i => State.Board[i].Level)
                .Take(Math.Max(0, needed))
                .ToList();
        }

        // Clears the plan: stores what fits in storage when asked to, sells the rest. Returns the coins earned.
        public int ClearSpace(bool store)
        {
            int coins = 0;
            foreach (int cell in SpacePlan())
            {
                if (store && State.Storage.Count < StorageCapacity)
                {
                    State.Storage.Add(State.Board[cell]);
                    State.Board[cell] = null;
                }
                else coins += Sell(cell);
            }
            Changed?.Invoke();
            return coins;
        }

        // ---------- Bubbles (GAME_SYSTEMS.md section 7) ----------

        public Bubble Bubble { get; private set; }

        public int BubbleGems(int level) => Config.Formulas.BubbleGems.Base << Math.Max(0, level - Config.Formulas.BubbleGems.FromLevel);

        void MaybeBubble(Item merged)
        {
            if (merged == null || merged.IsGenerator || merged.Level < Config.Formulas.BubbleGems.FromLevel) return;
            if (rng.NextDouble() >= Config.Bubbles.Chance) return;
            Bubble = new Bubble { Chain = merged.Chain, Level = merged.Level, ExpiresMs = ClockMs + Config.Bubbles.LifetimeSeconds * 1000L };
            BubbleShown?.Invoke(Bubble);
        }

        // After the platform took the bubble's gems.
        public void TakeBubble()
        {
            if (Bubble == null) return;
            AddPiece(Bubble.Chain, Bubble.Level);
            Bubble = null;
        }

        public void LetBubbleGo()
        {
            Bubble = null;
            Changed?.Invoke();
        }

        // ---------- What purchases add (only after the platform's reply) ----------

        public void AddPiece(string chain, int level)
        {
            Place(new Item(chain, level));
            Changed?.Invoke();
        }

        public int SecondsToFullEnergy(long nowMs) =>
            State.Energy >= EnergyCap ? 0 : SecondsToNextEnergy(nowMs) + (EnergyCap - State.Energy - 1) * Config.Energy.RegenSeconds;

        public void AddEnergy(int amount)
        {
            State.Energy += amount;
            Changed?.Invoke();
        }
    }
}
