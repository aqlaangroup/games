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
        public EnergyConfig Energy;
        public GeneratorsConfig Generators;
        public Dictionary<string, ChainConfig> Chains;
        public OrdersConfig Orders;
        public List<ChapterConfig> Chapters;
        public PlayerLevelsConfig PlayerLevels;
        public TutorialConfig Tutorial;
        public SavesConfig Saves;
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
        public TaskXpFormula TaskXp;
    }

    public sealed class PowerFormula { public int Base; }
    public sealed class LinearFormula { public float Base; public float PerLevel; }
    public sealed class QuadraticFormula { public int Base; public int PerLevel; public int PerLevelSquared; }
    public sealed class LevelUpGemsFormula { public int Base; public int LevelDivisor; }
    public sealed class TaskXpFormula { public int Base; public int PerChapter; }
    public sealed class OrderLevelPickConfig { public float TopLevelChance; public int Spread; }

    public sealed class BoardConfig
    {
        public int Cols;
        public int Rows;
    }

    public sealed class EnergyConfig
    {
        public int Cap;
        public int RegenSeconds;
        public int StartingEnergy;
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

    public sealed class SavesConfig
    {
        public float LocalEverySeconds;
        public float CloudEverySeconds;
    }

    public sealed class UiConfig
    {
        public KitchenHintsConfig KitchenHints;
    }

    public sealed class KitchenHintsConfig
    {
        public int LowEnergy;
        public int NearlyFullEmptySquares;
    }

    public sealed class LevelBConfig
    {
        public CafeConfig Cafe;
        public MemoriesConfig Memories;
    }

    public sealed class CafeConfig
    {
        public Dictionary<string, int> TimeOfDayStartHours; // morning, afternoon, evening, night
    }

    public sealed class MemoriesConfig
    {
        public List<int> Stars; // [min, max]
        public PourConfig Pour;
    }

    public sealed class PourConfig
    {
        public float FillPerSecond;
        public float TargetLow;
        public float TargetHigh;
        public float EasyWiden;
    }
}
