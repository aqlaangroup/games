using System.Collections.Generic;

namespace Gahwit.Core
{
    // Game events (GAME_SYSTEMS.md section 15). Game rules never call an analytics SDK; the boot code turns their
    // events into Track calls. Platform.PlatformAnalytics sends them to the shared platform; an SDK such as
    // Firebase can sit behind this interface later.
    public interface IAnalytics
    {
        void Track(string name, Dictionary<string, object> fields = null);
    }

    public sealed class NoAnalytics : IAnalytics
    {
        public void Track(string name, Dictionary<string, object> fields = null) { }
    }
}
