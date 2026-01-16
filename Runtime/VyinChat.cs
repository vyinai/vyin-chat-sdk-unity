// -----------------------------------------------------------------------------
// VyinChat SDK - Public API Entry Point
// -----------------------------------------------------------------------------

using System;
using VyinChatSdk.Internal.Platform.Unity;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk
{
    /// <summary>
    /// Main entry point for the VyinChat SDK.
    /// Provides static methods for initialization, connection, and messaging.
    /// </summary>
    public static class VyinChat
    {
        private static readonly VyinChatMain _impl;

        static VyinChat()
        {
            Logger.SetInstance(UnityLoggerImpl.Instance);
            _impl = VyinChatMain.Instance;
        }

        #region Properties

        public static bool IsInitialized => _impl.IsInitialized;
        public static bool UseLocalCaching => _impl.UseLocalCaching;
        public static string GetApplicationId() => _impl.AppId;
        public static VcLogLevel GetLogLevel() => _impl.LogLevel;
        public static string GetAppVersion() => _impl.AppVersion;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the VyinChat SDK with the specified parameters.
        /// This method must be called before any other SDK operations.
        /// </summary>
        /// <param name="initParams">Initialization parameters including AppId and optional settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when initParams is null.</exception>
        /// <exception cref="ArgumentException">Thrown when AppId is empty or mismatched.</exception>
        /// <example>
        /// <code>
        /// var initParams = new VcInitParams("your-app-id", logLevel: VcLogLevel.Debug);
        /// VyinChat.Init(initParams);
        /// </code>
        /// </example>
        public static void Init(VcInitParams initParams)
        {
            _impl.Init(initParams);

            var logLevel = Internal.Domain.Log.LogLevelMapper.FromVcLogLevel(initParams.LogLevel);
            Logger.SetLogLevel(logLevel);
        }

        #endregion

        #region Connection

        /// <summary>
        /// Connects to the VyinChat server with user ID only.
        /// Uses default hosts derived from the AppId.
        /// </summary>
        /// <param name="userId">The user ID to connect with.</param>
        /// <param name="callback">Callback invoked with the connected user or error message.</param>
        public static void Connect(string userId, VcUserHandler callback)
        {
            Connect(userId, null, null, null, callback);
        }

        /// <summary>
        /// Connects to the VyinChat server with user ID and auth token.
        /// Uses default hosts derived from the AppId.
        /// </summary>
        /// <param name="userId">The user ID to connect with.</param>
        /// <param name="authToken">Optional authentication token (pass null if not required).</param>
        /// <param name="callback">Callback invoked with the connected user or error message.</param>
        public static void Connect(string userId, string authToken, VcUserHandler callback)
        {
            Connect(userId, authToken, null, null, callback);
        }

        /// <summary>
        /// Connects to the VyinChat server with full configuration.
        /// </summary>
        /// <param name="userId">The user ID to connect with.</param>
        /// <param name="authToken">Optional authentication token (pass null if not required).</param>
        /// <param name="apiHost">Custom API host URL (e.g., "https://api.example.com"), or null for default.</param>
        /// <param name="wsHost">Custom WebSocket host URL (e.g., "wss://ws.example.com"), or null for default.</param>
        /// <param name="callback">Callback invoked with the connected user or error message.</param>
        /// <exception cref="InvalidOperationException">Thrown when SDK is not initialized.</exception>
        public static void Connect(string userId, string authToken, string apiHost, string wsHost, VcUserHandler callback)
        {
            _impl.Connect(userId, authToken, apiHost, wsHost, callback);
        }

        #endregion

        #region Testing Support

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Resets all SDK state for testing purposes.
        /// WARNING: Do not use in production code.
        /// </summary>
        public static void ResetForTesting()
        {
            _impl.Reset();

            UnityLoggerImpl.ResetForTesting();
            Logger.ResetForTesting();
            Logger.SetInstance(UnityLoggerImpl.Instance);
        }
#endif

        #endregion
    }
}
