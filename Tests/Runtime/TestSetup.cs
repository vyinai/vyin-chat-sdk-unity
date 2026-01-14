using NUnit.Framework;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Platform.Unity;

namespace VyinChatSdk.Tests.Runtime
{
    /// <summary>
    /// Global test setup for all Runtime tests
    /// Ensures Logger is initialized before any tests run
    /// </summary>
    [SetUpFixture]
    public class TestSetup
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Initialize Logger before any tests run
            // This prevents "Logger not initialized" errors
            Logger.SetInstance(UnityLoggerImpl.Instance);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clean up after all tests
            Logger.ResetForTesting();
            UnityLoggerImpl.ResetForTesting();
        }
    }
}
