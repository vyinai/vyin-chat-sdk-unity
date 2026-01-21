using NUnit.Framework;
using System;
using VyinChatSdk;

namespace VyinChatSdk.Tests.Editor
{
    /// <summary>
    /// Tests for VyinChat.Init() functionality
    /// Following TDD approach matching iOS SDK behavior
    /// </summary>
    [TestFixture]
    public class VyinChatInitTests
    {
        [SetUp]
        public void SetUp()
        {
            VyinChat.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            VyinChat.ResetForTesting();
        }

        [Test]
        public void Init_ShouldSetAppId()
        {
            // Arrange
            var appId = "test-app-id";
            var initParams = new VcInitParams(appId);

            // Act
            VyinChat.Init(initParams);

            // Assert
            Assert.AreEqual(appId, VyinChat.GetApplicationId(), "AppId should be set correctly");
            Assert.IsTrue(VyinChat.IsInitialized, "VyinChat should be initialized");
        }

        [Test]
        public void Init_ShouldInitializeTransports()
        {
            // Arrange
            var appId = "test-app-id";
            var initParams = new VcInitParams(appId);

            // Act
            VyinChat.Init(initParams);

            // Assert
            Assert.IsTrue(VyinChat.IsInitialized, "VyinChat should be initialized");
        }

        [Test]
        public void Init_CalledTwice_WithSameAppId_ShouldSucceed()
        {
            // Arrange
            var appId = "test-app-id";
            var initParams = new VcInitParams(appId);

            // Act & Assert - should not throw
            VyinChat.Init(initParams);
            VyinChat.Init(initParams);

            Assert.IsTrue(VyinChat.IsInitialized);
        }

        [Test]
        public void Init_CalledTwice_WithDifferentAppId_ShouldThrow()
        {
            // Arrange
            var firstAppId = "first-app-id";
            var secondAppId = "second-app-id";
            var firstParams = new VcInitParams(firstAppId);
            var secondParams = new VcInitParams(secondAppId);

            // Act
            VyinChat.Init(firstParams);

            // Assert
            var ex = Assert.Throws<ArgumentException>(() => VyinChat.Init(secondParams));
            Assert.That(ex.Message, Does.Contain("must match previous initialization"));
            Assert.AreEqual(firstAppId, VyinChat.GetApplicationId(), "AppId should remain the first one");
        }

        [Test]
        public void Init_WithNullParams_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => VyinChat.Init(null));
            Assert.IsFalse(VyinChat.IsInitialized, "VyinChat should not be initialized");
        }

        [Test]
        public void Init_WithEmptyAppId_ShouldThrow()
        {
            // Arrange
            var initParams = new VcInitParams("");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => VyinChat.Init(initParams));
            Assert.That(ex.Message, Does.Contain("AppId"));
            Assert.IsFalse(VyinChat.IsInitialized, "VyinChat should not be initialized");
        }

        [Test]
        public void Init_WithLocalCachingEnabled_ShouldSetFlag()
        {
            // Arrange
            var appId = "test-app-id";
            var initParams = new VcInitParams(appId, isLocalCachingEnabled: true);

            // Act
            VyinChat.Init(initParams);

            // Assert
            Assert.IsTrue(VyinChat.UseLocalCaching, "Local caching should be enabled");
        }

        [Test]
        public void Init_WithLogLevel_ShouldSetLogLevel()
        {
            // Arrange
            var appId = "test-app-id";
            var logLevel = VcLogLevel.Debug;
            var initParams = new VcInitParams(appId, logLevel: logLevel);

            // Act
            VyinChat.Init(initParams);

            // Assert
            Assert.AreEqual(logLevel, VyinChat.GetLogLevel(), "Log level should be set correctly");
        }

        [Test]
        public void Init_WithAppVersion_ShouldSetAppVersion()
        {
            // Arrange
            var appId = "test-app-id";
            var appVersion = "1.2.3";
            var initParams = new VcInitParams(appId, appVersion: appVersion);

            // Act
            VyinChat.Init(initParams);

            // Assert
            Assert.AreEqual(appVersion, VyinChat.GetAppVersion(), "App version should be set correctly");
        }

    }
}
