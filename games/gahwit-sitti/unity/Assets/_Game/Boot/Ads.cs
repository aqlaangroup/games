using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Gahwit.Boot
{
    // A rewarded ad network (missing/17). The decision is Unity LevelPlay; its SDK needs the publisher's account
    // and app key, so until then phones get NoAds and the Editor a test ad. Personalised ads are off for players
    // under 13 or whose age is unknown (04).
    public interface IAds
    {
        bool Ready { get; }
        bool Personalised { set; }
        Task<bool> Show(string placement); // true when watched to the end
    }

    public sealed class NoAds : IAds
    {
        public bool Ready => false;
        public bool Personalised { set { } }
        public Task<bool> Show(string placement) => Task.FromResult(false);
    }

    // The Editor's ad: a short wait standing in for the video, then watched.
    public sealed class EditorTestAds : IAds
    {
        public bool Ready => true;
        public bool Personalised { set => Debug.Log($"[Ads] Personalised ads {(value ? "on" : "off")}."); }

        public async Task<bool> Show(string placement)
        {
            Debug.Log($"[Ads] Editor test ad for {placement}.");
            await Task.Delay(800);
            return true;
        }
    }
}
