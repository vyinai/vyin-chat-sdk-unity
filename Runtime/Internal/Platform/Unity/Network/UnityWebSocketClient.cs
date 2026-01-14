// -----------------------------------------------------------------------------
//
// Unity WebSocket Client - Integrated ACK Management
// Concrete implementation using NativeWebSocket library
// Supports all Unity platforms including WebGL, iOS, Android
//
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NativeWebSocket;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using VyinChatSdk;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Domain.Commands;
using VyinChatSdk.Internal.Domain.Log;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk.Internal.Platform.Unity.Network
{
    /// <summary>
    /// Unity WebSocket client implementation using NativeWebSocket
    /// Supports WebGL, iOS, Android, and all Unity platforms
    /// Includes integrated ACK management
    /// </summary>
    public class UnityWebSocketClient : IWebSocketClient
    {
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<CommandType, string> OnCommandReceived;
        public event Action<string> OnAuthenticated;
        public event Action<string> OnError;

        public bool IsConnected => _webSocket != null && _webSocket.State == WebSocketState.Open;
        public string SessionKey => _sessionKey;

        private WebSocket _webSocket;
        private string _sessionKey;
        private CancellationTokenSource _authTimeoutCts;
        private readonly TimeSpan _authTimeout = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _defaultAckTimeout = TimeSpan.FromSeconds(5);

        // ACK management
        private readonly Dictionary<string, PendingAck> _pendingAcks = new Dictionary<string, PendingAck>();
        private readonly object _ackLock = new object();
        private readonly ICommandProtocol _commandProtocol = new CommandProtocol();

        /// <summary>
        /// Connect to WebSocket server with configuration
        /// </summary>
        public void Connect(WebSocketConfig config)
        {
            if (config == null)
            {
                OnError?.Invoke("WebSocketConfig cannot be null");
                return;
            }

            _sessionKey = null;

            // Set Unity platform version if not already set
            if (string.IsNullOrEmpty(config.PlatformVersion))
            {
                config.PlatformVersion = UnityEngine.Application.unityVersion;
            }

            StartAuthTimeout();

            // Build WSS URL using config
            string url;
            try
            {
                url = config.BuildWebSocketUrl();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to build URL: {ex.Message}");
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
                Logger.Error(LogCategory.WebSocket, $"Connect exception: {ex.Message}", ex);
                OnError?.Invoke($"Connect failed: {ex.Message}");
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
                CancelAuthTimeout();
                ClearAllPendingAcks();

                // Unregister Update callback
                MainThreadDispatcher.UnregisterUpdateCallback(Update);

                try
                {
                    _ = _webSocket.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error(LogCategory.WebSocket, $"Disconnect exception: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Send a command through WebSocket with ACK handling
        /// </summary>
        public async Task<string> SendCommandAsync(
            CommandType commandType,
            object payload,
            TimeSpan? ackTimeout = null,
            CancellationToken cancellationToken = default)
        {
            var (reqId, serialized) = _commandProtocol.BuildCommand(commandType, payload);

            // If command doesn't require ACK, send immediately and return
            if (!commandType.IsAckRequired())
            {
                SendRaw(serialized);
                return null;
            }

            // Create task completion source for ACK
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeout = ackTimeout ?? _defaultAckTimeout;
            timeoutCts.CancelAfter(timeout);

            // Register pending ACK
            RegisterPendingAck(reqId, tcs, timeoutCts);

            try
            {
                SendRaw(serialized);
            }
            catch
            {
                // If send fails immediately, clean up pending ACK
                CompletePendingAck(reqId, null, cancelTimeout: true);
                throw;
            }

            // Register timeout callback
            timeoutCts.Token.Register(() =>
            {
                CompletePendingAck(reqId, null, cancelTimeout: false);
            });

            return await tcs.Task;
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
            MainThreadDispatcher.Enqueue(() =>
            {
                OnConnected?.Invoke();
            });
        }

        private void HandleOnClose(WebSocketCloseCode closeCode)
        {
            Logger.Info(LogCategory.WebSocket, $"Connection closed: {closeCode}");
            CancelAuthTimeout();
            ClearAllPendingAcks();

            // Unregister Update callback
            MainThreadDispatcher.UnregisterUpdateCallback(Update);

            MainThreadDispatcher.Enqueue(() =>
            {
                OnDisconnected?.Invoke();
            });
        }

        private void HandleOnMessage(byte[] data)
        {
            try
            {
                string message = System.Text.Encoding.UTF8.GetString(data);
                Logger.Verbose(LogCategory.WebSocket, $"Received: {message}");

                var commandType = CommandParser.ExtractCommandType(message);
                if (commandType == null)
                {
                    Logger.Warning(LogCategory.WebSocket, $"Failed to parse command type from: {message}");
                    return;
                }

                var payload = CommandParser.ExtractPayload(message);

                if (commandType == CommandType.LOGI)
                {
                    HandleLogiCommand(message, payload);
                }
                else if (commandType == CommandType.MESG)
                {
                    HandleMesgCommand(payload);
                }
                else if (commandType == CommandType.MEDI)
                {
                    HandleMediCommand(payload);
                }
                else if (commandType == CommandType.EROR)
                {
                    HandleErorCommand(payload);
                }
                else
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        OnCommandReceived?.Invoke(commandType.Value, payload);
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.WebSocket, $"Message decode exception: {ex.Message}", ex);
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnError?.Invoke($"Failed to decode message: {ex.Message}");
                });
            }
        }

        private void HandleOnError(string errorMessage)
        {
            Logger.Error(LogCategory.WebSocket, $"WebSocket error: {errorMessage}");
            CancelAuthTimeout();
            MainThreadDispatcher.Enqueue(() =>
            {
                OnError?.Invoke(errorMessage);
            });
        }

        // ACK Management

        private void RegisterPendingAck(string reqId, TaskCompletionSource<string> tcs, CancellationTokenSource timeoutCts)
        {
            lock (_ackLock)
            {
                if (_pendingAcks.ContainsKey(reqId))
                {
                    throw new InvalidOperationException($"Duplicate reqId registration: {reqId}");
                }
                _pendingAcks.Add(reqId, new PendingAck(tcs, timeoutCts));
            }
        }

        private bool CompletePendingAck(string reqId, string ackPayload, bool cancelTimeout)
        {
            PendingAck ack;
            lock (_ackLock)
            {
                if (!_pendingAcks.TryGetValue(reqId, out ack))
                {
                    return false;
                }
                _pendingAcks.Remove(reqId);
            }

            if (cancelTimeout)
            {
                ack.TimeoutCts.Cancel();
            }

            ack.Tcs.TrySetResult(ackPayload);
            ack.Dispose();
            return true;
        }

        private void ClearAllPendingAcks()
        {
            lock (_ackLock)
            {
                foreach (var ack in _pendingAcks.Values)
                {
                    ack.TimeoutCts.Cancel();
                    ack.Tcs.TrySetCanceled();
                    ack.Dispose();
                }
                _pendingAcks.Clear();
            }
        }

        private void HandleMesgCommand(string payload)
        {
            var reqId = ExtractReqId(payload);

            // Check if this is an ACK response (has req_id)
            if (!string.IsNullOrWhiteSpace(reqId))
            {
                // This is an ACK for a message we sent
                bool completed = CompletePendingAck(reqId, payload, cancelTimeout: true);
                if (!completed)
                {
                    Logger.Warning(LogCategory.Command, $"MESG ACK received for unknown reqId: {reqId}");
                }
                // Do NOT return - continue to trigger handler
                // Server sends MESG with req_id as both ACK and broadcast
            }

            // Trigger handler for all MESG (including our own messages echoed back)
            Logger.Debug(LogCategory.Command, $"MESG message received, triggering handler: {payload}");
            TriggerMessageReceived(payload);
        }

        private void HandleMediCommand(string payload)
        {
            // MEDI (Message Edit) commands are for streaming messages (e.g., AI responses)
            // They include a "done" flag to indicate completion
            Logger.Debug(LogCategory.Command, $"MEDI message updated: {payload}");
            TriggerMessageUpdated(payload);
        }

        private void HandleErorCommand(string payload)
        {
            var reqId = ExtractReqId(payload);
            if (!string.IsNullOrWhiteSpace(reqId))
            {
                // EROR with req_id means command failed, complete pending ACK with null
                bool completed = CompletePendingAck(reqId, null, cancelTimeout: true);
                if (completed)
                {
                    Logger.Warning(LogCategory.Command, $"EROR received for reqId: {reqId}, payload: {payload}");
                    return;
                }
            }

            // EROR without req_id or unknown req_id - treat as authentication error
            CancelAuthTimeout();
            MainThreadDispatcher.Enqueue(() =>
            {
                OnError?.Invoke("Authentication failed (EROR message).");
                OnCommandReceived?.Invoke(CommandType.EROR, payload);
            });
        }

        private void HandleLogiCommand(string message, string payload)
        {
            var logi = CommandParser.ParseLogiCommand(message);
            if (logi != null)
            {
                Logger.Debug(LogCategory.Command, $"LOGI parsed - SessionKey: {logi.SessionKey}, Error: {logi.Error}");

                if (logi.IsSuccess())
                {
                    _sessionKey = logi.SessionKey;
                    Logger.Info(LogCategory.Connection, $"Authentication successful with session key: {_sessionKey}");
                    CancelAuthTimeout();
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        OnAuthenticated?.Invoke(_sessionKey);
                    });
                }
                else
                {
                    Logger.Error(LogCategory.Connection, "LOGI authentication failed");
                    CancelAuthTimeout();
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        OnError?.Invoke("Authentication failed (LOGI error).");
                    });
                }
            }
            else
            {
                Logger.Error(LogCategory.Command, $"Failed to parse LOGI command from message: {message}");
            }
        }

        private static string ExtractReqId(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            const string key = "\"req_id\":\"";
            var start = payload.IndexOf(key, StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }

            start += key.Length;
            var end = payload.IndexOf('"', start);
            if (end < 0 || end <= start)
            {
                return null;
            }

            return payload.Substring(start, end - start);
        }

        private void SendRaw(string message)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                string error = "Cannot send message: WebSocket is not connected";
                Logger.Error(LogCategory.WebSocket, error);
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnError?.Invoke(error);
                });
                throw new InvalidOperationException(error);
            }

            try
            {
                _ = _webSocket.SendText(message);
                Logger.Verbose(LogCategory.WebSocket, $"Sent: {message}");
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.WebSocket, $"Send exception: {ex.Message}", ex);
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnError?.Invoke($"Send failed: {ex.Message}");
                });
                throw;
            }
        }

        private void StartAuthTimeout()
        {
            CancelAuthTimeout();
            _authTimeoutCts = new CancellationTokenSource();
            var token = _authTimeoutCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_authTimeout, token);
                    if (token.IsCancellationRequested || !string.IsNullOrEmpty(_sessionKey))
                    {
                        return;
                    }
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        OnError?.Invoke("Authentication timeout (LOGI not received).");
                    });
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
            }, token);
        }

        private void CancelAuthTimeout()
        {
            if (_authTimeoutCts != null)
            {
                _authTimeoutCts.Cancel();
                _authTimeoutCts.Dispose();
                _authTimeoutCts = null;
            }
        }

        private void TriggerMessageReceived(string payload)
        {
            try
            {
                var message = ParseMessageFromPayload(payload);
                if (message == null)
                {
                    Logger.Warning(LogCategory.WebSocket, $"Failed to parse message from: {payload}");
                    return;
                }

                // Get or create channel
                var channel = new VcGroupChannel
                {
                    ChannelUrl = message.ChannelUrl
                };

                // Trigger on main thread
                MainThreadDispatcher.Enqueue(() =>
                {
                    VcGroupChannel.TriggerMessageReceived(channel, message);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.WebSocket, $"Error triggering message received: {ex.Message}", ex);
            }
        }

        private void TriggerMessageUpdated(string payload)
        {
            try
            {
                var message = ParseMessageFromPayload(payload);
                if (message == null)
                {
                    Logger.Warning(LogCategory.WebSocket, $"Failed to parse message from: {payload}");
                    return;
                }

                // Get or create channel
                var channel = new VcGroupChannel
                {
                    ChannelUrl = message.ChannelUrl
                };

                // Trigger on main thread
                MainThreadDispatcher.Enqueue(() =>
                {
                    VcGroupChannel.TriggerMessageUpdated(channel, message);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.WebSocket, $"Error triggering message updated: {ex.Message}", ex);
            }
        }

        private VcBaseMessage ParseMessageFromPayload(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            // Simple JSON parsing for message fields
            var message = new VcBaseMessage();

            // Extract message_id (server may return as "message_id" or "msg_id")
            var messageIdStr = ExtractJsonValue(payload, "message_id");
            if (string.IsNullOrEmpty(messageIdStr))
            {
                messageIdStr = ExtractJsonValue(payload, "msg_id");
            }
            if (long.TryParse(messageIdStr, out var messageId))
            {
                message.MessageId = messageId;
            }

            // Extract message
            message.Message = ExtractJsonStringValue(payload, "message");

            // Extract channel_url
            message.ChannelUrl = ExtractJsonStringValue(payload, "channel_url");

            // Extract sender_id (user_id)
            message.SenderId = ExtractJsonStringValue(payload, "user_id");

            // Extract sender_nickname
            message.SenderNickname = ExtractJsonStringValue(payload, "nickname");

            // Extract created_at
            var createdAtStr = ExtractJsonValue(payload, "created_at");
            if (long.TryParse(createdAtStr, out var createdAt))
            {
                message.CreatedAt = createdAt;
            }

            // Extract done flag
            var doneStr = ExtractJsonValue(payload, "done");
            if (bool.TryParse(doneStr, out var done))
            {
                message.Done = done;
            }

            // Extract custom_type
            message.CustomType = ExtractJsonStringValue(payload, "custom_type");

            // Extract data
            message.Data = ExtractJsonStringValue(payload, "data");

            return message;
        }

        private static string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var keyPattern = $"\"{key}\":";
            var start = json.IndexOf(keyPattern, StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }

            start += keyPattern.Length;

            // Skip whitespace
            while (start < json.Length && char.IsWhiteSpace(json[start]))
            {
                start++;
            }

            if (start >= json.Length)
            {
                return null;
            }

            // Check if value is a string (starts with ")
            if (json[start] == '"')
            {
                start++; // Skip opening quote

                // Find the closing quote, handling escaped quotes (\")
                var end = start;
                while (end < json.Length)
                {
                    if (json[end] == '"')
                    {
                        // Check if this quote is escaped
                        var backslashCount = 0;
                        var checkPos = end - 1;
                        while (checkPos >= start && json[checkPos] == '\\')
                        {
                            backslashCount++;
                            checkPos--;
                        }

                        // If even number of backslashes (including 0), the quote is not escaped
                        if (backslashCount % 2 == 0)
                        {
                            var raw = json.Substring(start, end - start);
                            try
                            {
                                // Unescape JSON string (e.g. \" -> ", \\ -> \, \n -> newline)
                                // Wrap with quotes so Json.NET can parse escape sequences correctly.
                                return JsonConvert.DeserializeObject<string>($"\"{raw}\"");
                            }
                            catch
                            {
                                // Fall back to raw value if unescape fails
                                return raw;
                            }
                        }
                    }
                    end++;
                }

                return null; // No closing quote found
            }

            // Value is not a string (number, boolean, null)
            var endChars = new[] { ',', '}', ']', '\r', '\n' };
            var end2 = json.IndexOfAny(endChars, start);
            if (end2 < 0)
            {
                end2 = json.Length;
            }

            return json.Substring(start, end2 - start).Trim();
        }

        private static string ExtractJsonStringValue(string json, string key)
        {
            return ExtractJsonValue(json, key);
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

        private sealed class PendingAck : IDisposable
        {
            public TaskCompletionSource<string> Tcs { get; }
            public CancellationTokenSource TimeoutCts { get; }

            public PendingAck(TaskCompletionSource<string> tcs, CancellationTokenSource timeoutCts)
            {
                Tcs = tcs;
                TimeoutCts = timeoutCts;
            }

            public void Dispose()
            {
                TimeoutCts.Dispose();
            }
        }
    }
}
