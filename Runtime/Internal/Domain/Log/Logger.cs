using System;

namespace VyinChatSdk.Internal.Domain.Log
{
    /// <summary>
    /// Static facade for logging
    /// </summary>
    internal static class Logger
    {
        private const string DEFAULT_TAG = "VyinChat";
        private static ILogger _instance;

        /// <summary>
        /// Set the logger implementation
        /// Can be called multiple times safely
        /// </summary>
        internal static void SetInstance(ILogger logger)
        {
            _instance ??= logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Reset logger instance for testing purposes only
        /// </summary>
        internal static void ResetForTesting()
        {
            _instance = null;
        }

        private static ILogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException(
                        "Logger not initialized. This is an internal error - Logger.SetInstance() should be called during SDK initialization.");
                }
                return _instance;
            }
        }

        #region Logging Methods - String Tag

        public static void Verbose(string tag = DEFAULT_TAG, string message = null)
        {
            if (message == null && tag != DEFAULT_TAG)
            {
                message = tag;
                tag = DEFAULT_TAG;
            }

            Instance.Verbose(tag, message);
        }

        public static void Debug(string tag = DEFAULT_TAG, string message = null)
        {
            if (message == null && tag != DEFAULT_TAG)
            {
                message = tag;
                tag = DEFAULT_TAG;
            }

            Instance.Debug(tag, message);
        }

        public static void Info(string tag = DEFAULT_TAG, string message = null)
        {
            if (message == null && tag != DEFAULT_TAG)
            {
                message = tag;
                tag = DEFAULT_TAG;
            }

            Instance.Info(tag, message);
        }

        public static void Warning(string tag = DEFAULT_TAG, string message = null)
        {
            if (message == null && tag != DEFAULT_TAG)
            {
                message = tag;
                tag = DEFAULT_TAG;
            }

            Instance.Warning(tag, message);
        }

        public static void Error(string tag = DEFAULT_TAG, string message = null, Exception exception = null)
        {
            if (message == null && tag != DEFAULT_TAG)
            {
                message = tag;
                tag = DEFAULT_TAG;
            }

            Instance.Error(tag, message, exception);
        }

        #endregion

        #region Logging Methods - LogCategory

        public static void Verbose(LogCategory category, string message)
        {
            Instance.Verbose(category, message);
        }

        public static void Debug(LogCategory category, string message)
        {
            Instance.Debug(category, message);
        }

        public static void Info(LogCategory category, string message)
        {
            Instance.Info(category, message);
        }

        public static void Warning(LogCategory category, string message)
        {
            Instance.Warning(category, message);
        }

        public static void Error(LogCategory category, string message, Exception exception = null)
        {
            Instance.Error(category, message, exception);
        }

        #endregion

        #region Configuration

        public static void SetLogLevel(LogLevel level)
        {
            Instance.SetLogLevel(level);
        }

        public static LogLevel GetLogLevel()
        {
            return Instance.GetLogLevel();
        }

        #endregion
    }
}
