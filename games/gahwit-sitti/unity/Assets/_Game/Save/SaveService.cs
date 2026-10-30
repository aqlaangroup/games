using System;
using Gahwit.Core;
using Gahwit.Merge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Gahwit.Save
{
    // Loads the save (upgrading old versions), and writes it. Cloud sync comes with the platform client.
    public sealed class SaveService
    {
        public const string Slot = "gahwit-sitti";

        readonly BalanceConfig config;
        readonly ChapterContent content;
        SaveData data = new SaveData();

        public SaveService(BalanceConfig config, ChapterContent content)
        {
            this.config = config;
            this.content = content;
        }

        public SaveData Data => data;

        // Returns null when there is no save, or it can't be read; the caller then starts a new game.
        public GameState Load(out TimeService.Anchor clock)
        {
            clock = null;
            string text = SaveStore.LoadText(Slot);
            if (text == null) return null;
            try
            {
                return FromJson(text, config, content, out data, out clock);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Could not load the save, starting a new game: {e.Message}");
                data = new SaveData();
                return null;
            }
        }

        public void Save(GameState state, TimeService time)
        {
            _ = time.NowMs; // brings the clock's last-seen time up to now, which stamps savedAt
            SaveCodec.Write(state, time.State, config, data);
            data.SaveVersion++;
            SaveStore.SaveText(Slot, JsonConvert.SerializeObject(data, SaveCodec.Json));
        }

        public static GameState FromJson(string json, BalanceConfig config, ChapterContent content, out SaveData data, out TimeService.Anchor clock)
        {
            var raw = SaveMigrations.Upgrade(JObject.Parse(json), config);
            data = raw.ToObject<SaveData>(SaveCodec.Serializer);
            clock = SaveCodec.ReadClock(data);
            return SaveCodec.Read(data, config, content);
        }
    }
}
