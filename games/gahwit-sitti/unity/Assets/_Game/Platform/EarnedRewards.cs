using System.Collections.Generic;

namespace Gahwit.Platform
{
    // The gem rewards a game state has earned, as the platform's reward ids (platform/README.md, /players/me/rewards).
    // The platform pays each once; the game asks again for any not yet paid. A finished chapter moves the state on
    // to the next chapter number, even after the last one.
    public static class EarnedRewards
    {
        public static IEnumerable<string> All(int level, int chapter)
        {
            for (int n = 2; n <= level; n++) yield return "levelUp:" + n;
            for (int n = 1; n < chapter; n++) yield return "chapter:" + n;
        }
    }
}
