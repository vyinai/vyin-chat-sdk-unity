using NUnit.Framework;
using VyinChatSdk;

namespace VyinChatSdk.Tests.Editor
{
    /// <summary>
    /// Basic sanity tests for VyinChat Init functionality
    /// </summary>
    [TestFixture]
    public class VyinChatInitBasicTests
    {
        [Test]
        public void VcInitParams_ShouldCreateWithAppId()
        {
            // Arrange & Act
            var initParams = new VcInitParams("test-app-id");

            // Assert
            Assert.IsNotNull(initParams);
            Assert.AreEqual("test-app-id", initParams.AppId);
            Assert.IsFalse(initParams.IsLocalCachingEnabled);
            Assert.AreEqual(VcLogLevel.Warning, initParams.LogLevel);
            Assert.IsNull(initParams.AppVersion);
        }

        [Test]
        public void VcInitParams_ShouldCreateWithAllParams()
        {
            // Arrange & Act
            var initParams = new VcInitParams(
                appId: "test-app-id",
                isLocalCachingEnabled: true,
                logLevel: VcLogLevel.Debug,
                appVersion: "1.0.0"
            );

            // Assert
            Assert.IsNotNull(initParams);
            Assert.AreEqual("test-app-id", initParams.AppId);
            Assert.IsTrue(initParams.IsLocalCachingEnabled);
            Assert.AreEqual(VcLogLevel.Debug, initParams.LogLevel);
            Assert.AreEqual("1.0.0", initParams.AppVersion);
        }
    }
}
