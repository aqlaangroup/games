using System.Collections.Generic;
using System.Threading.Tasks;
using Gahwit.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gahwit.Platform.Tests
{
    public sealed class PlatformAnalyticsTests
    {
        const string Slot = "test-analytics-events";

        // Answers the guest and events routes; everything sent is kept for the test to read.
        sealed class EventsServer : IPlatformHttp
        {
            public bool Down;
            public readonly List<JObject> Received = new List<JObject>();

            public Task<HttpReply> Send(string method, string url, string body, string bearer)
            {
                if (Down) return Task.FromResult(new HttpReply { Status = 0 });
                if (url.EndsWith("/players/guest"))
                    return Task.FromResult(new HttpReply
                    {
                        Status = 200,
                        Body = new JObject { ["playerId"] = "p1", ["accessToken"] = "a", ["accessExpiresAt"] = "2099-01-01T00:00:00Z", ["refreshToken"] = "r", ["account"] = new JObject { ["playerId"] = "p1" } }.ToString()
                    });
                foreach (var e in JObject.Parse(body)["events"]) Received.Add((JObject)e);
                return Task.FromResult(new HttpReply { Status = 202, Body = "{\"accepted\":1}" });
            }
        }

        sealed class MemorySecrets : ISecretStore
        {
            readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public string Get(string key) => values.TryGetValue(key, out var v) ? v : null;
            public void Set(string key, string value) => values[key] = value;
            public void Delete(string key) => values.Remove(key);
        }

        EventsServer server;
        PlatformClient client;

        [SetUp]
        public void SetUp()
        {
            SaveStore.Delete(Slot);
            server = new EventsServer();
            client = new PlatformClient(server, new MemorySecrets(), "https://platform.test", "gahwit-sitti", "phone", "0.1.0", () => 0);
        }

        [TearDown]
        public void TearDown() => SaveStore.Delete(Slot);

        [Test]
        public void EventsWaitOfflineAndGoOnceThePlatformAnswers()
        {
            var analytics = new PlatformAnalytics(client, () => 1_000, Slot);
            server.Down = true;
            analytics.Track("merge", new Dictionary<string, object> { ["chain"] = "coffee", ["level"] = 3 });
            analytics.Flush().Wait();
            Assert.AreEqual(1, analytics.Queued);

            // A new launch still has it.
            var relaunched = new PlatformAnalytics(client, () => 2_000, Slot);
            Assert.AreEqual(1, relaunched.Queued);
            server.Down = false;
            relaunched.Flush().Wait();

            Assert.AreEqual(0, relaunched.Queued);
            Assert.AreEqual("merge", (string)server.Received[0]["name"]);
            Assert.AreEqual(3, (int)server.Received[0]["fields"]["level"]);
            Assert.AreEqual(1_000, (long)server.Received[0]["timeMs"]);
        }

        [Test]
        public void AnErrorIsReportedOncePerMessage()
        {
            var analytics = new PlatformAnalytics(client, () => 1_000, Slot);
            analytics.CatchErrors();
            LogAssert.ignoreFailingMessages = true;
            Debug.LogError("The same thing broke");
            Debug.LogError("The same thing broke");
            Debug.Log("Not an error");
            LogAssert.ignoreFailingMessages = false;
            analytics.StopCatchingErrors();
            Assert.AreEqual(1, analytics.Queued);
        }
    }
}
