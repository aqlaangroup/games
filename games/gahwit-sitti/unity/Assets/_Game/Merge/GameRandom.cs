namespace Gahwit.Merge
{
    // Seeded xorshift whose state lives in the save, so a reported bug can be replayed exactly.
    public sealed class GameRandom
    {
        readonly GameState state;

        public GameRandom(GameState state)
        {
            this.state = state;
            if (state.Rng == 0) state.Rng = 0x9E3779B97F4A7C15UL;
        }

        ulong Next()
        {
            ulong x = state.Rng;
            x ^= x << 13;
            x ^= x >> 7;
            x ^= x << 17;
            state.Rng = x;
            return x;
        }

        public double NextDouble() => (Next() >> 11) * (1.0 / (1UL << 53));

        public int Range(int minInclusive, int maxExclusive) =>
            minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive));
    }
}
