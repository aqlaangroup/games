using NUnit.Framework;
using Sentry.Unity;
using UnityEngine;

namespace Gahwit.Core.Tests
{
    // The crash reporter sends to our own GlitchTip and nothing personal (missing/16, legal/DATA_INVENTORY.md).
    public sealed class CrashReportingTests
    {
        [Test]
        public void CrashReportsGoToOurServerWithoutPersonalData()
        {
            var options = Resources.Load<ScriptableSentryUnityOptions>("Sentry/SentryOptions");
            Assert.IsNotNull(options, "run Gahwit Sitti > Set up crash reporting");
            Assert.IsTrue(options.Enabled);
            StringAssert.Contains("@errors.aqlaan.com/", options.Dsn);
            Assert.IsFalse(options.SendDefaultPii);
            Assert.IsFalse(options.IsEnvironmentUser);
            Assert.IsFalse(options.AttachScreenshot);
            Assert.IsFalse(options.AttachViewHierarchy);
            Assert.IsFalse(options.CaptureInEditor);
            Assert.IsTrue(options.IosNativeSupportEnabled && options.AndroidNativeSupportEnabled, "native crashes too");
            Assert.IsNotNull(options.OptionsConfiguration, "the release and environment come from Boot/CrashReportingOptions");
        }
    }
}
