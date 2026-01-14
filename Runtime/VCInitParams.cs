namespace VyinChatSdk
{
    /// <summary>
    /// Initialization parameters for VyinChat SDK
    /// </summary>
    public class VcInitParams
    {
        /// <summary>
        /// Application ID (required)
        /// </summary>
        public string AppId { get; }

        /// <summary>
        /// Determines to use local caching
        /// </summary>
        public bool IsLocalCachingEnabled { get; }

        /// <summary>
        /// Log level
        /// </summary>
        public VcLogLevel LogLevel { get; }

        /// <summary>
        /// Host app version
        /// </summary>
        public string AppVersion { get; }

        /// <summary>
        /// Initialize VcInitParams with required and optional parameters
        /// </summary>
        /// <param name="appId">Application ID (required)</param>
        /// <param name="isLocalCachingEnabled">Enable local caching (default: false)</param>
        /// <param name="logLevel">Log level (default: Warning)</param>
        /// <param name="appVersion">Host app version (optional)</param>
        public VcInitParams(
            string appId,
            bool isLocalCachingEnabled = false,
            VcLogLevel logLevel = VcLogLevel.Warning,
            string appVersion = null)
        {
            AppId = appId;
            IsLocalCachingEnabled = isLocalCachingEnabled;
            LogLevel = logLevel;
            AppVersion = appVersion;
        }
    }
}