using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VyinChatSdk.Internal.Data.DTOs;
using VyinChatSdk.Internal.Data.Mappers;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Domain.Commands;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Domain.Models;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Internal.Data.Repositories
{
    internal class MessageRepositoryImpl : IMessageRepository
    {
        private readonly ConnectionManager _connectionManager;
        private readonly TimeSpan _ackTimeout = TimeSpan.FromSeconds(15);

        public MessageRepositoryImpl(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public async Task<MessageBO> SendMessageAsync(
            string channelUrl,
            VcUserMessageCreateParams createParams,
            CancellationToken cancellationToken = default)
        {
            if (!_connectionManager.IsConnected || string.IsNullOrEmpty(_connectionManager.SessionKey))
                throw new VcException(VcErrorCode.ConnectionRequired, "Cannot send message: Not connected.");

            try
            {
                var payload = new
                {
                    channel_url = channelUrl,
                    message = createParams.Message,
                    message_type = "MESG",
                    data = createParams.Data ?? "",
                    custom_type = createParams.CustomType ?? ""
                };

                Logger.Debug(LogCategory.Command, $"Sending MESG command for channel: {channelUrl}");

                string ackPayload = await _connectionManager.SendCommandAsync(
                    CommandType.MESG,
                    payload,
                    _ackTimeout,
                    cancellationToken
                );

                if (string.IsNullOrEmpty(ackPayload))
                {
                    throw new VcException(VcErrorCode.AckTimeout, "Message send timeout after 15 seconds");
                }

                Logger.Debug(LogCategory.Command, $"MESG ACK received: {ackPayload}");
                return ParseMessageFromAck(ackPayload, channelUrl, createParams.Message);
            }
            catch (TaskCanceledException)
            {
                throw new VcException(VcErrorCode.AckTimeout, "Message send timeout after 15 seconds");
            }
            catch (VcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new VcException(VcErrorCode.UnknownError, $"SendMessage error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parse ACK response with graceful degradation
        /// Always returns a message (uses fallback if parsing fails)
        /// </summary>
        private MessageBO ParseMessageFromAck(string ackPayload, string channelUrl, string messageText)
        {
            try
            {
                // Step 1: Parse JSON to DTO
                var dto = JsonConvert.DeserializeObject<MessageDTO>(ackPayload);

                if (dto == null)
                {
                    Logger.Warning(LogCategory.Message, "Failed to deserialize ACK payload, using fallback");
                    return CreateFallbackMessage(channelUrl, messageText);
                }

                // Step 2: Handle fallback values (business logic)
                if (string.IsNullOrEmpty(dto.channel_url))
                    dto.channel_url = channelUrl;
                if (string.IsNullOrEmpty(dto.message))
                    dto.message = messageText;

                // Step 3: DTO → BO
                return MessageDtoMapper.ToBusinessObject(dto);
            }
            catch (Exception ex)
            {
                Logger.Warning(LogCategory.Message, $"Failed to parse ACK payload: {ex.Message}. Using fallback.");
                return CreateFallbackMessage(channelUrl, messageText);
            }
        }

        /// <summary>
        /// Create fallback message when parsing fails
        /// </summary>
        private MessageBO CreateFallbackMessage(string channelUrl, string messageText)
        {
            return new MessageBO
            {
                ChannelUrl = channelUrl,
                Message = messageText,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }
}
