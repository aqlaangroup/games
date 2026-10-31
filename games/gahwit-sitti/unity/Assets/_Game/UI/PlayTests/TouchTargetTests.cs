using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Gahwit.UI.PlayTests
{
    // Boots the game and checks that every button on the main screens and panels is 44 pt or more (CLAUDE.md) on the
    // narrowest phone we support. Sizes are in canvas units, which don't change with the phone's height.
    public sealed class TouchTargetTests
    {
        [UnityTest]
        public IEnumerator EveryButtonIsFortyFourPointsOrMore()
        {
            SceneManager.LoadScene("Boot");
            // The game can reload once while it starts (a newer cloud save), so each step finds the live interface.
            yield return Settle(3f);
            yield return Live();
            Check("café");

            yield return Live();
            current.ShowKitchen();
            yield return Settle(0.6f);
            Check("kitchen");

            yield return Live();
            current.ShowCafe();
            current.Settings.Show();
            yield return Settle(0.6f);
            Check("settings");
            current.Settings.gameObject.SetActive(false);

            yield return Live();
            current.Prompt.Show("", "1", "", new[] { PromptView.Primary("", null), PromptView.Quiet("") });
            yield return Settle(0.6f);
            Check("prompt card");
            current.Prompt.Close();

            yield return Live();
            current.ShowKitchen();
            current.Storage.Show();
            yield return Settle(0.6f);
            Check("storage");
            current.Storage.gameObject.SetActive(false);
        }

        GameUi current;

        IEnumerator Live()
        {
            current = null;
            for (float waited = 0; current == null && waited < 30f; waited += Time.unscaledDeltaTime)
            {
                current = Object.FindFirstObjectByType<GameUi>();
                yield return null;
            }
            Assert.IsNotNull(current, "the game didn't reach the café within 30 seconds");
        }

        // Lets the layout and the panels' spring finish.
        static IEnumerator Settle(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        static void Check(string screen)
        {
            var small = TouchTargets.TooSmall();
            Assert.IsEmpty(small, $"On the {screen}, under 44 pt:\n" + string.Join("\n", small));
        }
    }
}
