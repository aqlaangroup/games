using System.Linq;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Merge.Tests
{
    public sealed class KitchenExtrasTests
    {
        const long Now = 1_000_000_000;

        BalanceConfig config;
        ChapterContent content;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
        }

        Game NewGame(int level = 2)
        {
            var game = new Game(config, content, Game.NewState(config, content, 9, Now));
            game.State.Level = level;
            game.Tick(Now);
            return game;
        }

        static int FirstItem(Game game) => System.Array.FindIndex(game.State.Board, i => i != null && !i.IsGenerator);

        [Test]
        public void StorageOpensAtLevelTwoAndHoldsUpToItsSlots()
        {
            var early = NewGame(level: 1);
            Assert.IsFalse(early.Store(FirstItem(early)));

            var game = NewGame();
            Assert.AreEqual(config.Board.InventorySlots, game.StorageCapacity);
            for (int n = 0; n < config.Board.InventorySlots; n++)
            {
                int cell = System.Array.FindIndex(game.State.Board, i => i == null);
                game.State.Board[cell] = new Item("coffee", 1);
                Assert.IsTrue(game.Store(cell));
            }
            int another = System.Array.FindIndex(game.State.Board, i => i == null);
            game.State.Board[another] = new Item("coffee", 1);
            Assert.IsFalse(game.Store(another), "full");

            game.AddStorageSlot(1);
            Assert.AreEqual(config.Board.InventorySlots + 1, game.StorageCapacity);
            Assert.IsTrue(game.Store(another));
            Assert.IsTrue(game.TakeOut(0));
            Assert.AreEqual(config.Board.InventorySlots, game.State.Storage.Count);
        }

        [Test]
        public void TheFreeSpaceOfferClearsLowItemsNoOrderWants()
        {
            var game = NewGame();
            for (int i = 0; i < game.State.Board.Length; i++)
                if (game.State.Board[i] == null) game.State.Board[i] = new Item("sweets", 1 + i % 2);
            Assert.IsTrue(game.NeedsSpace);
            var plan = game.SpacePlan();
            Assert.IsTrue(plan.All(cell => !game.State.Board[cell].IsGenerator && !game.IsWanted(game.State.Board[cell])));
            int coins = game.State.Coins;
            int earned = game.ClearSpace(store: false);
            Assert.Greater(earned, 0);
            Assert.AreEqual(coins + earned, game.State.Coins);
            Assert.IsFalse(game.NeedsSpace);
        }

        [Test]
        public void BigMergesSometimesFloatABubbleThatLasts()
        {
            var game = NewGame();
            Bubble shown = null;
            game.BubbleShown += b => shown = b;
            int lifetime = config.Bubbles.LifetimeSeconds;
            for (int n = 0; n < 400 && shown == null; n++)
            {
                game.State.Board[0] = new Item("coffee", 3);
                game.State.Board[1] = new Item("coffee", 3);
                game.Move(0, 1);
            }
            Assert.IsNotNull(shown, "a 6% chance shows up in 400 merges");
            Assert.AreEqual(4, shown.Level);
            Assert.AreEqual(config.Formulas.BubbleGems.Base, game.BubbleGems(4));
            Assert.AreEqual(config.Formulas.BubbleGems.Base * 4, game.BubbleGems(6));
            game.Tick(Now + lifetime * 1000L + 1);
            Assert.IsNull(game.Bubble, "gone after its lifetime");
        }

        [Test]
        public void EachUnlockCardShowsOnceAndOnePerSession()
        {
            var game = NewGame(level: 4);
            var unlocks = new Unlocks(game, new[] { "storage", "wardrobe", "decor_shop" });
            string first = unlocks.NextCard();
            Assert.IsNotNull(first);
            unlocks.Shown(first);
            Assert.IsNull(unlocks.NextCard(), "one card a session");

            var nextSession = new Unlocks(game, new[] { "storage", "wardrobe", "decor_shop" });
            string second = nextSession.NextCard();
            Assert.IsNotNull(second);
            Assert.AreNotEqual(first, second);
            Assert.IsFalse(nextSession.Reached.Contains("recipes"), "level 5");
        }
    }
}
