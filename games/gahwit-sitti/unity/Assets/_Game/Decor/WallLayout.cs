using System;
using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.Merge;

namespace Gahwit.Decor
{
    // The café's two walls as one run of cells (missing/07): up the side wall from the front, then along the back wall.
    // A wall piece takes W cells on one wall, can't turn the corner or overlap another, and keeps clear of the door, the
    // window, the sign and the shelf. A placed wall piece's X is its first cell on the run; its Z and Rot stay 0.
    public sealed class WallLayout
    {
        readonly float cell, width, depth;
        readonly bool[] blocked;
        public int Cells { get; }
        public int SideCells { get; } // the side wall's cells come first

        // keepClear: stretches of the run, in metres from its start (the front end of the side wall).
        public WallLayout(float width, float depth, float cellMeters, IEnumerable<(float from, float to)> keepClear)
        {
            cell = cellMeters;
            this.width = width;
            this.depth = depth;
            SideCells = (int)Math.Round(depth / cell);
            Cells = SideCells + (int)Math.Round(width / cell);
            blocked = new bool[Cells];
            foreach (var (from, to) in keepClear)
                for (int i = 0; i < Cells; i++)
                    if ((i + 1) * cell > from && i * cell < to) blocked[i] = true;
        }

        public bool Fits(DecorDef def, int x, IReadOnlyList<PlacedDecor> placed, DecorCatalog catalog, PlacedDecor ignore = null)
        {
            if (x < 0 || x + def.W > Cells || (x < SideCells && x + def.W > SideCells)) return false;
            for (int i = x; i < x + def.W; i++)
                if (blocked[i]) return false;
            foreach (var other in placed)
            {
                if (other == ignore) continue;
                var otherDef = catalog.Get(other.Id);
                if (otherDef == null || !otherDef.Wall) continue;
                if (x < other.X + otherDef.W && other.X < x + def.W) return false;
            }
            return true;
        }

        // The first free stretch, from the back wall's start, then the side wall.
        public bool FindSpot(DecorDef def, IReadOnlyList<PlacedDecor> placed, DecorCatalog catalog, out int x)
        {
            for (int n = 0; n < Cells; n++)
            {
                x = (SideCells + n) % Cells;
                if (Fits(def, x, placed, catalog)) return true;
            }
            x = -1;
            return false;
        }

        // The cells that centre a piece on the wall nearer a point on the floor (beyond a wall counts as nearer it), kept
        // on that one wall.
        public int Snap(float worldX, float worldZ, DecorDef def)
        {
            bool side = worldX + width / 2 < worldZ + depth / 2;
            float run = side ? depth / 2 - worldZ : depth + worldX + width / 2;
            int x = (int)Math.Round(run / cell - def.W / 2f);
            return side ? Math.Max(0, Math.Min(x, SideCells - def.W)) : Math.Max(SideCells, Math.Min(x, Cells - def.W));
        }

        // Where a piece hangs: the middle of its cells on the wall's face, in metres, and which way it faces (degrees
        // about up: 90 on the side wall, 0 on the back wall).
        public (float x, float z, int facing) Hang(DecorDef def, int x)
        {
            const float offWall = 0.03f;
            float middle = (x + def.W / 2f) * cell;
            return x < SideCells
                ? (-width / 2 + offWall, depth / 2 - middle, 90)
                : (-width / 2 + (middle - depth), -depth / 2 + offWall, 0);
        }
    }
}
