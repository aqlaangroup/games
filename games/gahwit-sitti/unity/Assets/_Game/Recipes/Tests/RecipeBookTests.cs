using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using NUnit.Framework;

namespace Gahwit.Recipes.Tests
{
    public sealed class RecipeBookTests
    {
        const long Now = 1_000_000_000;

        BalanceConfig config;
        ChapterContent content;
        RecipeContent recipes;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
            recipes = RecipeContent.Load();
        }

        (Game game, RecipeBook book) NewBook(int level = 5, int chapter = 10)
        {
            var game = new Game(config, content, Game.NewState(config, content, 3, Now));
            game.State.Level = level;
            game.State.Chapter = chapter;
            for (int i = 0; i < game.CellCount; i++)
                if (game.State.Board[i] != null && !game.State.Board[i].IsGenerator) game.State.Board[i] = null;
            return (game, new RecipeBook(game, recipes));
        }

        static int Place(Game game, string chain, int level)
        {
            int cell = System.Array.FindIndex(game.State.Board, i => i == null);
            game.State.Board[cell] = new Item(chain, level);
            return cell;
        }

        [Test]
        public void TheSeasonHasTwentyRecipesFiveWithCluesAndEveryPairIsReal()
        {
            Assert.AreEqual(config.LevelB.Recipes.Season1Count, recipes.Recipes.Count);
            Assert.AreEqual(config.LevelB.Recipes.StoryClueRecipes, recipes.Recipes.Count(r => r.Clue));
            foreach (var r in recipes.Recipes)
            {
                Assert.AreNotEqual(r.ChainA, r.ChainB, r.Id);
                Assert.That(r.LevelA, Is.InRange(1, config.MaxItemLevel(r.ChainA)), r.Id);
                Assert.That(r.LevelB, Is.InRange(1, config.MaxItemLevel(r.ChainB)), r.Id);
            }
            Assert.IsNotNull(recipes.Get("two_cities"), "the spec's own example");
        }

        [Test]
        public void ARightPairInEitherOrderFillsAPageAndIsUsedUp()
        {
            var (game, book) = NewBook();
            var two = recipes.Get("two_cities");
            Assert.IsTrue(book.PutInPan(Place(game, two.ChainB, two.LevelB)));
            Assert.IsTrue(book.PutInPan(Place(game, two.ChainA, two.LevelA)));
            RecipeDef found = null;
            book.Discovered += r => found = r;
            Assert.AreEqual(CookResult.Discovered, book.Cook(out _));
            Assert.AreEqual(two, found);
            Assert.IsTrue(book.Found("two_cities"));
            Assert.AreEqual(0, book.Pan.Count);
        }

        [Test]
        public void AWrongPairComesBackAndThreeInARowShowAHint()
        {
            var (game, book) = NewBook();
            Assert.IsNull(book.Hint);
            for (int tries = 0; tries < config.LevelB.Recipes.HintAfterWrongTries; tries++)
            {
                book.PutInPan(Place(game, "coffee", 1));
                book.PutInPan(Place(game, "sweets", 1));
                int before = game.State.Board.Count(i => i != null);
                Assert.AreEqual(CookResult.Wrong, book.Cook(out _));
                Assert.AreEqual(0, book.Pan.Count);
                Assert.AreEqual(before + 2, game.State.Board.Count(i => i != null), "nothing is lost");
            }
            Assert.IsNotNull(book.Hint);
            Assert.IsFalse(book.Found(book.Hint.Id));
        }

        [Test]
        public void ThePanTakesNoGeneratorsNoSameChainPairAndNothingBeforeItOpens()
        {
            var (game, book) = NewBook();
            int generator = System.Array.FindIndex(game.State.Board, i => i != null && i.IsGenerator);
            Assert.IsFalse(book.PutInPan(generator));
            Assert.IsTrue(book.PutInPan(Place(game, "coffee", 2)));
            Assert.IsFalse(book.PutInPan(Place(game, "coffee", 3)), "two items from one chain");

            var (_, closed) = NewBook(level: config.Unlocks.Level("recipes") - 1);
            Assert.IsFalse(closed.Open);
        }

        [Test]
        public void FoundRecipesBecomeSignatureOrdersThatPayMore()
        {
            var (game, book) = NewBook();
            var kaak = recipes.Get("coffee_kaak");
            game.State.RecipesFound.Add(kaak.Id);
            var signatures = game.Signatures(new[] { "coffee", "sweets" });
            Assert.AreEqual(1, signatures.Count);
            Assert.AreEqual(kaak.Id, signatures[0].Recipe);
            Assert.AreEqual(0, game.Signatures(new[] { "coffee" }).Count, "only chains the player can make");

            // Over many new orders, some ask for the recipe by name and pay signatureCoinMultiplier.
            game.State.EasyOrdersLeft = 0;
            game.State.OrdersServed = 1;
            Order signature = null;
            for (int i = 0; i < 200 && signature == null; i++)
            {
                game.State.Orders.Clear();
                FillOrders(game);
                signature = game.State.Orders.Find(o => o.Recipe != null);
            }
            Assert.IsNotNull(signature);
            Assert.AreEqual(2, signature.Lines.Count);
            var normal = game.MakeOrder(null, false, signature.Lines);
            Assert.AreEqual((int)System.Math.Round(normal.Coins * config.LevelB.Recipes.SignatureCoinMultiplier), signature.Coins);
        }

        // New orders come when a slot frees up; this asks for them directly.
        static void FillOrders(Game game)
        {
            var method = typeof(Game).GetMethod("FillOrders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(game, null);
        }
    }
}
