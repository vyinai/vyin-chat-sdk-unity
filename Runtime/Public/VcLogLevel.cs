namespace VyinChatSdk
{
    /// <summary>
    /// Log levels for VyinChat SDK logging configuration
    /// </summary>
    public enum VcLogLevel
    {
        /// <summary>
        /// Most detailed logging, includes all messages
        /// </summary>
        Verbose,

        /// <summary>
        /// Debug information for development and troubleshooting
        /// </summary>
        Debug,

        /// <summary>
        /// General informational messages
        /// </summary>
        Info,

        /// <summary>
        /// Warnings about potential issues (default level)
        /// </summary>
        Warning,

        /// <summary>
        /// Error messages only
        /// </summary>
        Error,

        /// <summary>
        /// Disable all logging
        /// </summary>
        None
    }
}
