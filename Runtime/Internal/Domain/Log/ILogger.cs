using System;

namespace VyinChatSdk.Internal.Domain.Log
{
    /// <summary>
    /// Logger interface for SDK logging
    /// </summary>
    internal interface ILogger
    {
        // Logging methods with string tag
        void Verbose(string tag, string message);
        void Debug(string tag, string message);
        void Info(string tag, string message);
        void Warning(string tag, string message);
        void Error(string tag, string message, Exception exception = null);

        // Logging methods with category tag
        void Verbose(LogCategory category, string message);
        void Debug(LogCategory category, string message);
        void Info(LogCategory category, string message);
        void Warning(LogCategory category, string message);
        void Error(LogCategory category, string message, Exception exception = null);
        void SetLogLevel(LogLevel level);
        LogLevel GetLogLevel();
    }
}
