using System;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Data.Repositories;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Domain.Repositories;
using VyinChatSdk.Internal.Platform.Unity.Network;

namespace VyinChatSdk.Internal.Platform.Unity
{
    internal class VyinChatMain : IVyinChat
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

            _initParams = initParams;
            Logger.Info($"Initialized with AppId: {initParams.AppId}, LocalCaching: {initParams.IsLocalCachingEnabled}, LogLevel: {initParams.LogLevel}");
        }

        public void Connect(string userId, string authToken, string apiHost, string wsHost, VcUserHandler callback)
        {
            if (_initParams == null)
            {
                var errorMsg = "VyinChatMain instance hasn't been initialized. Try VyinChat.Init().";
                Logger.Error(LogCategory.Connection, errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            if (string.IsNullOrEmpty(userId))
            {
                var errorMsg = "userId is empty.";
                Logger.Error(LogCategory.Connection, errorMsg);
                callback?.Invoke(null, errorMsg);
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
            Action<string> onErrorHandler = null;

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
                Logger.Error(LogCategory.WebSocket, $"WebSocket error: {error}");

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
        /// Get HTTP Client instance
        /// </summary>
        public IHttpClient GetHttpClient()
        {
            return _httpClient;
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
        /// Send a message to a channel
        /// </summary>
        /// <param name="channelUrl">Channel URL</param>
        /// <param name="message">Message text</param>
        /// <param name="callback">Callback with sent message or error</param>
        public void SendMessage(string channelUrl, string message, VcUserMessageHandler callback)
        {
            Logger.Debug(LogCategory.Message, $"SendMessage called for channel: {channelUrl}");

            // Check if connected (has session key)
            if (_webSocketClient == null || !_webSocketClient.IsConnected || string.IsNullOrEmpty(_webSocketClient.SessionKey))
            {
                var errorMsg = "Cannot send message: Not connected (no session key).";
                Logger.Error(LogCategory.Message, errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Validate channelUrl
            if (string.IsNullOrEmpty(channelUrl))
            {
                var errorMsg = "channelUrl is empty.";
                Logger.Error(LogCategory.Message, errorMsg);
                callback?.Invoke(null, errorMsg);
                return;
            }

            // Validate message
            if (string.IsNullOrEmpty(message))
            {
                var errorMsg = "message is empty.";
                Logger.Error(LogCategory.Message, errorMsg);
                callback?.Invoke(null, errorMsg);
                return;
            }

            SendMessageInternal(channelUrl, message, callback);
        }

        private async void SendMessageInternal(string channelUrl, string message, VcUserMessageHandler callback)
        {
            try
            {
                // Build MESG command payload
                var payload = new
                {
                    channel_url = channelUrl,
                    message = message,
                    message_type = "MESG",
                    data = "",
                    custom_type = ""
                };

                Logger.Debug(LogCategory.Command, $"Sending MESG command for channel: {channelUrl}");

                // Send command and wait for ACK (15 second timeout)
                var ackTimeout = TimeSpan.FromSeconds(15);
                string ackPayload = await _webSocketClient.SendCommandAsync(
                    Domain.Commands.CommandType.MESG,
                    payload,
                    ackTimeout
                );

                // Check for null/empty ackPayload (timeout or error)
                if (string.IsNullOrEmpty(ackPayload))
                {
                    Logger.Error(LogCategory.Command, "SendMessage timeout or empty ACK");
                    callback?.Invoke(null, "Message send timeout after 15 seconds");
                    return;
                }

                Logger.Debug(LogCategory.Command, $"MESG ACK received: {ackPayload}");

                // Parse ACK response to get message details
                var sentMessage = ParseMessageFromAck(ackPayload, channelUrl, message);

                // Invoke success callback
                callback?.Invoke(sentMessage, null);
            }
            catch (TaskCanceledException)
            {
                Logger.Error(LogCategory.Message, "SendMessage timeout after 15 seconds");
                callback?.Invoke(null, "Message send timeout after 15 seconds");
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.Message, $"SendMessage error: {ex.Message}", ex);
                callback?.Invoke(null, ex.Message);
            }
        }

        private VcBaseMessage ParseMessageFromAck(string ackPayload, string channelUrl, string messageText)
        {
            try
            {
                // Parse the actual ACK response to get the server-assigned message ID and timestamp
                // The ACK payload is the JSON part of the MESG command
                
                var message = new VcBaseMessage
                {
                    ChannelUrl = channelUrl,
                    Message = messageText
                };

                // Try to extract message ID (Server may use "msg_id" or "message_id")
                string messageIdRaw = ExtractValue(ackPayload, "msg_id");
                if (string.IsNullOrEmpty(messageIdRaw))
                {
                    messageIdRaw = ExtractValue(ackPayload, "message_id");
                }

                if (long.TryParse(messageIdRaw, out long messageId))
                {
                    message.MessageId = messageId;
                }

                // Extract timestamp
                string timestampRaw = ExtractValue(ackPayload, "ts");
                if (long.TryParse(timestampRaw, out long timestamp))
                {
                    message.CreatedAt = timestamp;
                }
                else
                {
                    message.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                // Extract sender info from "user" object if needed
                // For now, these are the essentials to prevent UI replacement
                return message;
            }
            catch (Exception ex)
            {
                Logger.Warning(LogCategory.Message, $"Failed to parse ACK payload: {ex.Message}. Falling back to basic message.");
                return new VcBaseMessage
                {
                    ChannelUrl = channelUrl,
                    Message = messageText,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }
        }

        private string ExtractValue(string json, string key)
        {
            var pattern = $"\"{key}\":";
            var start = json.IndexOf(pattern, StringComparison.Ordinal);
            if (start < 0) return null;
            start += pattern.Length;
            
            // Basic extraction for numbers or strings
            var end = json.IndexOfAny(new[] { ',', '}', ']' }, start);
            if (end < 0) end = json.Length;
            var val = json.Substring(start, end - start).Trim().Trim('"');
            return val;
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
