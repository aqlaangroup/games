using System.Linq;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Platform.Tests
{
    public sealed class EarnedRewardsTests
    {
        [Test]
        public void LevelUpsFromLevelTwoAndEachFinishedChapterAreEarned()
        {
            CollectionAssert.AreEqual(new[] { "levelUp:2", "levelUp:3", "chapter:1" }, EarnedRewards.All(3, 2).ToArray());
            CollectionAssert.IsEmpty(EarnedRewards.All(1, 1));
            Assert.IsTrue(EarnedRewards.All(20, 11).Contains("chapter:10"), "after the last chapter the state moves to 11");
        }

        [TestCase("0.1.0", "0.1.0", false)]
        [TestCase("0.1.0", "0.2.0", true)]
        [TestCase("0.10.0", "0.9.5", false)]
        [TestCase("1.0", "1.0.1", true)]
        [TestCase("2.0.0", "", false)]
        public void OlderVersionsAreFound(string version, string minimum, bool older)
        {
            Assert.AreEqual(older, VersionRules.IsOlder(version, minimum));
        }
    }
}
