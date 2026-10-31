using System;
using System.Collections.Generic;
using System.Globalization;
using Gahwit.Core;

namespace Gahwit.Merge
{
    // The season pass (GAME_SYSTEMS.md section 14; missing/18): seasons of 28 days, each with 30 tiers. Points come from
    // energy spent and daily tasks. Every tier pays on the free track, and on the premium track too with this season's
    // premium pass. Coins and energy are paid here; gems come from the platform ("pass:<season>:<track>:<tier>"), which
    // pays each once; the premium track's items are entitlements (a decor:<id> becomes a piece, Decor.DecorRules).
    public sealed partial class Game
    {
        public const string FreeTrack = "free", PremiumTrack = "premium";

        PassConfig PassRules => Config.Pass;
        public bool PassOpen => State.Level >= Config.Unlocks.Level("season_pass");

        // The season a UTC day falls in: 1 for the first DurationDays from SeasonStart, then 2, and so on.
        public int PassSeasonOn(DateTime utcDay)
        {
            var start = DateTime.ParseExact(PassRules.SeasonStart, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            int days = (int)Math.Floor((utcDay.Date - start).TotalDays);
            return Math.Max(1, days / PassRules.DurationDays + 1);
        }

        DateTime UtcDay => DateTimeOffset.FromUnixTimeMilliseconds(ClockMs).UtcDateTime.Date;
        public int PassSeason => PassSeasonOn(UtcDay);

        public int PassDaysLeft
        {
            get
            {
                var start = DateTime.ParseExact(PassRules.SeasonStart, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                var end = start.AddDays(PassSeason * PassRules.DurationDays);
                return Math.Max(1, (int)Math.Ceiling((end - UtcDay).TotalDays));
            }
        }

        public int PassPoints
        {
            get
            {
                SyncPassSeason();
                return State.PassPoints;
            }
        }

        public int PassTier => Math.Min(PassRules.Tiers, PassPoints / PassRules.PointsPerTier);
        public bool HasPremiumPass => State.PassPremiumSeason == PassSeason;

        // A new season starts the pass again from nothing. Not before the first tick, when the clock isn't known yet.
        void SyncPassSeason()
        {
            if (ClockMs <= 0) return;
            int season = PassSeason;
            if (State.PassSeason == season) return;
            if (State.PassSeason == 0) // a save from before seasons: its points count toward this one
            {
                State.PassSeason = season;
                return;
            }
            State.PassSeason = season;
            State.PassPoints = 0;
            State.PassClaimed.Clear();
        }

        void AddPassPoints(int points)
        {
            SyncPassSeason();
            if (PassOpen && points > 0) State.PassPoints += points;
        }

        public PassReward PassRewardAt(string track, int tier)
        {
            var cycle = (track == PremiumTrack ? PassRules.Rewards.Premium : PassRules.Rewards.Free).Cycle;
            return cycle[(tier - 1) % cycle.Count];
        }

        // The premium track's items at a tier, such as decor:winter_soba; none at most tiers.
        public IReadOnlyList<string> PassItemsAt(int tier) => PassRules.Rewards.Premium.ItemsAt(tier);

        public bool PassClaimed(string track, int tier) => State.PassClaimed.Contains(track + ":" + tier);

        public bool CanClaimPass(string track, int tier) =>
            PassOpen && tier >= 1 && tier <= PassTier && !PassClaimed(track, tier) && (track == FreeTrack || track == PremiumTrack && HasPremiumPass);

        // How many tiers are waiting to be claimed, for the badge on the pass.
        public int PassToClaim
        {
            get
            {
                int count = 0;
                for (int tier = 1; tier <= PassTier; tier++)
                {
                    if (CanClaimPass(FreeTrack, tier)) count++;
                    if (CanClaimPass(PremiumTrack, tier)) count++;
                }
                return count;
            }
        }

        public bool ClaimPass(string track, int tier)
        {
            if (!CanClaimPass(track, tier)) return false;
            var reward = PassRewardAt(track, tier);
            State.Coins += reward.Coins;
            State.Energy += reward.Energy;
            if (reward.Gems > 0) State.PendingRewards.Add($"pass:{State.PassSeason}:{track}:{tier}");
            if (track == PremiumTrack)
                foreach (var item in PassItemsAt(tier))
                    if (!State.Entitlements.Contains(item)) State.Entitlements.Add(item);
            State.PassClaimed.Add(track + ":" + tier);
            Changed?.Invoke();
            return true;
        }
    }
}
