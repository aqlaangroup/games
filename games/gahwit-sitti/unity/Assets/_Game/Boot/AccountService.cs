using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aqlaan.CozyKit;
using Aqlaan.CozyKit.Platform;
using Gahwit.Core;
using Gahwit.Merge;
using Gahwit.Platform;
using Gahwit.Save;
using Gahwit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Gahwit.Boot
{
    // The player's account on the platform: guest sign-in at launch, server time, cloud save, gems (wallet and
    // earned rewards), linking Apple, support messages and account deletion. The game always works offline; the
    // cloud catches up when it can.
    public sealed class AccountService : IAccountUi
    {
        const string MarkSlot = "gahwit-sitti-sync";
        const string ClaimsSlot = "gahwit-sitti-rewards";

        // Rewards the platform has already paid for this account, so they aren't asked for again.
        sealed class PaidRewards
        {
            public string PlayerId;
            public HashSet<string> Ids = new HashSet<string>();
        }

        readonly PlatformClient client;
        readonly PlatformConfig config;
        readonly SaveService saves;
        readonly TimeService time;
        readonly Game game;
        readonly Action saveNow;
        readonly Action<string> restart; // reloads the game, from the given save text when not null
        readonly Action restartFresh;    // reloads the game with no save at all
        SyncMark mark;
        PaidRewards paid;
        bool syncing, choosing;
        CloudStatus cloud = CloudStatus.NotSaved;

        public AccountService(PlatformClient client, PlatformConfig config, SaveService saves, TimeService time, Game game,
            Action saveNow, Action<string> restart, Action restartFresh)
        {
            this.client = client;
            this.config = config;
            this.saves = saves;
            this.time = time;
            this.game = game;
            this.saveNow = saveNow;
            this.restart = restart;
            this.restartFresh = restartFresh;
            string saved = SaveStore.LoadText(MarkSlot);
            mark = saved != null ? JsonConvert.DeserializeObject<SyncMark>(saved) : null;
            client.AccountChanged += () => Changed?.Invoke();
        }

        public GameUi Ui { private get; set; }
        public event Action Changed;
        public event Action SettingChanged;

        public CloudStatus Cloud => cloud;
        public string LinkedProvider => client.Account?.Providers?.FirstOrDefault();
        public bool CanLinkApple => AppleSignIn.Available;
        public string Language => saves.Data.Settings.Language;
        public AgeBand AgeBand => AgeRules.Band(saves.Data.Account.BirthYear, DateTimeOffset.FromUnixTimeMilliseconds(time.NowMs).UtcDateTime.Year);
        public bool NeedsAgeGate => saves.Data.Account.BirthYear == null;

        // Under-13s and unknown ages get neither until a parent agrees (COMPLETE_SPEC.md section 14). Ads (missing/17)
        // and social features (missing/20) must ask these before they show anything.
        public bool PersonalisedAdsAllowed => AgeRules.PersonalisedAdsAllowed(AgeBand, saves.Data.Account.ParentConsent);
        public bool SocialAllowed => AgeRules.SocialAllowed(AgeBand, saves.Data.Account.ParentConsent);

        // At launch: sign in (a guest the first time), take the server's time, then compare saves.
        public async Task Start()
        {
            var connected = await client.Connect();
            if (!connected.Ok)
            {
                SetCloud(CloudStatus.Offline);
                return;
            }
            var serverTime = await client.ServerTimeMs();
            if (serverTime.Ok) time.ApplyServerTime(serverTime.Value);
            var minimum = await client.MinVersion();
            if (minimum.Ok && VersionRules.IsOlder(Application.version, minimum.Value))
            {
                Ui?.UpdateRequired.Show(config.StoreUrl);
                return;
            }
            var gems = await client.Gems();
            if (gems.Ok) Wallet.ApplyPlatformBalance(gems.Value);
            await PushProfile();
            await Sync(true);
        }

        // The age gate's answer. A new language reloads the game in it.
        public void SetAge(string language, int birthYear)
        {
            saves.Data.Account.BirthYear = birthYear;
            game.State.TutorialStep = Math.Max(game.State.TutorialStep, 1); // step 1 of COMPLETE_SPEC.md section 1
            if (language != Language)
            {
                SetLanguage(language);
                return;
            }
            saveNow();
            Run(PushProfile);
        }

        async Task PushProfile()
        {
            var account = client.Account;
            if (account == null) return;
            int? year = saves.Data.Account.BirthYear;
            bool newYear = year != null && year != account.BirthYear;
            bool newLanguage = account.Language != Language;
            if (newYear || newLanguage) await client.UpdateAccount(newYear ? year : null, newLanguage ? Language : null);
        }

        // Uploads this phone's changes. With pull, it first asks the platform for its newest save (at launch).
        public async Task Sync(bool pull)
        {
            if (syncing || choosing) return;
            syncing = true;
            try
            {
                saveNow();
                var connected = await client.Connect();
                if (!connected.Ok)
                {
                    SetCloud(CloudStatus.Offline);
                    return;
                }
                mark = SyncMark.For(connected.Value.PlayerId, mark);
                long local = saves.Data.SaveVersion;

                CloudSave server = null;
                long? serverVersion;
                if (pull)
                {
                    var got = await client.GetSave();
                    if (!got.Ok)
                    {
                        SetCloud(got.Outcome == Outcome.Offline ? CloudStatus.Offline : CloudStatus.NotSaved);
                        return;
                    }
                    server = got.Value;
                    serverVersion = server?.Version;
                }
                else
                {
                    if (local == mark.LocalVersion)
                    {
                        if (mark.ServerVersion > 0) SetCloud(CloudStatus.Saved);
                        return;
                    }
                    // Assume nothing changed on the platform; the upload reports a conflict if it did.
                    serverVersion = mark.ServerVersion > 0 ? mark.ServerVersion : (long?)null;
                }
                await Act(CloudSync.Decide(mark, local, IsFresh(game.State), serverVersion), server);
                if (cloud == CloudStatus.Saved) await ClaimRewards();
            }
            finally
            {
                syncing = false;
            }
        }

        async Task Act(SyncStep step, CloudSave server)
        {
            switch (step)
            {
                case SyncStep.None:
                    SetCloud(CloudStatus.Saved);
                    break;
                case SyncStep.Upload:
                    await Upload(false);
                    break;
                case SyncStep.Download:
                    await Download(server);
                    break;
                case SyncStep.AskPlayer:
                    Ask(server);
                    break;
            }
        }

        async Task Upload(bool force)
        {
            SetCloud(CloudStatus.Saving);
            saveNow();
            long local = saves.Data.SaveVersion;
            var put = await client.PutSave(mark.ServerVersion, SaveStore.LoadText(SaveService.Slot), force);
            if (put.Ok)
            {
                CloudSync.Agree(mark, put.Value.Value<long>("version"), local);
                KeepMark();
                SetCloud(CloudStatus.Saved);
                return;
            }
            if (put.Outcome == Outcome.Conflict && put.Body?["server"] is JObject info)
            {
                // Another phone saved first.
                var server = info.ToObject<CloudSave>();
                await Act(CloudSync.Decide(mark, local, IsFresh(game.State), server.Version), server);
                return;
            }
            SetCloud(put.Outcome == Outcome.Offline ? CloudStatus.Offline : CloudStatus.NotSaved);
        }

        // Daily gifts, tasks and event milestones pay at once, without waiting for an upload.
        public void ClaimNow() => Run(ClaimRewards);

        // Asks for the gems of every level-up and finished chapter this save has earned; the platform pays each once.
        // It runs after an upload, so the platform's copy of the save already shows them.
        async Task ClaimRewards()
        {
            string playerId = client.Account?.PlayerId;
            if (playerId == null) return;
            if (paid == null || paid.PlayerId != playerId)
            {
                string saved = SaveStore.LoadText(ClaimsSlot);
                paid = saved != null ? JsonConvert.DeserializeObject<PaidRewards>(saved) : null;
                if (paid == null || paid.PlayerId != playerId) paid = new PaidRewards { PlayerId = playerId };
            }
            long gained = 0;
            int before = paid.Ids.Count;
            // Level-ups and chapters from the save, then gems earned in daily play and events (missing/18).
            foreach (var id in EarnedRewards.All(game.State.Level, game.State.Chapter).Concat(game.State.PendingRewards.ToList()))
            {
                if (paid.Ids.Contains(id))
                {
                    game.State.PendingRewards.Remove(id);
                    continue;
                }
                var reply = await client.ClaimReward(id);
                if (reply.Outcome == Outcome.Conflict) continue; // not in the cloud save yet: after the next upload
                if (reply.Outcome == Outcome.Offline || reply.Outcome == Outcome.SignedOut) break; // next time
                game.State.PendingRewards.Remove(id);
                if (!reply.Ok) continue;                         // refused, such as a day too long ago: nothing to pay
                paid.Ids.Add(id);
                gained += reply.Value["granted"]?["gems"]?.Value<long>() ?? 0;
                Wallet.ApplyPlatformBalance(PlatformClient.WalletGems(reply.Value));
            }
            if (paid.Ids.Count != before) SaveStore.SaveText(ClaimsSlot, JsonConvert.SerializeObject(paid));
            if (gained > 0) Ui?.Toast.Show(Loc.T("toast.gems", gained));
        }

        // Replaces this phone's save with the platform's and reloads the game from it.
        async Task Download(CloudSave server)
        {
            if (server?.Save == null)
            {
                var got = await client.GetSave();
                if (!got.Ok || got.Value?.Save == null)
                {
                    SetCloud(CloudStatus.NotSaved);
                    return;
                }
                server = got.Value;
            }
            if ((server.Save.Value<int?>("schemaVersion") ?? 0) > SaveData.CurrentSchema)
            {
                // Saved by a newer version of the game: this one could lose parts of it.
                Ui?.Toast.Show(Loc.T("cloud.updateGame"));
                SetCloud(CloudStatus.NotSaved);
                return;
            }
            CloudSync.Agree(mark, server.Version, server.Save.Value<long?>("saveVersion") ?? 0);
            KeepMark();
            restart(server.Save.ToString(Formatting.None));
        }

        // Both phones changed: the player picks one (COMPLETE_SPEC.md section 9).
        void Ask(CloudSave server)
        {
            if (Ui == null) return;
            choosing = true;
            Ui.CloudChoice.Show(Here(), Other(server?.Summary, server?.Device), keepThis =>
            {
                choosing = false;
                Run(() => keepThis ? Upload(true) : Download(server));
            });
        }

        CloudChoiceView.Side Here() => new CloudChoiceView.Side { Level = game.State.Level, Chapter = game.State.Chapter };

        static CloudChoiceView.Side Other(SaveSummary summary, string device) =>
            new CloudChoiceView.Side { Level = summary?.Level ?? 1, Chapter = summary?.Chapter ?? 1, Device = device };

        // A new install with nothing worth keeping takes the cloud save without asking.
        static bool IsFresh(GameState state) =>
            state.OrdersServed == 0 && state.TaskIndex == 0 && state.Chapter == 1 && state.Level == 1;

        public async Task<ActionResult> LinkApple()
        {
            if (!AppleSignIn.Available) return ActionResult.Unavailable;
            var connected = await client.Connect();
            if (!connected.Ok) return Failure(connected.Outcome);
            var apple = await AppleSignIn.Request();
            if (apple.Cancelled) return ActionResult.Cancelled;

            var linked = await client.Link("apple", apple.IdToken, apple.Nonce);
            if (linked.Ok) return ActionResult.Done;
            if (linked.Error != "identity_taken") return Failure(linked.Outcome);

            // This Apple id already has a café. Move this phone to that account, keeping the save the player picks.
            var otherSave = (linked.Body?["other"]?["save"] as JObject)?.ToObject<CloudSave>();
            var done = new TaskCompletionSource<ActionResult>();
            async Task Switch(bool keepThis)
            {
                var signed = await client.SignIn("apple", apple.IdToken, apple.Nonce);
                if (!signed.Ok)
                {
                    done.TrySetResult(Failure(signed.Outcome));
                    return;
                }
                mark = SyncMark.For(signed.Value.PlayerId, null);
                done.TrySetResult(ActionResult.Done);
                if (keepThis) await Upload(true);
                else await Download(null);
            }
            // A café on only one side needs no question: a new phone takes the cloud one, and vice versa.
            if (otherSave == null) Run(() => Switch(true));
            else if (IsFresh(game.State)) Run(() => Switch(false));
            else
            {
                choosing = true;
                Ui.CloudChoice.Show(Here(), Other(otherSave.Summary, otherSave.Device), keepThis =>
                {
                    choosing = false;
                    Run(() => Switch(keepThis));
                });
            }
            return await done.Task;
        }

        public async Task<ActionResult> SendSupport(string message, string email)
        {
            var reply = await client.SendSupport(message, email, Language);
            return reply.Ok ? ActionResult.Done : Failure(reply.Outcome);
        }

        // Deletes the account on the platform (gone within 30 days) and everything of it on this phone.
        public async Task<ActionResult> DeleteAccount()
        {
            if (client.HasSession)
            {
                var reply = await client.DeleteAccount();
                if (!reply.Ok) return Failure(reply.Outcome);
            }
            SaveStore.Delete(MarkSlot);
            SaveStore.Delete(ClaimsSlot);
            Wallet.ApplyPlatformBalance(0);
            restartFresh();
            return ActionResult.Done;
        }

        static ActionResult Failure(Outcome outcome) => outcome == Outcome.Offline ? ActionResult.Offline : ActionResult.Failed;

        public bool GetSetting(string id)
        {
            var s = saves.Data.Settings;
            switch (id)
            {
                case "sound": return s.Sound;
                case "haptics": return s.Haptics;
                case "music": return s.Music;
                case "notify.energyFull": return s.Notifications.EnergyFull;
                case "notify.regular": return s.Notifications.Regular;
                case "notify.task": return s.Notifications.Task;
                case "notify.away": return s.Notifications.Away;
                case "glossary": return s.Glossary;
                case "voice": return s.Voice;
                case "largeText": return s.LargeText;
                case "reducedMotion": return s.ReducedMotion || PhoneSettings.ReduceMotion; // the phone's setting wins
                case "memoryAssist": return s.MemoryAssist;
                default: return false;
            }
        }

        public void SetSetting(string id, bool on)
        {
            var s = saves.Data.Settings;
            switch (id)
            {
                case "sound": s.Sound = on; break;
                case "haptics": s.Haptics = on; break;
                case "music": s.Music = on; break;
                case "notify.energyFull": s.Notifications.EnergyFull = on; break;
                case "notify.regular": s.Notifications.Regular = on; break;
                case "notify.task": s.Notifications.Task = on; break;
                case "notify.away": s.Notifications.Away = on; break;
                case "glossary": s.Glossary = on; break;
                case "voice": s.Voice = on; break;
                case "reducedMotion": s.ReducedMotion = on; break;
                case "memoryAssist": s.MemoryAssist = on; break;
                case "largeText":
                    // Every screen is laid out for its text size, so the game reloads with the new one, like a language.
                    s.LargeText = on;
                    saveNow();
                    restart(null);
                    return;
            }
            if (Ui != null) Ui.Dialogue.ShowGlosses = s.Glossary;
            SettingChanged?.Invoke();
            saveNow();
        }

        public string Quality => UiRules.ValidQuality(saves.Data.Settings.Quality);

        public void SetQuality(string choice)
        {
            saves.Data.Settings.Quality = UiRules.ValidQuality(choice);
            SettingChanged?.Invoke();
            saveNow();
        }

        public void SetLanguage(string code)
        {
            saves.Data.Settings.Language = code;
            saveNow();
            Run(PushProfile);
            restart(null);
        }

        public void OpenPrivacy() => Open(config.Page(config.PrivacyUrl, Language));
        public void OpenTerms() => Open(config.Page(config.TermsUrl, Language));

        void Open(string url)
        {
            if (url != null) Application.OpenURL(url);
            else Ui?.Toast.Show(Loc.T("legal.soon"));
        }

        void SetCloud(CloudStatus status)
        {
            if (cloud == status) return;
            cloud = status;
            Changed?.Invoke();
        }

        void KeepMark() => SaveStore.SaveText(MarkSlot, JsonConvert.SerializeObject(mark));

        static async void Run(Func<Task> work)
        {
            try { await work(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
