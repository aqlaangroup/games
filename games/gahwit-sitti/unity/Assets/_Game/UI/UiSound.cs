using System;

namespace Gahwit.UI
{
    public enum Haptic { Light, Medium, Success }

    // How the interface asks for sounds and haptics by cue id (missing/14). The audio service (Boot.AudioService)
    // fills these in at start; until then they do nothing, so screens and tests work without it.
    public static class UiSound
    {
        public static Action<string> Play = _ => { };
        public static Action<string, bool> Loop = (_, __) => { };
        public static Action<bool> Duck = _ => { };
        public static Action<Haptic> Buzz = _ => { };
    }
}
