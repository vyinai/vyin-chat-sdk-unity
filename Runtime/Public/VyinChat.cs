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

        /// <summary>
        /// Get current WebSocket connection state.
        /// </summary>
        public static VcConnectionState GetConnectionState() => _impl.GetConnectionState();

        /// <summary>
        /// Get current connected user.
        /// </summary>
        public static VcUser CurrentUser => _impl.GetCurrentUser();

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

        #region Background Disconnection Configuration

        /// <summary>
        /// Sets background disconnection configuration. Call before Connect().
        /// Controls how the SDK behaves when the app enters background.
        /// </summary>
        public static void SetBackgroundDisconnectionConfig(VcBackgroundDisconnectionConfig config)
        {
            _impl.SetBackgroundDisconnectionConfig(config ?? VcBackgroundDisconnectionConfig.Default);
        }

        /// <summary>
        /// Enable/disable automatic reconnection on network change.
        /// </summary>
        public static void SetNetworkAwarenessReconnection(bool isOn)
        {
            _impl.SetNetworkAwarenessReconnection(isOn);
        }

        /// <summary>
        /// Enable/disable application lifecycle tracking.
        /// </summary>
        public static void SetTrackingApplicationState(bool isOn)
        {
            _impl.SetTrackingApplicationState(isOn);
        }

        /// <summary>
        /// Get current background disconnection configuration.
        /// </summary>
        public static VcBackgroundDisconnectionConfig GetBackgroundDisconnectionConfig()
        {
            return _impl.GetBackgroundDisconnectionConfig();
        }

        #endregion

        /// <summary>
        /// Sets the session handler for token refresh callbacks. Pass null to clear.
        /// </summary>
        /// <param name="handler">Session handler implementation.</param>
        public static void SetSessionHandler(IVcSessionHandler handler)
        {
            _impl.SetSessionHandler(handler);
        }

        /// <summary>
        /// Enable or disable automatic message resending on reconnection.
        /// When enabled, failed messages due to connection issues will be automatically
        /// resent when the connection is restored.
        /// Default: false (opt-in feature)
        /// </summary>
        /// <param name="enable">True to enable auto-resend, false to disable.</param>
        public static void SetEnableMessageAutoResend(bool enable)
        {
            _impl.SetEnableMessageAutoResend(enable);
        }

        #region Connection Handler

        /// <summary>
        /// Registers a connection handler to receive connection state events.
        /// </summary>
        /// <param name="id">Unique identifier for the handler.</param>
        /// <param name="handler">Handler instance with event callbacks.</param>
        public static void AddConnectionHandler(string id, VcConnectionHandler handler)
        {
            _impl.AddConnectionHandler(id, handler);
        }

        /// <summary>
        /// Removes a previously registered connection handler.
        /// </summary>
        /// <param name="id">Identifier of the handler to remove.</param>
        public static void RemoveConnectionHandler(string id)
        {
            _impl.RemoveConnectionHandler(id);
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
 
