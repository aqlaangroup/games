using Newtonsoft.Json;
using UnityEngine;

namespace Gahwit.Platform
{
    // Resources/Config/platform.json: the platform's address and the legal pages.
    public sealed class PlatformConfig
    {
        public string Slug;
        public string BaseUrl;
        public string EditorBaseUrl;
        public string PrivacyUrl;
        public string TermsUrl;
        public string StoreUrl; // where "update required" sends the player

        public string ActiveBaseUrl => Application.isEditor ? EditorBaseUrl : BaseUrl;

        // Values in [brackets] are placeholders until the real address exists.
        public static bool IsSet(string value) => !string.IsNullOrEmpty(value) && !value.StartsWith("[");

        // A legal page: a full address, or a path the platform serves for this game ("/legal/privacy"), in the
        // player's language. Null when the address isn't known yet.
        public string Page(string url, string language)
        {
            if (!IsSet(url)) return null;
            if (!url.StartsWith("/")) return url;
            return IsSet(ActiveBaseUrl) ? $"{ActiveBaseUrl.TrimEnd('/')}/games/{Slug}{url}?lang={language}" : null;
        }

        public static PlatformConfig Load() =>
            JsonConvert.DeserializeObject<PlatformConfig>(Resources.Load<TextAsset>("Config/platform").text);
    }
}
