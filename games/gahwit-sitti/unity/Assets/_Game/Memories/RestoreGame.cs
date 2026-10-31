using System;
using Gahwit.Core;

namespace Gahwit.Memories
{
    // "Restore" memory (SYSTEMS_B.md section 5): drag the pieces of a torn photo back into place. A piece dropped
    // within snapDistance of its spot snaps there. Positions are fractions of the photo (0..1). Numbers come from
    // balance.json levelB.memories.restore.
    public sealed class RestoreGame : IMemoryGame
    {
        readonly RestoreConfig config;
        readonly MemoriesConfig memories;
        readonly (float x, float y)[] home;
        readonly (float x, float y)[] at;
        readonly bool[] placed;

        public RestoreGame(MemoriesConfig memories, int seed)
        {
            this.memories = memories;
            config = memories.Restore;
            int count = config.Pieces;
            Columns = (int)Math.Ceiling(Math.Sqrt(count * 4 / 3.0));
            Rows = (int)Math.Ceiling(count / (double)Columns);
            home = new (float, float)[count];
            at = new (float, float)[count];
            placed = new bool[count];
            var random = new Random(seed);
            for (int i = 0; i < count; i++)
            {
                home[i] = ((i % Columns + 0.5f) / Columns, (i / Columns + 0.5f) / Rows);
                // Scattered below the photo's frame, on the table.
                at[i] = ((float)random.NextDouble(), 1.15f + (float)random.NextDouble() * 0.5f);
            }
            SecondsLeft = config.Seconds;
        }

        public int Columns { get; }
        public int Rows { get; }
        public int Count => home.Length;
        public float SecondsLeft { get; private set; }
        public (float x, float y) Home(int piece) => home[piece];
        public (float x, float y) At(int piece) => at[piece];
        public bool Placed(int piece) => placed[piece];
        public bool Finished => SecondsLeft <= 0 || Array.TrueForAll(placed, p => p);
        public int Stars => MemoryStars.From(Array.TrueForAll(placed, p => p) ? SecondsLeft : 0, config.StarsAtSecondsLeft, memories.Stars);

        public void Tick(float seconds)
        {
            if (!Finished) SecondsLeft = Math.Max(0, SecondsLeft - seconds);
        }

        // Returns true when the piece snaps into its place.
        public bool Drop(int piece, float x, float y)
        {
            if (Finished || placed[piece]) return false;
            float dx = x - home[piece].x, dy = y - home[piece].y;
            if (dx * dx + dy * dy <= config.SnapDistance * config.SnapDistance)
            {
                at[piece] = home[piece];
                return placed[piece] = true;
            }
            at[piece] = (x, y);
            return false;
        }
    }
}
