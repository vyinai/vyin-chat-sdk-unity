using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Platform.Unity;
using ILogger = VyinChatSdk.Internal.Domain.Log.ILogger;

namespace VyinChatSdk.Tests.Runtime.Platform
{
    [TestFixture]
    public class UnityLoggerImplTests
    {
        private UnityLoggerImpl _logger;

        [SetUp]
        public void SetUp()
        {
            // Reset singleton and get fresh instance for each test
            UnityLoggerImpl.ResetForTesting();
            _logger = UnityLoggerImpl.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            UnityLoggerImpl.ResetForTesting();
        }

        #region Singleton Tests

        [Test]
        public void Instance_CalledMultipleTimes_ReturnsSameInstance()
        {
            // Act
            var instance1 = UnityLoggerImpl.Instance;
            var instance2 = UnityLoggerImpl.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void Instance_ThreadSafe_ReturnsSameInstance()
        {
            // Arrange
            ILogger instance1 = null;
            ILogger instance2 = null;

            // Act
            var task1 = Task.Run(() => instance1 = UnityLoggerImpl.Instance);
            var task2 = Task.Run(() => instance2 = UnityLoggerImpl.Instance);

            Task.WaitAll(task1, task2);

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        #endregion

        #region Log Level Filtering Tests

        [Test]
        public void LogLevelFiltering_VerboseLevel_OutputsVerboseAndAbove()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Verbose);

            // Act & Assert - Verbose outputs
            LogAssert.Expect(LogType.Log, "[Test] Verbose message");
            _logger.Verbose("Test", "Verbose message");

            LogAssert.Expect(LogType.Log, "[Test] Debug message");
            _logger.Debug("Test", "Debug message");

            LogAssert.Expect(LogType.Log, "[Test] Info message");
            _logger.Info("Test", "Info message");

            LogAssert.Expect(LogType.Warning, "[Test] Warning message");
            _logger.Warning("Test", "Warning message");

            LogAssert.Expect(LogType.Error, "[Test] Error message");
            _logger.Error("Test", "Error message");
        }

        [Test]
        public void LogLevelFiltering_DebugLevel_FiltersVerbose()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Debug);

            // Act - Verbose is filtered
            _logger.Verbose("Test", "Verbose message");

            // Assert
            LogAssert.NoUnexpectedReceived();

            // Act & Assert - Debug and above output
            LogAssert.Expect(LogType.Log, "[Test] Debug message");
            _logger.Debug("Test", "Debug message");
        }

        [Test]
        public void LogLevelFiltering_InfoLevel_FiltersVerboseAndDebug()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Info);

            // Act - Verbose and Debug are filtered
            _logger.Verbose("Test", "Verbose");
            _logger.Debug("Test", "Debug");

            // Assert
            LogAssert.NoUnexpectedReceived();

            // Act & Assert - Info and above output
            LogAssert.Expect(LogType.Log, "[Test] Info message");
            _logger.Info("Test", "Info message");
        }

        [Test]
        public void LogLevelFiltering_WarningLevel_FiltersInfoAndBelow()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Warning);

            // Act - Info and below are filtered
            _logger.Verbose("Test", "Verbose");
            _logger.Debug("Test", "Debug");
            _logger.Info("Test", "Info");

            // Assert
            LogAssert.NoUnexpectedReceived();

            // Act & Assert - Warning and above output
            LogAssert.Expect(LogType.Warning, "[Test] Warning message");
            _logger.Warning("Test", "Warning message");
        }

        [Test]
        public void LogLevelFiltering_ErrorLevel_FiltersWarningAndBelow()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Error);

            // Act - Warning and below are filtered
            _logger.Verbose("Test", "Verbose");
            _logger.Debug("Test", "Debug");
            _logger.Info("Test", "Info");
            _logger.Warning("Test", "Warning");

            // Assert
            LogAssert.NoUnexpectedReceived();

            // Act & Assert - Error outputs
            LogAssert.Expect(LogType.Error, "[Test] Error message");
            _logger.Error("Test", "Error message");
        }

        [Test]
        public void LogLevelFiltering_NoneLevel_FiltersAllLogs()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.None);

            // Act
            _logger.Verbose("Test", "Verbose");
            _logger.Debug("Test", "Debug");
            _logger.Info("Test", "Info");
            _logger.Warning("Test", "Warning");
            _logger.Error("Test", "Error");

            // Assert
            LogAssert.NoUnexpectedReceived();
        }

        #endregion

        #region Log Format Tests

        [Test]
        public void LogFormat_WithTagAndMessage_FormatsCorrectly()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Info);

            // Act & Assert
            LogAssert.Expect(LogType.Log, "[MyTag] My message");
            _logger.Info("MyTag", "My message");
        }

        [Test]
        public void LogFormat_Error_HandlesExceptionCorrectly()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Error);
            var exception = new InvalidOperationException("Test exception");

            // Act & Assert - Without exception
            LogAssert.Expect(LogType.Error, "[MyTag] Error occurred");
            _logger.Error("MyTag", "Error occurred");

            // Act & Assert - With exception
            LogAssert.Expect(LogType.Error, "[MyTag] Error occurred | Exception: Test exception");
            _logger.Error("MyTag", "Error occurred", exception);
        }

        #endregion

        #region PII Redaction Tests

        [Test]
        public void PIIRedaction_SensitiveData_RedactsCorrectly()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Info);

            // Act & Assert - user_id
            LogAssert.Expect(LogType.Log, "[Test] User logged in: user_id: ***");
            _logger.Info("Test", "User logged in: user_id: 123456");

            // Act & Assert - token
            LogAssert.Expect(LogType.Log, "[Test] Auth token: ***");
            _logger.Info("Test", "Auth token: abc123xyz789");

            // Act & Assert - session_key
            LogAssert.Expect(LogType.Log, "[Test] Session session_key: ***");
            _logger.Info("Test", "Session session_key: xyz789abc123");

            // Act & Assert - email
            LogAssert.Expect(LogType.Log, "[Test] User email: u***@e***.com");
            _logger.Info("Test", "User email: user@example.com");

            // Act & Assert - multiple PII
            LogAssert.Expect(LogType.Log, "[Test] Login: user_id: ***, token: ***, email: u***@e***.com");
            _logger.Info("Test", "Login: user_id: 123, token: abc123, email: user@example.com");
        }

        [Test]
        public void PIIRedaction_NormalText_DoesNotRedact()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Info);

            // Act & Assert
            LogAssert.Expect(LogType.Log, "[Test] Normal message without PII");
            _logger.Info("Test", "Normal message without PII");
        }

        [Test]
        public void PIIRedaction_NullOrEmptyMessage_DoesNotThrow()
        {
            // Arrange
            _logger.SetLogLevel(LogLevel.Info);

            // Act & Assert - null message
            LogAssert.Expect(LogType.Log, "[Test] ");
            Assert.DoesNotThrow(() => _logger.Info("Test", null));

            // Act & Assert - empty message
            LogAssert.Expect(LogType.Log, "[Test] ");
            Assert.DoesNotThrow(() => _logger.Info("Test", ""));
        }

        #endregion

        #region LogCategory Tests

        [Test]
        public void LogCategory_DifferentCategories_FormatWithCorrectNames()
        {
            // Test Verbose with WebSocket category
            _logger.SetLogLevel(LogLevel.Verbose);
            LogAssert.Expect(LogType.Log, "[WebSocket] Raw message: {\"cmd\":\"MESG\"}");
            _logger.Verbose(LogCategory.WebSocket, "Raw message: {\"cmd\":\"MESG\"}");

            // Test Debug with Http category
            _logger.SetLogLevel(LogLevel.Debug);
            LogAssert.Expect(LogType.Log, "[Http] GET /api/channels");
            _logger.Debug(LogCategory.Http, "GET /api/channels");

            // Test Info with Connection category
            _logger.SetLogLevel(LogLevel.Info);
            LogAssert.Expect(LogType.Log, "[Connection] Connected to server");
            _logger.Info(LogCategory.Connection, "Connected to server");
        }

        #endregion

        #region GetLogLevel Tests

        [Test]
        public void GetLogLevel_ReturnsCorrectLevel()
        {
            // Test default level
            Assert.AreEqual(LogLevel.Info, _logger.GetLogLevel());

            // Test after setting to Debug
            _logger.SetLogLevel(LogLevel.Debug);
            Assert.AreEqual(LogLevel.Debug, _logger.GetLogLevel());

            // Test after setting to Error
            _logger.SetLogLevel(LogLevel.Error);
            Assert.AreEqual(LogLevel.Error, _logger.GetLogLevel());
        }

        #endregion
    }
}
