using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gahwit.Boot
{
    // The first scene: loads config, content, the save, the text and the time service, starts signing in, then opens
    // the café. Signing in carries on in the background, so the game opens just as fast offline.
    public sealed class BootScene : MonoBehaviour
    {
        public const string Name = "Boot";
        public const string Cafe = "Cafe";

        IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            yield return Services.Start();
            _ = Services.Current.Platform.Connect();
            SceneManager.LoadScene(Cafe);
        }

        // Starts the game again from the Boot scene, with everything loaded afresh.
        public static void Restart()
        {
            Services.Reset();
            SceneManager.LoadScene(Name);
        }
    }
}
