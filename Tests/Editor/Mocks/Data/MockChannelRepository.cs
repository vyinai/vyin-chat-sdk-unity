// Tests/Runtime/Mocks/Data/MockChannelRepository.cs
// Mock Channel Repository for testing

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Models;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Tests.Mocks.Data
{
    /// <summary>
    /// Mock Channel Repository for testing Use Cases
    /// Allows you to test business logic without HTTP layer
    /// </summary>
    public class MockChannelRepository : IChannelRepository
    {
        private readonly Dictionary<string, ChannelBO> _channels = new Dictionary<string, ChannelBO>();
        private Exception _exceptionToThrow;

        // For verification in tests
        public List<(string operation, object parameters)> OperationHistory { get; } = new List<(string, object)>();

        /// <summary>
        /// Add a channel to the mock database
        /// </summary>
        public void AddChannel(ChannelBO channel)
        {
            if (channel != null && !string.IsNullOrEmpty(channel.ChannelUrl))
            {
                _channels[channel.ChannelUrl] = channel;
            }
        }

        /// <summary>
        /// Set exception to throw on next operation
        /// </summary>
        public void SetExceptionToThrow(Exception exception)
        {
            _exceptionToThrow = exception;
        }

        /// <summary>
        /// Clear all mock data and history
        /// </summary>
        public void Reset()
        {
            _channels.Clear();
            OperationHistory.Clear();
            _exceptionToThrow = null;
        }

        public Task<ChannelBO> GetChannelAsync(
            string channelUrl,
            CancellationToken cancellationToken = default)
        {
            OperationHistory.Add(("GetChannel", channelUrl));

            if (_exceptionToThrow != null)
            {
                var ex = _exceptionToThrow;
                _exceptionToThrow = null;
                throw ex;
            }

            _channels.TryGetValue(channelUrl, out var channel);
            return Task.FromResult(channel);
        }

        public Task<ChannelBO> CreateChannelAsync(
            VcGroupChannelCreateParams createParams,
            CancellationToken cancellationToken = default)
        {
            OperationHistory.Add(("CreateChannel", createParams));

            if (_exceptionToThrow != null)
            {
                var ex = _exceptionToThrow;
                _exceptionToThrow = null;
                throw ex;
            }

            // Create a fake channel
            var channel = new ChannelBO
            {
                ChannelUrl = $"mock_channel_{Guid.NewGuid()}",
                Name = createParams.Name ?? "Mock Channel"
            };

            _channels[channel.ChannelUrl] = channel;
            return Task.FromResult(channel);
        }

        public Task<ChannelBO> UpdateChannelAsync(
            string channelUrl,
            VcGroupChannelUpdateParams updateParams,
            CancellationToken cancellationToken = default)
        {
            OperationHistory.Add(("UpdateChannel", new { channelUrl, updateParams }));

            if (_exceptionToThrow != null)
            {
                var ex = _exceptionToThrow;
                _exceptionToThrow = null;
                throw ex;
            }

            if (_channels.TryGetValue(channelUrl, out var channel))
            {
                if (!string.IsNullOrEmpty(updateParams.Name))
                    channel.Name = updateParams.Name;

                return Task.FromResult(channel);
            }

            return Task.FromResult<ChannelBO>(null);
        }

        public Task DeleteChannelAsync(
            string channelUrl,
            CancellationToken cancellationToken = default)
        {
            OperationHistory.Add(("DeleteChannel", channelUrl));

            if (_exceptionToThrow != null)
            {
                var ex = _exceptionToThrow;
                _exceptionToThrow = null;
                throw ex;
            }

            _channels.Remove(channelUrl);
            return Task.CompletedTask;
        }

        public Task<ChannelBO> InviteUsersAsync(
            string channelUrl,
            string[] userIds,
            CancellationToken cancellationToken = default)
        {
            OperationHistory.Add(("InviteUsers", new { channelUrl, userIds }));

            if (_exceptionToThrow != null)
            {
                var ex = _exceptionToThrow;
                _exceptionToThrow = null;
                throw ex;
            }

            _channels.TryGetValue(channelUrl, out var channel);
            return Task.FromResult(channel);
        }
    }
}
