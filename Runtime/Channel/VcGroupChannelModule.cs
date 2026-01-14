using System;
using System.Threading.Tasks;
using UnityEngine;
using VyinChatSdk.Internal.Platform;
using VyinChatSdk.Internal.Platform.Unity;
using VyinChatSdk.Internal.Domain.UseCases;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        /// Retrieves a group channel by its URL using callback pattern (legacy).
        /// </summary>
        /// <param name="channelUrl">The unique URL of the channel to retrieve</param>
        /// <param name="callback">Callback invoked with the channel or error message</param>
        public static void GetGroupChannel(
            string channelUrl,
            VcGroupChannelCallbackHandler callback)
        {
            if (callback == null)
            {
                Logger.Warning(TAG, "GetGroupChannel: callback is null");
                return;
            }

#if UNITY_EDITOR
            // Unity Editor: Use Pure C# implementation
            _ = ExecuteAsyncWithCallback(
                () => GetGroupChannelAsync(channelUrl),
                callback,
                "GetGroupChannel"
            );
#else
            // Runtime: Check if platform supports GetChannel
            if (Application.platform == RuntimePlatform.Android)
            {
                // Android: Not yet implemented, use Pure C# fallback
                Logger.Warning(TAG, "Android GetChannel not implemented, using Pure C# implementation");
                _ = ExecuteAsyncWithCallback(
                    () => GetGroupChannelAsync(channelUrl),
                    callback,
                    "GetGroupChannel"
                );
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                // iOS: Not yet implemented, use Pure C# fallback
                Logger.Warning(TAG, "iOS GetChannel not implemented, using Pure C# implementation");
                _ = ExecuteAsyncWithCallback(
                    () => GetGroupChannelAsync(channelUrl),
                    callback,
                    "GetGroupChannel"
                );
            }
            else
            {
                // Fallback to Pure C# for other platforms
                _ = ExecuteAsyncWithCallback(
                    () => GetGroupChannelAsync(channelUrl),
                    callback,
                    "GetGroupChannel"
                );
            }
#endif
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
        /// Creates a new group channel using callback pattern (legacy).
        /// </summary>
        /// <param name="createParams">Parameters for creating the channel</param>
        /// <param name="callback">Callback invoked with the created channel or error message</param>
        public static void CreateGroupChannel(
            VcGroupChannelCreateParams createParams,
            VcGroupChannelCallbackHandler callback)
        {
            if (callback == null)
            {
                Logger.Warning(TAG, "CreateGroupChannel: callback is null");
                return;
            }

#if UNITY_EDITOR
            // Unity Editor: Use Pure C# implementation
            _ = ExecuteAsyncWithCallback(
                () => CreateGroupChannelAsync(createParams),
                callback,
                "CreateGroupChannel"
            );
#else
            // Runtime: Use platform-specific implementations
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    AndroidJavaClass androidBridge = new AndroidJavaClass("com.gamania.gim.unitybridge.UnityBridge");
                    AndroidJavaObject paramsObj = createParams.ToAndroidJavaObject();
                    var proxy = new GroupChannelCallbackProxy(callback);
                    androidBridge.CallStatic("createChannel", paramsObj, proxy);
                }
                catch (Exception e)
                {
                    callback?.Invoke(null, e.Message);
                }
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
#if UNITY_IOS
                try
                {
                    Internal.ChatSDKWrapper.CreateGroupChannel(createParams, callback);
                }
                catch (Exception e)
                {
                    Logger.Error(TAG, "Error calling iOS CreateGroupChannel: " + e, e);
                    callback?.Invoke(null, e.Message);
                }
#else
                callback?.Invoke(null, "iOS SDK not available in this build");
#endif
            }
            else
            {
                // Fallback to Pure C# for other platforms
                _ = ExecuteAsyncWithCallback(
                    () => CreateGroupChannelAsync(createParams),
                    callback,
                    "CreateGroupChannel"
                );
            }
#endif
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
                if (!string.IsNullOrEmpty(error))
                {
                    callback.Invoke(null, error);
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
                Logger.Error(TAG, $"{operationName} failed: {vcEx.Message}", vcEx);
                MainThreadDispatcher.Enqueue(() =>
                {
                    callback?.Invoke(null, vcEx.Message);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(TAG, $"{operationName} error: {ex.Message}", ex);
                var errorMessage = $"Unexpected error: {ex.Message}";
                MainThreadDispatcher.Enqueue(() =>
                {
                    callback?.Invoke(null, errorMessage);
                });
            }
        }

        #endregion

        private class GroupChannelCallbackProxy : AndroidJavaProxy
        {
            private readonly VcGroupChannelCallbackHandler callback;
            public GroupChannelCallbackProxy(VcGroupChannelCallbackHandler callback)
                : base("com.gamania.gim.sdk.handler.GroupChannelCallbackHandler")
            {
                this.callback = callback;
            }

            void onResult(AndroidJavaObject channel, AndroidJavaObject exception)
            {
                Logger.Debug(TAG, "onResult: channel=" + channel + ", error=" + exception);
                var error = exception.GetErrorMessage();
                var groupChannel = !string.IsNullOrEmpty(error) ? null : channel.ToVcGroupChannel();

                MainThreadDispatcher.Enqueue(() =>
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        callback?.Invoke(null, error);
                        Logger.Error(TAG, "onResult: error=" + error);
                        return;
                    }
                    Logger.Debug(TAG, "onResult: groupChannel=" + groupChannel);
                    callback.Invoke(groupChannel, null);
                });
            }
        }
    }
}