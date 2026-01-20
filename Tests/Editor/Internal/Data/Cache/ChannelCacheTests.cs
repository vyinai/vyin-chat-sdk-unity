// Tests/Editor/Internal/Data/Cache/ChannelCacheTests.cs
// Unit tests for ChannelCache

using NUnit.Framework;
using System;
using System.Threading;
using VyinChatSdk.Internal.Data.Cache;
using VyinChatSdk.Internal.Domain.Models;

namespace VyinChatSdk.Tests.Editor.Internal.Data.Cache
{
    [TestFixture]
    public class ChannelCacheTests
    {
        private ChannelCache _cache;

        [SetUp]
        public void SetUp()
        {
            _cache = new ChannelCache(maxCacheSize: 3, defaultTtl: TimeSpan.FromSeconds(1));
        }

        #region Basic Operations

        [Test]
        public void Set_AndGet_ShouldReturnCachedChannel()
        {
            // Arrange
            var channel = CreateTestChannel("test-channel-1");

            // Act
            _cache.Set("test-channel-1", channel);
            var result = _cache.TryGet("test-channel-1", out var cachedChannel);

            // Assert
            Assert.IsTrue(result, "Should find cached channel");
            Assert.IsNotNull(cachedChannel);
            Assert.AreEqual("test-channel-1", cachedChannel.ChannelUrl);
        }

        [Test]
        public void TryGet_NonExistentKey_ShouldReturnFalse()
        {
            // Act
            var result = _cache.TryGet("non-existent", out var channel);

            // Assert
            Assert.IsFalse(result, "Should not find non-existent channel");
            Assert.IsNull(channel);
        }

        [Test]
        public void TryGet_NullKey_ShouldReturnFalse()
        {
            // Act
            var result = _cache.TryGet(null, out var channel);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(channel);
        }

        [Test]
        public void TryGet_EmptyKey_ShouldReturnFalse()
        {
            // Act
            var result = _cache.TryGet("", out var channel);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(channel);
        }

        [Test]
        public void Set_Update_ShouldReplaceExisting()
        {
            // Arrange
            var channel1 = CreateTestChannel("test-channel", "Original Name");
            var channel2 = CreateTestChannel("test-channel", "Updated Name");

            // Act
            _cache.Set("test-channel", channel1);
            _cache.Set("test-channel", channel2);
            _cache.TryGet("test-channel", out var result);

            // Assert
            Assert.AreEqual("Updated Name", result.Name);
            Assert.AreEqual(1, _cache.Count, "Should only have one entry");
        }

        #endregion

        #region TTL and Expiration

        [Test]
        public void TryGet_ExpiredEntry_ShouldReturnFalse()
        {
            // Arrange
            var channel = CreateTestChannel("test-channel");
            _cache.Set("test-channel", channel);

            // Act - Wait for expiration (TTL = 1 second)
            Thread.Sleep(1100);
            var result = _cache.TryGet("test-channel", out var cachedChannel);

            // Assert
            Assert.IsFalse(result, "Should not return expired channel");
            Assert.IsNull(cachedChannel);
        }

        [Test]
        public void Set_CustomTtl_ShouldUseCustomExpiration()
        {
            // Arrange
            var channel = CreateTestChannel("test-channel");
            var customTtl = TimeSpan.FromMilliseconds(500);

            // Act
            _cache.Set("test-channel", channel, customTtl);
            Thread.Sleep(600);
            var result = _cache.TryGet("test-channel", out var cachedChannel);

            // Assert
            Assert.IsFalse(result, "Should expire with custom TTL");
            Assert.IsNull(cachedChannel);
        }

        [Test]
        public void InvalidateExpired_ShouldRemoveExpiredEntries()
        {
            // Arrange
            _cache.Set("channel-1", CreateTestChannel("channel-1"));
            _cache.Set("channel-2", CreateTestChannel("channel-2"));
            _cache.Set("channel-3", CreateTestChannel("channel-3"));

            // Act - Wait for expiration
            Thread.Sleep(1100);
            var removedCount = _cache.InvalidateExpired();

            // Assert
            Assert.AreEqual(3, removedCount, "Should remove all expired entries");
            Assert.AreEqual(0, _cache.Count, "Cache should be empty");
        }

        #endregion

        #region LRU Eviction

        [Test]
        public void Set_ExceedMaxSize_ShouldEvictOldest()
        {
            // Arrange - Cache size is 3
            _cache.Set("channel-1", CreateTestChannel("channel-1"));
            _cache.Set("channel-2", CreateTestChannel("channel-2"));
            _cache.Set("channel-3", CreateTestChannel("channel-3"));

            // Act - Add 4th channel, should evict channel-1 (oldest)
            _cache.Set("channel-4", CreateTestChannel("channel-4"));

            // Assert
            Assert.AreEqual(3, _cache.Count, "Should maintain max size");
            Assert.IsFalse(_cache.TryGet("channel-1", out _), "Oldest channel should be evicted");
            Assert.IsTrue(_cache.TryGet("channel-2", out _), "Channel 2 should remain");
            Assert.IsTrue(_cache.TryGet("channel-3", out _), "Channel 3 should remain");
            Assert.IsTrue(_cache.TryGet("channel-4", out _), "Channel 4 should be cached");
        }

        [Test]
        public void TryGet_ShouldUpdateLruPosition()
        {
            // Arrange - Cache size is 3
            _cache.Set("channel-1", CreateTestChannel("channel-1"));
            _cache.Set("channel-2", CreateTestChannel("channel-2"));
            _cache.Set("channel-3", CreateTestChannel("channel-3"));

            // Act - Access channel-1, making it most recently used
            _cache.TryGet("channel-1", out _);

            // Add 4th channel, should now evict channel-2 (oldest)
            _cache.Set("channel-4", CreateTestChannel("channel-4"));

            // Assert
            Assert.IsTrue(_cache.TryGet("channel-1", out _), "Recently accessed channel-1 should remain");
            Assert.IsFalse(_cache.TryGet("channel-2", out _), "Channel-2 should be evicted");
            Assert.IsTrue(_cache.TryGet("channel-3", out _), "Channel-3 should remain");
            Assert.IsTrue(_cache.TryGet("channel-4", out _), "Channel-4 should be cached");
        }

        #endregion

        #region Remove and Clear

        [Test]
        public void Remove_ShouldRemoveFromCache()
        {
            // Arrange
            _cache.Set("test-channel", CreateTestChannel("test-channel"));

            // Act
            _cache.Remove("test-channel");

            // Assert
            Assert.IsFalse(_cache.TryGet("test-channel", out _), "Channel should be removed");
            Assert.AreEqual(0, _cache.Count);
        }

        [Test]
        public void Remove_NonExistentKey_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _cache.Remove("non-existent"));
        }

        [Test]
        public void Clear_ShouldRemoveAllEntries()
        {
            // Arrange
            _cache.Set("channel-1", CreateTestChannel("channel-1"));
            _cache.Set("channel-2", CreateTestChannel("channel-2"));
            _cache.Set("channel-3", CreateTestChannel("channel-3"));

            // Act
            _cache.Clear();

            // Assert
            Assert.AreEqual(0, _cache.Count, "Cache should be empty");
            Assert.IsFalse(_cache.TryGet("channel-1", out _));
            Assert.IsFalse(_cache.TryGet("channel-2", out _));
            Assert.IsFalse(_cache.TryGet("channel-3", out _));
        }

        #endregion

        #region Constructor Validation

        [Test]
        public void Constructor_InvalidMaxSize_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ChannelCache(maxCacheSize: 0));
            Assert.Throws<ArgumentException>(() => new ChannelCache(maxCacheSize: -1));
        }

        [Test]
        public void Constructor_ValidParams_ShouldSucceed()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ChannelCache(maxCacheSize: 100, defaultTtl: TimeSpan.FromMinutes(10)));
        }

        #endregion

        #region Set Validation

        [Test]
        public void Set_NullChannelUrl_ShouldThrow()
        {
            // Arrange
            var channel = CreateTestChannel("test");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _cache.Set(null, channel));
        }

        [Test]
        public void Set_EmptyChannelUrl_ShouldThrow()
        {
            // Arrange
            var channel = CreateTestChannel("test");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _cache.Set("", channel));
        }

        [Test]
        public void Set_NullChannel_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _cache.Set("test-channel", null));
        }

        #endregion

        #region Helper Methods

        private ChannelBO CreateTestChannel(string channelUrl, string name = null)
        {
            return new ChannelBO
            {
                ChannelUrl = channelUrl,
                Name = name ?? $"Channel {channelUrl}",
                CreatedAt = 1234567890
            };
        }

        #endregion
    }
}
