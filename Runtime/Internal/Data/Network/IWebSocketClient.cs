using System;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Commands;

namespace VyinChatSdk.Internal.Data.Network
{
    /// <summary>
    /// WebSocket client interface for Data layer
    /// Platform-agnostic interface following Clean Architecture principles
    /// Includes ACK management similar to iOS SDK's GIMSocketManager
    /// </summary>
    public interface IWebSocketClient
    {
        /// <summary>
        /// Event triggered when WebSocket connection is established
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// Event triggered when WebSocket connection is closed
        /// </summary>
        event Action OnDisconnected;

        /// <summary>
        /// Event triggered when a non-ACK command is received (MESG, FILE, SYEV, etc.)
        /// Parameters: (commandType, payload)
        /// </summary>
        event Action<CommandType, string> OnCommandReceived;

        /// <summary>
        /// Event triggered when an error occurs
        /// </summary>
        event Action<VcException> OnError;

        /// <summary>
        /// Event triggered when authentication is successful (LOGI received)
        /// Parameter: session key
        /// </summary>
        event Action<string> OnAuthenticated;

        /// <summary>
        /// Current connection state
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Session key received from LOGI command
        /// </summary>
        string SessionKey { get; }

        /// <summary>
        /// Connect to WebSocket server with configuration
        /// </summary>
        /// <param name="config">WebSocket connection configuration</param>
        void Connect(WebSocketConfig config);

        /// <summary>
        /// Disconnect from WebSocket server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send a command through WebSocket with ACK handling
        /// If the command requires ACK, waits for MESG ACK or timeout
        /// Returns the ACK payload if successful, null if timeout or command doesn't require ACK
        /// </summary>
        /// <param name="commandType">Type of command to send</param>
        /// <param name="payload">Command payload object</param>
        /// <param name="ackTimeout">Custom timeout duration (optional)</param>
        /// <param name="cancellationToken">Cancellation token (optional)</param>
        /// <returns>ACK payload if successful, null otherwise</returns>
        Task<string> SendCommandAsync(
            CommandType commandType,
            object payload,
            TimeSpan? ackTimeout = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update method to process WebSocket events (call from Unity Update loop)
        /// </summary>
        void Update();
    }
}
