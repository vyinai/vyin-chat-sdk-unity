using System;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Data.Repositories;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Domain.Repositories;
using VyinChatSdk.Internal.Platform.Unity.Network;

namespace VyinChatSdk.Internal.Platform.Unity
{
    internal class VyinChatMain
    {
        private static VyinChatMain _instance;
        private IHttpClient _httpClient;
        private IWebSocketClient _webSocketClient;
        private IChannelRepository _channelRepository;
        private string _baseUrl;
        private VcInitParams _initParams;

        // Host configuration constants
        private const string API_HOST_PREFIX = "https://";
        private const string WS_HOST_PREFIX = "wss://";
        private const string HOST_POSTFIX = "gamania.chat";

        public static VyinChatMain Instance
        {
            get
            {
                _instance ??= new VyinChatMain();
                return _instance;
            }
        }

        public bool IsInitialized => _initParams != null;
        public string AppId => _initParams?.AppId;
        public string AppVersion => _initParams?.AppVersion;
        public VcLogLevel LogLevel => _initParams?.LogLevel ?? VcLogLevel.None;
        public bool UseLocalCaching => _initParams?.IsLocalCachingEnabled ?? false;

        public VyinChatMain()
        {
            _httpClient = new UnityHttpClient();
            _webSocketClient = new UnityWebSocketClient();
        }

        public void Init(VcInitParams initParams)
        {
            if (initParams == null)
            {
                throw new ArgumentNullException(nameof(initParams));
            }

            if (string.IsNullOrEmpty(initParams.AppId))
            {
                throw new ArgumentException("AppId cannot be null or empty", nameof(initParams));
            }

            if (IsInitialized && AppId != initParams.AppId)
            {
                throw new ArgumentException(
                    $"AppId must match previous initialization. Previous: {AppId}, New: {initParams.AppId}",
                    nameof(initParams));
            }

            _initParams = initParams;
            Logger.Info($"Initialized with AppId: {initParams.AppId}, LocalCaching: {initParams.IsLocalCachingEnabled}, LogLevel: {initParams.LogLevel}");
        }

        public void Connect(string userId, string authToken, string apiHost, string wsHost, VcUserHandler callback)
        {
            if (_initParams == null)
            {
                var errorMsg = "VyinChatMain instance hasn't been initialized. Try VyinChat.Init().";
                var error = new VcException(VcErrorCode.InvalidInitialization, errorMsg);
                Logger.Error(LogCategory.Connection, errorMsg, error);
                callback?.Invoke(null, error);
                return;
            }

            if (string.IsNullOrEmpty(userId))
            {
                var errorMsg = "userId is empty.";
                var error = new VcException(VcErrorCode.InvalidParameter, errorMsg);
                Logger.Error(LogCategory.Connection, errorMsg, error);
                callback?.Invoke(null, error);
                return;
            }

            ConnectInternal(userId, authToken, apiHost, wsHost, callback);
        }

        private void ConnectInternal(string userId, string authToken, string apiHost, string wsHost, VcUserHandler callback)
        {
            apiHost = string.IsNullOrWhiteSpace(apiHost) ? GetDefaultApiHost(_initParams.AppId) : apiHost;
            wsHost = string.IsNullOrWhiteSpace(wsHost) ? GetDefaultWsHost(_initParams.AppId) : wsHost;

            Logger.Info(LogCategory.Connection, $"Connecting with API host: {apiHost}, WS host: {wsHost}");

            // Initialize HTTP repositories with API host
            _baseUrl = apiHost;
            _channelRepository = new ChannelRepositoryImpl(_httpClient, _baseUrl);
            Logger.Debug(LogCategory.Http, $"HTTP initialized with API host: {_baseUrl}");

            // Create WebSocket configuration
            var wsConfig = new WebSocketConfig
            {
                ApplicationId = _initParams.AppId,
                UserId = userId,
                AccessToken = authToken,
                AppVersion = _initParams.AppVersion,
                CustomWebSocketBaseUrl = wsHost
            };

            // Setup event handlers
            Action<string> onAuthenticatedHandler = null;
            Action<VcException> onErrorHandler = null;

            onAuthenticatedHandler = (sessionKey) =>
            {
                Logger.Info(LogCategory.Connection, "Authentication successful, session key received");

                // Store session key for HTTP requests
                SetSessionKey(sessionKey);

                // Create user object
                var user = new VcUser
                {
                    UserId = userId
                };

                // Cleanup handlers
                _webSocketClient.OnAuthenticated -= onAuthenticatedHandler;
                _webSocketClient.OnError -= onErrorHandler;

                // Invoke success callback
                callback?.Invoke(user, null);
            };

            onErrorHandler = (error) =>
            {
                Logger.Error(LogCategory.WebSocket, "WebSocket error", error);

                // Cleanup handlers
                _webSocketClient.OnAuthenticated -= onAuthenticatedHandler;
                _webSocketClient.OnError -= onErrorHandler;

                // Invoke error callback
                callback?.Invoke(null, error);
            };

            _webSocketClient.OnAuthenticated += onAuthenticatedHandler;
            _webSocketClient.OnError += onErrorHandler;

            // Start WebSocket connection
            Logger.Info(LogCategory.Connection, "Starting WebSocket connection");
            _webSocketClient.Connect(wsConfig);
        }

        private string GetDefaultApiHost(string appId)
        {
            return $"{API_HOST_PREFIX}{appId}.{HOST_POSTFIX}";
        }

        private string GetDefaultWsHost(string appId)
        {
            return $"{WS_HOST_PREFIX}{appId}.{HOST_POSTFIX}";
        }

        /// <summary>
        /// Set session key for authenticated HTTP requests
        /// Called after WebSocket connection establishes session
        /// </summary>
        public void SetSessionKey(string sessionKey)
        {
            if (_httpClient is UnityHttpClient unityHttpClient)
            {
                unityHttpClient.SetSessionKey(sessionKey);
                Logger.Debug(LogCategory.Http, "Session key updated");
            }
        }

        /// <summary>
        /// Get Channel Repository instance
        /// </summary>
        public IChannelRepository GetChannelRepository()
        {
            EnsureInitialized();
            return _channelRepository;
        }

        /// <summary>
        /// Get WebSocket Client instance
        /// </summary>
        public IWebSocketClient GetWebSocketClient()
        {
            EnsureInitialized();
            return _webSocketClient;
        }

        /// <summary>
        /// Check if connected to server (has valid session key)
        /// </summary>
        public bool IsConnected()
        {
            return _webSocketClient != null &&
                   _webSocketClient.IsConnected &&
                   !string.IsNullOrEmpty(_webSocketClient.SessionKey);
        }

        private void EnsureInitialized()
        {
            if (_initParams == null)
            {
                throw new VcException(
                    VcErrorCode.InvalidInitialization,
                    "VyinChat SDK is not initialized. Call VyinChat.Init() first.");
            }
        }

        /// <summary>
        /// Reset instance state (for testing)
        /// </summary>
        public void Reset()
        {
            // Disconnect WebSocket if connected
            if (_webSocketClient != null && _webSocketClient.IsConnected)
            {
                _webSocketClient.Disconnect();
            }

            _initParams = null;
            _httpClient = new UnityHttpClient();
            _webSocketClient = new UnityWebSocketClient();
            _channelRepository = null;
            _baseUrl = null;
        }
    }
}
