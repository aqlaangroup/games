using System.Collections.Generic;
using Gahwit.Core;
using Newtonsoft.Json.Linq;

namespace Gahwit.Save
{
    // The save file, shaped exactly like design/save_schema.json (written in camelCase).
    // Sections the game doesn't use yet (regulars, recipes, memories, wardrobe, events, daily) are kept as they are,
    // so a save written by a newer build loses nothing here.
    public sealed class SaveData
    {
        public const int CurrentSchema = 13;

        public int SchemaVersion = CurrentSchema;
        public long SaveVersion;
        public string SavedAt;
        public string Rng;
        public ClockSave Clock = new ClockSave();
        public AccountSave Account = new AccountSave();
        public TutorialSave Tutorial = new TutorialSave();
        public PlayerSave Player = new PlayerSave();
        public BoardSave Board = new BoardSave();
        public List<OrderSave> Orders = new List<OrderSave>();
        public StorySave Story = new StorySave();
        public CafeSave Cafe = new CafeSave();
        public JObject Regulars = new JObject();
        public JObject Recipes = new JObject();
        public JObject Purchases = new JObject(); // applied: platform purchase ids already in this save; entitlements
        public JObject Memories = new JObject();
        public JObject Wardrobe = new JObject();
        public JObject Events = new JObject();
        public JObject Daily = new JObject();
        public KitchenSave Kitchen = new KitchenSave();
        public SettingsSave Settings = new SettingsSave();
    }

    public sealed class KitchenSave
    {
        public List<string> Notebook = new List<string>(); // Teta's notebook: "coffee_6" for every item made at least once
    }

    public sealed class ClockSave
    {
        public long TrustedMs;
        public long UptimeMs;
        public long LastSeenMs;
        public bool FromServer;
    }

    // From the age gate (COMPLETE_SPEC.md section 14). Null birthYear means the gate hasn't been answered.
    public sealed class AccountSave
    {
        public int? BirthYear;
        public bool ParentConsent;
    }

    public sealed class TutorialSave
    {
        public int LastStep;
        public bool Done;
        public int EasyOrdersLeft;
    }

    public sealed class PlayerSave
    {
        public int Level = 1;
        public int Xp;
        public int Coins;
        public int Energy;
        public long EnergyStampMs;
        public string LastServerTime;
        public int OrdersServed;
        public List<string> UnlocksShown = new List<string>();
    }

    public sealed class BoardSave
    {
        public int Cols;
        public int Rows;
        public List<CellSave> Cells = new List<CellSave>();
        public List<PieceSave> Storage = new List<PieceSave>();
        public int StorageSlots;
    }

    // {"i": 0, "chain": "coffee", "level": 3} for an item, {"i": 9, "gen": "gen_coffee", "genLevel": 2} for a generator.
    public sealed class CellSave
    {
        public int I;
        public string Chain;
        public int? Level;
        public string Gen;
        public int? GenLevel;
    }

    public sealed class PieceSave
    {
        public string Chain;
        public int? Level;
        public string Gen;
        public int? GenLevel;
    }

    public sealed class OrderSave
    {
        public string Id;
        public List<OrderItemSave> Items = new List<OrderItemSave>();
        public int Coins;
        public int Xp;
        public string Character; // null for a walk-in
        public long CreatedMs;   // when the customer walked in (trusted time)
        public string Recipe;    // a signature dish, or null
        public string Story;     // a story order's id, or null
    }

    public sealed class OrderItemSave
    {
        public string Chain;
        public int Level;
    }

    public sealed class StorySave
    {
        public int Chapter = 1;
        public int Task;
        public Dictionary<string, string> Choices = new Dictionary<string, string>();
        public List<string> ScenesSeen = new List<string>();
        public List<string> LettersFound = new List<string>();
        public List<string> OrdersDone = new List<string>(); // story orders served
    }

    public sealed class CafeSave
    {
        public string Space = "floor";
        public JArray Placed = new JArray();
        public List<string> Owned = new List<string>();
        public Dictionary<string, string> TaskStyles = new Dictionary<string, string>();
        public int Cosiness;
    }

    public sealed class SettingsSave
    {
        public bool Sound = true;
        public bool Music = true;
        public bool Haptics = true;
        public NotificationSwitches Notifications = new NotificationSwitches();
        public bool Glossary = true;
        public bool Voice; // full dialogue voice; off means only the main characters speak (screens/Settings)
        public bool LargeText;
        public string Quality = "auto";
        public bool ReducedMotion;
        public bool MemoryAssist; // the memory games' easy assist: a wider target (COMPLETE_SPEC.md section 5)
        public bool Subtitles;    // neutral-Arabic subtitles under the dialect lines (COMPLETE_SPEC.md section 4)
        public string Language = "ar";
    }
}
