using System;
using System.Threading.Tasks;

namespace Gahwit.UI
{
    public enum CloudStatus { Offline, Saved, Saving, NotSaved }

    public enum ActionResult { Done, Offline, Failed, Cancelled, Unavailable, NotEnoughGems }

    // What the settings, help and delete screens need from the account side (Boot.AccountService).
    public interface IAccountUi
    {
        CloudStatus Cloud { get; }
        string LinkedProvider { get; }   // "apple", "google" or null for a guest
        bool CanLinkApple { get; }       // only on iPhone
        event Action Changed;

        bool GetSetting(string id);      // sound, music, haptics, notify.<type>, glossary, voice, reducedMotion
        void SetSetting(string id, bool on);
        string Language { get; }
        void SetLanguage(string code);   // reloads the game in that language

        Task<ActionResult> LinkApple();
        Task<ActionResult> SendSupport(string message, string email);
        Task<ActionResult> DeleteAccount();
        void OpenPrivacy();
        void OpenTerms();
    }
}
