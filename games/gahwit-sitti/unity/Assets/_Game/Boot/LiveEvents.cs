using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gahwit.Merge;
using Gahwit.Platform;
using Gahwit.UI;
using Newtonsoft.Json.Linq;

namespace Gahwit.Boot
{
    // The events staff schedule on the platform (missing/18; GAME_SYSTEMS.md section 13). Fetched at launch; they
    // need a connection, and while offline the game shows a gentle line instead. Times are checked with trusted
    // time. Implemented templates: the mini board and the double weekend ("frenzy").
    public sealed class LiveEvents : ILiveEventsUi
    {
        readonly PlatformClient client;
        readonly Game game;
        readonly Func<long> now;
        readonly Dictionary<string, MiniBoard> boards = new Dictionary<string, MiniBoard>();
        List<LiveEventInfo> running = new List<LiveEventInfo>();

        public LiveEvents(PlatformClient client, Game game, Func<long> now)
        {
            this.client = client;
            this.game = game;
            this.now = now;
        }

        public bool Offline { get; private set; } = true;
        public event Action Changed;

        // Events running now that the player's level has opened.
        public IReadOnlyList<LiveEventInfo> Running =>
            running.Where(e => e.StartsMs <= now() && now() < e.EndsMs && Open(e.Template)).ToList();

        bool Open(string template) =>
            template == "miniBoard" ? game.State.Level >= game.Config.Unlocks.Level("mini_board_event")
            : template == "frenzy"; // the double weekend is for everyone (decided 2026-10-31)

        public async Task Refresh()
        {
            var reply = await client.Events();
            Offline = !reply.Ok;
            if (reply.Ok)
            {
                running = reply.Value.OfType<JObject>().Select(e => new LiveEventInfo
                {
                    Id = (string)e["id"],
                    Template = (string)e["template"],
                    StartsMs = Ms(e.Value<DateTime>("startsAt")),
                    EndsMs = Ms(e.Value<DateTime>("endsAt")),
                    Settings = ParseSettings((string)e["settingsJson"])
                }).ToList();
                // A finished mini board's progress goes once the platform no longer lists its event.
                foreach (var id in game.State.MiniBoards.Keys.Where(id => running.All(e => e.Id != id)).ToList())
                    game.State.MiniBoards.Remove(id);
            }
            Apply();
            Changed?.Invoke();
        }

        static long Ms(DateTime time) => new DateTimeOffset(time.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeMilliseconds();

        static JObject ParseSettings(string json)
        {
            try
            {
                return string.IsNullOrEmpty(json) ? new JObject() : JObject.Parse(json);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return new JObject();
            }
        }

        // The double weekend multiplies order coins while it runs; checked again as time passes.
        public void Apply()
        {
            var frenzy = Running.FirstOrDefault(e => e.Template == "frenzy");
            game.CoinMultiplier = frenzy == null ? 1
                : frenzy.Settings.Value<int?>("coinMultiplier") ?? (int)game.Config.Events.Templates["frenzy"]["coinMultiplier"];
        }

        public MiniBoard Board(string eventId)
        {
            if (boards.TryGetValue(eventId, out var board)) return board;
            var info = running.FirstOrDefault(e => e.Id == eventId);
            if (info == null) return null;
            return boards[eventId] = new MiniBoard(game, eventId, MiniBoard.Settings(game.Config, info.Settings));
        }
    }
}
