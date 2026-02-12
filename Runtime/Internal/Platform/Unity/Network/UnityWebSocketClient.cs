// -----------------------------------------------------------------------------
//
// Unity WebSocket Client - Layer 1
// Pure WebSocket communication layer using NativeWebSocket
//
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NativeWebSocket;
using VyinChatSdk;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Domain.Log;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk.Internal.Platform.Unity.Network
{
    /// <summary>
    /// Unity WebSocket client implementation using NativeWebSocket
    /// Layer 1: Pure communication, no business logic or ACK handling
    /// </summary>
    public class UnityWebSocketClient : IWebSocketClient
    {
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnMessageReceived;
        public event Action<VcException> OnError;
        
        public VcConnectionState State => _state;
        public bool IsConnected => _state.IsConnected();
        public WebSocketConfig Config => _config;

        private WebSocket _webSocket;
        private VcConnectionState _state = VcConnectionState.Closed;
        private WebSocketConfig _config;

        public UnityWebSocketClient()
        {
        }

        /// <summary>
        /// Connect to WebSocket server with configuration
        /// </summary>
        public void Connect(WebSocketConfig config)
        {
            if (config == null)
            {
                OnError?.Invoke(new VcException(VcErrorCode.InvalidParameter, "WebSocketConfig cannot be null"));
                return;
            }

            _config = config;
            TransitionToState(VcConnectionState.Connecting, "Connect() called");

            // Set Unity platform version if not already set
            if (string.IsNullOrEmpty(config.PlatformVersion))
            {
                config.PlatformVersion = UnityEngine.Application.unityVersion;
            }

            // Build WSS URL using config
            string url;
            try
            {
                url = config.BuildWebSocketUrl();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new VcException(VcErrorCode.InvalidParameter, $"Failed to build URL: {ex.Message}", ex));
                return;
            }

            Logger.Info(LogCategory.WebSocket, $"Connecting to {url}");

            try
            {
                _webSocket = new WebSocket(url);

                // Register event handlers
                _webSocket.OnOpen += HandleOnOpen;
                _webSocket.OnClose += HandleOnClose;
                _webSocket.OnMessage += HandleOnMessage;
                _webSocket.OnError += HandleOnError;

                // Register Update callback for message dispatching
                MainThreadDispatcher.RegisterUpdateCallback(Update);

                // Start connection
                _ = _webSocket.Connect();
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.WebSocket, "Connect exception", ex);
                OnError?.Invoke(new VcException(VcErrorCode.WebSocketConnectionFailed, $"Connect failed: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Disconnect from WebSocket server
        /// </summary>
        public void Disconnect()
        {
            if (_webSocket != null)
            {
                Logger.Info(LogCategory.WebSocket, "Disconnecting");

                if (_state == VcConnectionState.Open)
                {
                    TransitionToState(VcConnectionState.Closing, "Disconnect() called");
                }

                // Unregister Update callback
                MainThreadDispatcher.UnregisterUpdateCallback(Update);

                try
                {
                    _ = _webSocket.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error(LogCategory.WebSocket, "Disconnect exception", ex);
                    TransitionToState(VcConnectionState.Closed, "Disconnect exception");
                }
            }
        }

        /// <summary>
        /// Send raw string message
        /// </summary>
        public Task SendAsync(string message)
        {
            if (!_state.IsConnected())
            {
                throw new VcException(VcErrorCode.ConnectionRequired, "Cannot send message: not connected");
            }
            
            if (!SendRaw(message))
            {
                throw new VcException(VcErrorCode.WebSocketConnectionFailed, "Failed to send WebSocket message.");
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Update method to process WebSocket events
        /// Must be called from Unity Update loop
        /// </summary>
        public void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            _webSocket?.DispatchMessageQueue();
            #endif
        }

        // Event handlers

        private void HandleOnOpen()
        {
            Logger.Info(LogCategory.WebSocket, "Connection opened");
            TransitionToState(VcConnectionState.Open, "WebSocket Open");

            MainThreadDispatcher.Enqueue(() =>
            {
                OnConnected?.Invoke();
            });
        }

        private void HandleOnClose(WebSocketCloseCode closeCode)
        {
            Logger.Info(LogCategory.WebSocket, $"Connection closed: {closeCode}");

            var reason = closeCode == WebSocketCloseCode.Normal
                ? "Normal closure"
                : $"Close code: {closeCode}";
            TransitionToState(VcConnectionState.Closed, reason);

            // Unregister Update callback
            MainThreadDispatcher.UnregisterUpdateCallback(Update);

            MainThreadDispatcher.Enqueue(() =>
            {
                // Clear event separation:
                // - Normal closure → OnDisconnected only
                // - Abnormal closure → OnError only (error includes disconnection info)
                if (closeCode == WebSocketCloseCode.Normal || closeCode == WebSocketCloseCode.Away)
                {
                    // Normal closure - trigger OnDisconnected
                    OnDisconnected?.Invoke();
                }
                else
                {
                    // Abnormal closure - trigger OnError only (includes disconnection)
                    var errorCode = closeCode == WebSocketCloseCode.Abnormal
                        ? VcErrorCode.WebSocketConnectionFailed
                        : VcErrorCode.WebSocketConnectionClosed;
                    OnError?.Invoke(new VcException(errorCode, $"WebSocket closed with code: {(ushort)closeCode}"));
                }
            });
        }

        private void HandleOnMessage(byte[] data)
        {
            try
            {
                string message = System.Text.Encoding.UTF8.GetString(data);
                Logger.Verbose(LogCategory.WebSocket, $"Received: {message}");

                // Pass raw message up to Layer 3
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnMessageReceived?.Invoke(message);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.WebSocket, "Message decode exception", ex);
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnError?.Invoke(new VcException(VcErrorCode.MalformedData, $"Failed to decode message: {ex.Message}", ex));
                });
            }
        }

        private void HandleOnError(string errorMessage)
        {
            Logger.Error(LogCategory.WebSocket, $"WebSocket error: {errorMessage}");

            if (_state == VcConnectionState.Connecting)
            {
                TransitionToState(VcConnectionState.Closed, "Connection error");
            }
            else if (_state == VcConnectionState.Open)
            {
                TransitionToState(VcConnectionState.Closed, "Network error");
            }

            MainThreadDispatcher.Enqueue(() =>
            {
                OnError?.Invoke(new VcException(VcErrorCode.WebSocketConnectionFailed, errorMessage));
            });
        }

        private void TransitionToState(VcConnectionState newState, string reason)
        {
            if (_state == newState)
                return;

            var oldState = _state;
            _state = newState;

            Logger.Info(LogCategory.Connection,
                $"State transition: {oldState} -> {newState} (Reason: {reason})");
        }

        private bool SendRaw(string message)
        {
            if (!_state.IsConnected() || _webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                string error = "Cannot send message: WebSocket is not connected";
                Logger.Error(LogCategory.WebSocket, error);
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnError?.Invoke(new VcException(VcErrorCode.ConnectionRequired, error));
                });
                return false;
            }

            try
            {
                _ = _webSocket.SendText(message);
                Logger.Verbose(LogCategory.WebSocket, $"Sent: {message}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.WebSocket, "Send exception", ex);
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnError?.Invoke(new VcException(VcErrorCode.WebSocketConnectionFailed, $"Send failed: {ex.Message}", ex));
                });
                return false;
            }
        }
        
        /// <summary>
        /// Inject test message for testing purposes only
        /// Only accessible from test assemblies via InternalsVisibleTo
        /// </summary>
        internal void InjectTestMessage(string message)
        {
            InjectTestMessage(System.Text.Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Inject test message for testing purposes only (raw byte array)
        /// Only accessible from test assemblies via InternalsVisibleTo
        /// </summary>
        internal void InjectTestMessage(byte[] data)
        {
            HandleOnMessage(data);
        }
    }
}
