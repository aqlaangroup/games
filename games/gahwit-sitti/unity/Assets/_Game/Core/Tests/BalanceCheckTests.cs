using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Gahwit.Core.Tests
{
    public sealed class BalanceCheckTests
    {
        static string Bundled => Resources.Load<TextAsset>(BalanceConfig.ResourcePath).text;

        [Test]
        public void TheGamesOwnConfigIsSafe() => CollectionAssert.IsEmpty(BalanceCheck.Problems(Bundled));

        [Test]
        public void APublishedChangeOverridesOnlyWhatItNames()
        {
            string merged = BalanceCheck.Merge(Bundled, "{ \"energy\": { \"cap\": 140 } }");
            var config = BalanceConfig.Parse(merged);
            Assert.AreEqual(140, config.Energy.Cap);
            Assert.AreEqual(BalanceConfig.Parse(Bundled).Energy.RegenSeconds, config.Energy.RegenSeconds, "the rest of the section stays");
            CollectionAssert.IsEmpty(BalanceCheck.Problems(merged));
        }

        [Test]
        public void ABrokenConfigIsCaught()
        {
            CollectionAssert.IsNotEmpty(BalanceCheck.Problems("not json"));
            CollectionAssert.Contains(BalanceCheck.Problems(BalanceCheck.Merge(Bundled, "{ \"energy\": { \"cap\": 0 } }")), "energy");
            var noChains = JObject.Parse(Bundled);
            noChains["chains"] = new JObject();
            CollectionAssert.Contains(BalanceCheck.Problems(noChains.ToString()), "chains");
        }
    }
}
