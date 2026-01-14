using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using VyinChatSdk;

namespace VyinChatSdk.Tests.Editor
{
    /// <summary>
    /// Tests for VyinChat.Connect() functionality - EditMode synchronous tests.
    /// These tests only cover parameter validation and synchronous exception checks.
    /// For connection/LOGI/timeout behavior, see PlayMode:
    /// Tests/Runtime/Internal/Integration/WebSocketIntegrationTests.cs
    /// </summary>
    [TestFixture]
    public class VyinChatConnectTests
    {
        private const string TEST_APP_ID = "test-app-id";
        private const string TEST_USER_ID = "test-user-001";
        private const string TEST_AUTH_TOKEN = "test-auth-token";

        [SetUp]
        public void SetUp()
        {
            // Reset and initialize before each test
            VyinChat.ResetForTesting();
            var initParams = new VcInitParams(TEST_APP_ID);
            VyinChat.Init(initParams);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            VyinChat.ResetForTesting();
        }

        [Test]
        public void Connect_WithoutInit_ShouldThrow()
        {
            // Arrange
            VyinChat.ResetForTesting(); // Reset to uninitialize state

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Connection] VyinChatMain instance hasn't been initialized. Try VyinChat.Init().");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                VyinChat.Connect(TEST_USER_ID, TEST_AUTH_TOKEN, (user, error) => { });
            });
        }

        [Test]
        public void Connect_WithNullUserId_ShouldCallCallbackWithError()
        {
            // Arrange
            string resultError = null;
            VcUser resultUser = null;
            bool callbackCalled = false;

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Connection] userId is empty.");

            // Act
            VyinChat.Connect(null, TEST_AUTH_TOKEN, (user, error) =>
            {
                resultUser = user;
                resultError = error;
                callbackCalled = true;
            });

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(resultUser, "User should be null");
            Assert.IsNotNull(resultError, "Error should not be null");
            Assert.That(resultError, Does.Contain("userId"));
        }

        [Test]
        public void Connect_WithEmptyUserId_ShouldCallCallbackWithError()
        {
            // Arrange
            string resultError = null;
            VcUser resultUser = null;
            bool callbackCalled = false;

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Connection] userId is empty.");

            // Act
            VyinChat.Connect("", TEST_AUTH_TOKEN, (user, error) =>
            {
                resultUser = user;
                resultError = error;
                callbackCalled = true;
            });

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(resultUser, "User should be null");
            Assert.IsNotNull(resultError, "Error should not be null");
            Assert.That(resultError, Does.Contain("userId"));
        }
    }
}
