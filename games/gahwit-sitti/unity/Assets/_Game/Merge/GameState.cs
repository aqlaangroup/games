using System.Collections.Generic;

namespace Gahwit.Merge
{
    // Everything a save holds. Plain data so it serialises straight to JSON.
    // No gems: the platform's wallet owns them (CLAUDE.md, save_schema.json).
    public sealed class GameState
    {
        public int Version = 1;
        public Item[] Board;
        public List<Generator> Generators = new List<Generator>();
        public int Energy;
        public long EnergyStamp; // unix seconds the next refill counts from
        public int Coins;
        public int Xp; // progress inside the current level
        public int Level = 1;
        public List<Order> Orders = new List<Order>();
        public int NextOrderId = 1;
        public int OrdersServed;
        public int Chapter = 1;
        public int TaskIndex;
        public List<string> CompletedTasks = new List<string>();
        public bool IntroSeen;
        public ulong Rng;
    }

    public sealed class Item
    {
        public string Chain;
        public int Level; // 1-based

        public Item() { }

        public Item(string chain, int level)
        {
            Chain = chain;
            Level = level;
        }

        public bool Is(string chain, int level) => Chain == chain && Level == level;

        public bool SameAs(Item other) => other != null && other.Is(Chain, Level);
    }

    public sealed class Generator
    {
        public string Id;
        public string Chain;
        public int Level = 1;
    }

    public sealed class Order
    {
        public int Id;
        public string Customer;
        public bool IsCharacter;
        public List<OrderLine> Lines = new List<OrderLine>();
        public int Units;
        public int Coins;
        public int Xp;
    }

    public sealed class OrderLine
    {
        public string Chain;
        public int Level;
        public int Count;
    }
}
