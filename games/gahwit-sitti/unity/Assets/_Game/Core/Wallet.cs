using System;

namespace Gahwit.Core
{
    // Gems as last reported by the platform. The game shows them but never changes them (CLAUDE.md).
    public static class Wallet
    {
        public static long Gems { get; private set; }
        public static event Action Changed;

        public static void ApplyPlatformBalance(long gems)
        {
            if (gems == Gems) return;
            Gems = gems;
            Changed?.Invoke();
        }
    }
}
