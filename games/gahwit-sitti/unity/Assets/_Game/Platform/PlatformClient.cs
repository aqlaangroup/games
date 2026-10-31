using System;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gahwit.Platform
{
    // Talks to the shared platform (platform/README.md): the player's account, cloud save, server time and support.
    // The first launch makes a guest account; its sign-in token is kept on the phone (ISecretStore).
    // Linking Apple or Google later keeps the same account. Every call works offline by returning Outcome.Offline.
    public sealed class PlatformClient
    {
        const long RefreshEarlyMs = 60_000;

        readonly IPlatformHttp http;
        readonly ISecretStore secrets;
        readonly string root, slug, device, gameVersion;
        readonly Func<long> utcNowMs;
        string accessToken;
        long accessExpiresMs;
        Task<Reply<Account>> connecting;

        public PlatformClient(IPlatformHttp http, ISecretStore secrets, string baseUrl, string slug,
            string device, string gameVersion, Func<long> utcNowMs)
        {
            this.http = http;
            this.secrets = secrets;
            this.slug = slug;
            this.device = device;
            this.gameVersion = gameVersion;
            this.utcNowMs = utcNowMs;
            Configured = PlatformConfig.IsSet(baseUrl);
            root = Configured ? baseUrl.TrimEnd('/') + "/games/" + slug : null;
        }

        public bool Configured { get; }
        public Account Account { get; private set; }
        public event Action AccountChanged;

        string RefreshKey => "platform." + slug + ".refresh";
        public bool HasSession => secrets.Get(RefreshKey) != null;
        bool HasValidAccess => Account != null && accessToken != null && utcNowMs() < accessExpiresMs - RefreshEarlyMs;

        // Signs in with the kept token, or starts a guest account the first time.
        public async Task<Reply<Account>> Connect()
        {
            if (HasValidAccess) return Reply<Account>.Of(Account);
            if (connecting == null) connecting = ConnectNow();
            try { return await connecting; }
            finally { connecting = null; }
        }

        async Task<Reply<Account>> ConnectNow()
        {
            if (!Configured) return Reply<Account>.Fail(Outcome.Offline);
            string refresh = secrets.Get(RefreshKey);
            if (refresh != null)
            {
                var renewed = await Send<AuthReply>("POST", "/players/refresh", new JObject { ["refreshToken"] = refresh }, null);
                if (renewed.Ok) return Signed(renewed.Value);
                if (renewed.Outcome != Outcome.SignedOut) return Reply<Account>.Fail(renewed.Outcome, renewed.Error);
                // Revoked, expired or deleted on another phone: carry on here as a new guest.
                secrets.Delete(RefreshKey);
            }
            var guest = await Send<AuthReply>("POST", "/players/guest", new JObject { ["device"] = device, ["gameVersion"] = gameVersion }, null);
            return guest.Ok ? Signed(guest.Value) : Reply<Account>.Fail(guest.Outcome, guest.Error);
        }

        Reply<Account> Signed(AuthReply auth)
        {
            accessToken = auth.AccessToken;
            accessExpiresMs = DateTimeOffset.TryParse(auth.AccessExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expires)
                ? expires.ToUnixTimeMilliseconds()
                : 0;
            secrets.Set(RefreshKey, auth.RefreshToken);
            SetAccount(auth.Account);
            return Reply<Account>.Of(Account);
        }

        void SetAccount(Account account)
        {
            Account = account;
            AccountChanged?.Invoke();
        }

        public async Task<Reply<long>> ServerTimeMs()
        {
            if (!Configured) return Reply<long>.Fail(Outcome.Offline);
            var reply = await Send<JObject>("GET", "/time", null, null);
            long? now = reply.Value?.Value<long?>("nowMs");
            return reply.Ok && now != null ? Reply<long>.Of(now.Value) : Reply<long>.Fail(reply.Ok ? Outcome.Failed : reply.Outcome, reply.Error);
        }

        // The newest cloud save, or Value null when the account has none yet.
        public Task<Reply<CloudSave>> GetSave() => Call<CloudSave>("GET", "/players/me/save", null);

        // Conflict when another phone saved since baseVersion; the reply's "server" says which save won.
        public Task<Reply<JObject>> PutSave(long baseVersion, string saveJson, bool force) =>
            Call<JObject>("PUT", "/players/me/save", new JObject
            {
                ["baseVersion"] = baseVersion,
                ["save"] = JObject.Parse(saveJson),
                ["device"] = device,
                ["force"] = force
            });

        public async Task<Reply<Account>> UpdateAccount(int? birthYear, string language)
        {
            var body = new JObject();
            if (birthYear != null) body["birthYear"] = birthYear.Value;
            if (language != null) body["language"] = language;
            var reply = await Call<Account>("PATCH", "/players/me", body);
            if (reply.Ok) SetAccount(reply.Value);
            return reply;
        }

        // Adds Apple or Google to this account. Conflict "identity_taken" when that id already has another café;
        // Body["other"] then holds its playerId and its save's details ({version, summary, updatedAt, device}, or null).
        public async Task<Reply<Account>> Link(string provider, string idToken, string nonce)
        {
            var reply = await Call<JObject>("POST", "/players/me/link", Identity(provider, idToken, nonce));
            if (!reply.Ok) return Reply<Account>.Fail(reply.Outcome, reply.Error, reply.Body);
            SetAccount(reply.Value["account"]?.ToObject<Account>());
            return Reply<Account>.Of(Account);
        }

        // Switches this phone to the account behind an Apple or Google id (made new if it has none).
        public async Task<Reply<Account>> SignIn(string provider, string idToken, string nonce)
        {
            if (!Configured) return Reply<Account>.Fail(Outcome.Offline);
            var reply = await Send<AuthReply>("POST", "/players/sign-in", Identity(provider, idToken, nonce), null);
            return reply.Ok ? Signed(reply.Value) : Reply<Account>.Fail(reply.Outcome, reply.Error, reply.Body);
        }

        static JObject Identity(string provider, string idToken, string nonce) =>
            new JObject { ["provider"] = provider, ["idToken"] = idToken, ["nonce"] = nonce };

        // The oldest game version the platform still serves (game.json minGameVersion), or null.
        // The balance.json staff published on the platform, as text, with its version (missing/18). No sign-in needed.
        public async Task<Reply<(string Json, string Version)>> Config()
        {
            if (!Configured) return Reply<(string, string)>.Fail(Outcome.Offline);
            HttpReply raw;
            try
            {
                raw = await http.Send("GET", root + "/config", null, null);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Platform] GET /config: {e.Message}");
                return Reply<(string, string)>.Fail(Outcome.Offline);
            }
            if (raw.Status == 0) return Reply<(string, string)>.Fail(Outcome.Offline);
            if (raw.Status != 200) return Reply<(string, string)>.Fail(Outcome.Failed, "http_" + raw.Status);
            return Reply<(string, string)>.Of((raw.Body, raw.ConfigVersion));
        }

        // The events running now (missing/18). No sign-in needed.
        public async Task<Reply<JArray>> Events()
        {
            if (!Configured) return Reply<JArray>.Fail(Outcome.Offline);
            return await Send<JArray>("GET", "/events", null, null);
        }

        public async Task<Reply<string>> MinVersion()
        {
            if (!Configured) return Reply<string>.Fail(Outcome.Offline);
            var reply = await Send<JObject>("GET", "/version", null, null);
            return reply.Ok ? Reply<string>.Of(reply.Value?.Value<string>("minVersion")) : Reply<string>.Fail(reply.Outcome, reply.Error);
        }

        public async Task<Reply<long>> Gems()
        {
            var reply = await Call<JObject>("GET", "/players/me/wallet", null);
            return reply.Ok ? Reply<long>.Of(WalletGems(reply.Value)) : Reply<long>.Fail(reply.Outcome, reply.Error);
        }

        // "levelUp:7" or "chapter:2". Conflict "not_earned_yet" until the cloud save shows it.
        public Task<Reply<JObject>> ClaimReward(string rewardId) =>
            Call<JObject>("POST", "/players/me/rewards", new JObject { ["id"] = rewardId });

        // Gems for an item; the reply's "grant" is applied only after this. A repeated requestId spends once.
        public Task<Reply<JObject>> Spend(string item, string requestId) =>
            Call<JObject>("POST", "/players/me/spend", new JObject { ["item"] = item, ["requestId"] = requestId });

        // A store receipt for the platform to check with Apple or Google (missing/17). The reply's items apply once per
        // purchaseId; its wallet carries the gems. Sent again, it returns the items without granting twice.
        public Task<Reply<JObject>> VerifyPurchase(string store, string sku, string receipt) =>
            Call<JObject>("POST", "/purchases/verify", new JObject { ["store"] = store, ["sku"] = sku, ["receipt"] = receipt });

        // This player's purchases with their items, for restoring after a reinstall and hiding once-only offers.
        public Task<Reply<JArray>> Purchases() => Call<JArray>("GET", "/players/me/purchases", null);

        public Task<Reply<JObject>> SendEvents(JArray events) =>
            Call<JObject>("POST", "/players/me/events", new JObject
            {
                ["events"] = events,
                ["gameVersion"] = gameVersion,
                ["device"] = device
            });

        public static long WalletGems(JObject reply) => reply?["wallet"]?["gems"]?.Value<long>() ?? 0;

        // Sends the player id automatically, with the game version and the phone (COMPLETE_SPEC.md section 16).
        public Task<Reply<JObject>> SendSupport(string message, string email, string language) =>
            Call<JObject>("POST", "/players/me/support", new JObject
            {
                ["message"] = message,
                ["email"] = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                ["gameVersion"] = gameVersion,
                ["device"] = device,
                ["language"] = language
            });

        // The platform deletes everything within 30 days; this phone forgets the account at once.
        public async Task<Reply<JObject>> DeleteAccount()
        {
            var reply = await Call<JObject>("DELETE", "/players/me", null);
            if (reply.Ok) Forget();
            return reply;
        }

        public void Forget()
        {
            secrets.Delete(RefreshKey);
            accessToken = null;
            SetAccount(null);
        }

        async Task<Reply<T>> Call<T>(string method, string path, JObject body)
        {
            var connected = await Connect();
            if (!connected.Ok) return Reply<T>.Fail(connected.Outcome, connected.Error);
            var reply = await Send<T>(method, path, body, accessToken);
            if (reply.Outcome != Outcome.SignedOut) return reply;

            accessToken = null; // the access token ran out early: renew it once and try again
            connected = await Connect();
            if (!connected.Ok) return Reply<T>.Fail(connected.Outcome, connected.Error);
            return await Send<T>(method, path, body, accessToken);
        }

        async Task<Reply<T>> Send<T>(string method, string path, JObject body, string bearer)
        {
            HttpReply raw;
            try
            {
                raw = await http.Send(method, root + path, body?.ToString(Formatting.None), bearer);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Platform] {method} {path}: {e.Message}");
                return Reply<T>.Fail(Outcome.Offline);
            }
            if (raw.Status == 0) return Reply<T>.Fail(Outcome.Offline);

            JToken parsed = null;
            try
            {
                string start = raw.Body?.TrimStart() ?? "";
                if (start.StartsWith("{") || start.StartsWith("[")) parsed = JToken.Parse(raw.Body);
            }
            catch (JsonException) { }
            var detail = parsed as JObject;
            string error = detail?.Value<string>("error");

            if (raw.Status == 401) return Reply<T>.Fail(Outcome.SignedOut, error, detail);
            if (raw.Status == 409) return Reply<T>.Fail(Outcome.Conflict, error, detail);
            if (raw.Status < 200 || raw.Status >= 300) return Reply<T>.Fail(Outcome.Failed, error ?? "http_" + raw.Status, detail);
            if (parsed == null) return Reply<T>.Of(default);
            if (parsed is T same) return Reply<T>.Of(same);
            return Reply<T>.Of(parsed.ToObject<T>());
        }
    }
}
