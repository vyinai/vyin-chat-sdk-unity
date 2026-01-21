using System;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Mappers;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Internal.Domain.UseCases
{
    /// <summary>
    /// Retrieves channel information by URL
    /// </summary>
    public class GetChannelUseCase
    {
        private readonly IChannelRepository _channelRepository;

        public GetChannelUseCase(IChannelRepository channelRepository)
        {
            _channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
        }

        /// <summary>
        /// Retrieves a channel by its URL
        /// </summary>
        /// <param name="channelUrl">The URL of the channel to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The channel information</returns>
        /// <exception cref="VcException">Thrown when the channel URL is invalid or the channel is not found</exception>
        public async Task<VcGroupChannel> ExecuteAsync(
            string channelUrl,
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

            try
            {
                var channelBo = await _channelRepository.GetChannelAsync(channelUrl, cancellationToken);

                if (channelBo == null)
                {
                    throw new VcException(
                        VcErrorCode.ErrChannelNotFound,
                        $"Channel not found: {channelUrl}",
                        channelUrl);
                }

                return ChannelBoMapper.ToPublicModel(channelBo);
            }
            catch (VcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new VcException(
                    VcErrorCode.UnknownError,
                    "Failed to get channel",
                    channelUrl,
                    ex);
            }
        }
    }
}
