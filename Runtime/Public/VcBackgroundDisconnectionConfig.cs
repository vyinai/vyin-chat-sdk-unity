namespace VyinChatSdk
{
    /// <summary>
    /// Background disconnection and lifecycle management configuration.
    /// Controls how SDK responds to app state changes and network events.
    /// 
    /// Note: For reconnection backoff strategy (retry delays, max attempts),
    /// see ReconnectionPolicy in ConnectionManager (internal).
    /// </summary>
    public class VcBackgroundDisconnectionConfig
    {
        /// <summary>
        /// Track application lifecycle events (pause/resume/quit).
        /// When false, SDK ignores application state changes.
        /// Default: true
        /// </summary>
        public bool IsTrackingApplicationState { get; set; } = true;

        /// <summary>
        /// Disconnect when application enters background.
        /// Only effective when IsTrackingApplicationState = true.
        /// Default: true
        /// </summary>
        public bool DisconnectOnBackground { get; set; } = true;

        /// <summary>
        /// Delay before disconnecting in background (seconds).
        /// 0 = immediate, > 0 = delayed disconnect.
        /// Default: 0
        /// </summary>
        public float BackgroundDisconnectDelaySeconds { get; set; } = 0f;

        /// <summary>
        /// Enable automatic reconnection when network becomes available.
        /// Default: true
        /// </summary>
        public bool NetworkAwarenessReconnection { get; set; } = true;

        /// <summary>
        /// Default configuration
        /// </summary>
        public static VcBackgroundDisconnectionConfig Default => new VcBackgroundDisconnectionConfig();

        /// <summary>
        /// Keep connection alive in background
        /// </summary>
        public static VcBackgroundDisconnectionConfig KeepAliveInBackground => new VcBackgroundDisconnectionConfig
        {
            IsTrackingApplicationState = true,
            DisconnectOnBackground = false
        };

        /// <summary>
        /// Ignore all lifecycle events
        /// </summary>
        public static VcBackgroundDisconnectionConfig IgnoreLifecycle => new VcBackgroundDisconnectionConfig
        {
            IsTrackingApplicationState = false
        };

        /// <summary>
        /// Custom background disconnect delay
        /// </summary>
        public static VcBackgroundDisconnectionConfig WithBackgroundDelay(float delaySeconds) => new VcBackgroundDisconnectionConfig
        {
            IsTrackingApplicationState = true,
            DisconnectOnBackground = true,
            BackgroundDisconnectDelaySeconds = delaySeconds
        };
    }
}
