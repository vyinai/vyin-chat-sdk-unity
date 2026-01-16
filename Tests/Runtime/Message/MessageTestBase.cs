// -----------------------------------------------------------------------------
//
// MessageTestBase - Shared Test Infrastructure for Message Tests
// Provides common constants, setup logic, and helper methods for message-related tests
//
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VyinChatSdk;
using VyinChatSdk.Internal.Platform;

namespace VyinChatSdk.Tests.Runtime.Message
{
    /// <summary>
    /// Base class for message-related integration tests.
    /// Provides shared setup logic for Init, Connect, and CreateChannel operations.
    /// </summary>
    public abstract class MessageTestBase
    {
        #region Test Constants

        // Test environment configuration
        protected const string TEST_APP_ID = "adb53e88-4c35-469a-a888-9e49ef1641b2";
        protected const string TEST_USER_ID = "tester01";
        protected const string TEST_CHANNEL_NAME = "Unity SDK Test Channel";
        protected const string TEST_HANDLER_ID = "test_handler";

        // Timeouts
        protected const float CONNECTION_TIMEOUT = 10f;
        protected const float CHANNEL_CREATE_TIMEOUT = 10f;
        protected const float SEND_MESSAGE_TIMEOUT = 20f;
        protected const float MESSAGE_RECEIVE_TIMEOUT = 5f;

        #endregion

        #region Test State

        protected string TestChannelUrl { get; set; }
        protected VcGroupChannelHandler Handler { get; set; }

        #endregion

        #region Setup / Teardown

        [SetUp]
        public virtual void SetUp()
        {
            VyinChat.ResetForTesting();
            MainThreadDispatcher.ClearQueue();
            TestChannelUrl = null;
            Handler = null;
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (Handler != null)
            {
                VcGroupChannel.RemoveGroupChannelHandler(TEST_HANDLER_ID);
                Handler = null;
            }

            VyinChat.ResetForTesting();
            TestChannelUrl = null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Initialize and connect to the server, then create a test channel.
        /// Use this at the start of tests that need a connected channel.
        /// </summary>
        protected IEnumerator ConnectAndCreateChannel()
        {
            // Step 1: Initialize
            var initParams = new VcInitParams(TEST_APP_ID);
            VyinChat.Init(initParams);

            Assert.IsTrue(VyinChat.IsInitialized, "Should be initialized");

            // Step 2: Connect
            VcUser connectedUser = null;
            string connectionError = null;
            bool connected = false;

            VyinChat.Connect(TEST_USER_ID, null, (user, error) =>
            {
                connectedUser = user;
                connectionError = error;
                connected = true;
            });

            float elapsed = 0f;
            while (!connected && elapsed < CONNECTION_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(connected, $"Should connect within {CONNECTION_TIMEOUT}s");
            Assert.IsNull(connectionError, $"Connection should succeed without error: {connectionError}");
            Assert.IsNotNull(connectedUser, "Connected user should not be null");

            // Step 3: Create Channel
            VcGroupChannel createdChannel = null;
            string channelError = null;
            bool channelCreated = false;

            var channelParams = new VcGroupChannelCreateParams
            {
                Name = TEST_CHANNEL_NAME,
                UserIds = new List<string> { TEST_USER_ID },
                IsDistinct = true
            };

            VcGroupChannelModule.CreateGroupChannel(channelParams, (channel, error) =>
            {
                createdChannel = channel;
                channelError = error;
                channelCreated = true;
            });

            elapsed = 0f;
            while (!channelCreated && elapsed < CHANNEL_CREATE_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(channelCreated, $"Should create channel within {CHANNEL_CREATE_TIMEOUT}s");
            Assert.IsNull(channelError, $"Channel creation should succeed without error: {channelError}");
            Assert.IsNotNull(createdChannel, "Created channel should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(createdChannel.ChannelUrl), "ChannelUrl should not be empty");

            TestChannelUrl = createdChannel.ChannelUrl;
            Debug.Log($"[MessageTestBase] Channel created: {TestChannelUrl}");
        }

        /// <summary>
        /// Initialize and connect to the server only (no channel creation).
        /// Use this for tests that don't need a channel or create their own.
        /// </summary>
        protected IEnumerator ConnectOnly()
        {
            // Step 1: Initialize
            var initParams = new VcInitParams(TEST_APP_ID);
            VyinChat.Init(initParams);

            Assert.IsTrue(VyinChat.IsInitialized, "Should be initialized");

            // Step 2: Connect
            VcUser connectedUser = null;
            string connectionError = null;
            bool connected = false;

            VyinChat.Connect(TEST_USER_ID, null, (user, error) =>
            {
                connectedUser = user;
                connectionError = error;
                connected = true;
            });

            float elapsed = 0f;
            while (!connected && elapsed < CONNECTION_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(connected, $"Should connect within {CONNECTION_TIMEOUT}s");
            Assert.IsNull(connectionError, $"Connection should succeed without error: {connectionError}");
            Assert.IsNotNull(connectedUser, "Connected user should not be null");
        }

        /// <summary>
        /// Register a handler with the default TEST_HANDLER_ID.
        /// </summary>
        protected void RegisterHandler(VcGroupChannelHandler handler)
        {
            Handler = handler;
            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, handler);
        }

        /// <summary>
        /// Wait for a condition to become true, with timeout.
        /// </summary>
        protected IEnumerator WaitForCondition(System.Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        #endregion
    }
}
