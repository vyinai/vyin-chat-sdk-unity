using System;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Mappers;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Internal.Domain.UseCases
{
    /// <summary>
    /// Handles the creation of a new group channel, including input validation and repository interaction.
    /// </summary>
    public class CreateChannelUseCase
    {
        private readonly IChannelRepository _channelRepository;

        public CreateChannelUseCase(IChannelRepository channelRepository)
        {
            _channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
        }

        /// <summary>
        /// Asynchronously creates a new group channel based on the provided parameters.
        /// </summary>
        /// <param name="createParams">Channel creation parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created VcGroupChannel object</returns>
        /// <exception cref="VcException">If validation fails or creation fails</exception>
        public async Task<VcGroupChannel> ExecuteAsync(
            VcGroupChannelCreateParams createParams,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (createParams == null)
            {
                throw new VcException(
                    VcErrorCode.InvalidParameter,
                    "Channel creation parameters cannot be null",
                    "createParams");
            }

            if (createParams.UserIds == null || createParams.UserIds.Count == 0)
            {
                throw new VcException(
                    VcErrorCode.InvalidParameter,
                    "UserIds cannot be null or empty",
                    "createParams.UserIds");
            }

            // Execute repository call
            try
            {
                var channelBo = await _channelRepository.CreateChannelAsync(createParams, cancellationToken)
                ?? throw new VcException(
                        VcErrorCode.Unknown,
                        "Failed to create channel - repository returned null");

                // Convert BO to Public Model
                return ChannelBoMapper.ToPublicModel(channelBo);
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
                    VcErrorCode.Unknown,
                    "Failed to create channel",
                    ex);
            }
        }
    }
}
