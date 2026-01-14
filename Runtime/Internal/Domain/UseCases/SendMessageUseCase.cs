using System;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Internal.Domain.UseCases
{
    /// <summary>
    /// Use case for sending a message to a channel
    /// Phase 5: Task 5.1, 5.2
    /// </summary>
    public class SendMessageUseCase
    {
        private readonly IMessageRepository _messageRepository;

        public SendMessageUseCase(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        }

        /// <summary>
        /// Execute the use case - Send a message
        /// </summary>
        /// <param name="channelUrl">Channel URL</param>
        /// <param name="createParams">Message creation parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sent VcBaseMessage object</returns>
        /// <exception cref="VcException">If validation fails or send fails</exception>
        public async Task<VcBaseMessage> ExecuteAsync(
            string channelUrl,
            VcUserMessageCreateParams createParams,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(channelUrl))
            {
                throw new VcException(
                    VcErrorCode.InvalidParameter,
                    "Channel URL cannot be null or empty",
                    "channelUrl");
            }

            if (createParams == null)
            {
                throw new VcException(
                    VcErrorCode.InvalidParameter,
                    "Message creation parameters cannot be null",
                    "createParams");
            }

            if (string.IsNullOrWhiteSpace(createParams.Message))
            {
                throw new VcException(
                    VcErrorCode.InvalidParameterValueRequired,
                    "Message text cannot be null or empty",
                    "createParams.Message");
            }

            // Execute repository call
            try
            {
                var sentMessage = await _messageRepository.SendMessageAsync(channelUrl, createParams, cancellationToken);

                if (sentMessage == null)
                {
                    throw new VcException(
                        VcErrorCode.RequestFailed,
                        "Failed to send message - repository returned null");
                }

                return sentMessage;
            }
            catch (VcException)
            {
                // Re-throw VcException as is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions in VcException
                throw new VcException(
                    VcErrorCode.RequestFailed,
                    "Failed to send message",
                    channelUrl,
                    ex);
            }
        }
    }
}
