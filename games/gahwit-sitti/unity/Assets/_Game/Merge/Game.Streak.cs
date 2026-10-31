using System;

namespace Gahwit.Merge
{
    // Serving streaks (DEVLOG 2026-10-31): orders served one after another, each within streakWindowSeconds of the last,
    // build a streak that pays a growing share more each time, so a busy session has a rhythm. Not saved: a new session starts
    // a new streak.
    public sealed partial class Game
    {
        public event Action<int, int> StreakServed; // streak length, bonus coins

        public int Streak { get; private set; }
        public int LastStreakBonus { get; private set; }
        long lastServedMs = long.MinValue;

        void CountStreak()
        {
            var kitchen = Config.LevelB.Kitchen;
            bool inTime = lastServedMs != long.MinValue && ClockMs - lastServedMs <= kitchen.StreakWindowSeconds * 1000L;
            Streak = inTime ? Streak + 1 : 1;
            lastServedMs = ClockMs;
            var shares = kitchen.StreakBonusPercent;
            int percent = shares.Count == 0 ? 0 : shares[Math.Min(Streak, shares.Count) - 1];
            LastStreakBonus = (int)Math.Round(LastServedCoins * percent / 100.0);
            State.Coins += LastStreakBonus;
            StreakServed?.Invoke(Streak, LastStreakBonus);
        }
    }
}
