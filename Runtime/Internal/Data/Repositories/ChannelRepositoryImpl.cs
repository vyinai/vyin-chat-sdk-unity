using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VyinChatSdk.Internal.Data.Cache;
using VyinChatSdk.Internal.Data.DTOs;
using VyinChatSdk.Internal.Data.Mappers;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Domain.Models;
using VyinChatSdk.Internal.Domain.Repositories;

namespace VyinChatSdk.Internal.Data.Repositories
{
    /// <summary>
    /// Default implementation of channel repository with caching support
    /// </summary>
    public class ChannelRepositoryImpl : IChannelRepository
    {
        private readonly IHttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ChannelCache _cache;
        private readonly bool _cacheEnabled;

        public ChannelRepositoryImpl(
            IHttpClient httpClient,
            string baseUrl,
            bool enableCache = true,
            ChannelCache cache = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _cacheEnabled = enableCache;
            _cache = cache ?? new ChannelCache();
        }

        public async Task<ChannelBO> GetChannelAsync(
            string channelUrl,
            CancellationToken cancellationToken = default)
        {
            if (_cacheEnabled && _cache.TryGet(channelUrl, out var cachedChannel))
            {
                return cachedChannel;
            }

            return await ExecuteAsync(async () =>
            {
                var url = BuildChannelUrl(channelUrl);
                var response = await _httpClient.GetAsync(url, cancellationToken: cancellationToken);

                if (!response.IsSuccess)
                {
                    throw CreateExceptionFromResponse(response);
                }

                return ProcessAndCacheChannelResponse(response);
            }, "Failed to get channel", channelUrl);
        }

        public async Task<ChannelBO> CreateChannelAsync(
            VcGroupChannelCreateParams createParams,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(async () =>
            {
                var url = BuildChannelsEndpoint();
                var requestBody = JsonConvert.SerializeObject(new
                {
                    user_ids = createParams.UserIds,
                    operator_user_ids = createParams.OperatorUserIds,
                    name = createParams.Name,
                    cover_url = createParams.CoverUrl,
                    custom_type = createParams.CustomType,
                    data = createParams.Data,
                    is_distinct = createParams.IsDistinct
                });

                var response = await _httpClient.PostAsync(url, requestBody, cancellationToken: cancellationToken);

                if (!response.IsSuccess)
                {
                    throw CreateExceptionFromResponse(response);
                }

                return ProcessAndCacheChannelResponse(response);
            }, "Failed to create channel");
        }

        public async Task<ChannelBO> UpdateChannelAsync(
            string channelUrl,
            VcGroupChannelUpdateParams updateParams,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(async () =>
            {
                var url = BuildChannelUrl(channelUrl);
                var requestBody = JsonConvert.SerializeObject(new
                {
                    name = updateParams.Name,
                    cover_url = updateParams.CoverUrl,
                    custom_type = updateParams.CustomType,
                    data = updateParams.Data
                });

                var response = await _httpClient.PutAsync(url, requestBody, cancellationToken: cancellationToken);

                if (!response.IsSuccess)
                {
                    throw CreateExceptionFromResponse(response);
                }

                return ProcessAndCacheChannelResponse(response);
            }, "Failed to update channel", channelUrl);
        }

        public async Task DeleteChannelAsync(
            string channelUrl,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async () =>
            {
                var url = BuildChannelUrl(channelUrl);
                var response = await _httpClient.DeleteAsync(url, cancellationToken: cancellationToken);

                if (!response.IsSuccess)
                {
                    throw CreateExceptionFromResponse(response);
                }

                if (_cacheEnabled)
                {
                    _cache.Remove(channelUrl);
                }
            }, "Failed to delete channel", channelUrl);
        }

        public async Task<ChannelBO> InviteUsersAsync(
            string channelUrl,
            string[] userIds,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(async () =>
            {
                var url = $"{BuildChannelUrl(channelUrl)}/invite";
                var requestBody = JsonConvert.SerializeObject(new { user_ids = userIds });

                var response = await _httpClient.PostAsync(url, requestBody, cancellationToken: cancellationToken);

                if (!response.IsSuccess)
                {
                    throw CreateExceptionFromResponse(response);
                }

                return ProcessAndCacheChannelResponse(response);
            }, "Failed to invite users", channelUrl);
        }

        private async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            string errorMessage,
            string context = null)
        {
            try
            {
                return await operation();
            }
            catch (VcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new VcException(VcErrorCode.NetworkError, errorMessage, context, ex);
            }
        }

        private async Task ExecuteAsync(
            Func<Task> operation,
            string errorMessage,
            string context = null)
        {
            try
            {
                await operation();
            }
            catch (VcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new VcException(
                    VcErrorCode.NetworkError,
                    errorMessage,
                    context,
                    ex
                );
            }
        }
        
        private VcException CreateExceptionFromResponse(HttpResponse response)
        {
            return VcException.FromHttpResponse(response);
        }

        private string BuildChannelUrl(string channelUrl)
        {
            return $"{_baseUrl}/group_channels/{channelUrl}";
        }

        private string BuildChannelsEndpoint()
        {
            return $"{_baseUrl}/group_channels";
        }

        private ChannelBO ProcessChannelResponse(HttpResponse response)
        {
            var channelDto = JsonConvert.DeserializeObject<ChannelDTO>(response.Body);
            return ChannelDtoMapper.ToBusinessObject(channelDto);
        }

        /// <summary>
        /// Always cache using canonical channelUrl from response
        /// </summary>
        private ChannelBO ProcessAndCacheChannelResponse(HttpResponse response)
        {
            var channel = ProcessChannelResponse(response);

            if (_cacheEnabled && !string.IsNullOrWhiteSpace(channel.ChannelUrl))
            {
                _cache.Set(channel.ChannelUrl, channel);
            }

            return channel;
        }
    }
}