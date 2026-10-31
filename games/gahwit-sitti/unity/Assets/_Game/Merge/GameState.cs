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
        public int TutorialStep;   // the last finished step of COMPLETE_SPEC.md section 1
        public bool TutorialDone;
        public int EasyOrdersLeft; // free play starts with a few easy orders
        public List<string> UnlocksShown = new List<string>(); // unlock cards already shown
        public int Cosiness;       // from placed decor (Decor.DecorRules)
        public List<string> OwnedDecor = new List<string>();
        public List<PlacedDecor> PlacedDecor = new List<PlacedDecor>();
        public List<string> WardrobeOwned = new List<string>();
        public Dictionary<string, string> WardrobeWorn = new Dictionary<string, string>(); // slot → piece
        public Dictionary<int, int> OccasionHearts = new Dictionary<int, int>();           // chapter → hearts, paid once
        public Dictionary<string, RegularState> Regulars = new Dictionary<string, RegularState>();
        public List<string> RecipesFound = new List<string>();
        public int RecipeWrongStreak; // wrong pairs in a row; a hint shows after a few (Recipes.RecipeBook)
        public List<Item> Pan = new List<Item>(); // up to two items waiting in the cooking pan
        public string AdsDate;    // the local day the ad counts below are for
        public Dictionary<string, int> AdsWatched = new Dictionary<string, int>(); // placement → ads watched that day
        public List<string> PurchasesApplied = new List<string>(); // platform purchase ids whose items are in this save
        public List<string> Entitlements = new List<string>();     // bought switches such as passPremium (missing/18)
        public int LoginDay;          // daily gifts claimed so far; the next gift is dailyLogin[LoginDay % 7]
        public string LastGiftDate;   // the local day the last gift was claimed
        public string TasksDate;      // the local day DailyTasks are for
        public List<DailyTaskState> DailyTasks = new List<DailyTaskState>();
        public int PassPoints;        // from daily tasks, for the season pass (missing/18)
        public List<string> PendingRewards = new List<string>(); // platform reward ids earned in play, not paid yet
        public Dictionary<string, MiniBoardState> MiniBoards = new Dictionary<string, MiniBoardState>(); // by event id
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

    // A decor piece on the café floor: its footprint's corner cell and its quarter turns.
    public sealed class PlacedDecor
    {
        public string Id;
        public int X, Z;
        public int Rot; // 0 to 3
    }

    // A regular's friendship (SYSTEMS_B.md section 3): points inside the current level, like XP.
    public sealed class RegularState
    {
        public int Points;
        public int Level = 1;
        public List<int> StoriesSeen = new List<int>();
    }

    // Each order is a customer in the café (DEVLOG 2026-10-31).
    public sealed class Order
    {
        public int Id;
        public long CreatedMs; // trusted time the customer walked in
        public string Customer;
        public bool IsCharacter;
        public List<OrderLine> Lines = new List<OrderLine>();
        public int Units;
        public int Coins;
        public int Xp;
        public string Recipe; // a signature dish asked for by name (SYSTEMS_B.md section 4), or null
    }

    // One mini board event's progress (missing/18), kept until the event ends.
    public sealed class MiniBoardState
    {
        public Item[] Board;
        public List<int> Orders = new List<int>(); // the level each order wants
        public int Tokens;
        public List<int> Paid = new List<int>();   // milestones (1-based) already rewarded
        public ulong Rng;
    }

    public sealed class DailyTaskState
    {
        public string Id;
        public int Progress;
        public bool Claimed;
    }

    // A found recipe a customer can ask for: its id and the two items that make it.
    public sealed class Signature
    {
        public string Recipe;
        public List<OrderLine> Lines;
    }

    public sealed class OrderLine
    {
        public string Chain;
        public int Level;
        public int Count;
    }
}
