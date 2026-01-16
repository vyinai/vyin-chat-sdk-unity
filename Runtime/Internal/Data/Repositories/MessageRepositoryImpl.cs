using System;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Data.Mappers;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Domain.Commands;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Domain.Models;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Internal.Data.Repositories
{
    public class MessageRepositoryImpl : IMessageRepository
    {
        private readonly IWebSocketClient _webSocketClient;
        private readonly TimeSpan _ackTimeout = TimeSpan.FromSeconds(15);

        public MessageRepositoryImpl(IWebSocketClient webSocketClient)
        {
            _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        }

        public async Task<MessageBO> SendMessageAsync(
            string channelUrl,
            VcUserMessageCreateParams createParams,
            CancellationToken cancellationToken = default)
        {
            if (_webSocketClient == null || !_webSocketClient.IsConnected || string.IsNullOrEmpty(_webSocketClient.SessionKey))
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

                string ackPayload = await _webSocketClient.SendCommandAsync(
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
                throw new VcException(VcErrorCode.Unknown, $"SendMessage error: {ex.Message}", ex);
            }
        }

        private MessageBO ParseMessageFromAck(string ackPayload, string channelUrl, string messageText)
        {
            try
            {
                var dto = MessageDtoMapper.ParseFromJson(ackPayload, channelUrl, messageText);
                return MessageDtoMapper.ToBusinessObject(dto);
            }
            catch (Exception ex)
            {
                Logger.Warning(LogCategory.Message, $"Failed to parse ACK payload: {ex.Message}. Falling back to basic message.");
                return new MessageBO
                {
                    ChannelUrl = channelUrl,
                    Message = messageText,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }
        }
    }
}
