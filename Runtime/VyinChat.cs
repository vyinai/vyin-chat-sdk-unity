// -----------------------------------------------------------------------------
//
// Runtime SDK main entry
//
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using VyinChatSdk.Internal;
using VyinChatSdk.Internal.Platform.Unity;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk
{
    public static class VyinChat
    {
        private const string TAG = "VyinChat";
        private static readonly IVyinChat vyinChatImpl;

        static VyinChat()
        {
            Logger.SetInstance(UnityLoggerImpl.Instance);

#if UNITY_EDITOR
            // Unity Editor uses Pure C# implementation to connect to real server
            vyinChatImpl = VyinChatMain.Instance;
#else
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    vyinChatImpl = new VyinChatAndroid();
                    break;
                case RuntimePlatform.IPhonePlayer:
                    vyinChatImpl = new VyinChatIOS();
                    break;
                default:
                    vyinChatImpl = VyinChatMain.Instance;
                    break;
            }
#endif
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

        public static void Connect(string userId, string authToken, VcUserHandler callback)
        {
            Connect(userId, authToken, null, null, callback);
        }

        /// <summary>
        /// Connect with explicit API and WebSocket hosts
        /// Pure C# implementation: Uses provided hosts for HTTP and WebSocket connections
        /// Legacy implementations: Use SetConfiguration() before calling Connect with null hosts
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="authToken">Optional auth token</param>
        /// <param name="apiHost">API host URL (e.g., "https://api.gamania.chat"), null for default</param>
        /// <param name="wsHost">WebSocket host URL (e.g., "wss://ws.gamania.chat"), null for default</param>
        /// <param name="callback">Callback with user or error</param>
        public static void Connect(string userId, string authToken, string apiHost, string wsHost, VcUserHandler callback)
        {
            TryExecute(() => vyinChatImpl.Connect(userId, authToken, apiHost, wsHost, callback), "Connect");
        }

        /// <summary>
        /// Set custom configuration for ChatSDK
        /// Call this BEFORE Init() to override default settings
        ///
        /// TODO: This method will be removed in the future.
        /// Only iOS platform supports this. Editor and other platforms will show warnings.
        /// </summary>
        /// <param name="appId">Application ID (optional, pass null to keep current)</param>
        /// <param name="domain">Environment domain (e.g., "dev.gim.beango.com", "stg.gim.beango.com", "gamania.chat")</param>
        public static void SetConfiguration(string appId, string domain)
        {
#if UNITY_EDITOR
            // Unity Editor uses Pure C# implementation which doesn't support SetConfiguration yet
            Logger.Warning(TAG, "Editor mode (Pure C#) does not support SetConfiguration. " +
                "Please use Init() with appropriate parameters or set configuration before static constructor.");
#else
            if (Application.platform == RuntimePlatform.Android)
            {
                // TODO: Android implementation not yet available
                Logger.Warning(TAG, "SetConfiguration not implemented on Android yet");
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                 Internal.ChatSDKWrapper.SetConfiguration(appId, domain);
            }
            else
            {
                // Other platforms use Pure C# implementation
                Logger.Warning(TAG, "Pure C# implementation does not support SetConfiguration. " +
                    "Please use Init() with appropriate parameters.");
            }
#endif
        }

        /// <summary>
        /// Reset configuration to default values (PROD environment)
        /// iOS only for now
        /// </summary>
        public static void ResetConfiguration()
        {
#if UNITY_EDITOR
            Logger.Debug(TAG, "Simulate ResetConfiguration in Editor");
#else
            if (Application.platform == RuntimePlatform.Android)
            {
                // TODO: Android implementation not yet available
                Logger.Warning(TAG, "ResetConfiguration not implemented on Android yet");
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                try
                {
                    Internal.ChatSDKWrapper.ResetConfiguration();
                }
                catch (System.Exception e)
                {
                    Logger.Error(TAG, "Error calling iOS ResetConfiguration: " + e, e);
                }
            }
#endif
        }

        // TODO: replace with platform implementation
        public static void InviteUsers(string channelUrl, string[] userIds, Action<string, string> callback)
        {
            TryExecute(() =>
            {
#if UNITY_EDITOR
                SimulateEditorCall(() =>
                {
                    string fakeResult = $"{{\"success\":true,\"channelUrl\":\"{channelUrl}\"}}";
                    callback?.Invoke(fakeResult, null);
                });
#else
                if (Application.platform == RuntimePlatform.Android)
                {
                    // TODO: Android implementation not yet available
                    // Please implement SendMessage in Android UnityBridge
                    Logger.Warning(TAG, "SendMessage not implemented on Android yet");
                    callback?.Invoke(null, "Not implemented on Android");
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    try
                    {
                        Internal.ChatSDKWrapper.InviteUsers(channelUrl, userIds, callback);
                    }
                    catch (System.Exception e)
                    {
                        Logger.Error(TAG, "Error calling iOS InviteUsers: " + e, e);
                        callback?.Invoke(null, e.Message);
                    }
                }
#endif
            }, "InviteUsers");
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

#if UNITY_EDITOR
        private static void SimulateEditorCall(Action editorAction)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    editorAction.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error(TAG, "[Editor Simulate] " + e.Message, e);
                }
            };
        }
#endif

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
            if (vyinChatImpl is VyinChatMain vyinChatMain)
            {
                vyinChatMain.Reset();
            }
        }
#endif
    }
}
