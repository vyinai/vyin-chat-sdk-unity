using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace VyinChatSdk.Tests
{
    /// <summary>
    /// Helper for common LogAssert patterns in tests.
    /// </summary>
    public static class TestLogHelper
    {
        public static void ExpectHttpErrorFallback(int statusCode)
        {
            LogAssert.Expect(LogType.Error,
                new Regex($@".*\[Http\] HTTP error fallback.*status={statusCode}.*"));
        }

        public static void ExpectHttpErrorMapped(int apiCode, int? vcCode = null)
        {
            var pattern = vcCode.HasValue
                ? $@".*\[Http\] HTTP error code mapped.*apiCode={apiCode}.*vcCode={vcCode}.*"
                : $@".*\[Http\] HTTP error code mapped.*apiCode={apiCode}.*";
            LogAssert.Expect(LogType.Error, new Regex(pattern));
        }

        public static void ExpectConnectionError(string messagePattern = ".*")
        {
            LogAssert.Expect(LogType.Error,
                new Regex($@".*\[Connection\].*{messagePattern}.*"));
        }

        public static void ExpectWebSocketError(string messagePattern = ".*")
        {
            LogAssert.Expect(LogType.Error,
                new Regex($@".*\[WebSocket\].*{messagePattern}.*"));
        }

        public static void ExpectChannelError(string operationName)
        {
            LogAssert.Expect(LogType.Error,
                new Regex($@".*\[VcGroupChannel\].*{operationName}.*failed.*"));
        }

        public static void ExpectGroupChannelModuleError(string operationName)
        {
            LogAssert.Expect(LogType.Error,
                new Regex($@".*\[VcGroupChannelModule\].*{operationName}.*failed.*"));
        }
    }
}
