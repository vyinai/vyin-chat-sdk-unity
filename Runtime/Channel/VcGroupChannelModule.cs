using System;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.UseCases;
using VyinChatSdk.Internal.Platform;
using VyinChatSdk.Internal.Platform.Unity;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk
{
    public static class VcGroupChannelModule
    {
        private const string TAG = "VcGroupChannelModule";

        #region GetGroupChannel

        /// <summary>
        /// Retrieves a group channel by its URL using async/await pattern.
        /// </summary>
        /// <param name="channelUrl">The unique URL of the channel to retrieve</param>
        /// <returns>The requested group channel</returns>
        /// <exception cref="VcException">Thrown when the operation fails</exception>
        public static async Task<VcGroupChannel> GetGroupChannelAsync(string channelUrl)
        {
            var repository = VyinChatMain.Instance.GetChannelRepository();
            var useCase = new GetChannelUseCase(repository);
            return await useCase.ExecuteAsync(channelUrl);
        }

        /// <summary>
        /// Retrieves a group channel by its URL using callback pattern.
        /// </summary>
        /// <param name="channelUrl">The unique URL of the channel to retrieve</param>
        /// <param name="callback">Callback invoked with the channel or error</param>
        public static void GetGroupChannel(
            string channelUrl,
            VcGroupChannelCallbackHandler callback)
        {
            if (callback == null)
            {
                Logger.Warning(TAG, "GetGroupChannel: callback is null");
                return;
            }

            _ = ExecuteAsyncWithCallback(
                () => GetGroupChannelAsync(channelUrl),
                callback,
                "GetGroupChannel"
            );
        }

        #endregion

        #region CreateGroupChannel

        /// <summary>
        /// Creates a new group channel using async/await pattern.
        /// </summary>
        /// <param name="createParams">Parameters for creating the channel</param>
        /// <returns>The newly created group channel</returns>
        /// <exception cref="VcException">Thrown when the operation fails</exception>
        public static async Task<VcGroupChannel> CreateGroupChannelAsync(VcGroupChannelCreateParams createParams)
        {
            var repository = VyinChatMain.Instance.GetChannelRepository();
            var useCase = new CreateChannelUseCase(repository);
            return await useCase.ExecuteAsync(createParams);
        }

        /// <summary>
        /// Creates a new group channel using callback pattern.
        /// </summary>
        /// <param name="createParams">Parameters for creating the channel</param>
        /// <param name="callback">Callback invoked with the created channel or error</param>
        public static void CreateGroupChannel(
            VcGroupChannelCreateParams createParams,
            VcGroupChannelCallbackHandler callback)
        {
            if (callback == null)
            {
                Logger.Warning(TAG, "CreateGroupChannel: callback is null");
                return;
            }

            _ = ExecuteAsyncWithCallback(
                () => CreateGroupChannelAsync(createParams),
                callback,
                "CreateGroupChannel"
            );
        }

        #endregion

        #region Deprecated CreateGroupChannel (string, string callback)

        /// <summary>
        /// [DEPRECATED] Creates a group channel with JSON string callback - use CreateGroupChannelAsync instead
        /// </summary>
        [Obsolete("Use CreateGroupChannelAsync or CreateGroupChannel with VcGroupChannelCallbackHandler instead")]
        public static void CreateGroupChannel(
            VcGroupChannelCreateParams channelCreateParams,
            Action<string, string> callback)
        {
            if (callback == null)
            {
                Logger.Warning(TAG, "CreateGroupChannel: callback is null");
                return;
            }

            if (channelCreateParams == null)
            {
                callback.Invoke(null, "channelCreateParams is null");
                return;
            }

            // Convert to new API by wrapping the callback
            VcGroupChannelCallbackHandler handler = (channel, error) =>
            {
                if (error != null)
                {
                    callback.Invoke(null, error.Message);
                    return;
                }

                string channelUrl = channel?.ChannelUrl;
                string channelName = channel?.Name;
                string result = $"{{\"channelUrl\":\"{channelUrl}\",\"name\":\"{channelName}\"}}";
                callback.Invoke(result, null);
            };

            CreateGroupChannel(channelCreateParams, handler);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Executes an async operation and invokes callback with result or error.
        /// Ensures callback is always invoked on main thread.
        /// </summary>
        private static async Task ExecuteAsyncWithCallback(
            Func<Task<VcGroupChannel>> asyncOperation,
            VcGroupChannelCallbackHandler callback,
            string operationName)
        {
            try
            {
                var channel = await asyncOperation();
                MainThreadDispatcher.Enqueue(() =>
                {
                    callback?.Invoke(channel, null);
                });
            }
            catch (VcException vcEx)
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Logger.Error(TAG, $"{operationName} failed", vcEx);
                    callback?.Invoke(null, vcEx);
                });
            }
            catch (Exception ex)
            {
                var fallback = new VcException(VcErrorCode.UnknownError, $"Unexpected error: {ex.Message}", ex);
                MainThreadDispatcher.Enqueue(() =>
                {
                    Logger.Error(TAG, $"{operationName} error", fallback);
                    callback?.Invoke(null, fallback);
                });
            }
        }

        #endregion
    }
}
