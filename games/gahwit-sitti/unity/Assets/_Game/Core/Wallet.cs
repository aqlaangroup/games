namespace Gahwit.Core
{
    // Gems as last reported by the platform. The game shows them but never changes them (CLAUDE.md).
    // Stays at 0 until the platform client exists.
    public static class Wallet
    {
        public static int Gems { get; private set; }

        public static void ApplyPlatformBalance(int gems) => Gems = gems;
    }
}
