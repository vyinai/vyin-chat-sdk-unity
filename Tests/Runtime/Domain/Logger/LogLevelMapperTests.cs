using NUnit.Framework;
using VyinChatSdk;
using VyinChatSdk.Internal.Domain.Log;

namespace VyinChatSdk.Tests.Runtime.Domain.Logger
{
    [TestFixture]
    public class LogLevelMapperTests
    {
        [Test]
        public void FromVcLogLevel_AllValidLevels_MapCorrectly()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(LogLevel.Verbose, LogLevelMapper.FromVcLogLevel(VcLogLevel.Verbose));
            Assert.AreEqual(LogLevel.Debug, LogLevelMapper.FromVcLogLevel(VcLogLevel.Debug));
            Assert.AreEqual(LogLevel.Info, LogLevelMapper.FromVcLogLevel(VcLogLevel.Info));
            Assert.AreEqual(LogLevel.Warning, LogLevelMapper.FromVcLogLevel(VcLogLevel.Warning));
            Assert.AreEqual(LogLevel.Error, LogLevelMapper.FromVcLogLevel(VcLogLevel.Error));
            Assert.AreEqual(LogLevel.None, LogLevelMapper.FromVcLogLevel(VcLogLevel.None));
        }

        [Test]
        public void ToVcLogLevel_AllValidLevels_MapCorrectly()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(VcLogLevel.Verbose, LogLevelMapper.ToVcLogLevel(LogLevel.Verbose));
            Assert.AreEqual(VcLogLevel.Debug, LogLevelMapper.ToVcLogLevel(LogLevel.Debug));
            Assert.AreEqual(VcLogLevel.Info, LogLevelMapper.ToVcLogLevel(LogLevel.Info));
            Assert.AreEqual(VcLogLevel.Warning, LogLevelMapper.ToVcLogLevel(LogLevel.Warning));
            Assert.AreEqual(VcLogLevel.Error, LogLevelMapper.ToVcLogLevel(LogLevel.Error));
            Assert.AreEqual(VcLogLevel.None, LogLevelMapper.ToVcLogLevel(LogLevel.None));
        }

        [Test]
        public void FromVcLogLevel_InvalidValue_ReturnsDefaultInfo()
        {
            // Arrange
            var invalidLevel = (VcLogLevel)999;

            // Act
            var result = LogLevelMapper.FromVcLogLevel(invalidLevel);

            // Assert
            Assert.AreEqual(LogLevel.Info, result);
        }

        [Test]
        public void ToVcLogLevel_InvalidValue_ReturnsDefaultInfo()
        {
            // Arrange
            var invalidLevel = (LogLevel)999;

            // Act
            var result = LogLevelMapper.ToVcLogLevel(invalidLevel);

            // Assert
            Assert.AreEqual(VcLogLevel.Info, result);
        }

        [Test]
        public void BidirectionalMapping_AllLevels_AreConsistent()
        {
            // Test all valid levels for bidirectional consistency
            var vcLevels = new[]
            {
                VcLogLevel.Verbose,
                VcLogLevel.Debug,
                VcLogLevel.Info,
                VcLogLevel.Warning,
                VcLogLevel.Error,
                VcLogLevel.None
            };

            foreach (var vcLevel in vcLevels)
            {
                // Act
                var internalLevel = LogLevelMapper.FromVcLogLevel(vcLevel);
                var roundTrip = LogLevelMapper.ToVcLogLevel(internalLevel);

                // Assert
                Assert.AreEqual(vcLevel, roundTrip,
                    $"Bidirectional mapping failed for {vcLevel}");
            }
        }
    }
}
