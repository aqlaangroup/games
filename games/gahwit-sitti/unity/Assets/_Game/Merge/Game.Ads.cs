using System;

namespace Gahwit.Merge
{
    public enum AdPlacement { Energy, DoubleCoins, Bubble }

    // Rewarded ads (GAME_SYSTEMS.md section 12; missing/17): each placement has its own daily cap, and none show
    // before the shop level. What an ad gives isn't bought with money, so the game grants it itself. Numbers come
    // from balance.json (energy.rewardedAdAmount and rewardedAdsPerDay, orders.rewardedAdDoubleCoinsPerDay,
    // bubbles.rewardedAdPopsPerDay, unlocks.noShopOrAdsBeforeLevel).
    public sealed partial class Game
    {
        public static string Key(AdPlacement placement) =>
            placement == AdPlacement.Energy ? "energy" : placement == AdPlacement.DoubleCoins ? "doubleCoins" : "bubble";

        public bool AdsOpen => State.Level >= Config.Unlocks.NoShopOrAdsBeforeLevel;

        public int AdCap(AdPlacement placement) =>
            placement == AdPlacement.Energy ? Config.Energy.RewardedAdsPerDay
            : placement == AdPlacement.DoubleCoins ? Config.Orders.RewardedAdDoubleCoinsPerDay
            : Config.Bubbles.RewardedAdPopsPerDay;

        // today: the player's local date, such as "2026-10-31"; the counts start again each day.
        public int AdsLeft(AdPlacement placement, string today)
        {
            int watched = State.AdsDate == today && State.AdsWatched.TryGetValue(Key(placement), out int n) ? n : 0;
            return Math.Max(0, AdCap(placement) - watched);
        }

        public bool CanWatchAd(AdPlacement placement, string today) => AdsOpen && AdsLeft(placement, today) > 0;

        // After an ad was watched to the end. coins: the served order's coins, for doubling.
        public bool RewardAd(AdPlacement placement, string today, int coins = 0)
        {
            if (!CanWatchAd(placement, today)) return false;
            if (State.AdsDate != today)
            {
                State.AdsDate = today;
                State.AdsWatched.Clear();
            }
            State.AdsWatched[Key(placement)] = (State.AdsWatched.TryGetValue(Key(placement), out int n) ? n : 0) + 1;
            switch (placement)
            {
                case AdPlacement.Energy:
                    State.Energy += Config.Energy.RewardedAdAmount;
                    break;
                case AdPlacement.DoubleCoins:
                    State.Coins += coins;
                    break;
                case AdPlacement.Bubble:
                    TakeBubble();
                    break;
            }
            Changed?.Invoke();
            return true;
        }
    }
}
