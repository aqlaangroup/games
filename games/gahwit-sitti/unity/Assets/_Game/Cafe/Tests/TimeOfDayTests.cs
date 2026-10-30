using System;
using Gahwit.Core;
using NUnit.Framework;

namespace Gahwit.Cafe.Tests
{
    public sealed class TimeOfDayTests
    {
        [TestCase(5, TimeOfDay.Night)]
        [TestCase(6, TimeOfDay.Morning)]
        [TestCase(11, TimeOfDay.Morning)]
        [TestCase(12, TimeOfDay.Afternoon)]
        [TestCase(16, TimeOfDay.Afternoon)]
        [TestCase(17, TimeOfDay.Evening)]
        [TestCase(19, TimeOfDay.Evening)]
        [TestCase(20, TimeOfDay.Night)]
        [TestCase(23, TimeOfDay.Night)]
        public void TheHoursComeFromBalanceJson(int hour, TimeOfDay expected)
        {
            var hours = BalanceConfig.Load().LevelB.Cafe.TimeOfDayStartHours;
            Assert.AreEqual(expected, CafeRoom.TimeFromClock(new DateTime(2026, 10, 31, hour, 30, 0), hours));
        }
    }
}
