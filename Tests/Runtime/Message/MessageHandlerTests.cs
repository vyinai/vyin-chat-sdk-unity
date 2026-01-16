// -----------------------------------------------------------------------------
//
// MessageHandler Tests - Handler Registration and Event Triggering
// Tests for VcGroupChannelHandler registration and message event handling
//
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk;
using VyinChatSdk.Internal.Platform;
using VyinChatSdk.Internal.Platform.Unity.Network;

namespace VyinChatSdk.Tests.Runtime.Message
{
    /// <summary>
    /// Tests for MessageHandler functionality
    /// Verifies handler registration, event triggering, and message routing
    /// </summary>
    public class MessageHandlerTests
    {
        private const string TEST_HANDLER_ID_1 = "test_handler_1";
        private const string TEST_HANDLER_ID_2 = "test_handler_2";
        private const string TEST_CHANNEL_URL = "gim-test-channel-001";
        private const string TEST_MESSAGE = "Test message";

        [SetUp]
        public void SetUp()
        {
            // Clean up all handlers before each test
            CleanupAllHandlers();
            MainThreadDispatcher.ClearQueue();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupAllHandlers();
        }

        /// <summary>
        /// Test that a handler can be registered successfully
        /// </summary>
        [Test]
        public void AddHandler_ShouldRegisterSuccessfully()
        {
            // Arrange
            var handler = new VcGroupChannelHandler();
            bool receivedCalled = false;
            handler.OnMessageReceived += (channel, message) =>
            {
                receivedCalled = true;
            };

            // Act
            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler);

            // Trigger a test message
            var testMessage = CreateTestMessage();
            var testChannel = CreateTestChannel();
            VcGroupChannel.TriggerMessageReceived(testChannel, testMessage);

            // Assert
            Assert.IsTrue(receivedCalled, "Handler should be called after registration");
        }

        /// <summary>
        /// Test that a handler can be removed successfully
        /// </summary>
        [Test]
        public void RemoveHandler_ShouldUnregisterSuccessfully()
        {
            // Arrange
            var handler = new VcGroupChannelHandler();
            bool receivedCalled = false;
            handler.OnMessageReceived += (channel, message) =>
            {
                receivedCalled = true;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler);

            // Act - Remove handler
            VcGroupChannel.RemoveGroupChannelHandler(TEST_HANDLER_ID_1);

            // Trigger a test message
            var testMessage = CreateTestMessage();
            var testChannel = CreateTestChannel();
            VcGroupChannel.TriggerMessageReceived(testChannel, testMessage);

            // Assert
            Assert.IsFalse(receivedCalled, "Handler should not be called after removal");
        }

        /// <summary>
        /// Test that OnMessageReceived is triggered when receiving MESG command
        /// </summary>
        [UnityTest]
        public IEnumerator OnMessageReceived_ShouldTrigger_WhenReceiveMESGCommand()
        {
            // Arrange
            var handler = new VcGroupChannelHandler();
            bool receivedCalled = false;
            VcGroupChannel receivedChannel = null;
            VcBaseMessage receivedMessage = null;

            handler.OnMessageReceived += (channel, message) =>
            {
                receivedCalled = true;
                receivedChannel = channel;
                receivedMessage = message;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler);

            // Act
            var testMessage = CreateTestMessage();
            var testChannel = CreateTestChannel();
            VcGroupChannel.TriggerMessageReceived(testChannel, testMessage);

            yield return null; // Wait one frame for MainThreadDispatcher

            // Assert
            Assert.IsTrue(receivedCalled, "OnMessageReceived should be triggered");
            Assert.IsNotNull(receivedChannel, "Channel should not be null");
            Assert.IsNotNull(receivedMessage, "Message should not be null");
            Assert.AreEqual(TEST_CHANNEL_URL, receivedChannel.ChannelUrl);
            Assert.AreEqual(TEST_MESSAGE, receivedMessage.Message);
        }

        /// <summary>
        /// Test that message is deserialized correctly
        /// </summary>
        [UnityTest]
        public IEnumerator OnMessageReceived_ShouldDeserialize_Correctly()
        {
            // Arrange
            var handler = new VcGroupChannelHandler();
            VcBaseMessage receivedMessage = null;

            handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessage = message;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler);

            // Act
            var client = new UnityWebSocketClient();
            var rawCommandMessage = TestMessageBuilder.BuildMesgCommand(
                messageId: 123456789,
                message: "Test message content",
                channelUrl: TEST_CHANNEL_URL,
                userId: "sender_001",
                nickname: "Test Sender",
                createdAt: 1234567890,
                done: true);

            client.InjectTestMessage(rawCommandMessage);

            yield return null; // Wait one frame for MainThreadDispatcher

            // Assert
            Assert.IsNotNull(receivedMessage, "Message should be received");
            Assert.AreEqual(123456789, receivedMessage.MessageId);
            Assert.AreEqual("Test message content", receivedMessage.Message);
            Assert.AreEqual(TEST_CHANNEL_URL, receivedMessage.ChannelUrl);
            Assert.IsNotNull(receivedMessage.Sender, "Sender should not be null");
            Assert.AreEqual("sender_001", receivedMessage.Sender.UserId);
            Assert.AreEqual("Test Sender", receivedMessage.Sender.Nickname);
            Assert.AreEqual(1234567890, receivedMessage.CreatedAt);
            Assert.IsTrue(receivedMessage.Done);
        }

        /// <summary>
        /// Test that messages are routed to handlers by channel URL
        /// </summary>
        [UnityTest]
        public IEnumerator OnMessageReceived_ShouldRouteByChannelUrl()
        {
            // Arrange
            var handler = new VcGroupChannelHandler();
            string receivedChannelUrl = null;
            string receivedMessageChannelUrl = null;

            handler.OnMessageReceived += (channel, message) =>
            {
                receivedChannelUrl = channel?.ChannelUrl;
                receivedMessageChannelUrl = message?.ChannelUrl;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler);

            var client = new UnityWebSocketClient();

            // Act - Receive MESG for channel 1 (raw WebSocket command message)
            var message1 = TestMessageBuilder.BuildMesgCommand(
                messageId: 1,
                message: "Message 1",
                channelUrl: "gim-channel-001",
                userId: "u1",
                nickname: "n1",
                createdAt: 1,
                done: true);
            client.InjectTestMessage(message1);

            yield return null; // Wait one frame for MainThreadDispatcher

            // Assert - channel instance is created/routed by channel_url
            Assert.AreEqual("gim-channel-001", receivedChannelUrl);
            Assert.AreEqual("gim-channel-001", receivedMessageChannelUrl);

            // Act - Receive MESG for channel 2
            var message2 = TestMessageBuilder.BuildMesgCommand(
                messageId: 2,
                message: "Message 2",
                channelUrl: "gim-channel-002",
                userId: "u2",
                nickname: "n2",
                createdAt: 2,
                done: true);
            client.InjectTestMessage(message2);

            yield return null; // Wait one frame for MainThreadDispatcher

            // Assert
            Assert.AreEqual("gim-channel-002", receivedChannelUrl);
            Assert.AreEqual("gim-channel-002", receivedMessageChannelUrl);
        }

        /// <summary>
        /// Test that multiple handlers all receive the same message
        /// </summary>
        [Test]
        public void MultipleHandlers_ShouldAllReceive()
        {
            // Arrange
            var handler1 = new VcGroupChannelHandler();
            var handler2 = new VcGroupChannelHandler();
            bool handler1Called = false;
            bool handler2Called = false;

            handler1.OnMessageReceived += (channel, message) =>
            {
                handler1Called = true;
            };

            handler2.OnMessageReceived += (channel, message) =>
            {
                handler2Called = true;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler1);
            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_2, handler2);

            // Act
            var testMessage = CreateTestMessage();
            var testChannel = CreateTestChannel();
            VcGroupChannel.TriggerMessageReceived(testChannel, testMessage);

            // Assert
            Assert.IsTrue(handler1Called, "Handler 1 should be called");
            Assert.IsTrue(handler2Called, "Handler 2 should be called");
        }

        /// <summary>
        /// Test that removing one handler does not affect others
        /// </summary>
        [Test]
        public void RemoveHandler_ShouldNotAffectOthers()
        {
            // Arrange
            var handler1 = new VcGroupChannelHandler();
            var handler2 = new VcGroupChannelHandler();
            bool handler1Called = false;
            bool handler2Called = false;

            handler1.OnMessageReceived += (channel, message) =>
            {
                handler1Called = true;
            };

            handler2.OnMessageReceived += (channel, message) =>
            {
                handler2Called = true;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler1);
            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_2, handler2);

            // Act - Remove handler 1
            VcGroupChannel.RemoveGroupChannelHandler(TEST_HANDLER_ID_1);

            // Trigger message
            var testMessage = CreateTestMessage();
            var testChannel = CreateTestChannel();
            VcGroupChannel.TriggerMessageReceived(testChannel, testMessage);

            // Assert
            Assert.IsFalse(handler1Called, "Handler 1 should not be called after removal");
            Assert.IsTrue(handler2Called, "Handler 2 should still be called");
        }

        /// <summary>
        /// Test that handler is triggered on main thread
        /// </summary>
        [UnityTest]
        public IEnumerator Handler_ShouldTriggerOnMainThread()
        {
            // Arrange
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var handler = new VcGroupChannelHandler();
            bool receivedCalled = false;
            int callbackThreadId = -1;

            handler.OnMessageReceived += (channel, message) =>
            {
                receivedCalled = true;
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler);

            // Act
            var client = new UnityWebSocketClient();
            var rawCommandMessage = TestMessageBuilder.BuildMesgCommand(
                messageId: 3,
                message: "Thread test",
                channelUrl: TEST_CHANNEL_URL,
                userId: "u",
                nickname: "n",
                createdAt: 3,
                done: true);

            // Simulate incoming message from a background thread (as in native callbacks / WebSocket threads)
            Task.Run(() =>
            {
                client.InjectTestMessage(rawCommandMessage);
            });

            // Wait up to ~1s for MainThreadDispatcher to invoke callback
            for (int i = 0; i < 60 && !receivedCalled; i++)
            {
                yield return null;
            }

            // Assert
            Assert.IsTrue(receivedCalled, "Handler should be called");
            Assert.AreEqual(mainThreadId, callbackThreadId, "Handler should be triggered on Unity main thread");
        }

        /// <summary>
        /// Test that duplicate handler ID logs warning and does not throw
        /// </summary>
        [UnityTest]
        public IEnumerator DuplicateHandlerId_ShouldLogWarning()
        {
            // Arrange
            var handler1 = new VcGroupChannelHandler();
            var handler2 = new VcGroupChannelHandler();

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler1);

            // Expect warning log with new format [TAG] Message
            LogAssert.Expect(LogType.Warning, $"[VcGroupChannel] Handler already exists: {TEST_HANDLER_ID_1}");

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() =>
            {
                VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler2);
            });

            yield return null; // Wait for async Logger output
        }

        /// <summary>
        /// Test custom_type and data fields are parsed correctly
        /// </summary>
        [UnityTest]
        public IEnumerator OnMessageReceived_ShouldParseAdditionalInformation_Correctly()
        {
            // Arrange
            var handler = new VcGroupChannelHandler();
            VcBaseMessage receivedMessage = null;

            handler.OnMessageReceived += (channel, message) =>
            {
                receivedMessage = message;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler);

            // Act
            var client = new UnityWebSocketClient();
            var rawCommandMessage = TestMessageBuilder.BuildMesgCommand(
                messageId: 999,
                message: "Test with custom fields",
                channelUrl: TEST_CHANNEL_URL,
                customType: "ai_response",
                data: "{\"confidence\":0.95,\"source\":\"gpt-4\"}");

            client.InjectTestMessage(rawCommandMessage);

            yield return null; // Wait one frame for MainThreadDispatcher

            // Assert
            Assert.IsNotNull(receivedMessage, "Message should be received");
            Assert.AreEqual("ai_response", receivedMessage.CustomType, "CustomType should be parsed correctly");
            Assert.AreEqual("{\"confidence\":0.95,\"source\":\"gpt-4\"}", receivedMessage.Data, "Data should be parsed correctly");
        }

        /// <summary>
        /// Test OnMessageUpdated event
        /// </summary>
        [UnityTest]
        public IEnumerator OnMessageUpdated_ShouldTrigger_WhenReceiveMEDICommand()
        {
            // Arrange
            var handler = new VcGroupChannelHandler();
            bool updatedCalled = false;
            VcBaseMessage updatedMessage = null;

            handler.OnMessageUpdated += (channel, message) =>
            {
                updatedCalled = true;
                updatedMessage = message;
            };

            VcGroupChannel.AddGroupChannelHandler(TEST_HANDLER_ID_1, handler);

            // Act
            var client = new UnityWebSocketClient();
            var rawCommandMessage = TestMessageBuilder.BuildMediCommand(
                messageId: 123,
                message: "Updated message",
                channelUrl: TEST_CHANNEL_URL,
                userId: "u1",
                nickname: "n1",
                createdAt: 1234567890,
                done: true);

            client.InjectTestMessage(rawCommandMessage);

            yield return null; // Wait one frame for MainThreadDispatcher

            // Assert
            Assert.IsTrue(updatedCalled, "OnMessageUpdated should be triggered");
            Assert.IsNotNull(updatedMessage, "Updated message should not be null");
            Assert.AreEqual("Updated message", updatedMessage.Message);
            Assert.IsTrue(updatedMessage.Done);
        }

        // Helper methods

        private VcBaseMessage CreateTestMessage()
        {
            return new VcBaseMessage
            {
                MessageId = 123456,
                Message = TEST_MESSAGE,
                ChannelUrl = TEST_CHANNEL_URL,
                Sender = new VcSender
                {
                    UserId = "test_sender",
                    Nickname = "Test Sender"
                },
                CreatedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private VcGroupChannel CreateTestChannel()
        {
            return new VcGroupChannel
            {
                ChannelUrl = TEST_CHANNEL_URL,
                Name = "Test Channel"
            };
        }

        private void CleanupAllHandlers()
        {
            // Remove all test handlers
            VcGroupChannel.RemoveGroupChannelHandler(TEST_HANDLER_ID_1);
            VcGroupChannel.RemoveGroupChannelHandler(TEST_HANDLER_ID_2);
        }
    }

    /// <summary>
    /// Test message builder helper for creating WebSocket command messages
    /// Improves test readability and maintainability
    /// </summary>
    public static class TestMessageBuilder
    {
        /// <summary>
        /// Build a MESG (Message Received) command
        /// </summary>
        public static string BuildMesgCommand(
            long messageId = 123456789,
            string message = "Test message",
            string channelUrl = "gim-test-channel-001",
            string userId = "sender_001",
            string nickname = "Test Sender",
            long createdAt = 1234567890,
            bool done = true,
            string customType = null,
            string data = null,
            string role = null)
        {
            var json = $"MESG{{\"message_id\":{messageId}," +
                       $"\"message\":\"{message}\"," +
                       $"\"channel_url\":\"{channelUrl}\"," +
                       $"\"user\":{{\"user_id\":\"{userId}\",\"nickname\":\"{nickname}\"";

            if (!string.IsNullOrEmpty(role))
            {
                json += $",\"role\":\"{role}\"";
            }

            json += $"}}," +
                    $"\"created_at\":{createdAt}," +
                    $"\"done\":{done.ToString().ToLower()}";

            if (!string.IsNullOrEmpty(customType))
            {
                json += $",\"custom_type\":\"{customType}\"";
            }

            if (!string.IsNullOrEmpty(data))
            {
                var escapedData = data.Replace("\"", "\\\"");
                json += $",\"data\":\"{escapedData}\"";
            }

            json += "}";
            return json;
        }

        /// <summary>
        /// Build a MEDI (Message Updated) command
        /// </summary>
        public static string BuildMediCommand(
            long messageId = 123456789,
            string message = "Updated message",
            string channelUrl = "gim-test-channel-001",
            string userId = "sender_001",
            string nickname = "Test Sender",
            long createdAt = 1234567890,
            bool done = false,
            string customType = null,
            string data = null,
            string role = null)
        {
            var json = $"MEDI{{\"message_id\":{messageId}," +
                       $"\"message\":\"{message}\"," +
                       $"\"channel_url\":\"{channelUrl}\"," +
                       $"\"user\":{{\"user_id\":\"{userId}\",\"nickname\":\"{nickname}\"";

            if (!string.IsNullOrEmpty(role))
            {
                json += $",\"role\":\"{role}\"";
            }

            json += $"}}," +
                    $"\"created_at\":{createdAt}," +
                    $"\"done\":{done.ToString().ToLower()}";

            if (!string.IsNullOrEmpty(customType))
            {
                json += $",\"custom_type\":\"{customType}\"";
            }

            if (!string.IsNullOrEmpty(data))
            {
                var escapedData = data.Replace("\"", "\\\"");
                json += $",\"data\":\"{escapedData}\"";
            }

            json += "}";
            return json;
        }
    }
}
