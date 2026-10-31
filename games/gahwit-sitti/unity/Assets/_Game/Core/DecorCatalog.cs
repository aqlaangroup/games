using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Core
{
    // The decor pieces (Resources/Content/decor.json, missing/07).
    public sealed class DecorCatalog
    {
        public const string ResourcePath = "Content/decor";
        public List<DecorDef> Pieces = new List<DecorDef>();

        public static DecorCatalog Load() => JsonConvert.DeserializeObject<DecorCatalog>(Resources.Load<TextAsset>(ResourcePath).text);

        public DecorDef Get(string id) => Pieces.Find(p => p.Id == id);
    }

    public sealed class DecorDef
    {
        public string Id;
        public string Style;          // old, modern, gulf, or null for none
        public int Cosiness;
        public int PriceTier = -1;    // index into levelB.cafe.decorPriceCoinsByTier; -1 when not sold
        public int FromLevel;         // shown in the shop from this player level
        public string Source = "coins";  // coins, memory, friendship, event (a decor:<id> reward) or premium (a bought set)
        public string Memory;         // source "memory": earned with MemoryStars stars in this memory
        public int MemoryStars;
        public string Regular;        // source "friendship": this regular's gift at FriendshipLevel (SYSTEMS_B.md section 3)
        public int FriendshipLevel;
        public string Set;            // source "premium": the set a store product grants (decorSet:<set>)
        public int W, D;              // footprint in grid cells; a wall piece's W is in wall cells and D is unused
        public bool Wall;             // hangs on a wall; its parts are in metres from the wall at floor level, +z into the room
        public List<DecorPart> Parts = new List<DecorPart>();
    }

    // A placeholder shape: box, cylinder or sphere, in metres from the footprint's centre.
    public sealed class DecorPart
    {
        public string Shape;
        public float[] Size;
        public float[] At;
        public string Color;
        public bool Glow; // a light: it shines, brighter at night
    }
}
