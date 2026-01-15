// -----------------------------------------------------------------------------
//
// Runtime SDK main entry
//
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

using VyinChatSdk.Internal.Platform.Unity;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk
{
    public static class VyinChat
    {
        private const string TAG = "VyinChat";
        private static readonly VyinChatMain vyinChatImpl;

        static VyinChat()
        {
            Logger.SetInstance(UnityLoggerImpl.Instance);

            // All platforms now use Pure C# implementation
            vyinChatImpl = VyinChatMain.Instance;
        }

        private static VcInitParams _initParams;

        /// <summary>
        /// Gets initializing state
        /// </summary>
        /// <returns>If true, VyinChat instance is initialized</returns>
        public static bool IsInitialized => _initParams != null;

        /// <summary>
        /// Gets whether local caching is enabled
        /// </summary>
        public static bool UseLocalCaching => _initParams?.IsLocalCachingEnabled ?? false;

        /// <summary>
        /// Gets the Application ID which was used for initialization
        /// </summary>
        /// <returns>The Application ID, or null if not initialized</returns>
        public static string GetApplicationId() => _initParams?.AppId;

        /// <summary>
        /// Gets the log level
        /// </summary>
        /// <returns>The log level</returns>
        public static VcLogLevel GetLogLevel() => _initParams?.LogLevel ?? VcLogLevel.None;

        /// <summary>
        /// Gets the app version
        /// </summary>
        /// <returns>The app version</returns>
        public static string GetAppVersion() => _initParams?.AppVersion;

        /// <summary>
        /// Initializes VyinChat singleton instance with VyinChat Application ID
        /// This method must be run first in order to use VyinChat
        /// </summary>
        /// <param name="initParams">VcInitParams object</param>
        /// <returns>true if the applicationId is set successfully</returns>
        public static bool Init(VcInitParams initParams)
        {
            // Check for null params
            if (initParams == null)
            {
                Logger.Error(message: "Init failed: initParams is null");
                return false;
            }

            // Check for empty appId
            if (string.IsNullOrEmpty(initParams.AppId))
            {
                Logger.Error(message: "Init failed: AppId is empty");
                return false;
            }

            // Check if already initialized with different appId
            if (_initParams != null && _initParams.AppId != initParams.AppId)
            {
                Logger.Error(message: $"Init failed: App ID needs to be the same as the previous one. " +
                    $"Previous: {_initParams.AppId}, New: {initParams.AppId}");
                return false;
            }

            // Set init params
            _initParams = initParams;

            // Call platform-specific implementation
            try
            {
                vyinChatImpl.Init(initParams);

                // Configure Logger after init so init messages are not filtered
                var logLevel = Internal.Domain.Log.LogLevelMapper.FromVcLogLevel(initParams.LogLevel);
                Logger.SetLogLevel(logLevel);

                Logger.Info(message: $"SDK initialized with AppId: {initParams.AppId}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(message: $"Init failed with exception: {e.Message}", exception: e);
                _initParams = null;
                return false;
            }
        }

        /// <summary>
        /// Connect to VyinChat server with user ID only.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="callback">Callback with user or error</param>
        public static void Connect(string userId, VcUserHandler callback)
        {
            Connect(userId, null, null, null, callback);
        }

        /// <summary>
        /// Connect to VyinChat server with user ID and auth token.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="authToken">Auth token (pass null if not needed)</param>
        /// <param name="callback">Callback with user or error</param>
        public static void Connect(string userId, string authToken, VcUserHandler callback)
        {
            Connect(userId, authToken, null, null, callback);
        }

        /// <summary>
        /// Connect to VyinChat server with user ID, auth token, and custom hosts.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="authToken">Auth token (pass null if not needed)</param>
        /// <param name="apiHost">API host URL (e.g., "https://api.gamania.chat"), null for default</param>
        /// <param name="wsHost">WebSocket host URL (e.g., "wss://ws.gamania.chat"), null for default</param>
        /// <param name="callback">Callback with user or error</param>
        public static void Connect(string userId, string authToken, string apiHost, string wsHost, VcUserHandler callback)
        {
            TryExecute(() => vyinChatImpl.Connect(userId, authToken, apiHost, wsHost, callback), "Connect");
        }

        /// <summary>
        /// Set custom configuration for ChatSDK.
        /// This method is deprecated. Use Connect() with explicit apiHost and wsHost parameters instead.
        /// </summary>
        /// <param name="appId">Application ID (optional, pass null to keep current)</param>
        /// <param name="domain">Environment domain</param>
        [Obsolete("SetConfiguration is deprecated. Use Connect(userId, authToken, apiHost, wsHost, callback) with explicit host URLs instead.")]
        public static void SetConfiguration(string appId, string domain)
        {
            Logger.Warning(TAG, "SetConfiguration is deprecated. " +
                "Use Connect(userId, authToken, apiHost, wsHost, callback) with explicit host URLs instead.");
        }

        /// <summary>
        /// Reset configuration to default values.
        /// This method is deprecated. Configuration is now handled via Init() and Connect() parameters.
        /// </summary>
        [Obsolete("ResetConfiguration is deprecated. Configuration is now handled via Init() and Connect() parameters.")]
        public static void ResetConfiguration()
        {
            Logger.Warning(TAG, "ResetConfiguration is deprecated. " +
                "Configuration is now handled via Init() and Connect() parameters.");
        }

        /// <summary>
        /// Invite users to a channel.
        /// This method is not yet implemented in pure C#. Will be available in a future release.
        /// </summary>
        /// <param name="channelUrl">Channel URL</param>
        /// <param name="userIds">Array of user IDs to invite</param>
        /// <param name="callback">Callback with result or error</param>
        [Obsolete("InviteUsers is not yet implemented in pure C#. Will be available in a future release.")]
        public static void InviteUsers(string channelUrl, string[] userIds, Action<string, string> callback)
        {
            Logger.Warning(TAG, "InviteUsers is not yet implemented in pure C#.");
            callback?.Invoke(null, "InviteUsers is not yet implemented. Will be available in a future release.");
        }

        /// <summary>
        /// Send a message to a channel
        /// </summary>
        /// <param name="channelUrl">Channel URL</param>
        /// <param name="message">Message text</param>
        /// <param name="callback">Callback with sent message or error</param>
        public static void SendMessage(string channelUrl, string message, VcUserMessageHandler callback)
        {
            TryExecute(() => vyinChatImpl.SendMessage(channelUrl, message, callback), "SendMessage");
        }

        #region Helper Methods


        public static void TryExecute(Action action, string actionName)
        {
            try
            {
                action.Invoke();
            }
            catch (InvalidOperationException)
            {
                // Re-throw InvalidOperationException (e.g., not initialized)
                // so tests can catch it
                throw;
            }
            catch (Exception e)
            {
                Logger.Error(TAG, $"Error in {actionName}: {e.Message}", e);
            }
        }

        #endregion

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Reset VyinChat state (for testing only)
        /// WARNING: This is only for testing purposes. Do not use in production code.
        /// </summary>
        public static void ResetForTesting()
        {
            _initParams = null;

            // Reset Logger implementation and facade
            UnityLoggerImpl.ResetForTesting();
            Logger.ResetForTesting();
            Logger.SetInstance(UnityLoggerImpl.Instance);

            // Reset the implementation instance state
            vyinChatImpl.Reset();
        }
#endif
    }
}
