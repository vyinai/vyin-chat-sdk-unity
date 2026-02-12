using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.UseCases;
using VyinChatSdk.Internal.Platform.Unity;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk
{
    /// <summary>
    /// Represents a group channel for real-time messaging.
    /// </summary>
    public class VcGroupChannel
    {
        private const string TAG = "VcGroupChannel";

        #region Properties

        /// <summary>Unique URL identifier of the channel.</summary>
        public string ChannelUrl { get; set; }

        /// <summary>Display name of the channel.</summary>
        public string Name { get; set; }

        /// <summary>Unix timestamp (milliseconds) when the channel was created.</summary>
        public long CreatedAt { get; set; }

        /// <summary>The most recent message in the channel.</summary>
        public VcBaseMessage LastMessage { get; set; }

        /// <summary>List of users who are members of this channel.</summary>
        public List<VcUser> Members { get; set; }

        /// <summary>Total number of members in the channel.</summary>
        public int MemberCount { get; set; }

        /// <summary>Custom data associated with the channel (JSON string).</summary>
        public string Data { get; set; }

        /// <summary>URL of the channel's cover image.</summary>
        public string CoverUrl { get; set; }

        /// <summary>Custom type for categorizing the channel.</summary>
        public string CustomType { get; set; }

        /// <summary>Whether the channel is distinct.</summary>
        public bool IsDistinct { get; set; }

        /// <summary>Whether the channel is public.</summary>
        public bool IsPublic { get; set; }

        /// <summary>Role of current user in this channel.</summary>
        public VcRole MyRole { get; set; } = VcRole.None;

        #endregion

        #region Send Message

        /// <summary>
        /// Sends a user message to this group channel (callback version).
        /// Returns immediately with a pending message object.
        /// </summary>
        /// <param name="createParams">Parameters for creating the message.</param>
        /// <param name="callback">Callback invoked with the sent message or error.</param>
        /// <returns>Pending message object (status will be updated on completion).</returns>
        public VcUserMessage SendUserMessage(VcUserMessageCreateParams createParams, VcUserMessageHandler callback)
        {
            if (callback == null)
            {
                Logger.Warning(TAG, "SendUserMessage: callback is null");
                return null;
            }

            var pending = CreatePendingUserMessage(createParams);
            _ = AsyncCallbackHelper.ExecuteAsync(
                () => SendUserMessageCoreAsync(createParams, pending),
                (msg, err) => callback(msg, err),
                TAG,
                "SendUserMessage"
            );
            return pending;
        }

        /// <summary>
        /// Sends a user message to this group channel (async version).
        /// If auto-resend is enabled, failed messages due to connection issues
        /// will be automatically queued for resend on reconnection.
        /// </summary>
        /// <param name="createParams">Parameters for creating the user message.</param>
        /// <returns>The sent user message.</returns>
        public async Task<VcUserMessage> SendUserMessageAsync(VcUserMessageCreateParams createParams)
        {
            var pending = CreatePendingUserMessage(createParams);
            return await SendUserMessageCoreAsync(createParams, pending);
        }

        private VcUserMessage CreatePendingUserMessage(VcUserMessageCreateParams createParams)
        {
            return new VcUserMessage
            {
                ReqId = Guid.NewGuid().ToString("N"),
                ChannelUrl = ChannelUrl,
                Message = createParams?.Message,
                CustomType = createParams?.CustomType,
                Data = createParams?.Data,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SendingStatus = VcSendingStatus.Pending,
                ErrorCode = null,
                Sender = BuildPendingSender(MyRole)
            };
        }

        private static VcSender BuildPendingSender(VcRole role)
        {
            var currentUser = VyinChat.CurrentUser;
            if (currentUser == null)
            {
                return null;
            }

            return new VcSender
            {
                UserId = currentUser.UserId,
                Nickname = currentUser.Nickname,
                ProfileUrl = currentUser.ProfileUrl,
                Role = role
            };
        }

        private async Task<VcUserMessage> SendUserMessageCoreAsync(VcUserMessageCreateParams createParams, VcUserMessage pending)
        {
            var repository = VyinChatMain.Instance.GetMessageRepository();
            var autoResender = VyinChatMain.Instance.GetMessageAutoResender();
            var useCase = new SendMessageUseCase(repository, autoResender);
            var sent = await useCase.ExecuteAsync(ChannelUrl, createParams, pending);
            return VcUserMessage.FromBase(sent);
        }

        #endregion

        #region Resend Message

        /// <summary>
        /// Resends a failed user message (callback version).
        /// Only works for messages with resendable error codes.
        /// </summary>
        /// <param name="userMessage">The failed message to resend.</param>
        /// <param name="callback">Callback invoked with the resent message or error.</param>
        public void ResendUserMessage(VcUserMessage userMessage, VcUserMessageHandler callback)
        {
            if (callback == null)
            {
                Logger.Warning(TAG, "ResendUserMessage: callback is null");
                return;
            }

            _ = AsyncCallbackHelper.ExecuteAsync(
                () => ResendUserMessageAsync(userMessage),
                (msg, err) => callback(msg, err),
                TAG,
                "ResendUserMessage"
            );
        }

        /// <summary>
        /// Resends a failed user message (async version).
        /// Only works for messages with resendable error codes.
        /// </summary>
        /// <param name="userMessage">The failed message to resend.</param>
        /// <returns>The resent user message.</returns>
        /// <exception cref="VcException">Thrown if message is not resendable.</exception>
        public async Task<VcUserMessage> ResendUserMessageAsync(VcUserMessage userMessage)
        {
            ValidateResendable(userMessage);

            var createParams = new VcUserMessageCreateParams
            {
                Message = userMessage.Message,
                CustomType = userMessage.CustomType,
                Data = userMessage.Data
            };

            var sent = await SendUserMessageAsync(createParams);
            return VcUserMessage.FromBase(sent);
        }

        private void ValidateResendable(VcUserMessage userMessage)
        {
            if (userMessage == null)
                throw new VcException(VcErrorCode.InvalidParameter, "userMessage is null");

            if (string.IsNullOrEmpty(userMessage.ChannelUrl) || userMessage.ChannelUrl != ChannelUrl)
                throw new VcException(VcErrorCode.InvalidParameter, "message channel mismatch");

            if (userMessage.SendingStatus != VcSendingStatus.Failed)
                throw new VcException(VcErrorCode.InvalidParameter, "message is not in Failed state");

            if (!userMessage.ErrorCode.HasValue || !userMessage.ErrorCode.Value.IsResendable())
                throw new VcException(VcErrorCode.InvalidParameter, "message is not resendable");
        }

        #endregion

        #region Channel Event Handlers

        private static readonly Dictionary<string, VcGroupChannelHandler> _handlers = new();

        /// <summary>
        /// Adds a group channel handler to receive message events.
        /// </summary>
        /// <param name="handlerId">Unique identifier for this handler.</param>
        /// <param name="handler">Handler containing callback functions.</param>
        public static void AddGroupChannelHandler(string handlerId, VcGroupChannelHandler handler)
        {
            if (_handlers.ContainsKey(handlerId))
            {
                Logger.Warning(TAG, $"Handler already exists: {handlerId}");
                return;
            }

            _handlers[handlerId] = handler;
            Logger.Debug(TAG, $"Added group channel handler: {handlerId}");
        }

        /// <summary>
        /// Removes a group channel handler.
        /// </summary>
        /// <param name="handlerId">Unique identifier of the handler to remove.</param>
        public static void RemoveGroupChannelHandler(string handlerId)
        {
            if (_handlers.Remove(handlerId))
            {
                Logger.Debug(TAG, $"Removed group channel handler: {handlerId}");
            }
            else
            {
                Logger.Warning(TAG, $"Handler not found: {handlerId}");
            }
        }

        /// <summary>
        /// Triggers message received event for all registered handlers.
        /// Called internally when a new message is received via WebSocket.
        /// </summary>
        internal static void TriggerMessageReceived(VcGroupChannel channel, VcBaseMessage message)
        {
            foreach (var handler in _handlers.Values)
            {
                try
                {
                    handler.OnMessageReceived?.Invoke(channel, message);
                }
                catch (Exception e)
                {
                    Logger.Error(TAG, "Error in OnMessageReceived handler", e);
                }
            }
        }

        /// <summary>
        /// Triggers message updated event for all registered handlers.
        /// Called internally when a message is updated via WebSocket.
        /// </summary>
        internal static void TriggerMessageUpdated(VcGroupChannel channel, VcBaseMessage message)
        {
            foreach (var handler in _handlers.Values)
            {
                try
                {
                    handler.OnMessageUpdated?.Invoke(channel, message);
                }
                catch (Exception e)
                {
                    Logger.Error(TAG, "Error in OnMessageUpdated handler", e);
                }
            }
        }

        #endregion
    }
}
