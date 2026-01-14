// -----------------------------------------------------------------------------
//
// MessageE2ETests - Task 6.3: Full Message Scenario Verification
// Validates the complete Message flow:
// Init -> Connect -> CreateChannel -> Send Message -> Receive Message
//
// Test Coverage:
// 1. Full message loop verification (E2E)
// 2. AI messaging flow verification (receive / update / done)
// 3. Message lifecycle verification (send / receive / update)
// 4. Additional information parsing verification (custom_type, data)
//
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Collections;
using System.Collections.Generic;
using VyinChatSdk;
using VyinChatSdk.Internal.Platform;
using VyinChatSdk.Internal.Platform.Unity.Network;

namespace VyinChatSdk.Tests.Runtime.MessageHandler
{
    /// <summary>
    /// End-to-End tests for Message complete scenario verification
    /// Validates the full message lifecycle from initialization to message exchange
    /// </summary>
    public class MessageE2ETests
    {
        // Test environment configuration
        private const string TEST_APP_ID = "adb53e88-4c35-469a-a888-9e49ef1641b2";
        private const string TEST_USER_ID = "tester01";
        private const string TEST_CHANNEL_NAME = "Unity SDK E2E Test Channel";
        private const string TEST_HANDLER_ID = "e2e_test_handler";

        // Timeouts
        private const float CONNECTION_TIMEOUT = 10f;
        private const float CHANNEL_CREATE_TIMEOUT = 10f;
        private const float SEND_MESSAGE_TIMEOUT = 20f;
        private const float MESSAGE_RECEIVE_TIMEOUT = 5f;

        private string _testChannelUrl;
        private VcGroupChannelHandler _handler;

        [SetUp]
        public void SetUp()
        {
            VyinChat.ResetForTesting();
            MainThreadDispatcher.ClearQueue();
            _testChannelUrl = null;
            _handler = null;
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup handler
            if (_handler != null)
            {
                VcGroupChannel.RemoveGroupChannelHandler(TEST_HANDLER_ID);
                _handler = null;
            }

            VyinChat.ResetForTesting();
            _testChannelUrl = null;
        }

        #region Full Message Loop Verification (E2E Flow Tests)

        /// <summary>
        /// Task 6.3: Full message loop verification
        /// Init -> Connect -> CreateChannel -> Send Message -> Receive Message
        /// 
        /// Verification points:
        /// - Ensure the message can be sent successfully and received correctly
        /// - Ensure message state and content are consistent
        /// </summary>
        [UnityTest]
        public IEnumerator E2E_CompleteMessageFlow_ShouldSucceed()
        {
            // ===== Step 1: Init =====
            var initParams = new VcInitParams(TEST_APP_ID);
            bool initResult = VyinChat.Init(initParams);
            
            Assert.IsTrue(initResult, "Init should succeed");
            Assert.IsTrue(VyinChat.IsInitialized, "Should be initialized");
            Assert.AreEqual(TEST_APP_ID, VyinChat.GetApplicationId(), "AppId should match");

            Debug.Log("[E2E] Step 1: Init completed");

            // ===== Step 2: Connect =====
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
            Assert.AreEqual(TEST_USER_ID, connectedUser.UserId, "UserId should match");

            Debug.Log("[E2E] Step 2: Connect completed");

            // ===== Step 3: CreateChannel =====
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
            Assert.IsNull(channelError, $"Channel creation should succeed: {channelError}");
            Assert.IsNotNull(createdChannel, "Created channel should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(createdChannel.ChannelUrl), "ChannelUrl should not be empty");

            _testChannelUrl = createdChannel.ChannelUrl;
            Debug.Log($"[E2E] Step 3: CreateChannel completed - {_testChannelUrl}");

            // ===== Step 4: Register Handler for Message Receive =====
            _handler = new VcGroupChannelHandler();
            VcBaseMessage receivedMessage = null;
            VcGroupChannel receivedChannel = null;
            bool handlerTriggered = false;

            _handler.OnMessageReceived += (channel, message) =>
            {
                Debug.Log($"[E2E] Handler received: {message.Message}");
                receivedChannel = channel;
                receivedMessage = message;
                handlerTriggered = true;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);

            Debug.Log("[E2E] Step 4: Handler registered");

            // ===== Step 5: Send Message =====
            string testMessage = $"E2E Test Message at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            VcBaseMessage sentMessage = null;
            string sendError = null;
            bool sendCompleted = false;

            VyinChat.SendMessage(_testChannelUrl, testMessage, (message, error) =>
            {
                sentMessage = message;
                sendError = error;
                sendCompleted = true;
            });

            // Wait for both the send callback AND the handler receive event
            elapsed = 0f;
            while ((!sendCompleted || !handlerTriggered) && elapsed < SEND_MESSAGE_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // ===== Verify Send =====
            Assert.IsTrue(sendCompleted, "Send callback should be called");
            Assert.IsNull(sendError, $"Send should succeed without error: {sendError}");
            Assert.IsNotNull(sentMessage, "Sent message should not be null");
            Assert.AreEqual(testMessage, sentMessage.Message, "Sent message content should match");
            Assert.AreEqual(_testChannelUrl, sentMessage.ChannelUrl, "Sent message channelUrl should match");

            Debug.Log("[E2E] Step 5: Send Message completed");

            // ===== Step 6: Verify Receive =====
            Assert.IsTrue(handlerTriggered, "Handler should receive the message");
            Assert.IsNotNull(receivedMessage, "Received message should not be null");
            Assert.AreEqual(testMessage, receivedMessage.Message, "Received message content should match sent");
            Assert.AreEqual(_testChannelUrl, receivedMessage.ChannelUrl, "Received message channelUrl should match");
            Assert.IsNotNull(receivedChannel, "Received channel should not be null");
            Assert.AreEqual(_testChannelUrl, receivedChannel.ChannelUrl, "Received channel URL should match");

            Debug.Log("[E2E] Step 6: Receive verification completed");
            Debug.Log("[E2E] ✅ Complete message flow verified successfully!");
        }

        #endregion

        #region AI Messaging Flow Verification

        /// <summary>
        /// Task 6.3: AI message receive phase verification
        /// Verifies the initial MESG message receive behavior
        /// </summary>
        [UnityTest]
        public IEnumerator AIMessage_Receive_ShouldTriggerOnMessageReceived()
        {
            // Arrange
            _handler = new VcGroupChannelHandler();
            VcBaseMessage receivedMessage = null;
            bool received = false;

            _handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessage = message;
                received = true;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);

            // Act - Inject AI message (MESG command)
            var client = new UnityWebSocketClient();
            var aiMessage = TestMessageBuilder.BuildMesgCommand(
                messageId: 1001,
                message: "AI response: ",
                channelUrl: "gim-ai-test-channel",
                userId: "ai_bot_001",
                nickname: "AI Assistant",
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                done: false,
                customType: "ai_response",
                data: "{\"model\":\"gpt-4\",\"stream\":true}");

            client.InjectTestMessage(aiMessage);

            yield return null; // Wait one frame for MainThreadDispatcher

            // Assert
            Assert.IsTrue(received, "OnMessageReceived should be triggered for AI message");
            Assert.IsNotNull(receivedMessage, "Message should not be null");
            Assert.AreEqual("AI response: ", receivedMessage.Message);
            Assert.AreEqual("ai_response", receivedMessage.CustomType);
            Assert.AreEqual("{\"model\":\"gpt-4\",\"stream\":true}", receivedMessage.Data);
            Assert.IsFalse(receivedMessage.Done, "Initial AI message should have done=false");
        }

        /// <summary>
        /// Task 6.3: AI message update phase verification
        /// Verifies MEDI triggers OnMessageUpdated (streaming updates)
        /// </summary>
        [UnityTest]
        public IEnumerator AIMessage_Update_ShouldTriggerOnMessageUpdated()
        {
            // Arrange
            _handler = new VcGroupChannelHandler();
            VcBaseMessage updatedMessage = null;
            int updateCount = 0;

            _handler.OnMessageUpdated += (channel, message) =>
            {
                updatedMessage = message;
                updateCount++;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);

            var client = new UnityWebSocketClient();
            const long messageId = 1002;

            // Act - Simulate streaming updates (multiple MEDI commands with done=false)
            var streamChunks = new[]
            {
                "AI response: Hello",
                "AI response: Hello, how",
                "AI response: Hello, how can I help"
            };

            foreach (var chunk in streamChunks)
            {
                var updateCommand = TestMessageBuilder.BuildMediCommand(
                    messageId: messageId,
                    message: chunk,
                    channelUrl: "gim-ai-test-channel",
                    userId: "ai_bot_001",
                    nickname: "AI Assistant",
                    createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    done: false);

                client.InjectTestMessage(updateCommand);
                yield return null;
            }

            // Assert
            Assert.AreEqual(3, updateCount, "Should receive 3 update events");
            Assert.IsNotNull(updatedMessage, "Updated message should not be null");
            Assert.AreEqual("AI response: Hello, how can I help", updatedMessage.Message, "Should have latest content");
            Assert.AreEqual(messageId, updatedMessage.MessageId, "MessageId should be consistent");
            Assert.IsFalse(updatedMessage.Done, "Streaming updates should have done=false");
        }

        /// <summary>
        /// Task 6.3: AI message done phase verification
        /// Verifies completion state when MEDI includes done=true
        /// </summary>
        [UnityTest]
        public IEnumerator AIMessage_Done_ShouldMarkComplete()
        {
            // Arrange
            _handler = new VcGroupChannelHandler();
            var updatedMessages = new List<VcBaseMessage>();
            VcBaseMessage finalMessage = null;

            _handler.OnMessageUpdated += (channel, message) =>
            {
                updatedMessages.Add(message);
                if (message.Done)
                {
                    finalMessage = message;
                }
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);

            var client = new UnityWebSocketClient();
            const long messageId = 1003;

            // Act - Simulate a complete streaming session
            // Update 1: Partial content
            var update1 = TestMessageBuilder.BuildMediCommand(
                messageId: messageId,
                message: "Processing...",
                channelUrl: "gim-ai-test-channel",
                userId: "ai_bot_001",
                nickname: "AI Assistant",
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                done: false);
            client.InjectTestMessage(update1);
            yield return null;

            // Update 2: More content
            var update2 = TestMessageBuilder.BuildMediCommand(
                messageId: messageId,
                message: "Processing... The answer is",
                channelUrl: "gim-ai-test-channel",
                userId: "ai_bot_001",
                nickname: "AI Assistant",
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                done: false);
            client.InjectTestMessage(update2);
            yield return null;

            // Final: Complete with done=true
            var finalUpdate = TestMessageBuilder.BuildMediCommand(
                messageId: messageId,
                message: "Processing... The answer is 42.",
                channelUrl: "gim-ai-test-channel",
                userId: "ai_bot_001",
                nickname: "AI Assistant",
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                done: true);
            client.InjectTestMessage(finalUpdate);
            yield return null;

            // Assert
            Assert.AreEqual(3, updatedMessages.Count, "Should receive 3 update events");
            Assert.IsNotNull(finalMessage, "Final message should not be null");
            Assert.IsTrue(finalMessage.Done, "Final message should have done=true");
            Assert.AreEqual("Processing... The answer is 42.", finalMessage.Message, "Final content should be complete");
            Assert.AreEqual(messageId, finalMessage.MessageId, "MessageId should be consistent across updates");

            // Verify progression: done should only be true on the last message
            Assert.IsFalse(updatedMessages[0].Done, "First update should have done=false");
            Assert.IsFalse(updatedMessages[1].Done, "Second update should have done=false");
            Assert.IsTrue(updatedMessages[2].Done, "Third update should have done=true");
        }

        /// <summary>
        /// Task 6.3: Complete AI message lifecycle verification
        /// Verifies receive -> update -> done flow end-to-end
        /// </summary>
        [UnityTest]
        public IEnumerator AIMessage_CompleteLifecycle_ReceiveUpdateDone()
        {
            // Arrange
            _handler = new VcGroupChannelHandler();
            VcBaseMessage initialMessage = null;
            var updates = new List<VcBaseMessage>();
            bool messageReceived = false;

            _handler.OnMessageReceived += (channel, message) =>
            {
                initialMessage = message;
                messageReceived = true;
            };

            _handler.OnMessageUpdated += (channel, message) =>
            {
                updates.Add(message);
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);

            var client = new UnityWebSocketClient();
            const long messageId = 1004;
            string channelUrl = "gim-ai-lifecycle-channel";

            // ===== Phase 1: Initial Receive (MESG) =====
            var initialMesg = TestMessageBuilder.BuildMesgCommand(
                messageId: messageId,
                message: "",
                channelUrl: channelUrl,
                userId: "ai_bot",
                nickname: "AI",
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                done: false,
                customType: "ai_streaming");

            client.InjectTestMessage(initialMesg);
            yield return null;

            Assert.IsTrue(messageReceived, "Should receive initial MESG");
            Assert.IsNotNull(initialMessage, "Initial message should not be null");
            Assert.AreEqual(messageId, initialMessage.MessageId);
            Assert.IsFalse(initialMessage.Done, "Initial message done should be false");

            // ===== Phase 2: Streaming Updates (MEDI with done=false) =====
            var streamingContent = new[] { "H", "He", "Hel", "Hell", "Hello" };
            foreach (var content in streamingContent)
            {
                var medi = TestMessageBuilder.BuildMediCommand(
                    messageId: messageId,
                    message: content,
                    channelUrl: channelUrl,
                    userId: "ai_bot",
                    nickname: "AI",
                    createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    done: false);

                client.InjectTestMessage(medi);
                yield return null;
            }

            Assert.AreEqual(5, updates.Count, "Should receive 5 update events");
            foreach (var update in updates)
            {
                Assert.IsFalse(update.Done, "All streaming updates should have done=false");
                Assert.AreEqual(messageId, update.MessageId, "MessageId should be consistent");
            }

            // ===== Phase 3: Final Update (MEDI with done=true) =====
            var finalMedi = TestMessageBuilder.BuildMediCommand(
                messageId: messageId,
                message: "Hello, World!",
                channelUrl: channelUrl,
                userId: "ai_bot",
                nickname: "AI",
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                done: true);

            client.InjectTestMessage(finalMedi);
            yield return null;

            Assert.AreEqual(6, updates.Count, "Should receive 6 total update events");
            var lastUpdate = updates[updates.Count - 1];
            Assert.IsTrue(lastUpdate.Done, "Final update should have done=true");
            Assert.AreEqual("Hello, World!", lastUpdate.Message, "Final message content should be complete");

            Debug.Log("[AI Lifecycle] ✅ Complete AI message lifecycle verified: receive -> update(x5) -> done");
        }

        #endregion

        #region Additional Information Parsing Verification

        /// <summary>
        /// Task 6.3: custom_type parsing verification
        /// Verifies correct parsing for various custom_type values
        /// </summary>
        [UnityTest]
        public IEnumerator AdditionalInfo_CustomType_ShouldParseDifferentTypes()
        {
            // Arrange
            _handler = new VcGroupChannelHandler();
            var receivedMessages = new List<VcBaseMessage>();

            _handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessages.Add(message);
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);

            var client = new UnityWebSocketClient();
            var customTypes = new[]
            {
                "text",
                "image",
                "file",
                "ai_response",
                "system_message",
                "user_action",
                "" // empty custom_type
            };

            // Act
            int msgId = 2000;
            foreach (var customType in customTypes)
            {
                var mesg = TestMessageBuilder.BuildMesgCommand(
                    messageId: msgId++,
                    message: $"Message with type: {customType}",
                    channelUrl: "gim-custom-type-test",
                    customType: customType);

                client.InjectTestMessage(mesg);
                yield return null;
            }

            // Assert
            Assert.AreEqual(customTypes.Length, receivedMessages.Count, "Should receive all messages");

            for (int i = 0; i < customTypes.Length; i++)
            {
                var expected = customTypes[i];
                var actual = receivedMessages[i].CustomType;

                if (string.IsNullOrEmpty(expected))
                {
                    Assert.IsTrue(string.IsNullOrEmpty(actual),
                        $"Empty custom_type should result in null/empty, got: '{actual}'");
                }
                else
                {
                    Assert.AreEqual(expected, actual,
                        $"custom_type mismatch at index {i}");
                }
            }
        }

        /// <summary>
        /// Task 6.3: data JSON parsing verification
        /// Verifies correct parsing for more complex JSON data payloads
        /// </summary>
        [UnityTest]
        public IEnumerator AdditionalInfo_Data_ShouldParseComplexJson()
        {
            // Arrange
            _handler = new VcGroupChannelHandler();
            var receivedMessages = new List<VcBaseMessage>();

            _handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessages.Add(message);
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);

            var client = new UnityWebSocketClient();

            // Test various data JSON structures
            var testCases = new Dictionary<string, string>
            {
                { "simple_object", "{\"key\":\"value\"}" },
                { "nested_object", "{\"outer\":{\"inner\":\"nested\"}}" },
                { "array", "{\"items\":[1,2,3,4,5]}" },
                { "mixed", "{\"name\":\"test\",\"count\":42,\"active\":true,\"data\":null}" },
                { "unicode", "{\"greeting\":\"你好\",\"emoji\":\"🎉\"}" },
                { "ai_metadata", "{\"model\":\"gpt-4\",\"tokens\":150,\"confidence\":0.95}" },
                { "url_data", "{\"url\":\"https://example.com/path?a=1&b=2\"}" }
            };

            // Act
            int msgId = 3000;
            foreach (var testCase in testCases)
            {
                var mesg = TestMessageBuilder.BuildMesgCommand(
                    messageId: msgId++,
                    message: $"Test: {testCase.Key}",
                    channelUrl: "gim-data-test",
                    customType: "test",
                    data: testCase.Value);

                client.InjectTestMessage(mesg);
                yield return null;
            }

            // Assert
            Assert.AreEqual(testCases.Count, receivedMessages.Count, "Should receive all messages");

            int index = 0;
            foreach (var testCase in testCases)
            {
                var msg = receivedMessages[index];
                Assert.IsNotNull(msg.Data, $"Data should not be null for {testCase.Key}");
                Assert.AreEqual(testCase.Value, msg.Data,
                    $"Data mismatch for {testCase.Key}");
                index++;
            }
        }

        /// <summary>
        /// Task 6.3: Additional Information end-to-end propagation verification
        /// Ensures custom data is propagated correctly across MESG and MEDI events
        /// </summary>
        [UnityTest]
        public IEnumerator AdditionalInfo_ShouldPreserveAcrossLifecycle()
        {
            // Arrange
            _handler = new VcGroupChannelHandler();
            VcBaseMessage receivedMessage = null;
            VcBaseMessage updatedMessage = null;

            _handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessage = message;
            };

            _handler.OnMessageUpdated += (channel, message) =>
            {
                updatedMessage = message;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);

            var client = new UnityWebSocketClient();
            const long messageId = 4000;
            const string customType = "ai_streaming_response";
            const string initialMetadata = "{\"session\":\"abc123\",\"sequence\":1}";
            const string updatedMetadata = "{\"session\":\"abc123\",\"sequence\":2,\"tokens\":150}";

            // Act - Send initial message with custom data (MESG)
            var initialMessage = TestMessageBuilder.BuildMesgCommand(
                messageId: messageId,
                message: "Initial",
                channelUrl: "gim-preserve-test",
                customType: customType,
                data: initialMetadata);

            client.InjectTestMessage(initialMessage);
            yield return null;

            // Assert received message - verify MESG phase customType and data
            Assert.IsNotNull(receivedMessage, "Should receive message");
            Assert.AreEqual(customType, receivedMessage.CustomType, "CustomType should be preserved on receive");
            Assert.AreEqual(initialMetadata, receivedMessage.Data, "Data should be preserved on receive");

            // Act - Send update with the same customType but updated data (MEDI)
            var updateMessage = TestMessageBuilder.BuildMediCommand(
                messageId: messageId,
                message: "Updated content with more tokens",
                channelUrl: "gim-preserve-test",
                userId: "sender",
                nickname: "Sender",
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                done: true,
                customType: customType,        // same customType
                data: updatedMetadata);        // updated data

            client.InjectTestMessage(updateMessage);
            yield return null;

            // Assert updated message - verify MEDI phase customType and data propagation
            Assert.IsNotNull(updatedMessage, "Should receive update");
            Assert.AreEqual(messageId, updatedMessage.MessageId, "MessageId should be consistent");
            Assert.AreEqual(customType, updatedMessage.CustomType, "CustomType should be preserved on update");
            Assert.AreEqual(updatedMetadata, updatedMessage.Data, "Data should be preserved on update");
            Assert.IsTrue(updatedMessage.Done, "Update should have done=true");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Setup helper: Initialize, Connect, and CreateChannel
        /// Call this at the start of tests that need a connected channel
        /// </summary>
        private IEnumerator SetupConnectedChannel()
        {
            // Init
            var initParams = new VcInitParams(TEST_APP_ID);
            VyinChat.Init(initParams);

            // Connect
            bool connected = false;
            VyinChat.Connect(TEST_USER_ID, null, (user, error) =>
            {
                connected = true;
            });

            float elapsed = 0f;
            while (!connected && elapsed < CONNECTION_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(connected, "Should connect");

            // Create Channel
            bool channelCreated = false;
            var channelParams = new VcGroupChannelCreateParams
            {
                Name = TEST_CHANNEL_NAME,
                UserIds = new List<string> { TEST_USER_ID },
                IsDistinct = true
            };

            VcGroupChannelModule.CreateGroupChannel(channelParams, (channel, error) =>
            {
                _testChannelUrl = channel?.ChannelUrl;
                channelCreated = true;
            });

            elapsed = 0f;
            while (!channelCreated && elapsed < CHANNEL_CREATE_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(channelCreated, "Should create channel");
            Assert.IsFalse(string.IsNullOrEmpty(_testChannelUrl), "ChannelUrl should not be empty");

            // Register handler
            _handler = new VcGroupChannelHandler();
            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, _handler);
        }

        #endregion
    }
}
