using NUnit.Framework;

namespace Gahwit.Core.Tests
{
    public sealed class UiRulesTests
    {
        static UiConfig Ui => BalanceConfig.Load().Ui;

        [Test]
        public void FortyFourPointsIsAboutAThirdOfTheCanvasOnTheNarrowestPhone()
        {
            // 44 pt on a 360 pt phone across a 1080-unit canvas.
            Assert.AreEqual(132f, UiRules.MinTouchUnits(Ui), 0.01f);
        }

        [Test]
        public void LargeTextGrowsEverySizeAndLiftsSmallTextToTwelvePoints()
        {
            var ui = Ui;
            Assert.AreEqual(1f, UiRules.TextScale(ui, false));
            Assert.AreEqual(0f, UiRules.MinTextUnits(ui, false));
            Assert.AreEqual(ui.LargeTextScale, UiRules.TextScale(ui, true));
            Assert.AreEqual(36f, UiRules.MinTextUnits(ui, true), 0.01f, "12 pt on the narrowest phone");
        }

        [Test]
        public void AutoGraphicsIsLowOnAThreeGigabytePhone()
        {
            var graphics = Ui.Graphics;
            Assert.IsFalse(UiRules.HighGraphics("auto", 3072, graphics));
            Assert.IsTrue(UiRules.HighGraphics("auto", 6144, graphics));
            Assert.IsTrue(UiRules.HighGraphics("high", 2048, graphics), "the player's choice wins");
            Assert.IsFalse(UiRules.HighGraphics("low", 8192, graphics));
            Assert.AreEqual(30, UiRules.FrameRate(false, graphics));
            Assert.AreEqual(60, UiRules.FrameRate(true, graphics));
            Assert.AreEqual("auto", UiRules.ValidQuality("ultra"));
        }
    }
}
