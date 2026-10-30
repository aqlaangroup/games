using NUnit.Framework;

namespace Gahwit.Core.Tests
{
    public sealed class AgeRulesTests
    {
        [TestCase(2014, 2026, AgeBand.Under13)]   // 11 or 12
        [TestCase(2013, 2026, AgeBand.Under13)]   // 12 or 13: counted as 12
        [TestCase(2012, 2026, AgeBand.From13To15)]
        [TestCase(2010, 2026, AgeBand.From13To15)] // 15 or 16: counted as 15
        [TestCase(2009, 2026, AgeBand.Over15)]
        [TestCase(1994, 2026, AgeBand.Over15)]
        public void TheBandCountsTheYoungerPossibleAge(int birthYear, int year, AgeBand expected)
        {
            Assert.AreEqual(expected, AgeRules.Band(birthYear, year));
        }

        [Test]
        public void AMissingOrImpossibleYearIsUnknown()
        {
            Assert.AreEqual(AgeBand.Unknown, AgeRules.Band(null, 2026));
            Assert.AreEqual(AgeBand.Unknown, AgeRules.Band(2027, 2026));
            Assert.AreEqual(AgeBand.Unknown, AgeRules.Band(1899, 2026));
        }

        [Test]
        public void ChildrenAndUnknownAgesNeedAParentForAdsAndSocial()
        {
            Assert.IsFalse(AgeRules.PersonalisedAdsAllowed(AgeBand.Under13, false));
            Assert.IsFalse(AgeRules.SocialAllowed(AgeBand.Unknown, false));
            Assert.IsTrue(AgeRules.SocialAllowed(AgeBand.Under13, true));
            Assert.IsTrue(AgeRules.PersonalisedAdsAllowed(AgeBand.From13To15, false));
            Assert.AreEqual("13to15", AgeRules.Code(AgeBand.From13To15));
        }
    }
}
