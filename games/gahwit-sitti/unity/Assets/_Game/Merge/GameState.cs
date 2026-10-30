using System.Collections.Generic;

namespace Gahwit.Merge
{
    // The runtime game state. The save system maps it to and from design/save_schema.json.
    // No gems: the platform's wallet owns them (CLAUDE.md).
    public sealed class GameState
    {
        public Item[] Board;
        public List<Item> Storage = new List<Item>();
        public int StorageSlots;
        public int Energy;
        public long EnergyStampMs; // trusted time (TimeService) the next refill counts from
        public int Coins;
        public int Xp; // progress inside the current level
        public int Level = 1;
        public List<Order> Orders = new List<Order>();
        public int NextOrderId = 1;
        public int OrdersServed;
        public int Chapter = 1;
        public int TaskIndex;
        public Dictionary<string, string> TaskStyles = new Dictionary<string, string>();
        public Dictionary<string, string> Choices = new Dictionary<string, string>();
        public List<string> ScenesSeen = new List<string>();
        public List<string> LettersFound = new List<string>();
        public Dictionary<string, int> MemoryStars = new Dictionary<string, int>(); // best stars per memory
        public int TutorialStep;
        public bool TutorialDone;
        public ulong Rng;
    }

    // A board piece: an item of a chain, or a generator when Generator is set (then Level is the generator's level).
    public sealed class Item
    {
        public string Chain;
        public int Level;
        public string Generator;

        public Item() { }

        public Item(string chain, int level, string generator = null)
        {
            Chain = chain;
            Level = level;
            Generator = generator;
        }

        public bool IsGenerator => Generator != null;

        public bool Is(string chain, int level) => Generator == null && Chain == chain && Level == level;

        public bool SameAs(Item other) =>
            other != null && other.Generator == Generator && other.Chain == Chain && other.Level == Level;
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
