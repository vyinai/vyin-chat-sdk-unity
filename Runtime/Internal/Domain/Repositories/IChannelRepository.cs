using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Models;

namespace VyinChatSdk.Internal.Domain.Repositories
{
    /// <summary>
    /// Repository interface for channel data access operations
    /// </summary>
    public interface IChannelRepository
    {
        /// <summary>
        /// Retrieves a channel by its URL
        /// </summary>
        /// <param name="channelUrl">The URL of the channel to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The channel data</returns>
        Task<ChannelBO> GetChannelAsync(
            string channelUrl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new group channel
        /// </summary>
        /// <param name="createParams">Parameters for channel creation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created channel data</returns>
        Task<ChannelBO> CreateChannelAsync(
            VcGroupChannelCreateParams createParams,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing channel's properties
        /// </summary>
        /// <param name="channelUrl">The URL of the channel to update</param>
        /// <param name="updateParams">Parameters for channel update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated channel data</returns>
        Task<ChannelBO> UpdateChannelAsync(
            string channelUrl,
            VcGroupChannelUpdateParams updateParams,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a channel
        /// </summary>
        /// <param name="channelUrl">The URL of the channel to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteChannelAsync(
            string channelUrl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invites users to join a channel
        /// </summary>
        /// <param name="channelUrl">The URL of the channel</param>
        /// <param name="userIds">Array of user IDs to invite</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated channel data</returns>
        Task<ChannelBO> InviteUsersAsync(
            string channelUrl,
            string[] userIds,
            CancellationToken cancellationToken = default);
    }
}
