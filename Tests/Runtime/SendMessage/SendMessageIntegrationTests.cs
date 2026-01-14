// -----------------------------------------------------------------------------
//
// SendMessage Integration Tests - Real Connection Tests
// Tests using REAL WebSocket connection to test server
//
// Test Server: wss://adb53e88-4c35-469a-a888-9e49ef1641b2.gamania.chat/ws
// Test User: tester01
//
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine.TestTools;
using System;
using System.Collections;
using VyinChatSdk;
using VyinChatSdk.Internal.Platform;
using UnityEngine;

namespace VyinChatSdk.Tests.Runtime.SendMessage
{
    /// <summary>
    /// Integration tests using REAL WebSocket connection
    /// Tests SendMessage functionality including MESG command sending and ACK handling
    /// </summary>
    public class SendMessageIntegrationTests
    {
        private const string TAG = "SendMessageIntegrationTests";
        private const string TEST_APP_ID = "adb53e88-4c35-469a-a888-9e49ef1641b2";
        private const string TEST_USER_ID = "tester01";
        private const string TEST_CHANNEL_NAME = "Unity SDK SendMessage Test Channel";
        private const string TEST_MESSAGE = "Hello from Unity SDK!";
        private const float CONNECTION_TIMEOUT = 10f;
        private const float CHANNEL_CREATE_TIMEOUT = 10f;
        private const float SEND_MESSAGE_TIMEOUT = 20f; // 15s ACK timeout + buffer

        private string _testChannelUrl; // Populated after channel creation

        [SetUp]
        public void SetUp()
        {
            VyinChat.ResetForTesting();
            MainThreadDispatcher.ClearQueue();
            _testChannelUrl = null;
        }

        [TearDown]
        public void TearDown()
        {
            VyinChat.ResetForTesting();
            _testChannelUrl = null;
        }

        /// <summary>
        /// Test SendMessage with null channelUrl should call callback with error
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_WithNullChannelUrl_ShouldCallCallbackWithError()
        {
            // Arrange - Connect first
            yield return ConnectAndWait();

            VcBaseMessage resultMessage = null;
            string resultError = null;
            bool callbackCalled = false;

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Message] channelUrl is empty.");

            // Act
            VyinChat.SendMessage(null, TEST_MESSAGE, (message, error) =>
            {
                resultMessage = message;
                resultError = error;
                callbackCalled = true;
            });

            // Wait for callback
            float elapsed = 0f;
            while (!callbackCalled && elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(resultMessage, "Message should be null");
            Assert.IsNotNull(resultError, "Error should not be null");
            Assert.That(resultError, Does.Contain("channelUrl"));
        }

        /// <summary>
        /// Test SendMessage with empty channelUrl should call callback with error
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_WithEmptyChannelUrl_ShouldCallCallbackWithError()
        {
            // Arrange - Connect first
            yield return ConnectAndWait();

            VcBaseMessage resultMessage = null;
            string resultError = null;
            bool callbackCalled = false;

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Message] channelUrl is empty.");

            // Act
            VyinChat.SendMessage("", TEST_MESSAGE, (message, error) =>
            {
                resultMessage = message;
                resultError = error;
                callbackCalled = true;
            });

            // Wait for callback
            float elapsed = 0f;
            while (!callbackCalled && elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(resultMessage, "Message should be null");
            Assert.IsNotNull(resultError, "Error should not be null");
            Assert.That(resultError, Does.Contain("channelUrl"));
        }

        /// <summary>
        /// Test SendMessage with null message should call callback with error
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_WithNullMessage_ShouldCallCallbackWithError()
        {
            // Arrange - Connect first
            yield return ConnectAndWait();

            VcBaseMessage resultMessage = null;
            string resultError = null;
            bool callbackCalled = false;

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Message] message is empty.");

            // Act
            VyinChat.SendMessage(_testChannelUrl, null, (message, error) =>
            {
                resultMessage = message;
                resultError = error;
                callbackCalled = true;
            });

            // Wait for callback
            float elapsed = 0f;
            while (!callbackCalled && elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(resultMessage, "Message should be null");
            Assert.IsNotNull(resultError, "Error should not be null");
            Assert.That(resultError, Does.Contain("message"));
        }

        /// <summary>
        /// Test SendMessage with empty message should call callback with error
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_WithEmptyMessage_ShouldCallCallbackWithError()
        {
            // Arrange - Connect first
            yield return ConnectAndWait();

            VcBaseMessage resultMessage = null;
            string resultError = null;
            bool callbackCalled = false;

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Message] message is empty.");

            // Act
            VyinChat.SendMessage(_testChannelUrl, "", (message, error) =>
            {
                Debug.Log($"[{TAG}][SendMessage_WithEmptyMessage] Callback invoked! message={message}, error={error}");
                resultMessage = message;
                resultError = error;
                callbackCalled = true;
            });

            // Wait for callback
            float elapsed = 0f;
            while (!callbackCalled && elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Debug output
            Debug.Log($"[{TAG}][SendMessage_WithEmptyMessage] After wait: callbackCalled={callbackCalled}, resultMessage={resultMessage}, resultError={resultError}");

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(resultMessage, "Message should be null");
            Assert.IsNotNull(resultError, "Error should not be null");
            Assert.That(resultError, Does.Contain("message"));
        }

        /// <summary>
        /// Test SendMessage should call callback with success when ACK is received
        /// This test verifies:
        /// 1. MESG command is sent with req_id
        /// 2. SDK waits for ACK with matching req_id
        /// 3. Callback is invoked with success (no error)
        /// 4. Message content is correct
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_ShouldCallCallback_WithSuccess_WhenACKReceived()
        {
            // Arrange - Connect first
            yield return ConnectAndWait();

            VcBaseMessage resultMessage = null;
            string resultError = null;
            bool callbackCalled = false;

            // Act
            VyinChat.SendMessage(_testChannelUrl, TEST_MESSAGE, (message, error) =>
            {
                resultMessage = message;
                resultError = error;
                callbackCalled = true;
            });

            // Wait for callback
            float elapsed = 0f;
            while (!callbackCalled && elapsed < SEND_MESSAGE_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");

            // With a real channel created, we expect success
            Assert.IsNotNull(resultMessage, "Message should not be null when sent to valid channel");
            Assert.IsNull(resultError, "Error should be null when message is sent successfully");
            Assert.AreEqual(TEST_MESSAGE, resultMessage.Message);
            Assert.AreEqual(_testChannelUrl, resultMessage.ChannelUrl);
        }

        /// <summary>
        /// Test SendMessage before Connect should throw InvalidOperationException
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_BeforeConnect_ShouldThrow_NoSessionKey()
        {
            // Arrange - Init only, do NOT connect
            var initParams = new VcInitParams(TEST_APP_ID);
            VyinChat.Init(initParams);

            VcBaseMessage resultMessage = null;
            string resultError = null;
            bool callbackCalled = false;
            bool exceptionThrown = false;

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Message] Cannot send message: Not connected (no session key).");

            // Act & Assert
            try
            {
                VyinChat.SendMessage("some-channel", TEST_MESSAGE, (message, error) =>
                {
                    resultMessage = message;
                    resultError = error;
                    callbackCalled = true;
                });
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            // Wait a bit to ensure no callback
            yield return new WaitForSeconds(0.5f);

            // Assert
            Assert.IsTrue(exceptionThrown, "Should throw InvalidOperationException when not connected");
            Assert.IsFalse(callbackCalled, "Callback should not be called when exception is thrown");
        }

        /// <summary>
        /// Test SendMessage when disconnected should throw InvalidOperationException
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_WhenDisconnected_ShouldReturnError()
        {
            // Arrange - Connect first
            yield return ConnectAndWait();

            // Disconnect
            VyinChat.ResetForTesting();

            // Re-init (without connecting)
            var initParams = new VcInitParams(TEST_APP_ID);
            VyinChat.Init(initParams);

            VcBaseMessage resultMessage = null;
            string resultError = null;
            bool callbackCalled = false;
            bool exceptionThrown = false;

            // Expect error log
            LogAssert.Expect(LogType.Error, "[Message] Cannot send message: Not connected (no session key).");

            // Act & Assert
            try
            {
                VyinChat.SendMessage(_testChannelUrl, TEST_MESSAGE, (message, error) =>
                {
                    resultMessage = message;
                    resultError = error;
                    callbackCalled = true;
                });
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            // Wait a bit to ensure no callback
            yield return new WaitForSeconds(0.5f);

            // Assert
            Assert.IsTrue(exceptionThrown, "Should throw InvalidOperationException when disconnected");
            Assert.IsFalse(callbackCalled, "Callback should not be called when exception is thrown");
        }

        /// <summary>
        /// Test complete message flow: Send → ACK → Handler receives broadcast
        /// This verifies the entire E2E message cycle:
        /// 1. SendMessage with callback (tests ACK)
        /// 2. Handler receives server broadcast (tests message distribution)
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_ShouldTriggerHandler_WhenServerBroadcasts()
        {
            // Arrange - Connect and register handler
            yield return ConnectAndWait();

            // Register handler to receive messages
            var handler = new VcGroupChannelHandler();
            VcBaseMessage handlerReceivedMessage = null;
            bool handlerCalled = false;

            handler.OnMessageReceived += (channel, message) =>
            {
                Debug.Log($"[E2E Test] Handler received: {message.Message} from channel {channel?.ChannelUrl}");
                handlerReceivedMessage = message;
                handlerCalled = true;
            };

            VcGroupChannel.AddGroupChannelHandler("e2e_test_handler", handler);

            // Callback for SendMessage ACK
            VcBaseMessage ackMessage = null;
            string ackError = null;
            bool ackCallbackCalled = false;

            // Act - Send message
            VyinChat.SendMessage(_testChannelUrl, TEST_MESSAGE, (message, error) =>
            {
                Debug.Log($"[E2E Test] ACK received: message={message?.Message}, error={error}");
                ackMessage = message;
                ackError = error;
                ackCallbackCalled = true;
            });

            // Wait for both ACK callback AND handler to be triggered
            float elapsed = 0f;
            while ((!ackCallbackCalled || !handlerCalled) && elapsed < SEND_MESSAGE_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Cleanup
            VcGroupChannel.RemoveGroupChannelHandler("e2e_test_handler");

            // Assert - Verify ACK
            Assert.IsTrue(ackCallbackCalled, "ACK callback should be called");
            Assert.IsNotNull(ackMessage, "ACK message should not be null");
            Assert.IsNull(ackError, "ACK error should be null");
            Assert.AreEqual(TEST_MESSAGE, ackMessage.Message);

            // Assert - Verify handler received broadcast
            Assert.IsTrue(handlerCalled, "Handler should receive broadcast message from server");
            Assert.IsNotNull(handlerReceivedMessage, "Handler message should not be null");
            Assert.AreEqual(TEST_MESSAGE, handlerReceivedMessage.Message, "Handler should receive same message content");
            Assert.AreEqual(_testChannelUrl, handlerReceivedMessage.ChannelUrl, "Handler should receive message for correct channel");

            Debug.Log($"[E2E Test] Complete message cycle verified: Send → ACK → Broadcast → Handler");
        }

        /// <summary>
        /// Test SendMessage with invalid channel URL should return error
        /// Server validates channel URL and returns error in ACK for invalid channels
        /// </summary>
        [UnityTest]
        public IEnumerator SendMessage_WithInvalidChannelUrl_ShouldReturnError()
        {
            // Arrange - Connect first
            yield return ConnectAndWait();

            VcBaseMessage resultMessage = null;
            string resultError = null;
            bool callbackCalled = false;

            string invalidChannelUrl = "invalid-channel-!@#$%";

            // Expect error log from null ACK handling
            LogAssert.Expect(LogType.Error, "[Command] SendMessage timeout or empty ACK");

            // Act
            VyinChat.SendMessage(invalidChannelUrl, TEST_MESSAGE, (message, error) =>
            {
                resultMessage = message;
                resultError = error;
                callbackCalled = true;
            });

            // Wait for callback
            float elapsed = 0f;
            while (!callbackCalled && elapsed < SEND_MESSAGE_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNull(resultMessage, "Message should be null for invalid channel");
            Assert.IsNotNull(resultError, "Error should not be null for invalid channel");
        }

        // Helper method to connect and create test channel
        private IEnumerator ConnectAndWait()
        {
            // Step 1: Initialize
            var initParams = new VcInitParams(TEST_APP_ID);
            VyinChat.Init(initParams);

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

            // Wait for connection
            float elapsed = 0f;
            while (!connected && elapsed < CONNECTION_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(connected, $"Should connect within {CONNECTION_TIMEOUT}s");
            Assert.IsNull(connectionError, $"Connection should succeed without error: {connectionError}");
            Assert.IsNotNull(connectedUser, "Connected user should not be null");

            // Step 3: Create test channel (or reuse if already exists via IsDistinct)
            VcGroupChannel createdChannel = null;
            string channelError = null;
            bool channelCreated = false;

            var channelParams = new VcGroupChannelCreateParams
            {
                Name = TEST_CHANNEL_NAME,
                UserIds = new System.Collections.Generic.List<string> { TEST_USER_ID },
                IsDistinct = true // Reuse same channel if it exists
            };

            VcGroupChannelModule.CreateGroupChannel(channelParams, (channel, error) =>
            {
                createdChannel = channel;
                channelError = error;
                channelCreated = true;
            });

            // Wait for channel creation
            elapsed = 0f;
            while (!channelCreated && elapsed < CHANNEL_CREATE_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(channelCreated, $"Should create channel within {CHANNEL_CREATE_TIMEOUT}s");
            Assert.IsNull(channelError, $"Channel creation should succeed without error: {channelError}");
            Assert.IsNotNull(createdChannel, "Created channel should not be null");

            _testChannelUrl = createdChannel.ChannelUrl;
            Debug.Log($"[{TAG}] Channel created/retrieved: {_testChannelUrl}");
            Assert.IsFalse(string.IsNullOrEmpty(_testChannelUrl), "Channel URL should not be empty");
        }
    }
}
