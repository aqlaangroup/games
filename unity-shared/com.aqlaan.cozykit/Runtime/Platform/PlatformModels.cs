using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Aqlaan.CozyKit.Platform
{
    // Shapes of the platform's replies (platform/README.md, API). JSON is camelCase.

    public sealed class AuthReply
    {
        public string PlayerId;
        public string AccessToken;
        public string AccessExpiresAt;
        public string RefreshToken;
        public Account Account;
    }

    public sealed class Account
    {
        public string PlayerId;
        public List<string> Providers = new List<string>();
        public int? BirthYear;
        public string AgeBand;
        public string Country;
        public string Language;
        public string CreatedAt;
        public string DeletionRequestedAt;

        public bool IsGuest => Providers == null || Providers.Count == 0;
        public bool Has(string provider) => Providers != null && Providers.Contains(provider);
    }

    public sealed class SaveSummary
    {
        public int Level;
        public int Chapter;
    }

    // The newest save on the platform, or just its details when a conflict is reported.
    public sealed class CloudSave
    {
        public long Version;
        public JObject Save;
        public SaveSummary Summary;
        public string UpdatedAt;
        public string Device;
    }

    public enum Outcome { Ok, Offline, SignedOut, Conflict, Failed }

    public sealed class Reply<T>
    {
        public Outcome Outcome;
        public T Value;
        public string Error;    // the platform's error code, such as "identity_taken"
        public JObject Body;    // the whole reply, for conflicts

        public bool Ok => Outcome == Outcome.Ok;

        public static Reply<T> Of(T value) => new Reply<T> { Outcome = Outcome.Ok, Value = value };
        public static Reply<T> Fail(Outcome outcome, string error = null, JObject body = null) =>
            new Reply<T> { Outcome = outcome, Error = error, Body = body };
    }
}
