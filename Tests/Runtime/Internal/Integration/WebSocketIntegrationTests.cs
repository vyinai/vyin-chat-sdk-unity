// -----------------------------------------------------------------------------
//
// WebSocket Integration Tests - Real Connection Tests
// Tests using REAL WebSocket connection to test server
//
// Test Server: wss://adb53e88-4c35-469a-a888-9e49ef1641b2.gamania.chat/ws
// Test User: tester01
//
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using System.Collections;
using VyinChatSdk;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Domain.Commands;
using VyinChatSdk.Internal.Platform.Unity.Network;
using VyinChatSdk.Internal.Platform;
using UnityEngine;

namespace VyinChatSdk.Tests.Runtime.Internal.Integration
{
    /// <summary>
    /// Integration tests using REAL WebSocket connection
    /// Tests connection, LOGI handling, and session_key extraction
    /// </summary>
    public class WebSocketIntegrationTests
    {
        private UnityWebSocketClient client;
        private WebSocketConfig testConfig;

        // Test environment configuration
        private const string TEST_APP_ID = "adb53e88-4c35-469a-a888-9e49ef1641b2";
        private const string TEST_USER_ID = "tester01";
        private const float CONNECTION_TIMEOUT = 10f;
        private const float LOGI_TIMEOUT = 10f;
        private const string VALID_ENV = "gamania.chat";

        [SetUp]
        public void SetUp()
        {
            MainThreadDispatcher.ClearQueue();

            client = new UnityWebSocketClient();
            testConfig = new WebSocketConfig
            {
                ApplicationId = TEST_APP_ID,
                UserId = TEST_USER_ID,
                AccessToken = null,
                EnvironmentDomain = VALID_ENV
            };
        }

        [TearDown]
        public void TearDown()
        {
            client?.Disconnect();
        }

        /// <summary>
        /// Should use wss and include query params
        /// </summary>
        [Test]
        public void Connect_ShouldUseWSS_WithQueryParams()
        {
            var url = testConfig.BuildWebSocketUrl();

            Assert.IsTrue(url.StartsWith("wss://"), "Should use wss");
            Assert.IsTrue(url.Contains("user_id="), "Should include user_id");
            Assert.IsTrue(url.Contains("ai="), "Should include application id");
        }

        /// <summary>
        /// Should include user_id and access_token in URL (not asserting validity)
        /// </summary>
        [Test]
        public void Connect_ShouldInclude_UserId_AccessToken()
        {
            var cfg = new WebSocketConfig
            {
                ApplicationId = TEST_APP_ID,
                UserId = TEST_USER_ID,
                AccessToken = "valid-token-placeholder",
                EnvironmentDomain = VALID_ENV
            };

            var url = cfg.BuildWebSocketUrl();

            Assert.IsTrue(url.Contains("user_id=" + TEST_USER_ID));
            Assert.IsTrue(url.Contains("access_token=valid-token-placeholder"));
        }

        /// <summary>
        /// OnConnected should fire after handshake
        /// </summary>
        [UnityTest]
        public IEnumerator Connect_ShouldTriggerOnConnected_AfterHandshake()
        {
            bool connected = false;
            VcException error = null;

            client.OnConnected += () => connected = true;
            client.OnError += (ex) => error = ex;

            client.Connect(testConfig);

            float elapsed = 0f;
            while (!connected && error == null && elapsed < CONNECTION_TIMEOUT)
            {
                client.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(connected, $"Should connect to WS. Error: {error?.Message}");
            Assert.IsTrue(client.IsConnected, "IsConnected should be true");
        }

        /// <summary>
        /// After connected, should wait for LOGI (authentication)
        /// </summary>
        [UnityTest]
        public IEnumerator OnConnected_ShouldWaitForLOGI()
        {
            bool connected = false;
            bool authenticated = false;

            client.OnConnected += () => connected = true;
            client.OnAuthenticated += (sessionKey) => authenticated = true;

            client.Connect(testConfig);

            float elapsed = 0f;
            while (!authenticated && elapsed < LOGI_TIMEOUT)
            {
                client.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(connected, "Should connect first");
            Assert.IsTrue(authenticated, "Should receive LOGI and authenticate");
        }

        /// <summary>
        /// LOGI should carry session_key (record locally in test)
        /// </summary>
        [UnityTest]
        public IEnumerator ReceiveLOGI_ShouldStoreSessionKey()
        {
            string sessionKey = null;

            client.OnAuthenticated += (key) => sessionKey = key;

            client.Connect(testConfig);

            float elapsed = 0f;
            while (sessionKey == null && elapsed < LOGI_TIMEOUT)
            {
                client.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(string.IsNullOrEmpty(sessionKey), "Should get session_key");
            Assert.AreEqual(sessionKey, client.SessionKey, "Client should store session key");
        }

        /// <summary>
        /// LOGI should expose session_key through client
        /// </summary>
        [UnityTest]
        public IEnumerator ReceiveLOGI_ShouldExtractSessionKey()
        {
            string sessionKey = null;

            client.OnAuthenticated += (key) => sessionKey = key;

            client.Connect(testConfig);

            float elapsed = 0f;
            while (sessionKey == null && elapsed < LOGI_TIMEOUT)
            {
                client.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsNotNull(sessionKey, "Should receive session key");
            Assert.IsFalse(string.IsNullOrEmpty(sessionKey), "SessionKey should not be empty");
            Assert.AreEqual(sessionKey, client.SessionKey, "Client SessionKey property should match");
        }

        /// <summary>
        /// LOGI authentication should succeed
        /// Note: Ping/pong settings are handled internally by UnityWebSocketClient
        /// </summary>
        [UnityTest]
        public IEnumerator ReceiveLOGI_ShouldAuthenticateSuccessfully()
        {
            bool authenticated = false;
            string sessionKey = null;

            client.OnAuthenticated += (key) =>
            {
                authenticated = true;
                sessionKey = key;
            };

            client.Connect(testConfig);

            float elapsed = 0f;
            while (!authenticated && elapsed < LOGI_TIMEOUT)
            {
                client.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(authenticated, "Should authenticate successfully");
            Assert.IsNotNull(sessionKey, "Should have session key");
            Assert.IsFalse(string.IsNullOrEmpty(sessionKey), "Session key should not be empty");
        }

        /// <summary>
        /// Invalid token should fail (OnError triggered)
        /// </summary>
        [UnityTest]
        public IEnumerator Connect_ShouldFail_WithInvalidToken()
        {
            var invalidConfig = new WebSocketConfig
            {
                ApplicationId = TEST_APP_ID,
                UserId = TEST_USER_ID,
                AccessToken = "invalid_token",
                EnvironmentDomain = VALID_ENV
            };

            bool gotError = false;
            bool authenticated = false;

            client.OnError += _ => gotError = true;
            client.OnAuthenticated += _ => authenticated = true;

            LogAssert.Expect(LogType.Error, new Regex(@".*\[WebSocket\].*"));

            client.Connect(invalidConfig);

            float elapsed = 0f;
            while (!gotError && !authenticated && elapsed < LOGI_TIMEOUT)
            {
                client.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(gotError || !authenticated, "Invalid token should fail auth or not authenticate");
        }

        /// <summary>
        /// If no LOGI within timeout, treat as failure (simulate with invalid domain)
        /// </summary>
        [UnityTest]
        public IEnumerator NoLOGI_Within10Seconds_ShouldTimeout()
        {
            var badConfig = new WebSocketConfig
            {
                ApplicationId = TEST_APP_ID,
                UserId = TEST_USER_ID,
                AccessToken = null,
                EnvironmentDomain = "invalid.gamania.chat"
            };

            bool authenticated = false;
            bool gotError = false;

            client.OnAuthenticated += _ => authenticated = true;
            client.OnError += _ => gotError = true;

            LogAssert.Expect(LogType.Error, new Regex("WebSocket error"));

            client.Connect(badConfig);

            float elapsed = 0f;
            while (!authenticated && !gotError && elapsed < LOGI_TIMEOUT)
            {
                client.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(gotError || !authenticated, "Should error or timeout when no LOGI");
        }

        /// <summary>
        /// Invalid LOGI should flag auth failure
        /// </summary>
        [UnityTest]
        public IEnumerator InvalidLOGI_ShouldTriggerAuthFailed()
        {
            bool gotError = false;
            bool authenticated = false;

            client.OnError += _ => gotError = true;
            client.OnAuthenticated += _ => authenticated = true;

            LogAssert.Expect(LogType.Error, new Regex(@".*\[WebSocket\].*"));

            var invalidTokenConfig = new WebSocketConfig
            {
                ApplicationId = TEST_APP_ID,
                UserId = TEST_USER_ID,
                AccessToken = "invalid_token",
                EnvironmentDomain = VALID_ENV
            };

            client.Connect(invalidTokenConfig);

            float elapsed = 0f;
            while (!gotError && !authenticated && elapsed < LOGI_TIMEOUT)
            {
                client.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(gotError || !authenticated, "Invalid LOGI or error should fail auth");
        }

        #region VyinChat.Connect() Integration Tests

        /// <summary>
        /// VyinChat.Connect() should return VcUser when authentication succeeds
        /// </summary>
        [UnityTest]
        public IEnumerator VyinChatConnect_ShouldReturnUser_WhenSuccess()
        {
            // Arrange
            VyinChat.ResetForTesting();
            var initParams = new VcInitParams(TEST_APP_ID);
            VyinChat.Init(initParams);

            VcUser resultUser = null;
            VcException resultError = null;
            bool callbackCalled = false;

            // Act
            VyinChat.Connect(TEST_USER_ID, null, (user, error) =>
            {
                resultUser = user;
                resultError = error;
                callbackCalled = true;
            });

            // Wait for callback
            float elapsed = 0f;
            while (!callbackCalled && elapsed < LOGI_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(resultError, $"Error should be null. Got: {resultError?.Message}");
            Assert.IsNotNull(resultUser, "User should not be null");
            Assert.AreEqual(TEST_USER_ID, resultUser.UserId, "UserId should match");

            // Cleanup
            VyinChat.ResetForTesting();
        }

        #endregion
    }
}
