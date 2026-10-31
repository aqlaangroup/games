using Gahwit.Core;
using Gahwit.Merge;
using NUnit.Framework;

namespace Gahwit.Wardrobe.Tests
{
    public sealed class WardrobeRulesTests
    {
        const long Now = 1_000_000_000;

        BalanceConfig config;
        ChapterContent content;
        WardrobeContent wardrobe;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
            wardrobe = WardrobeContent.Load();
        }

        Game NewGame(int level = 4, int coins = 5000)
        {
            var game = new Game(config, content, Game.NewState(config, content, 5, Now));
            game.State.Level = level;
            game.State.Coins = coins;
            return game;
        }

        [Test]
        public void RingOneHasFivePiecesAndEveryOneIsInASlot()
        {
            Assert.AreEqual(5, wardrobe.Pieces.Count);
            var slots = new[] { "top", "bottom_or_dress", "outer", "shoes", "accessory", "hijab_optional" };
            Assert.IsTrue(wardrobe.Pieces.TrueForAll(p => System.Array.IndexOf(slots, p.Slot) >= 0));
            Assert.IsNotNull(wardrobe.Occasions.Find(o => o.Chapter == 1));
        }

        [Test]
        public void BuyingAndWearingSwapsWhatIsInTheSlot()
        {
            var game = NewGame();
            var rules = new WardrobeRules(game, wardrobe);
            int coins = game.State.Coins;
            Assert.IsTrue(rules.Buy("pants_sage"));
            Assert.AreEqual(coins - config.LevelB.Wardrobe.PriceCoinsByTier[0], game.State.Coins);
            Assert.IsTrue(rules.Toggle("pants_sage"));
            Assert.AreEqual("pants_sage", rules.Worn("bottom_or_dress"));
            Assert.IsTrue(rules.Toggle("pants_sage"));
            Assert.IsNull(rules.Worn("bottom_or_dress"), "wearing it again takes it off");
            Assert.IsFalse(rules.Toggle("cardigan_cream"), "not owned");
        }

        [Test]
        public void TheWardrobeOpensAtLevelFour()
        {
            Assert.IsFalse(new WardrobeRules(NewGame(level: 3), wardrobe).Buy("pants_sage"));
            Assert.IsTrue(new WardrobeRules(NewGame(level: 4), wardrobe).Buy("pants_sage"));
        }

        [Test]
        public void DressingForTheOccasionPaysHeartsOnceAndNeverBlocks()
        {
            var game = NewGame();
            var rules = new WardrobeRules(game, wardrobe);
            var lunch = rules.CurrentOccasion;
            Assert.AreEqual(1, rules.Hearts(lunch), "always at least one heart");

            foreach (var id in new[] { "top_blush_linen", "cardigan_cream", "hijab_lilac" })
            {
                rules.Buy(id);
                rules.Toggle(id);
            }
            Assert.AreEqual(2, rules.Hearts(lunch));

            game.State.Coins = 100;
            game.CompleteTask("old");    // sign
            game.CompleteTask(null);     // floor
            game.CompleteTask("old");    // chairs
            game.State.Coins += 100;
            game.CompleteTask(null);     // dallahs: earns the dress
            Assert.IsTrue(rules.Owns("dress_embroidered"));
            rules.Toggle("dress_embroidered");
            Assert.AreEqual(3, rules.Hearts(lunch));

            int coins = game.State.Coins;
            Assert.AreEqual(3, rules.DressFor(lunch));
            Assert.AreEqual(coins + config.LevelB.Wardrobe.OccasionBonusPerHeartPerChapter * 3, game.State.Coins);
            Assert.AreEqual(0, rules.DressFor(lunch), "paid once");
        }
    }
}
