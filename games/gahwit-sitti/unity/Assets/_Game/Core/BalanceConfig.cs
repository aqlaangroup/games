using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Core
{
    // Typed view of design/balance.json, which the editor copies into Resources/Config.
    // Field names match the JSON keys; Newtonsoft matches them case-insensitively and skips keys the game doesn't read.
    public sealed class BalanceConfig
    {
        public const string ResourcePath = "Config/balance";

        public FormulasConfig Formulas;
        public BoardConfig Board;
        public BubblesConfig Bubbles;
        public EnergyConfig Energy;
        public GeneratorsConfig Generators;
        public Dictionary<string, ChainConfig> Chains;
        public OrdersConfig Orders;
        public List<ChapterConfig> Chapters;
        public PlayerLevelsConfig PlayerLevels;
        public UnlocksConfig Unlocks;
        public TutorialConfig Tutorial;
        public SavesConfig Saves;
        public NotificationsConfig Notifications;
        public WeatherConfig Weather;
        public UiConfig Ui;
        public LevelBConfig LevelB;

        public static BalanceConfig Parse(string json) => JsonConvert.DeserializeObject<BalanceConfig>(json);

        public static BalanceConfig Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Missing Resources/{ResourcePath}.json. Run Gahwit Sitti > Sync balance.json.");
            return Parse(asset.text);
        }

        public int ItemUnits(int level) => (int)Math.Pow(Formulas.ItemUnits.Base, level - 1);

        public int XpToNext(int level)
        {
            var f = Formulas.XpToNext;
            return f.Base + f.PerLevel * level + f.PerLevelSquared * level * level;
        }

        public int LevelUpGems(int newLevel) => Formulas.LevelUpGems.Base + newLevel / Formulas.LevelUpGems.LevelDivisor;

        public float OrderSizeUnits(int playerLevel) => Formulas.OrderSizeUnits.Base + Formulas.OrderSizeUnits.PerLevel * playerLevel;

        public int TaskXp(int chapter) => Formulas.TaskXp.Base + Formulas.TaskXp.PerChapter * chapter;

        public int MaxItemLevel(string chain) => Chains[chain].Items.Count;

        public int OrderSlots(int playerLevel) => StepLookup(Orders.SlotsByPlayerLevel, playerLevel);

        public int MaxOrderItemLevel(int playerLevel) => StepLookup(Orders.MaxItemLevelByPlayerLevel, playerLevel);

        public ChapterConfig Chapter(int n) => Chapters.Find(c => c.N == n);

        public GeneratorDef GeneratorDef(string id) => Generators.List.Find(g => g.Id == id);

        // Value of the highest key at or below the level, for tables like {"1": 3, "4": 4, "12": 5}.
        public static int StepLookup(Dictionary<string, int> table, int level)
        {
            int bestKey = int.MinValue, value = 0;
            foreach (var kv in table)
            {
                int key = int.Parse(kv.Key);
                if (key <= level && key > bestKey)
                {
                    bestKey = key;
                    value = kv.Value;
                }
            }
            return value;
        }
    }

    public sealed class FormulasConfig
    {
        public PowerFormula ItemUnits;
        public int SellCoinsPerLevel;
        public LinearFormula OrderSizeUnits;
        public int OrderMaxItemsPerLine;
        public OrderLevelPickConfig OrderLevelPick;
        public int OrderMinCoins;
        public QuadraticFormula XpToNext;
        public LevelUpGemsFormula LevelUpGems;
        public BubbleGemsFormula BubbleGems;
        public TaskXpFormula TaskXp;
    }

    public sealed class PowerFormula { public int Base; }
    public sealed class LinearFormula { public float Base; public float PerLevel; }
    public sealed class QuadraticFormula { public int Base; public int PerLevel; public int PerLevelSquared; }
    public sealed class LevelUpGemsFormula { public int Base; public int LevelDivisor; }
    public sealed class BubbleGemsFormula { public int Base; public int FromLevel; }
    public sealed class TaskXpFormula { public int Base; public int PerChapter; }
    public sealed class OrderLevelPickConfig { public float TopLevelChance; public int Spread; }

    public sealed class BoardConfig
    {
        public int Cols;
        public int Rows;
        public int InventorySlots;
        public List<int> InventorySlotGemCosts;
        public int FreeSpaceTargetPct;
        public int FreeSpaceOfferAfterHoursAway;
    }

    public sealed class BubblesConfig
    {
        public float Chance;
        public int LifetimeSeconds;
        public int RewardedAdPopsPerDay;
    }

    public sealed class EnergyConfig
    {
        public int Cap;
        public int RegenSeconds;
        public int StartingEnergy;
        public int RewardedAdAmount;
        public GemRefillConfig GemRefill;
    }

    public sealed class GemRefillConfig
    {
        public int Amount;
        public List<int> GemCostsByPurchaseOfDay;
    }

    public sealed class GeneratorsConfig
    {
        public int TapEnergyCost;
        public Dictionary<string, GeneratorLevelOutput> LevelOutput;
        public List<GeneratorDef> List;
    }

    public sealed class GeneratorLevelOutput
    {
        public Dictionary<string, float> ItemLevels;
        public float UnitsPerTap;
    }

    public sealed class GeneratorDef
    {
        public string Id;
        public string Chain;
        public string Ar;
        public string En;
        public int UnlockChapter;
    }

    public sealed class ChainConfig
    {
        public string Ar;
        public int UnlockChapter;
        public List<List<string>> Items; // [arabic, english] per level
    }

    public sealed class OrdersConfig
    {
        public float CoinsPerUnit;
        public float XpPerUnit;
        public Dictionary<string, int> SlotsByPlayerLevel;
        public Dictionary<string, int> MaxItemLevelByPlayerLevel;
        public float TwoItemOrderChance;
        public int TwoItemOrderFromLevel;
        public float CharacterOrderChance;
    }

    public sealed class ChapterConfig
    {
        public int N;
        public string Ar;
        public string En;
        public List<int> TaskCosts;
        public ChapterReward Reward;
        public int TargetDay; // the typical player's finish day (GAME_SYSTEMS.md section 9)
    }

    public sealed class ChapterReward
    {
        public int Gems;
        public int Energy;
        public string GeneratorCopy;
    }

    public sealed class PlayerLevelsConfig
    {
        public PlayerLevelRewards Rewards;
    }

    public sealed class PlayerLevelRewards
    {
        public int Energy; // gems come from Formulas.LevelUpGems
    }

    public sealed class UnlocksConfig
    {
        public Dictionary<string, List<string>> ByPlayerLevel;
        public Dictionary<string, List<string>> ByChapter;
        public int UnlockCardsPerSession;

        // The player level a feature opens at (balance.json unlocks), or 1 when it isn't listed.
        public int Level(string feature)
        {
            foreach (var pair in ByPlayerLevel)
                if (pair.Value.Contains(feature)) return int.Parse(pair.Key);
            return 1;
        }
    }

    public sealed class TutorialConfig
    {
        public int EasyOrdersAfter;
        public EasyOrderConfig EasyOrder;
    }

    public sealed class EasyOrderConfig
    {
        public int Lines;
        public int MaxItemLevel;
    }

    public sealed class WeatherConfig
    {
        public List<int> WinterMonths;
        public float RainChance;
        public float FogChance;
    }

    public sealed class NotificationsConfig
    {
        public int MaxPerDay;
        public int QuietHoursStart;
        public int QuietHoursEnd;
        public int AwayDays;
        public int EventEndingHours;
        public int TaskReminderHours;
    }

    public sealed class SavesConfig
    {
        public float LocalEverySeconds;
        public float CloudEverySeconds;
    }

    public sealed class UiConfig
    {
        public float LargeTextScale;
        public float MinTextPt;
        public float MinTouchTargetPt;
        public float PanelSlideSeconds;
        public float SmallestPhoneWidthPt;
        public GraphicsConfig Graphics;
        public KitchenHintsConfig KitchenHints;
    }

    public sealed class GraphicsConfig
    {
        public int AutoLowAtOrBelowMb;
        public int LowFps;
        public int HighFps;
        public float PerfSampleSeconds;
    }

    public sealed class KitchenHintsConfig
    {
        public int LowEnergy;
        public int NearlyFullEmptySquares;
    }

    public sealed class LevelBConfig
    {
        public CafeConfig Cafe;
        public WalkInsConfig WalkIns;
        public WardrobeConfig Wardrobe;
        public RegularsConfig Regulars;
        public MemoriesConfig Memories;
        public RecipesConfig Recipes;
    }

    public sealed class RecipesConfig
    {
        public int Season1Count;
        public float SignatureCoinMultiplier;
        public int HintAfterWrongTries;
        public int StoryClueRecipes;
        public float SignatureOrderChance;
    }

    public sealed class CafeConfig
    {
        public Dictionary<string, int> TimeOfDayStartHours; // morning, afternoon, evening, night
        public float GridCellMeters;
        public List<string> Styles;
        public int StyleSetBonusAt;
        public int StyleSetBonus;
        public List<int> DecorPriceCoinsByTier;
        public List<int> CosinessPerPiece; // [min, max]
        public MinMax WalkInTables;
        public List<int> CosinessForExtraTable;
        public float TipBonusPerCosinessPoint;
        public float TipBonusMax;
    }

    public sealed class MinMax
    {
        public int Min;
        public int Max;
    }

    public sealed class WardrobeConfig
    {
        public List<int> PriceCoinsByTier;
        public int OccasionBonusPerHeartPerChapter;
        public List<int> HeartsAtMatches; // matching pieces worn for 1, 2 and 3 hearts
    }

    public sealed class WalkInsConfig
    {
        public List<int> TipCoins; // [min, max]
        public int LeaveAfterMinutes;
    }

    public sealed class RegularsConfig
    {
        public int FriendshipMax;
        public int PointsFavourite;
        public int PointsOther;
        public int PointsToNextPerLevel; // level L needs PointsToNextPerLevel * L points to reach L + 1
        public List<int> StoryAtLevels;
        public Dictionary<string, List<string>> Schedule; // times of day each regular visits
    }

    public sealed class MemoriesConfig
    {
        public List<int> Stars; // [min, max]
        public Dictionary<string, string> ByChapter;
        public int SkipRetryGems;
        public PourConfig Pour;
        public RhythmConfig Rhythm;
        public FindConfig Find;
        public RestoreConfig Restore;
        public TraceConfig Trace;
        public TuneConfig Tune;
        public ChooseConfig Choose;
        public ReassembleConfig Reassemble;
    }

    public sealed class RhythmConfig
    {
        public float BeatsPerMinute;
        public float PerfectWindowMs;
        public float GoodWindowMs;
        public float EasyWindowScale;
        public int Beats;
        public List<float> StarsAtAccuracy;
    }

    public sealed class FindConfig
    {
        public int Objects;
        public float Seconds;
        public float HintAfterSeconds;
        public List<float> StarsAtSecondsLeft;
    }

    public sealed class RestoreConfig
    {
        public int Pieces;
        public float SnapDistance;
        public float Seconds;
        public List<float> StarsAtSecondsLeft;
    }

    public sealed class TraceConfig
    {
        public int PathPoints;
        public float ToleranceCm;
        public float EasyToleranceScale;
        public List<float> StarsAtAccuracy;
    }

    public sealed class TuneConfig
    {
        public List<float> DialRange; // [low, high] MHz
        public float TargetWidth;
        public float HintWidth;
        public float HoldSeconds;
        public float EasyWidthScale;
        public float Seconds;
        public List<float> StarsAtSecondsLeft;
    }

    public sealed class ChooseConfig
    {
        public int Choices;
        public float SecondsPerChoice;
        public float EasyTimeScale;
        public List<float> StarsAtGoodChoices;
    }

    public sealed class ReassembleConfig
    {
        public int Strips;
        public float Seconds;
        public List<float> StarsAtSecondsLeft;
    }

    public sealed class PourConfig
    {
        public float FillPerSecond;
        public float TargetLow;
        public float TargetHigh;
        public float EasyWiden;
    }
}
