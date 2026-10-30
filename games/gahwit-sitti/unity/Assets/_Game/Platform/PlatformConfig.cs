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

        public string ActiveBaseUrl => Application.isEditor ? EditorBaseUrl : BaseUrl;

        // Values in [brackets] are placeholders until the real address exists.
        public static bool IsSet(string value) => !string.IsNullOrEmpty(value) && !value.StartsWith("[");

        public static PlatformConfig Load() =>
            JsonConvert.DeserializeObject<PlatformConfig>(Resources.Load<TextAsset>("Config/platform").text);
    }
}
