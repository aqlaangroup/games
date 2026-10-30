using NUnit.Framework;

namespace Gahwit.Platform.Tests
{
    public sealed class CloudSyncTests
    {
        static SyncMark Mark(long server, long local) => new SyncMark { PlayerId = "p1", ServerVersion = server, LocalVersion = local };

        [Test]
        public void AnAccountWithNoCloudSaveGetsThisPhonesSave()
        {
            Assert.AreEqual(SyncStep.Upload, CloudSync.Decide(Mark(0, 0), 5, false, null));
            Assert.AreEqual(SyncStep.None, CloudSync.Decide(Mark(0, 0), 0, true, null));
        }

        [Test]
        public void NothingChangedAnywhereSendsNothing()
        {
            Assert.AreEqual(SyncStep.None, CloudSync.Decide(Mark(4, 30), 30, false, 4));
        }

        [Test]
        public void ChangesOnThisPhoneAreUploaded()
        {
            Assert.AreEqual(SyncStep.Upload, CloudSync.Decide(Mark(4, 30), 31, false, 4));
        }

        [Test]
        public void AnotherPhonesNewerSaveIsTakenWhenThisPhoneDidNothing()
        {
            Assert.AreEqual(SyncStep.Download, CloudSync.Decide(Mark(4, 30), 30, false, 6));
        }

        [Test]
        public void BothPhonesChangedSoThePlayerChooses()
        {
            Assert.AreEqual(SyncStep.AskPlayer, CloudSync.Decide(Mark(4, 30), 35, false, 6));
        }

        [Test]
        public void ANewInstallTakesTheCloudSaveWithoutAsking()
        {
            Assert.AreEqual(SyncStep.Download, CloudSync.Decide(Mark(0, 0), 3, true, 6));
            Assert.AreEqual(SyncStep.AskPlayer, CloudSync.Decide(Mark(0, 0), 3, false, 6));
        }

        [Test]
        public void AMarkFromAnotherAccountStartsOver()
        {
            var old = Mark(9, 40);
            var mark = SyncMark.For("p2", old);
            Assert.AreEqual(0, mark.ServerVersion);
            Assert.AreSame(old, SyncMark.For("p1", old));

            CloudSync.Agree(mark, 3, 41);
            Assert.AreEqual(SyncStep.None, CloudSync.Decide(mark, 41, false, 3));
        }
    }
}
