using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gahwit.Core
{
    public enum AudioGroup { Music, Ambience, Interface, Kitchen, Cafe, Story, Voice }

    // Every sound cue by id (missing/14), so code never names a file. Built from Assets/Audio by
    // Gahwit Sitti > Rebuild audio catalog (Editor/AudioCatalogBuilder.cs).
    public sealed class AudioCatalog : ScriptableObject
    {
        public const string ResourcePath = "Audio/AudioCatalog";

        [Serializable]
        public sealed class Cue
        {
            public string Id;
            public AudioClip Clip;
            public AudioGroup Group;
        }

        public List<Cue> Cues = new List<Cue>();

        public static AudioCatalog Load() => Resources.Load<AudioCatalog>(ResourcePath);

        // The group a cue belongs to, from its folder and its name (the cue tables in missing/14).
        public static AudioGroup GroupFor(string id, bool isMusic)
        {
            if (isMusic) return AudioGroup.Music;
            if (id.StartsWith("ui_")) return AudioGroup.Interface;
            if (id.StartsWith("gen_") || id.StartsWith("merge") || id == "sell" || id == "bubble_pop" || id == "order_served") return AudioGroup.Kitchen;
            if (id == "door_bell" || id == "decor_place") return AudioGroup.Cafe;
            return AudioGroup.Story;
        }
    }
}
