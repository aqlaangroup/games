using System;
using System.Collections.Generic;
using Gahwit.Core;
using Gahwit.Merge;

namespace Gahwit.Decor
{
    // The café floor as a grid of 0.25 m cells (SYSTEMS_B.md section 2): pieces snap to cells, turn in quarter turns,
    // can't overlap each other, and keep clear of the counter, the tables and the way in from the door.
    public sealed class DecorLayout
    {
        public readonly struct Area
        {
            public readonly float MinX, MinZ, MaxX, MaxZ;
            public Area(float minX, float minZ, float maxX, float maxZ) { MinX = minX; MinZ = minZ; MaxX = maxX; MaxZ = maxZ; }
        }

        readonly float cell, originX, originZ;
        readonly bool[,] blocked;
        public int Cols { get; }
        public int Rows { get; }

        public DecorLayout(float width, float depth, float cellMeters, IEnumerable<Area> keepClear)
        {
            cell = cellMeters;
            Cols = (int)Math.Round(width / cell);
            Rows = (int)Math.Round(depth / cell);
            originX = -width / 2;
            originZ = -depth / 2;
            blocked = new bool[Cols, Rows];
            foreach (var area in keepClear)
                for (int x = 0; x < Cols; x++)
                for (int z = 0; z < Rows; z++)
                {
                    float cx = originX + (x + 0.5f) * cell, cz = originZ + (z + 0.5f) * cell;
                    if (cx > area.MinX && cx < area.MaxX && cz > area.MinZ && cz < area.MaxZ) blocked[x, z] = true;
                }
        }

        public static (int w, int d) Footprint(DecorDef def, int rot) => rot % 2 == 0 ? (def.W, def.D) : (def.D, def.W);

        public bool Fits(DecorDef def, int x, int z, int rot, IReadOnlyList<PlacedDecor> placed, DecorCatalog catalog, PlacedDecor ignore = null)
        {
            var (w, d) = Footprint(def, rot);
            if (x < 0 || z < 0 || x + w > Cols || z + d > Rows) return false;
            for (int i = x; i < x + w; i++)
            for (int j = z; j < z + d; j++)
                if (blocked[i, j]) return false;
            foreach (var other in placed)
            {
                if (other == ignore || catalog.Get(other.Id).Wall) continue; // wall pieces hang above the floor
                var (ow, od) = Footprint(catalog.Get(other.Id), other.Rot);
                if (x < other.X + ow && other.X < x + w && z < other.Z + od && other.Z < z + d) return false;
            }
            return true;
        }

        // The first free spot, scanning from the back wall forwards.
        public bool FindSpot(DecorDef def, int rot, IReadOnlyList<PlacedDecor> placed, DecorCatalog catalog, out int x, out int z)
        {
            for (z = 0; z < Rows; z++)
            for (x = 0; x < Cols; x++)
                if (Fits(def, x, z, rot, placed, catalog)) return true;
            x = z = -1;
            return false;
        }

        // The corner cell that centres the footprint under a point on the floor.
        public (int x, int z) Snap(float worldX, float worldZ, DecorDef def, int rot)
        {
            var (w, d) = Footprint(def, rot);
            return ((int)Math.Round((worldX - originX) / cell - w / 2f), (int)Math.Round((worldZ - originZ) / cell - d / 2f));
        }

        // The footprint's centre on the floor, in metres.
        public (float x, float z) Centre(DecorDef def, int x, int z, int rot)
        {
            var (w, d) = Footprint(def, rot);
            return (originX + (x + w / 2f) * cell, originZ + (z + d / 2f) * cell);
        }
    }
}
