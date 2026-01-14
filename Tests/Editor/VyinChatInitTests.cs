using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
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
            // Reset VyinChat and VyinChatMain instance before each test
            VyinChat.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            VyinChat.ResetForTesting();
        }

        [Test]
        public void Init_ShouldSetAppId()
        {
            // Arrange
            var appId = "test-app-id";
            var initParams = new VcInitParams(appId);

            // Expect log from VyinChatMain.Init
            // Note: The second log "SDK initialized" is filtered because LogLevel is set to Warning
            LogAssert.Expect(LogType.Log, $"[VyinChat] Initialized with AppId: {appId}, LocalCaching: False, LogLevel: Warning");

            // Act
            var result = VyinChat.Init(initParams);

            // Assert
            Assert.IsTrue(result, "Init should return true on success");
            Assert.AreEqual(appId, VyinChat.GetApplicationId(), "AppId should be set correctly");
        }

        [Test]
        public void Init_ShouldInitializeTransports()
        {
            // Arrange
            var appId = "test-app-id";
            var initParams = new VcInitParams(appId);

            // Act
            var result = VyinChat.Init(initParams);

            // Assert
            Assert.IsTrue(result, "Init should return true on success");
            Assert.IsTrue(VyinChat.IsInitialized, "VyinChat should be initialized");
        }

        [Test]
        public void Init_CalledTwice_WithSameAppId_ShouldSucceed()
        {
            // Arrange
            var appId = "test-app-id";
            var initParams = new VcInitParams(appId);

            // Act
            var firstResult = VyinChat.Init(initParams);
            var secondResult = VyinChat.Init(initParams);

            // Assert
            Assert.IsTrue(firstResult, "First init should succeed");
            Assert.IsTrue(secondResult, "Second init with same appId should succeed");
        }

        [Test]
        public void Init_CalledTwice_WithDifferentAppId_ShouldReturnFalse()
        {
            // Arrange
            var firstAppId = "first-app-id";
            var secondAppId = "second-app-id";
            var firstParams = new VcInitParams(firstAppId);
            var secondParams = new VcInitParams(secondAppId);

            // Act
            var firstResult = VyinChat.Init(firstParams);

            // Expect error log for second init with different appId
            LogAssert.Expect(LogType.Error, $"[VyinChat] Init failed: App ID needs to be the same as the previous one. Previous: {firstAppId}, New: {secondAppId}");
            var secondResult = VyinChat.Init(secondParams);

            // Assert
            Assert.IsTrue(firstResult, "First init should succeed");
            Assert.IsFalse(secondResult, "Second init with different appId should fail");
            Assert.AreEqual(firstAppId, VyinChat.GetApplicationId(), "AppId should remain the first one");
        }

        [Test]
        public void Init_WithNullParams_ShouldReturnFalse()
        {
            // Expect error log for null params
            LogAssert.Expect(LogType.Error, "[VyinChat] Init failed: initParams is null");

            // Act
            var result = VyinChat.Init(null);

            // Assert
            Assert.IsFalse(result, "Init with null params should return false");
            Assert.IsFalse(VyinChat.IsInitialized, "VyinChat should not be initialized");
        }

        [Test]
        public void Init_WithEmptyAppId_ShouldReturnFalse()
        {
            // Arrange
            var initParams = new VcInitParams("");

            // Expect error log for empty appId
            LogAssert.Expect(LogType.Error, "[VyinChat] Init failed: AppId is empty");

            // Act
            var result = VyinChat.Init(initParams);

            // Assert
            Assert.IsFalse(result, "Init with empty appId should return false");
            Assert.IsFalse(VyinChat.IsInitialized, "VyinChat should not be initialized");
        }

        [Test]
        public void Init_WithLocalCachingEnabled_ShouldSetFlag()
        {
            // Arrange
            var appId = "test-app-id";
            var initParams = new VcInitParams(
                appId,
                isLocalCachingEnabled: true
            );

            // Act
            var result = VyinChat.Init(initParams);

            // Assert
            Assert.IsTrue(result, "Init should succeed");
            Assert.IsTrue(VyinChat.UseLocalCaching, "Local caching should be enabled");
        }

        [Test]
        public void Init_WithLogLevel_ShouldSetLogLevel()
        {
            // Arrange
            var appId = "test-app-id";
            var logLevel = VcLogLevel.Debug;
            var initParams = new VcInitParams(
                appId,
                logLevel: logLevel
            );

            // Act
            var result = VyinChat.Init(initParams);

            // Assert
            Assert.IsTrue(result, "Init should succeed");
            Assert.AreEqual(logLevel, VyinChat.GetLogLevel(), "Log level should be set correctly");
        }

        [Test]
        public void Init_WithAppVersion_ShouldSetAppVersion()
        {
            // Arrange
            var appId = "test-app-id";
            var appVersion = "1.2.3";
            var initParams = new VcInitParams(
                appId,
                appVersion: appVersion
            );

            // Act
            var result = VyinChat.Init(initParams);

            // Assert
            Assert.IsTrue(result, "Init should succeed");
            Assert.AreEqual(appVersion, VyinChat.GetAppVersion(), "App version should be set correctly");
        }
    }
}
