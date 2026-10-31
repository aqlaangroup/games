using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aqlaan.CozyKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Aqlaan.CozyKit.Platform
{
    // Queues game events and sends them to the platform in batches (/players/me/events). Events wait on the phone
    // while it's offline, up to a cap, so a tester's first session isn't lost. Errors and exceptions become
    // "client_error" events, once per message per session.
    public sealed class PlatformAnalytics : IAnalytics
    {
        public const int BatchSize = 200, MaxQueued = 2000;

        readonly PlatformClient client;
        readonly Func<long> nowMs;
        readonly string slot;
        readonly List<JObject> queue = new List<JObject>();
        readonly HashSet<string> errorsSeen = new HashSet<string>();
        bool sending;

        public string SessionId { get; } = Guid.NewGuid().ToString("N");
        public int Queued => queue.Count;

        public PlatformAnalytics(PlatformClient client, Func<long> nowMs, string slot)
        {
            this.client = client;
            this.nowMs = nowMs;
            this.slot = slot;
            string saved = SaveStore.LoadText(slot);
            if (saved != null)
                try { queue.AddRange(JArray.Parse(saved).Children<JObject>()); }
                catch (JsonException) { }
        }

        public void Track(string name, Dictionary<string, object> fields = null)
        {
            queue.Add(new JObject
            {
                ["name"] = name,
                ["sessionId"] = SessionId,
                ["timeMs"] = nowMs(),
                ["fields"] = fields != null ? JObject.FromObject(fields) : new JObject()
            });
            if (queue.Count > MaxQueued) queue.RemoveRange(0, queue.Count - MaxQueued);
        }

        public void CatchErrors() => Application.logMessageReceived += OnLog;
        public void StopCatchingErrors() => Application.logMessageReceived -= OnLog;

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            string first = message.Length > 300 ? message.Substring(0, 300) : message;
            if (!errorsSeen.Add(first)) return;
            Track("client_error", new Dictionary<string, object>
            {
                ["type"] = type.ToString(),
                ["message"] = first,
                ["stack"] = stack.Length > 1000 ? stack.Substring(0, 1000) : stack
            });
        }

        // Sends what's queued; whatever couldn't be sent is kept on the phone for next time.
        public async Task Flush()
        {
            if (sending || queue.Count == 0) return;
            sending = true;
            try
            {
                while (queue.Count > 0)
                {
                    int count = Math.Min(BatchSize, queue.Count);
                    var reply = await client.SendEvents(new JArray(queue.GetRange(0, count)));
                    if (!reply.Ok) break;
                    queue.RemoveRange(0, count);
                }
            }
            finally
            {
                sending = false;
                Keep();
            }
        }

        public void Keep()
        {
            if (queue.Count == 0) SaveStore.Delete(slot);
            else SaveStore.SaveText(slot, new JArray(queue).ToString(Formatting.None));
        }
    }
}
