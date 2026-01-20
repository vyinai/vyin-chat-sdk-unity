using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Platform.Unity;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk.Tests.Runtime.Platform
{
    [TestFixture]
    public class LoggerTests
    {
        [SetUp]
        public void SetUp()
        {
            // Reset and initialize Logger with fresh UnityLoggerImpl instance
            UnityLoggerImpl.ResetForTesting();
            Logger.ResetForTesting();
            Logger.SetInstance(UnityLoggerImpl.Instance);
            Logger.SetLogLevel(LogLevel.Verbose);
        }

        [TearDown]
        public void TearDown()
        {
            Logger.ResetForTesting();
            UnityLoggerImpl.ResetForTesting();
        }

        #region Static Method Delegation Tests

        [Test]
        public void Verbose_WithBothTagTypes_DelegatesToImplementation()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.Verbose);

            // Act & Assert - String tag
            LogAssert.Expect(LogType.Log, "[TestTag] Verbose message");
            Logger.Verbose("TestTag", "Verbose message");

            // Act & Assert - LogCategory
            LogAssert.Expect(LogType.Log, "[WebSocket] WebSocket message");
            Logger.Verbose(LogCategory.WebSocket, "WebSocket message");
        }

        [Test]
        public void Debug_WithBothTagTypes_DelegatesToImplementation()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.Debug);

            // Act & Assert - String tag
            LogAssert.Expect(LogType.Log, "[TestTag] Debug message");
            Logger.Debug("TestTag", "Debug message");

            // Act & Assert - LogCategory
            LogAssert.Expect(LogType.Log, "[Http] HTTP request");
            Logger.Debug(LogCategory.Http, "HTTP request");
        }

        [Test]
        public void Info_WithBothTagTypes_DelegatesToImplementation()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.Info);

            // Act & Assert - String tag
            LogAssert.Expect(LogType.Log, "[TestTag] Test message");
            Logger.Info("TestTag", "Test message");

            // Act & Assert - LogCategory
            LogAssert.Expect(LogType.Log, "[Connection] Connected");
            Logger.Info(LogCategory.Connection, "Connected");
        }

        [Test]
        public void Warning_WithBothTagTypes_DelegatesToImplementation()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.Warning);

            // Act & Assert - String tag
            LogAssert.Expect(LogType.Warning, "[TestTag] Warning message");
            Logger.Warning("TestTag", "Warning message");

            // Act & Assert - LogCategory
            LogAssert.Expect(LogType.Warning, "[Connection] Retry attempt");
            Logger.Warning(LogCategory.Connection, "Retry attempt");
        }

        [Test]
        public void Error_WithAllOverloads_DelegatesToImplementation()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.Error);
            var exception = new InvalidOperationException("Test exception");

            // Act & Assert - String tag without exception
            LogAssert.Expect(LogType.Error, "[TestTag] Error message");
            Logger.Error("TestTag", "Error message");

            // Act & Assert - String tag with exception
            LogAssert.Expect(LogType.Error, "[TestTag] Error message | Exception: Test exception");
            Logger.Error("TestTag", "Error message", exception);

            // Act & Assert - LogCategory without exception
            LogAssert.Expect(LogType.Error, "[Connection] Connection failed");
            Logger.Error(LogCategory.Connection, "Connection failed");

            // Act & Assert - LogCategory with exception
            LogAssert.Expect(LogType.Error, "[Connection] Connection failed | Exception: Test exception");
            Logger.Error(LogCategory.Connection, "Connection failed", exception);
        }

        #endregion

        #region Default Tag Tests

        [Test]
        public void AllLogLevels_WithoutTag_UseDefaultTag()
        {
            // Test Verbose
            Logger.SetLogLevel(LogLevel.Verbose);
            LogAssert.Expect(LogType.Log, "[VyinChat] Verbose info");
            Logger.Verbose(message: "Verbose info");

            // Test Debug
            Logger.SetLogLevel(LogLevel.Debug);
            LogAssert.Expect(LogType.Log, "[VyinChat] Debug info");
            Logger.Debug(message: "Debug info");

            // Test Info
            Logger.SetLogLevel(LogLevel.Info);
            LogAssert.Expect(LogType.Log, "[VyinChat] SDK initialized");
            Logger.Info(message: "SDK initialized");

            // Test Warning
            Logger.SetLogLevel(LogLevel.Warning);
            LogAssert.Expect(LogType.Warning, "[VyinChat] Warning info");
            Logger.Warning(message: "Warning info");

            // Test Error
            Logger.SetLogLevel(LogLevel.Error);
            LogAssert.Expect(LogType.Error, "[VyinChat] Error info");
            Logger.Error(message: "Error info");
        }

        #endregion

        #region SetLogLevel and GetLogLevel Tests

        [Test]
        public void SetLogLevel_ChangesCurrentLevel()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.Debug);

            // Act
            var result = Logger.GetLogLevel();

            // Assert
            Assert.AreEqual(LogLevel.Debug, result);
        }

        [Test]
        public void SetLogLevel_ToNone_DisablesAllLogs()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.None);

            // Act
            Logger.Verbose("Test", "Verbose");
            Logger.Debug("Test", "Debug");
            Logger.Info("Test", "Info");
            Logger.Warning("Test", "Warning");
            Logger.Error("Test", "Error");

            // Assert
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SetLogLevel_ToVerbose_EnablesAllLogs()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.Verbose);

            // Act & Assert
            LogAssert.Expect(LogType.Log, "[Test] Verbose");
            Logger.Verbose("Test", "Verbose");

            LogAssert.Expect(LogType.Log, "[Test] Debug");
            Logger.Debug("Test", "Debug");

            LogAssert.Expect(LogType.Log, "[Test] Info");
            Logger.Info("Test", "Info");

            LogAssert.Expect(LogType.Warning, "[Test] Warning");
            Logger.Warning("Test", "Warning");

            LogAssert.Expect(LogType.Error, "[Test] Error");
            Logger.Error("Test", "Error");
        }

        #endregion

        #region Thread Safety Tests

        [Test]
        public void MultipleConcurrentCalls_DoNotThrow()
        {
            // Arrange
            Logger.SetLogLevel(LogLevel.Info);
            var tasks = new System.Threading.Tasks.Task[10];

            // Act
            for (int i = 0; i < tasks.Length; i++)
            {
                int taskId = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    Logger.Info("Thread", $"Message from task {taskId}");
                });
            }

            // Assert
            Assert.DoesNotThrow(() => System.Threading.Tasks.Task.WaitAll(tasks));
        }

        #endregion
    }
}
