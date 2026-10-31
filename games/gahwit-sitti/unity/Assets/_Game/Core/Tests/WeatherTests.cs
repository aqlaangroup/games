using System;
using System.Linq;
using NUnit.Framework;

namespace Gahwit.Core.Tests
{
    public sealed class WeatherTests
    {
        [Test]
        public void SummerIsAlwaysClearAndWinterHasSomeRainAndFog()
        {
            var config = BalanceConfig.Load().Weather;
            var july = Enumerable.Range(1, 31).Select(d => Weather.For(new DateTime(2027, 7, d), config));
            Assert.IsTrue(july.All(w => w == WeatherKind.Clear));

            var winter = Enumerable.Range(0, 90).Select(d => Weather.For(new DateTime(2026, 12, 1).AddDays(d), config)).ToList();
            Assert.Greater(winter.Count(w => w == WeatherKind.Rain), 5);
            Assert.Greater(winter.Count(w => w == WeatherKind.Fog), 3);
            Assert.AreEqual(Weather.For(new DateTime(2027, 1, 9), config), Weather.For(new DateTime(2027, 1, 9), config));
        }
    }
}
