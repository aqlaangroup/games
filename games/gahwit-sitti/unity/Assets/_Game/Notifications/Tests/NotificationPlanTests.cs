using System;
using System.Linq;
using Gahwit.Core;
using Gahwit.Merge;
using NUnit.Framework;

namespace Gahwit.Notifications.Tests
{
    public sealed class NotificationPlanTests
    {
        const long Now = 1_000_000_000;

        BalanceConfig config;
        ChapterContent content;

        [SetUp]
        public void SetUp()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
        }

        Game PlayedGame()
        {
            var game = new Game(config, content, Game.NewState(config, content, 4, Now));
            game.State.MemoryStars["m1_pour"] = 3;
            game.State.Energy = 10;
            game.State.EnergyStampMs = Now;
            return game;
        }

        [Test]
        public void NothingIsPlannedBeforeTheFirstMemory()
        {
            var game = new Game(config, content, Game.NewState(config, content, 4, Now));
            game.State.Energy = 0;
            CollectionAssert.IsEmpty(NotificationPlan.For(game, new NotificationSwitches(), Now, new DateTime(2026, 11, 2, 15, 0, 0)));
        }

        [Test]
        public void EnergyFullComesWhenTheBarFillsUnlessThatIsAtNight()
        {
            var game = PlayedGame();
            var afternoon = new DateTime(2026, 11, 2, 12, 0, 0);
            var energy = NotificationPlan.For(game, new NotificationSwitches(), Now, afternoon).Single(n => n.Type == "energy_full");
            Assert.AreEqual(afternoon.AddSeconds(game.SecondsToFullEnergy(Now)), energy.At);

            var late = new DateTime(2026, 11, 2, 22, 0, 0);
            var moved = NotificationPlan.For(game, new NotificationSwitches(), Now, late).Single(n => n.Type == "energy_full");
            Assert.AreEqual(new DateTime(2026, 11, 3, config.Notifications.QuietHoursEnd, 0, 0), moved.At, "quiet hours end the next morning");
        }

        [Test]
        public void EachTypeHasItsOwnSwitchAndTheDailyCapHolds()
        {
            var game = PlayedGame();
            var noon = new DateTime(2026, 11, 2, 12, 0, 0);
            var off = new NotificationSwitches { EnergyFull = false };
            Assert.IsFalse(NotificationPlan.For(game, off, Now, noon).Any(n => n.Type == "energy_full"));

            game.State.Coins = 1000; // a task is affordable too
            var plan = NotificationPlan.For(game, new NotificationSwitches(), Now, noon);
            Assert.IsTrue(plan.GroupBy(n => n.At.Date).All(day => day.Count() <= config.Notifications.MaxPerDay));
            Assert.IsTrue(plan.Any(n => n.Type == "away" && n.At.Date == noon.AddDays(config.Notifications.AwayDays).Date));
        }

        [Test]
        public void AbuFadiIsDueAtTheStartOfHisNextVisit()
        {
            var game = PlayedGame();
            game.State.Energy = game.EnergyCap; // only the regular and the away note
            var evening = new DateTime(2026, 11, 2, 19, 0, 0);
            var regular = NotificationPlan.For(game, new NotificationSwitches { Away = false }, Now, evening).Single(n => n.Arg == "char.abu_fadi");
            Assert.AreEqual(new DateTime(2026, 11, 3, config.Notifications.QuietHoursEnd, 0, 0), regular.At, "morning starts at 6, inside the quiet hours");
            Assert.AreEqual("regular", regular.Type);
        }
    }
}
