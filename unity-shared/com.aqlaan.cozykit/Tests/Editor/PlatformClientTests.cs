using System.Collections.Generic;
using System.Threading.Tasks;
using Aqlaan.CozyKit.Platform;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Aqlaan.CozyKit.Tests
{
    // The fake platform answers at once, so every task here is already complete when it returns.
    public sealed class PlatformClientTests
    {
        const long Now = 1_793_372_400_000;
        const string Base = "https://platform.test";

        sealed class MemorySecrets : ISecretStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public string Get(string key) => Values.TryGetValue(key, out var v) ? v : null;
            public void Set(string key, string value) => Values[key] = value;
            public void Delete(string key) => Values.Remove(key);
        }

        // Just enough of platform/README.md's API: guest accounts, rotating refresh tokens, the save and link routes.
        sealed class FakePlatform : IPlatformHttp
        {
            public readonly List<string> Calls = new List<string>();
            public bool Down;
            public bool RejectNextAccess;
            public HttpReply? LinkReply;
            int tokens;
            string validRefresh, validAccess;

            public Task<HttpReply> Send(string method, string url, string body, string bearer)
            {
                string path = url.Substring((Base + "/games/gahwit-sitti").Length);
                Calls.Add(method + " " + path);
                return Task.FromResult(Down ? new HttpReply { Status = 0 } : Answer(method, path, body, bearer));
            }

            HttpReply Answer(string method, string path, string body, string bearer)
            {
                switch (path)
                {
                    case "/players/guest":
                        return Signed();
                    case "/players/refresh":
                        return (string)JObject.Parse(body)["refreshToken"] == validRefresh ? Signed() : Status(401, "invalid_refresh");
                }
                if (bearer != validAccess || RejectNextAccess)
                {
                    RejectNextAccess = false;
                    return Status(401, "expired");
                }
                if (path == "/players/me/save") return new HttpReply { Status = 204 };
                if (path == "/players/me/link" && LinkReply != null) return LinkReply.Value;
                if (path == "/players/me" && method == "DELETE") return new HttpReply { Status = 202, Body = "{\"deleteBy\":\"2026-11-30T00:00:00Z\"}" };
                return Status(404, "not_found");
            }

            HttpReply Signed()
            {
                tokens++;
                validRefresh = "r" + tokens;
                validAccess = "a" + tokens;
                var reply = new JObject
                {
                    ["playerId"] = "p1",
                    ["accessToken"] = validAccess,
                    ["accessExpiresAt"] = "2026-10-30T16:00:00Z",
                    ["refreshToken"] = validRefresh,
                    ["account"] = new JObject { ["playerId"] = "p1", ["providers"] = new JArray(), ["ageBand"] = "unknown" }
                };
                return new HttpReply { Status = 200, Body = reply.ToString() };
            }

            static HttpReply Status(int status, string error) =>
                new HttpReply { Status = status, Body = new JObject { ["error"] = error }.ToString() };
        }

        FakePlatform server;
        MemorySecrets secrets;

        [SetUp]
        public void SetUp()
        {
            server = new FakePlatform();
            secrets = new MemorySecrets();
        }

        PlatformClient Client(string baseUrl = Base) =>
            new PlatformClient(server, secrets, baseUrl, "gahwit-sitti", "iPhone15,2", "0.1.0", () => Now);

        [Test]
        public void TheFirstLaunchMakesAGuestAndKeepsItsToken()
        {
            var client = Client();
            var reply = client.Connect().Result;

            Assert.IsTrue(reply.Ok);
            Assert.IsTrue(client.Account.IsGuest);
            CollectionAssert.AreEqual(new[] { "POST /players/guest" }, server.Calls);
            Assert.AreEqual("r1", secrets.Get("platform.gahwit-sitti.refresh"));
        }

        [Test]
        public void ALaterLaunchRenewsTheSameAccountInsteadOfMakingAnother()
        {
            Client().Connect().Wait();
            server.Calls.Clear();

            var reply = Client().Connect().Result;

            Assert.IsTrue(reply.Ok);
            CollectionAssert.AreEqual(new[] { "POST /players/refresh" }, server.Calls);
            Assert.AreEqual("r2", secrets.Get("platform.gahwit-sitti.refresh"), "the refresh token rotates");
        }

        [Test]
        public void ARejectedTokenCarriesOnAsANewGuest()
        {
            secrets.Set("platform.gahwit-sitti.refresh", "deleted-elsewhere");
            var reply = Client().Connect().Result;

            Assert.IsTrue(reply.Ok);
            CollectionAssert.AreEqual(new[] { "POST /players/refresh", "POST /players/guest" }, server.Calls);
        }

        [Test]
        public void AnAccessTokenThatRanOutIsRenewedOnceAndTheCallRetried()
        {
            var client = Client();
            client.Connect().Wait();
            server.RejectNextAccess = true;
            server.Calls.Clear();

            var save = client.GetSave().Result;

            Assert.IsTrue(save.Ok);
            Assert.IsNull(save.Value, "no cloud save yet");
            CollectionAssert.AreEqual(new[] { "GET /players/me/save", "POST /players/refresh", "GET /players/me/save" }, server.Calls);
        }

        [Test]
        public void OfflineIsReportedNotThrown()
        {
            server.Down = true;
            var client = Client();

            Assert.AreEqual(Outcome.Offline, client.Connect().Result.Outcome);
            Assert.AreEqual(Outcome.Offline, client.GetSave().Result.Outcome);
            Assert.IsNull(secrets.Get("platform.gahwit-sitti.refresh"));
        }

        [Test]
        public void WithoutAPlatformAddressNothingIsSent()
        {
            var client = Client("[set after the platform is deployed]");

            Assert.IsFalse(client.Configured);
            Assert.AreEqual(Outcome.Offline, client.Connect().Result.Outcome);
            Assert.AreEqual(Outcome.Offline, client.ServerTimeMs().Result.Outcome);
            CollectionAssert.IsEmpty(server.Calls);
        }

        [Test]
        public void LinkingAnAppleIdThatHasAnotherCafeReturnsThatCafe()
        {
            var other = new JObject
            {
                ["error"] = "identity_taken",
                ["other"] = new JObject
                {
                    ["playerId"] = "p9",
                    ["save"] = new JObject { ["version"] = 12, ["summary"] = new JObject { ["level"] = 7, ["chapter"] = 2 }, ["device"] = "iPad" }
                }
            };
            server.LinkReply = new HttpReply { Status = 409, Body = other.ToString() };
            var client = Client();

            var reply = client.Link("apple", "token", "nonce").Result;

            Assert.AreEqual(Outcome.Conflict, reply.Outcome);
            Assert.AreEqual("identity_taken", reply.Error);
            var theirs = reply.Body["other"]["save"].ToObject<CloudSave>();
            Assert.AreEqual(7, theirs.Summary.Level);
            Assert.AreEqual("iPad", theirs.Device);
        }

        [Test]
        public void DeletingTheAccountForgetsItOnThisPhone()
        {
            var client = Client();
            client.Connect().Wait();

            var reply = client.DeleteAccount().Result;

            Assert.IsTrue(reply.Ok);
            Assert.IsNull(client.Account);
            Assert.IsFalse(client.HasSession);
        }
    }
}
