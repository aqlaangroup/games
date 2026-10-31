namespace Aqlaan.CozyKit.Platform
{
    public enum SyncStep { None, Upload, Download, AskPlayer }

    // What this phone and the platform last agreed on, for one account. Kept beside the save, not in it,
    // because the save itself goes to the cloud.
    public sealed class SyncMark
    {
        public string PlayerId;
        public long ServerVersion; // the platform's save version this phone last uploaded or downloaded
        public long LocalVersion;  // the local save's own counter (saveVersion) at that moment

        public static SyncMark For(string playerId, SyncMark saved) =>
            saved != null && saved.PlayerId == playerId ? saved : new SyncMark { PlayerId = playerId };
    }

    // Cloud save rules (COMPLETE_SPEC.md section 9). The phone uploads its changes; when another phone saved in
    // between, the player chooses "this phone or the other one", unless this phone has nothing worth keeping.
    public static class CloudSync
    {
        // localVersion goes up with every local save. localIsFresh: no progress yet (a new install).
        // serverVersion: the platform's newest version, or null when the account has no save.
        public static SyncStep Decide(SyncMark mark, long localVersion, bool localIsFresh, long? serverVersion)
        {
            bool changedHere = localVersion != mark.LocalVersion;
            if (serverVersion == null) return localVersion > 0 ? SyncStep.Upload : SyncStep.None;
            if (serverVersion == mark.ServerVersion) return changedHere ? SyncStep.Upload : SyncStep.None;
            return localIsFresh || !changedHere ? SyncStep.Download : SyncStep.AskPlayer;
        }

        // After an upload or a download, both sides hold the same save.
        public static void Agree(SyncMark mark, long serverVersion, long localVersion)
        {
            mark.ServerVersion = serverVersion;
            mark.LocalVersion = localVersion;
        }
    }
}
