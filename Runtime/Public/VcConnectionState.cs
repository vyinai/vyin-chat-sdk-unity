// -----------------------------------------------------------------------------
//
// VcConnectionState - Public API
// WebSocket connection state enum with extension methods
//
// -----------------------------------------------------------------------------

namespace VyinChatSdk
{
    /// <summary>
    /// WebSocket connection state
    /// </summary>
    public enum VcConnectionState
    {
        /// <summary>
        /// Connection is closed or not yet established
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Connection is being established
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// Connection is being closed gracefully
        /// </summary>
        Closing = 2,

        /// <summary>
        /// Connection is open and ready for communication
        /// </summary>
        Open = 3
    }

    /// <summary>
    /// Extension methods for VcConnectionState
    /// </summary>
    public static class VcConnectionStateExtensions
    {
        /// <summary>
        /// Check if connection is open
        /// </summary>
        public static bool IsConnected(this VcConnectionState state)
            => state == VcConnectionState.Open;

        /// <summary>
        /// Check if connection is being established
        /// </summary>
        public static bool IsConnecting(this VcConnectionState state)
            => state == VcConnectionState.Connecting;

        /// <summary>
        /// Check if connection is being closed
        /// </summary>
        public static bool IsClosing(this VcConnectionState state)
            => state == VcConnectionState.Closing;

        /// <summary>
        /// Check if connection is closed
        /// </summary>
        public static bool IsClosed(this VcConnectionState state)
            => state == VcConnectionState.Closed;
    }
}
