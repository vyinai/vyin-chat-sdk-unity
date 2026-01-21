namespace VyinChatSdk.Internal.Domain.Log
{
    /// <summary>
    /// Maps between public VcLogLevel and internal LogLevel
    /// </summary>
    internal static class LogLevelMapper
    {
        public static LogLevel FromVcLogLevel(VcLogLevel vcLevel)
        {
            return vcLevel switch
            {
                VcLogLevel.Verbose => LogLevel.Verbose,
                VcLogLevel.Debug => LogLevel.Debug,
                VcLogLevel.Info => LogLevel.Info,
                VcLogLevel.Warning => LogLevel.Warning,
                VcLogLevel.Error => LogLevel.Error,
                VcLogLevel.None => LogLevel.None,
                _ => LogLevel.Info
            };
        }

        public static VcLogLevel ToVcLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => VcLogLevel.Verbose,
                LogLevel.Debug => VcLogLevel.Debug,
                LogLevel.Info => VcLogLevel.Info,
                LogLevel.Warning => VcLogLevel.Warning,
                LogLevel.Error => VcLogLevel.Error,
                LogLevel.None => VcLogLevel.None,
                _ => VcLogLevel.Info
            };
        }
    }
}
