using System;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Commands;

namespace VyinChatSdk.Internal.Data.Network
{
    /// <summary>
    /// WebSocket client interface for Data layer (Layer 1)
    /// Pure WebSocket communication, no business logic or ACK handling.
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
        /// Event triggered when a raw text message is received
        /// </summary>
        event Action<string> OnMessageReceived;

        /// <summary>
        /// Event triggered when an error occurs
        /// </summary>
        event Action<VcException> OnError;

        /// <summary>
        /// Current connection state
        /// </summary>
        VcConnectionState State { get; }

        /// <summary>
        /// Check if connection is open (convenience property)
        /// Equivalent to State.IsConnected()
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Current WebSocket configuration
        /// Needed for reconnection attempts
        /// </summary>
        WebSocketConfig Config { get; }

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
        /// Send a raw string message through WebSocket
        /// </summary>
        Task SendAsync(string message);

        /// <summary>
        /// Update method to process WebSocket events (call from Unity Update loop)
        /// </summary>
        void Update();
    }
}
