using System;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Domain.Mappers;
using VyinChatSdk.Internal.Domain.Message;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Internal.Domain.UseCases
{
    public class SendMessageUseCase
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageAutoResender _autoResender;

        public SendMessageUseCase(IMessageRepository messageRepository, IMessageAutoResender autoResender = null)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _autoResender = autoResender;
        }

        public Task<VcBaseMessage> ExecuteAsync(
            string channelUrl,
            VcUserMessageCreateParams createParams,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(channelUrl, createParams, null, cancellationToken);
        }

        public async Task<VcBaseMessage> ExecuteAsync(
            string channelUrl,
            VcUserMessageCreateParams createParams,
            VcUserMessage pendingBaseMessage,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(channelUrl))
                throw new VcException(VcErrorCode.InvalidParameter, "ChannelUrl cannot be empty");
            if (createParams == null)
                throw new VcException(VcErrorCode.InvalidParameter, "createParams cannot be null");

            // Create pending message if auto-resend is enabled
            PendingMessage pendingMessage = null;
            if (_autoResender != null && _autoResender.IsEnabled)
            {
                var requestId = pendingBaseMessage?.ReqId ?? Guid.NewGuid().ToString("N");
                var baseMessage = pendingBaseMessage ?? new VcUserMessage
                {
                    ReqId = requestId,
                    ChannelUrl = channelUrl,
                    Message = createParams?.Message,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                pendingMessage = new PendingMessage(requestId, createParams, baseMessage);

                if (!_autoResender.Register(pendingMessage))
                {
                    Logger.Debug(LogCategory.Message, $"[SendMessage] Queue full or disabled: {requestId}");
                    pendingMessage = null;
                }
            }

            return await SendWithRetryHandlingAsync(pendingMessage, channelUrl, createParams, cancellationToken);
        }

        private async Task<VcBaseMessage> SendWithRetryHandlingAsync(
            PendingMessage pendingMessage,
            string channelUrl,
            VcUserMessageCreateParams createParams,
            CancellationToken cancellationToken)
        {
            pendingMessage?.MarkAsPending();

            try
            {
                var messageBO = await _messageRepository.SendMessageAsync(channelUrl, createParams, cancellationToken);
                var message = MessageBoMapper.ToPublicModel(messageBO);

                // If no pending message (auto-resend disabled), still mark as succeeded.
                if (pendingMessage == null && message != null)
                {
                    message.SendingStatus = VcSendingStatus.Succeeded;
                    message.ErrorCode = null;
                }

                if (pendingMessage != null)
                {
                    pendingMessage.MarkAsSucceeded();
                    _autoResender?.Unregister(pendingMessage.RequestId);

                    if (pendingMessage.BaseMessage != null && message != null)
                    {
                        ApplyServerFields(pendingMessage.BaseMessage, message);
                        return pendingMessage.BaseMessage;
                    }
                }
                Logger.Debug(LogCategory.Message, $"[SendMessage] Success: {pendingMessage?.RequestId ?? "no-queue"}");

                return message;
            }
            catch (VcException vcEx)
            {
                return HandleFailure(pendingMessage, vcEx);
            }
            catch (Exception ex)
            {
                var fallback = new VcException(VcErrorCode.UnknownError, $"Unexpected error: {ex.Message}", ex);
                return HandleFailure(pendingMessage, fallback);
            }
        }

        private static void ApplyServerFields(VcBaseMessage target, VcBaseMessage source)
        {
            target.MessageId = source.MessageId;
            target.Message = source.Message;
            target.ChannelUrl = source.ChannelUrl;
            target.CreatedAt = source.CreatedAt;
            target.Done = source.Done;
            target.CustomType = source.CustomType;
            target.Data = source.Data;
            target.Sender = source.Sender;
            // Only update ReqId if target doesn't have one (preserve original pending ReqId)
            if (string.IsNullOrEmpty(target.ReqId) && !string.IsNullOrEmpty(source.ReqId))
                target.ReqId = source.ReqId;
        }

        private VcBaseMessage HandleFailure(PendingMessage pendingMessage, VcException error)
        {
            if (pendingMessage == null)
            {
                // No auto-resend - throw immediately
                throw error;
            }

            pendingMessage.MarkAsFailed(error.ErrorCode);

            // Check if error is auto-resendable and can retry
            if (error.ErrorCode.IsAutoResendable() && pendingMessage.CanRetry())
            {
                // Keep in queue for auto-resend on reconnection
                pendingMessage.MarkAsPending();
                pendingMessage.IncrementRetry();
                Logger.Info(LogCategory.Message,
                    $"[SendMessage] Queued for resend: {pendingMessage.RequestId}, retry #{pendingMessage.RetryCount}");

                // Throw to notify caller, but message stays in queue
                throw error;
            }

            // Non-resendable error or max retries reached
            Logger.Info(LogCategory.Message,
                $"[SendMessage] Permanent failure: {pendingMessage.RequestId}, error: {error.ErrorCode}");
            throw error;
        }

        /// <summary>
        /// Resend a pending message from the queue.
        /// Called by auto-resender on reconnection.
        /// </summary>
        public async Task<VcBaseMessage> ResendAsync(PendingMessage pendingMessage, CancellationToken cancellationToken = default)
        {
            if (pendingMessage == null)
                throw new ArgumentNullException(nameof(pendingMessage));

            Logger.Info(LogCategory.Message, $"[SendMessage] Resending: {pendingMessage.RequestId}");

            return await SendWithRetryHandlingAsync(
                pendingMessage,
                pendingMessage.ChannelUrl,
                pendingMessage.CreateParams,
                cancellationToken);
        }
    }
}
