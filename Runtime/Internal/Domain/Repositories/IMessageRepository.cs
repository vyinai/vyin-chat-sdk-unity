using System.Threading;
using System.Threading.Tasks;

namespace VyinChatSdk.Internal.Domain.Repositories
{
    /// <summary>
    /// Message repository interface
    /// Defines operations for message management
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>
        /// Send a user message to a channel
        /// Phase 5: Task 5.1, 5.2
        /// </summary>
        /// <param name="channelUrl">Channel URL</param>
        /// <param name="createParams">Message creation parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sent message object</returns>
        Task<VcBaseMessage> SendMessageAsync(
            string channelUrl,
            VcUserMessageCreateParams createParams,
            CancellationToken cancellationToken = default);
    }
}
