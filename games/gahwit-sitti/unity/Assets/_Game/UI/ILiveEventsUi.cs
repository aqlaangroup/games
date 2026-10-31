using System;
using System.Collections.Generic;
using Gahwit.Merge;
using Newtonsoft.Json.Linq;

namespace Gahwit.UI
{
    public sealed class LiveEventInfo
    {
        public string Id;
        public string Template; // miniBoard, frenzy, ...
        public long StartsMs, EndsMs;
        public JObject Settings;
    }

    // What the Today sheet and the mini board need from the events side (Boot.LiveEvents).
    public interface ILiveEventsUi
    {
        bool Offline { get; }
        IReadOnlyList<LiveEventInfo> Running { get; }
        MiniBoard Board(string eventId);
        event Action Changed;
    }
}
