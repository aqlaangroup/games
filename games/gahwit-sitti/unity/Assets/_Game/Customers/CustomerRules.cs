using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;

namespace Gahwit.Customers
{
    // The café's customers (SYSTEMS_B.md section 3). Every order card is a customer sitting in the café (DEVLOG
    // 2026-10-31). Walk-ins tip when served and leave after a while unserved, with no penalty; regulars gain
    // friendship, and reach their story scenes at set levels. Plain C#: the café view only moves the figures.
    public sealed class CustomerRules
    {
        readonly Game game;
        readonly GameRandom rng;

        public event Action<Order, int> Tipped;              // order, coins
        public event Action<string, int> FriendshipLevelUp;  // regular, new level
        public event Action<Order> Left;                     // a walk-in gave up waiting

        public CustomerRules(Game game)
        {
            this.game = game;
            rng = new GameRandom(game.State);
            game.OrderServed += OnServed;
        }

        BalanceConfig Config => game.Config;

        // How many customers can sit: 2 to 6 with cosiness (levelB.cafe.walkInTables and cosinessForExtraTable).
        // Before the chairs are restored nobody can sit; the rest wait standing by the counter.
        public int Seats(bool chairsRestored)
        {
            if (!chairsRestored) return 0;
            var cafe = Config.LevelB.Cafe;
            int reached = cafe.CosinessForExtraTable.Count(threshold => game.State.Cosiness >= threshold);
            return Math.Min(cafe.WalkInTables.Max, cafe.WalkInTables.Min - 1 + reached);
        }

        public float TipBonus => Math.Min(Config.LevelB.Cafe.TipBonusMax, game.State.Cosiness * Config.LevelB.Cafe.TipBonusPerCosinessPoint);

        // Walk-ins who have waited too long leave; regulars and the tutorial's first order stay.
        public void Tick(long nowMs)
        {
            long limit = Config.LevelB.WalkIns.LeaveAfterMinutes * 60_000L;
            var gone = game.State.Orders.Where(o => !o.IsCharacter && o.CreatedMs > 0 && nowMs - o.CreatedMs > limit).ToList();
            foreach (var order in gone)
            {
                game.DropOrder(order.Id);
                Left?.Invoke(order);
            }
        }

        public bool IsRegular(string id) => Config.LevelB.Regulars.Schedule.ContainsKey(id);

        // Friendship counts from player level 6 (balance.json unlocks), or from the start for Abu Fadi.
        public bool FriendshipOpen(string id) =>
            game.Content.Characters.TryGetValue(id, out var character) && character.FriendshipFromStart ||
            game.State.Level >= FriendshipUnlockLevel;

        public int FriendshipUnlockLevel => Config.Unlocks.Level("regular_friendship");

        public RegularState Regular(string id)
        {
            if (!game.State.Regulars.TryGetValue(id, out var regular)) game.State.Regulars[id] = regular = new RegularState();
            return regular;
        }

        public int PointsToNext(int level) => Config.LevelB.Regulars.PointsToNextPerLevel * level;

        void OnServed(Order order)
        {
            if (!order.IsCharacter)
            {
                var tips = Config.LevelB.WalkIns.TipCoins;
                int tip = (int)Math.Round(rng.Range(tips[0], tips[1] + 1) * (1f + TipBonus));
                game.State.Coins += tip;
                Tipped?.Invoke(order, tip);
                return;
            }
            if (!IsRegular(order.Customer) || !FriendshipOpen(order.Customer)) return;

            var rules = Config.LevelB.Regulars;
            var regular = Regular(order.Customer);
            if (regular.Level >= rules.FriendshipMax) return;
            var favourite = game.Content.Characters[order.Customer].Favourite;
            bool loved = favourite != null && order.Lines.Any(l => l.Chain == favourite.Chain && l.Level == favourite.Level);
            regular.Points += order.Story != null ? Config.LevelB.Kitchen.StoryOrderFriendshipPoints
                : loved ? rules.PointsFavourite : rules.PointsOther;
            while (regular.Level < rules.FriendshipMax && regular.Points >= PointsToNext(regular.Level))
            {
                regular.Points -= PointsToNext(regular.Level);
                regular.Level++;
                FriendshipLevelUp?.Invoke(order.Customer, regular.Level);
            }
        }

        // The story scene due at this level, if any, and not seen yet: "abu_fadi_3". A scene that waits for a later
        // chapter holds back the ones after it too, so they keep their order.
        public string StoryDue(string id)
        {
            var regular = Regular(id);
            foreach (int level in Config.LevelB.Regulars.StoryAtLevels)
            {
                if (regular.Level < level || regular.StoriesSeen.Contains(level)) continue;
                return WaitsForChapter(id, level) ? null : id + "_" + level;
            }
            return null;
        }

        bool WaitsForChapter(string id, int level) =>
            game.Content.Characters.TryGetValue(id, out var character) && character.StoryFromChapter != null &&
            character.StoryFromChapter.TryGetValue(level, out int chapter) && game.State.Chapter < chapter;

        public void StorySeen(string id, string knot)
        {
            if (int.TryParse(knot.Substring(knot.LastIndexOf('_') + 1), out int level) && !Regular(id).StoriesSeen.Contains(level))
                Regular(id).StoriesSeen.Add(level);
        }
    }
}
