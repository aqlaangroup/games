using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Recipes
{
    // Teta's recipe book (Content/recipes.json): two items from different chains make a recipe, in any order.
    public sealed class RecipeContent
    {
        public const string ResourcePath = "Content/recipes";
        public List<RecipeDef> Recipes = new List<RecipeDef>();

        public static RecipeContent Load() => JsonConvert.DeserializeObject<RecipeContent>(Resources.Load<TextAsset>(ResourcePath).text);
        public RecipeDef Get(string id) => Recipes.Find(r => r.Id == id);
    }

    public sealed class RecipeDef
    {
        public string Id;
        public List<object> A; // [chain, level]
        public List<object> B;
        public bool Clue;      // carries a story clue (its text comes with the story bible, missing/11)

        public string ChainA => (string)A[0];
        public int LevelA => Convert.ToInt32(A[1]);
        public string ChainB => (string)B[0];
        public int LevelB => Convert.ToInt32(B[1]);

        public bool Matches(Item x, Item y) =>
            x.Is(ChainA, LevelA) && y.Is(ChainB, LevelB) || x.Is(ChainB, LevelB) && y.Is(ChainA, LevelA);
    }

    public enum CookResult { NeedTwo, Discovered, AlreadyKnown, Wrong }

    // The cooking pan and the book (SYSTEMS_B.md section 4). Put two items from different chains in the pan and cook:
    // a right pair fills a page and is used up; a wrong pair (or a known recipe) goes back to the board, so nothing is
    // lost. After a few wrong pairs in a row the book shows a hint. Found recipes become signature orders.
    // Numbers come from balance.json levelB.recipes.
    public sealed class RecipeBook
    {
        readonly Game game;
        readonly RecipesConfig config;

        public RecipeBook(Game game, RecipeContent content)
        {
            this.game = game;
            Content = content;
            config = game.Config.LevelB.Recipes;
            game.Signatures = Signatures;
        }

        public RecipeContent Content { get; }
        GameState State => game.State;

        public bool Open => State.Level >= game.Config.Unlocks.Level("recipes");
        public IReadOnlyList<Item> Pan => State.Pan;
        public bool Found(string id) => State.RecipesFound.Contains(id);
        public int FoundCount => State.RecipesFound.Count;

        public event Action<RecipeDef> Discovered;

        // Moves a board item into the pan. Generators can't go in, nor a second item from the same chain.
        public bool PutInPan(int cell)
        {
            var item = game.ItemAt(cell);
            if (!Open || item == null || item.IsGenerator || State.Pan.Count >= 2) return false;
            if (State.Pan.Count == 1 && State.Pan[0].Chain == item.Chain) return false;
            State.Board[cell] = null;
            State.Pan.Add(item);
            game.Touch();
            return true;
        }

        // Takes an item out of the pan, back to the board (or storage when the board is full).
        public void TakeOut(int index)
        {
            if (index < 0 || index >= State.Pan.Count) return;
            var item = State.Pan[index];
            State.Pan.RemoveAt(index);
            game.AddPiece(item.Chain, item.Level);
        }

        public CookResult Cook(out RecipeDef recipe)
        {
            recipe = null;
            if (State.Pan.Count < 2) return CookResult.NeedTwo;
            recipe = Content.Recipes.Find(r => r.Matches(State.Pan[0], State.Pan[1]));
            if (recipe == null || Found(recipe.Id))
            {
                var result = recipe == null ? CookResult.Wrong : CookResult.AlreadyKnown;
                if (recipe == null) State.RecipeWrongStreak++;
                ReturnPan();
                return result;
            }
            State.RecipesFound.Add(recipe.Id);
            State.RecipeWrongStreak = 0;
            State.Pan.Clear();
            game.Touch();
            Discovered?.Invoke(recipe);
            return CookResult.Discovered;
        }

        void ReturnPan()
        {
            while (State.Pan.Count > 0) TakeOut(0);
            game.Touch();
        }

        // After hintAfterWrongTries wrong pairs in a row: the first recipe not found yet whose chains are both open.
        public RecipeDef Hint
        {
            get
            {
                if (State.RecipeWrongStreak < config.HintAfterWrongTries) return null;
                var open = OpenChains();
                return Content.Recipes.FirstOrDefault(r => !Found(r.Id) && open.Contains(r.ChainA) && open.Contains(r.ChainB));
            }
        }

        HashSet<string> OpenChains() =>
            new HashSet<string>(game.Config.Chains.Where(c => c.Value.UnlockChapter <= State.Chapter).Select(c => c.Key));

        // For Game's orders: found recipes whose chains the player has generators for (pool).
        List<Signature> Signatures(ICollection<string> pool) =>
            Content.Recipes.Where(r => Found(r.Id) && pool.Contains(r.ChainA) && pool.Contains(r.ChainB))
                .Select(r => new Signature
                {
                    Recipe = r.Id,
                    Lines = new List<OrderLine>
                    {
                        new OrderLine { Chain = r.ChainA, Level = r.LevelA, Count = 1 },
                        new OrderLine { Chain = r.ChainB, Level = r.LevelB, Count = 1 }
                    }
                }).ToList();
    }
}
