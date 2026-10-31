using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;

namespace Gahwit.Decor
{
    // Buying, placing, turning and storing decor, and the cosiness it adds up to (SYSTEMS_B.md section 2).
    // Cosiness is the sum of placed pieces plus a bonus for each style with enough pieces placed. Plain C#.
    public sealed class DecorRules
    {
        readonly Game game;
        readonly DecorLayout layout;

        public DecorCatalog Catalog { get; }
        public event Action Changed;

        public DecorRules(Game game, DecorCatalog catalog, DecorLayout layout)
        {
            this.game = game;
            Catalog = catalog;
            this.layout = layout;
            game.Changed += GrantEarnedPieces;
            GrantEarnedPieces();
            Recount();
        }

        GameState State => game.State;
        CafeConfig Cafe => game.Config.LevelB.Cafe;

        public bool ShopOpen => State.Level >= game.Config.Unlocks.Level("decor_shop");
        public int Price(DecorDef def) => def.PriceTier >= 0 ? Cafe.DecorPriceCoinsByTier[def.PriceTier] : 0;
        public bool Owns(string id) => State.OwnedDecor.Contains(id);
        public bool IsPlaced(string id) => State.PlacedDecor.Any(p => p.Id == id);

        public IEnumerable<DecorDef> InShop => Catalog.Pieces.Where(p => p.Source == "coins" && p.FromLevel <= State.Level && !Owns(p.Id));
        public IEnumerable<DecorDef> Stored => Catalog.Pieces.Where(p => Owns(p.Id) && !IsPlaced(p.Id));

        // Buys a piece and puts it in the first free spot (or in storage when the floor is full).
        public bool Buy(string id)
        {
            var def = Catalog.Get(id);
            if (def == null || def.Source != "coins" || Owns(id) || !ShopOpen || State.Coins < Price(def)) return false;
            State.Coins -= Price(def);
            State.OwnedDecor.Add(id);
            Place(id);
            return true;
        }

        public bool Place(string id)
        {
            var def = Catalog.Get(id);
            if (def == null || !Owns(id) || IsPlaced(id)) return false;
            if (!layout.FindSpot(def, 0, State.PlacedDecor, Catalog, out int x, out int z)) return false;
            State.PlacedDecor.Add(new PlacedDecor { Id = id, X = x, Z = z });
            Done();
            return true;
        }

        public bool Move(string id, int x, int z)
        {
            var piece = State.PlacedDecor.Find(p => p.Id == id);
            if (piece == null || (piece.X == x && piece.Z == z)) return false;
            if (!layout.Fits(Catalog.Get(id), x, z, piece.Rot, State.PlacedDecor, Catalog, piece)) return false;
            piece.X = x;
            piece.Z = z;
            Done();
            return true;
        }

        // Drags a piece to the floor point under a finger, snapped to the grid.
        public bool MoveTo(string id, float worldX, float worldZ)
        {
            var piece = State.PlacedDecor.Find(p => p.Id == id);
            if (piece == null) return false;
            var (x, z) = layout.Snap(worldX, worldZ, Catalog.Get(id), piece.Rot);
            return Move(id, x, z);
        }

        // The cosiness the next walk-in table needs, or -1 at the most tables.
        public int NextTableAt => Cafe.CosinessForExtraTable.Where(t => t > State.Cosiness).DefaultIfEmpty(-1).First();

        public (float x, float z) Centre(PlacedDecor piece) => layout.Centre(Catalog.Get(piece.Id), piece.X, piece.Z, piece.Rot);

        // A quarter turn where it stands, if it fits that way.
        public bool Rotate(string id)
        {
            var piece = State.PlacedDecor.Find(p => p.Id == id);
            if (piece == null) return false;
            int rot = (piece.Rot + 1) % 4;
            if (!layout.Fits(Catalog.Get(id), piece.X, piece.Z, rot, State.PlacedDecor, Catalog, piece)) return false;
            piece.Rot = rot;
            Done();
            return true;
        }

        public bool Store(string id)
        {
            if (State.PlacedDecor.RemoveAll(p => p.Id == id) == 0) return false;
            Done();
            return true;
        }

        // A task's style can change later for free (GAME_SYSTEMS.md section 9).
        public bool SetTaskStyle(string task, string style)
        {
            if (!game.CompletedTaskIds().Contains(task) || !Cafe.Styles.Contains(style)) return false;
            if (State.TaskStyles.TryGetValue(task, out var current) && current == style) return false;
            State.TaskStyles[task] = style;
            Done();
            return true;
        }

        public int CosinessOf(IEnumerable<string> placedIds)
        {
            var defs = placedIds.Select(Catalog.Get).Where(d => d != null).ToList();
            int sum = defs.Sum(d => d.Cosiness);
            int sets = defs.Where(d => d.Style != null).GroupBy(d => d.Style).Count(g => g.Count() >= Cafe.StyleSetBonusAt);
            return sum + sets * Cafe.StyleSetBonus;
        }

        // Pieces earned elsewhere: Teta's radio for 3 stars in her pour memory, or a regular's gift at friendship level 10.
        void GrantEarnedPieces()
        {
            foreach (var def in Catalog.Pieces)
            {
                if (Owns(def.Id)) continue;
                bool earned = def.Source == "memory"
                    ? State.MemoryStars.TryGetValue(def.Memory, out int stars) && stars >= def.MemoryStars
                    : def.Source == "friendship" && State.Regulars.TryGetValue(def.Regular, out var regular) && regular.Level >= def.FriendshipLevel;
                if (earned)
                {
                    State.OwnedDecor.Add(def.Id);
                    Place(def.Id);
                }
            }
        }

        void Recount() => State.Cosiness = CosinessOf(State.PlacedDecor.Select(p => p.Id));

        void Done()
        {
            Recount();
            Changed?.Invoke();
            game.Touch();
        }
    }
}
