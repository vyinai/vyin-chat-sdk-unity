// -----------------------------------------------------------------------------
//
// MessageE2ETests - End-to-End Message Flow Tests
//
// Test Coverage:
// 1. Full message loop verification (Send → Receive via Handler)
// 2. AI streaming flow verification (MESG → MEDI updates → done)
// 3. Additional information parsing (custom_type, data)
//
// Note: Parameter validation tests are in SendMessageValidationTests.cs
//
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Collections;
using System.Collections.Generic;
using VyinChatSdk;
using VyinChatSdk.Internal.Platform.Unity.Network;
using VyinChatSdk.Tests.Runtime;

namespace VyinChatSdk.Tests.Runtime.Message
{
    /// <summary>
    /// End-to-End tests for Message complete scenario verification.
    /// Validates the full message lifecycle from initialization to message exchange.
    /// </summary>
    public class MessageE2ETests : MessageTestBase
    {
        #region Full Message Loop Verification

        /// <summary>
        /// Full E2E test: Init → Connect → CreateChannel → Send → Receive via Handler
        /// Verifies both the send callback (ACK) and handler receive event.
        /// </summary>
        [UnityTest]
        public IEnumerator E2E_CompleteMessageFlow_ShouldSucceed()
        {
            // Setup: Init → Connect → CreateChannel
            yield return ConnectAndCreateChannel();

            Debug.Log("[E2E] Setup completed");

            // Register Handler
            Handler = new VcGroupChannelHandler();
            VcBaseMessage receivedMessage = null;
            VcGroupChannel receivedChannel = null;
            bool handlerTriggered = false;

            Handler.OnMessageReceived += (channel, message) =>
            {
                Debug.Log($"[E2E] Handler received: {message.Message}");
                receivedChannel = channel;
                receivedMessage = message;
                handlerTriggered = true;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, Handler);

            Debug.Log("[E2E] Handler registered");

            // Send Message
            string testMessage = $"E2E Test at {DateTime.UtcNow:HH:mm:ss}";
            VcBaseMessage sentMessage = null;
            string sendError = null;
            bool sendCompleted = false;

            var channel = new VcGroupChannel { ChannelUrl = TestChannelUrl };
            var createParams = new VcUserMessageCreateParams { Message = testMessage };
            channel.SendUserMessage(createParams, (message, error) =>
            {
                sentMessage = message;
                sendError = error;
                sendCompleted = true;
            });

            // Wait for both send callback AND handler receive
            yield return WaitForCondition(() => sendCompleted && handlerTriggered, SEND_MESSAGE_TIMEOUT);

            // Verify Send (ACK)
            Assert.IsTrue(sendCompleted, "Send callback should be called");
            Assert.IsNull(sendError, $"Send should succeed: {sendError}");
            Assert.IsNotNull(sentMessage, "Sent message should not be null");
            Assert.AreEqual(testMessage, sentMessage.Message);
            Assert.AreEqual(TestChannelUrl, sentMessage.ChannelUrl);

            Debug.Log("[E2E] Send verified");

            // Verify Receive (Handler)
            Assert.IsTrue(handlerTriggered, "Handler should receive the message");
            Assert.IsNotNull(receivedMessage, "Received message should not be null");
            Assert.AreEqual(testMessage, receivedMessage.Message);
            Assert.AreEqual(TestChannelUrl, receivedMessage.ChannelUrl);
            Assert.IsNotNull(receivedChannel, "Received channel should not be null");

            Debug.Log("[E2E] ✅ Complete message flow verified!");
        }

        #endregion

        #region AI Streaming Flow Verification

        /// <summary>
        /// AI message initial receive: MESG with done=false triggers OnMessageReceived
        /// </summary>
        [UnityTest]
        public IEnumerator AIMessage_Receive_ShouldTriggerOnMessageReceived()
        {
            // Arrange
            Handler = new VcGroupChannelHandler();
            VcBaseMessage receivedMessage = null;
            bool received = false;

            Handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessage = message;
                received = true;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, Handler);

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

            yield return null;

            // Assert
            Assert.IsTrue(received, "OnMessageReceived should be triggered");
            Assert.IsNotNull(receivedMessage);
            Assert.AreEqual("AI response: ", receivedMessage.Message);
            Assert.AreEqual("ai_response", receivedMessage.CustomType);
            Assert.AreEqual("{\"model\":\"gpt-4\",\"stream\":true}", receivedMessage.Data);
            Assert.IsFalse(receivedMessage.Done, "Initial AI message should have done=false");
        }

        /// <summary>
        /// AI message streaming: Multiple MEDI commands trigger OnMessageUpdated
        /// </summary>
        [UnityTest]
        public IEnumerator AIMessage_Update_ShouldTriggerOnMessageUpdated()
        {
            // Arrange
            Handler = new VcGroupChannelHandler();
            VcBaseMessage updatedMessage = null;
            int updateCount = 0;

            Handler.OnMessageUpdated += (channel, message) =>
            {
                updatedMessage = message;
                updateCount++;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, Handler);

            var client = new UnityWebSocketClient();
            const long messageId = 1002;

            // Act - Simulate streaming updates
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
            Assert.IsNotNull(updatedMessage);
            Assert.AreEqual("AI response: Hello, how can I help", updatedMessage.Message);
            Assert.AreEqual(messageId, updatedMessage.MessageId);
            Assert.IsFalse(updatedMessage.Done);
        }

        /// <summary>
        /// AI message completion: MEDI with done=true marks message as complete
        /// </summary>
        [UnityTest]
        public IEnumerator AIMessage_Done_ShouldMarkComplete()
        {
            // Arrange
            Handler = new VcGroupChannelHandler();
            var updatedMessages = new List<VcBaseMessage>();
            VcBaseMessage finalMessage = null;

            Handler.OnMessageUpdated += (channel, message) =>
            {
                updatedMessages.Add(message);
                if (message.Done)
                {
                    finalMessage = message;
                }
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, Handler);

            var client = new UnityWebSocketClient();
            const long messageId = 1003;

            // Act - Simulate streaming session
            var updates = new (string content, bool done)[]
            {
                ("Processing...", false),
                ("Processing... The answer is", false),
                ("Processing... The answer is 42.", true)
            };

            foreach (var (content, done) in updates)
            {
                var medi = TestMessageBuilder.BuildMediCommand(
                    messageId: messageId,
                    message: content,
                    channelUrl: "gim-ai-test-channel",
                    userId: "ai_bot_001",
                    nickname: "AI Assistant",
                    createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    done: done);

                client.InjectTestMessage(medi);
                yield return null;
            }

            // Assert
            Assert.AreEqual(3, updatedMessages.Count);
            Assert.IsNotNull(finalMessage);
            Assert.IsTrue(finalMessage.Done);
            Assert.AreEqual("Processing... The answer is 42.", finalMessage.Message);

            // Verify progression
            Assert.IsFalse(updatedMessages[0].Done);
            Assert.IsFalse(updatedMessages[1].Done);
            Assert.IsTrue(updatedMessages[2].Done);
        }

        /// <summary>
        /// Complete AI lifecycle: MESG (receive) → MEDI updates → MEDI done=true
        /// </summary>
        [UnityTest]
        public IEnumerator AIMessage_CompleteLifecycle_ReceiveUpdateDone()
        {
            // Arrange
            Handler = new VcGroupChannelHandler();
            VcBaseMessage initialMessage = null;
            var updates = new List<VcBaseMessage>();
            bool messageReceived = false;

            Handler.OnMessageReceived += (channel, message) =>
            {
                initialMessage = message;
                messageReceived = true;
            };

            Handler.OnMessageUpdated += (channel, message) =>
            {
                updates.Add(message);
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, Handler);

            var client = new UnityWebSocketClient();
            const long messageId = 1004;
            string channelUrl = "gim-ai-lifecycle-channel";

            // Phase 1: Initial Receive (MESG)
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

            Assert.IsTrue(messageReceived);
            Assert.IsNotNull(initialMessage);
            Assert.AreEqual(messageId, initialMessage.MessageId);
            Assert.IsFalse(initialMessage.Done);

            // Phase 2: Streaming Updates (MEDI with done=false)
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

            Assert.AreEqual(5, updates.Count);
            foreach (var update in updates)
            {
                Assert.IsFalse(update.Done);
                Assert.AreEqual(messageId, update.MessageId);
            }

            // Phase 3: Final Update (MEDI with done=true)
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

            Assert.AreEqual(6, updates.Count);
            var lastUpdate = updates[updates.Count - 1];
            Assert.IsTrue(lastUpdate.Done);
            Assert.AreEqual("Hello, World!", lastUpdate.Message);

            Debug.Log("[AI Lifecycle] ✅ receive → update(x5) → done verified");
        }

        #endregion

        #region Additional Information Parsing

        /// <summary>
        /// custom_type parsing for various message types
        /// </summary>
        [UnityTest]
        public IEnumerator AdditionalInfo_CustomType_ShouldParseDifferentTypes()
        {
            // Arrange
            Handler = new VcGroupChannelHandler();
            var receivedMessages = new List<VcBaseMessage>();

            Handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessages.Add(message);
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, Handler);

            var client = new UnityWebSocketClient();
            var customTypes = new[] { "text", "image", "file", "ai_response", "system_message", "" };

            // Act
            int msgId = 2000;
            foreach (var customType in customTypes)
            {
                var mesg = TestMessageBuilder.BuildMesgCommand(
                    messageId: msgId++,
                    message: $"Type: {customType}",
                    channelUrl: "gim-custom-type-test",
                    customType: customType);

                client.InjectTestMessage(mesg);
                yield return null;
            }

            // Assert
            Assert.AreEqual(customTypes.Length, receivedMessages.Count);

            for (int i = 0; i < customTypes.Length; i++)
            {
                var expected = customTypes[i];
                var actual = receivedMessages[i].CustomType;

                if (string.IsNullOrEmpty(expected))
                {
                    Assert.IsTrue(string.IsNullOrEmpty(actual));
                }
                else
                {
                    Assert.AreEqual(expected, actual);
                }
            }
        }

        /// <summary>
        /// data JSON parsing for complex payloads
        /// </summary>
        [UnityTest]
        public IEnumerator AdditionalInfo_Data_ShouldParseComplexJson()
        {
            // Arrange
            Handler = new VcGroupChannelHandler();
            var receivedMessages = new List<VcBaseMessage>();

            Handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessages.Add(message);
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, Handler);

            var client = new UnityWebSocketClient();

            var testCases = new Dictionary<string, string>
            {
                { "simple", "{\"key\":\"value\"}" },
                { "nested", "{\"outer\":{\"inner\":\"nested\"}}" },
                { "array", "{\"items\":[1,2,3,4,5]}" },
                { "mixed", "{\"name\":\"test\",\"count\":42,\"active\":true}" },
                { "unicode", "{\"greeting\":\"你好\",\"emoji\":\"🎉\"}" },
                { "ai_meta", "{\"model\":\"gpt-4\",\"tokens\":150}" }
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
            Assert.AreEqual(testCases.Count, receivedMessages.Count);

            int index = 0;
            foreach (var testCase in testCases)
            {
                var msg = receivedMessages[index];
                Assert.IsNotNull(msg.Data);
                Assert.AreEqual(testCase.Value, msg.Data);
                index++;
            }
        }

        /// <summary>
        /// Additional info preserved across MESG → MEDI lifecycle
        /// </summary>
        [UnityTest]
        public IEnumerator AdditionalInfo_ShouldPreserveAcrossLifecycle()
        {
            // Arrange
            Handler = new VcGroupChannelHandler();
            VcBaseMessage receivedMessage = null;
            VcBaseMessage updatedMessage = null;

            Handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessage = message;
            };

            Handler.OnMessageUpdated += (channel, message) =>
            {
                updatedMessage = message;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID, Handler);

            var client = new UnityWebSocketClient();
            const long messageId = 4000;
            const string customType = "ai_streaming_response";
            const string initialMetadata = "{\"session\":\"abc123\",\"sequence\":1}";
            const string updatedMetadata = "{\"session\":\"abc123\",\"sequence\":2,\"tokens\":150}";

            // Act - MESG with initial data
            var initialMsg = TestMessageBuilder.BuildMesgCommand(
                messageId: messageId,
                message: "Initial",
                channelUrl: "gim-preserve-test",
                customType: customType,
                data: initialMetadata);

            client.InjectTestMessage(initialMsg);
            yield return null;

            // Assert MESG
            Assert.IsNotNull(receivedMessage);
            Assert.AreEqual(customType, receivedMessage.CustomType);
            Assert.AreEqual(initialMetadata, receivedMessage.Data);

            // Act - MEDI with updated data
            var updateMsg = TestMessageBuilder.BuildMediCommand(
                messageId: messageId,
                message: "Updated content",
                channelUrl: "gim-preserve-test",
                userId: "sender",
                nickname: "Sender",
                createdAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                done: true,
                customType: customType,
                data: updatedMetadata);

            client.InjectTestMessage(updateMsg);
            yield return null;

            // Assert MEDI
            Assert.IsNotNull(updatedMessage);
            Assert.AreEqual(messageId, updatedMessage.MessageId);
            Assert.AreEqual(customType, updatedMessage.CustomType);
            Assert.AreEqual(updatedMetadata, updatedMessage.Data);
            Assert.IsTrue(updatedMessage.Done);
        }

        #endregion
    }
}
