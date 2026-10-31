using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Merge;
using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Wardrobe
{
    public sealed class WardrobeContent
    {
        public const string ResourcePath = "Content/wardrobe";
        public List<WardrobePiece> Pieces = new List<WardrobePiece>();
        public List<Occasion> Occasions = new List<Occasion>();

        public static WardrobeContent Load() => JsonConvert.DeserializeObject<WardrobeContent>(Resources.Load<TextAsset>(ResourcePath).text);
        public WardrobePiece Get(string id) => Pieces.Find(p => p.Id == id);
    }

    public sealed class WardrobePiece
    {
        public string Id;
        public string Slot;
        public List<string> Tags = new List<string>();
        public int PriceTier = -1;
        public string Source = "coins"; // coins, or task (earned by restoring Task)
        public string Task;
        public string Color;
        public bool Dress; // reaches the floor, instead of trousers
    }

    public sealed class Occasion
    {
        public int Chapter;
        public string Id;
        public List<string> Likes = new List<string>();
    }

    // Salma's wardrobe (SYSTEMS_B.md section 6): pieces in 6 slots, bought with coins or earned, and one occasion per
    // chapter. Dressing for it gives 1–3 hearts, worth 100 × hearts × chapter coins once; it never blocks the story.
    public sealed class WardrobeRules
    {
        readonly Game game;
        public WardrobeContent Content { get; }
        public event Action Changed;

        public WardrobeRules(Game game, WardrobeContent content)
        {
            this.game = game;
            Content = content;
            game.TaskCompleted += _ => GrantEarned();
            GrantEarned();
        }

        GameState State => game.State;
        public bool Open => State.Level >= game.Config.Unlocks.Level("wardrobe");
        public bool Owns(string id) => State.WardrobeOwned.Contains(id);
        public int Price(WardrobePiece piece) => piece.PriceTier >= 0 ? game.Config.LevelB.Wardrobe.PriceCoinsByTier[piece.PriceTier] : 0;
        public string Worn(string slot) => State.WardrobeWorn.TryGetValue(slot, out var id) ? id : null;
        public IEnumerable<WardrobePiece> ForSale => Content.Pieces.Where(p => p.Source == "coins" && !Owns(p.Id));

        public bool Buy(string id)
        {
            var piece = Content.Get(id);
            if (piece == null || piece.Source != "coins" || Owns(id) || !Open || State.Coins < Price(piece)) return false;
            State.Coins -= Price(piece);
            State.WardrobeOwned.Add(id);
            Done();
            return true;
        }

        // Wearing a piece takes off whatever was in its slot; wearing it again takes it off.
        public bool Toggle(string id)
        {
            var piece = Content.Get(id);
            if (piece == null || !Owns(id)) return false;
            if (Worn(piece.Slot) == id) State.WardrobeWorn.Remove(piece.Slot);
            else State.WardrobeWorn[piece.Slot] = id;
            Done();
            return true;
        }

        public Occasion CurrentOccasion => Content.Occasions.Find(o => o.Chapter == State.Chapter);
        public bool OccasionDone(Occasion occasion) => State.OccasionHearts.ContainsKey(occasion.Chapter);

        // Worn pieces with a style the occasion likes, against levelB.wardrobe.heartsAtMatches; there's always one heart.
        public int Hearts(Occasion occasion)
        {
            int matches = State.WardrobeWorn.Values.Select(Content.Get).Count(p => p != null && p.Tags.Any(occasion.Likes.Contains));
            var steps = game.Config.LevelB.Wardrobe.HeartsAtMatches;
            int hearts = 1;
            for (int h = 1; h < steps.Count; h++)
                if (matches >= steps[h]) hearts = h + 1;
            return hearts;
        }

        public int BonusCoins(Occasion occasion, int hearts) => game.Config.LevelB.Wardrobe.OccasionBonusPerHeartPerChapter * hearts * occasion.Chapter;

        // "I'm ready": pays the occasion's coins once per chapter, and returns the hearts (0 if already done).
        public int DressFor(Occasion occasion)
        {
            if (occasion == null || OccasionDone(occasion) || !Open) return 0;
            int hearts = Hearts(occasion);
            State.OccasionHearts[occasion.Chapter] = hearts;
            State.Coins += BonusCoins(occasion, hearts);
            Done();
            return hearts;
        }

        void GrantEarned()
        {
            bool any = false;
            var done = game.CompletedTaskIds();
            foreach (var piece in Content.Pieces)
                if (piece.Source == "task" && done.Contains(piece.Task) && !Owns(piece.Id))
                {
                    State.WardrobeOwned.Add(piece.Id);
                    any = true;
                }
            if (any) Done();
        }

        void Done()
        {
            Changed?.Invoke();
            game.Touch();
        }
    }
}
