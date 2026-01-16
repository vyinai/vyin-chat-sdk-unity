using System;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Mappers;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Internal.Domain.UseCases
{
    public class SendMessageUseCase
    {
        private readonly IMessageRepository _messageRepository;

        public SendMessageUseCase(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        }

        public async Task<VcBaseMessage> ExecuteAsync(
            string channelUrl,
            VcUserMessageCreateParams createParams,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(channelUrl))
                throw new VcException(VcErrorCode.InvalidParameter, "ChannelUrl cannot be empty");
            if (createParams == null)
                throw new VcException(VcErrorCode.InvalidParameter, "createParams cannot be null");

            var messageBO = await _messageRepository.SendMessageAsync(channelUrl, createParams, cancellationToken);
            return MessageBoMapper.ToPublicModel(messageBO);
        }
    }
}
