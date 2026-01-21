// Tests/Editor/Internal/Data/Repositories/ChannelRepositoryImplTests.cs
// Unit tests for ChannelRepositoryImpl
// Phase 3: Task 3.3 - GetChannel tests

using NUnit.Framework;
using System;
using System.Collections.Generic;
using VyinChatSdk.Internal.Data.Cache;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Data.Repositories;
using VyinChatSdk.Tests.Editor;
using VyinChatSdk.Tests.Mocks.Platform;

namespace VyinChatSdk.Tests.Editor.Internal.Data.Repositories
{
    [TestFixture]
    public class ChannelRepositoryImplTests
    {
        private MockHttpClient _mockHttpClient;
        private ChannelRepositoryImpl _repository;
        private const string TestChannelUrl = "test_channel_url";
        private const string TestSessionKey = "test-session-key-12345";
        private const string BaseApiUrl = "https://api.gamania.chat";

        [SetUp]
        public void SetUp()
        {
            _mockHttpClient = new MockHttpClient();
            // Disable cache for most tests to focus on HTTP behavior
            _repository = new ChannelRepositoryImpl(_mockHttpClient, BaseApiUrl, enableCache: false);
        }

        [TearDown]
        public void TearDown()
        {
            _mockHttpClient.Reset();
        }

        #region Helper Methods

        private ChannelRepositoryImpl CreateRepositoryWithCache()
        {
            return new ChannelRepositoryImpl(_mockHttpClient, BaseApiUrl, enableCache: true);
        }

        private void QueueSuccessResponse(string channelUrl = TestChannelUrl, string name = "Test Channel")
        {
            var json = $"{{\"channel_url\":\"{channelUrl}\",\"name\":\"{name}\"}}";
            _mockHttpClient.QueueResponse(new HttpResponse
            {
                StatusCode = 200,
                Body = json
            });
        }

        private void QueueErrorResponse(int statusCode, string error = "Error")
        {
            _mockHttpClient.QueueResponse(new HttpResponse
            {
                StatusCode = statusCode,
                Body = $"{{\"error\":\"{error}\"}}"
            });
        }

        private void SetupSessionKey(string sessionKey = TestSessionKey)
        {
            _mockHttpClient.SetSessionKey(sessionKey);
        }

        #endregion

        #region GetChannel - Success Cases

        [Test]
        public void GetChannel_ShouldReturnChannel_WhenSuccess()
        {
            // Arrange
            QueueSuccessResponse();
            SetupSessionKey();

            // Act
            var result = _repository.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TestChannelUrl, result.ChannelUrl);
            Assert.AreEqual("Test Channel", result.Name);
            Assert.AreEqual(1, _mockHttpClient.RequestHistory.Count);
        }

        [Test]
        public void GetChannel_ShouldUseSessionKey_InRequest()
        {
            // Arrange
            QueueSuccessResponse();
            SetupSessionKey();

            // Act
            var result = _repository.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();

            // Assert
            Assert.AreEqual(1, _mockHttpClient.RequestHistory.Count);
            var request = _mockHttpClient.RequestHistory[0];
            Assert.IsTrue(request.headers.ContainsKey("Session-Key"),
                "Request should include Session-Key header");
            Assert.AreEqual(TestSessionKey, request.headers["Session-Key"],
                "Session-Key header should match the set session key");
        }

        #endregion

        #region GetChannel - Error Cases

        [Test]
        public void GetChannel_ShouldThrow_When404_NotFound()
        {
            // Arrange
            QueueErrorResponse(404, "Channel not found");
            SetupSessionKey();

            // Expect error log from VcException.FromHttpResponse
            TestLogHelper.ExpectHttpErrorFallback(404);

            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
                _repository.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult());

            Assert.AreEqual(VcErrorCode.ErrNotFound, ex.ErrorCode);
            Assert.That(ex.Message, Does.Contain("not found").IgnoreCase);
        }

        [Test]
        public void GetChannel_ShouldThrow_When403_InvalidSessionKey()
        {
            // Arrange
            QueueErrorResponse(403, "Invalid session key");
            SetupSessionKey("invalid-session-key");

            // Expect error log from VcException.FromHttpResponse
            TestLogHelper.ExpectHttpErrorFallback(403);

            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
                _repository.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult());

            Assert.AreEqual(VcErrorCode.ErrForbidden, ex.ErrorCode);
            Assert.That(ex.Message, Does.Contain("session key").IgnoreCase);
        }

        [Test]
        public void GetChannel_BeforeConnect_ShouldThrow_NoSessionKey()
        {
            // Arrange - Don't set session key (simulating before WebSocket connect)
            QueueErrorResponse(401, "No session key");

            // Expect error log from VcException.FromHttpResponse
            TestLogHelper.ExpectHttpErrorFallback(401);

            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
                _repository.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult());

            Assert.AreEqual(VcErrorCode.ErrInvalidSession, ex.ErrorCode);
            Assert.That(ex.Message, Does.Contain("session key").IgnoreCase);
        }

        [Test]
        public void GetChannel_ShouldThrow_When500_ServerError()
        {
            // Arrange
            QueueErrorResponse(500, "Internal server error");
            SetupSessionKey();

            // Expect error log from VcException.FromHttpResponse
            TestLogHelper.ExpectHttpErrorFallback(500);

            // Act & Assert
            var ex = Assert.Throws<VcException>(() =>
                _repository.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult());

            Assert.AreEqual(VcErrorCode.ErrInternal, ex.ErrorCode);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_NullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ChannelRepositoryImpl(null, BaseApiUrl));
        }

        [Test]
        public void Constructor_NullBaseUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ChannelRepositoryImpl(_mockHttpClient, null));
        }

        #endregion

        #region Cache Tests

        [Test]
        public void GetChannel_WithCache_ShouldReturnCachedData_OnSecondCall()
        {
            // Arrange
            var repositoryWithCache = CreateRepositoryWithCache();
            QueueSuccessResponse();
            SetupSessionKey();

            // Act - First call fetches from network, second uses cache
            var result1 = repositoryWithCache.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();
            var result2 = repositoryWithCache.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();

            // Assert
            Assert.AreEqual(1, _mockHttpClient.RequestHistory.Count, "Should only make one HTTP request");
            Assert.AreEqual(TestChannelUrl, result1.ChannelUrl);
            Assert.AreEqual(TestChannelUrl, result2.ChannelUrl);
            Assert.AreEqual("Test Channel", result2.Name, "Cached data should match");
        }

        [Test]
        public void GetChannel_WithCacheDisabled_ShouldFetchEveryTime()
        {
            // Arrange
            QueueSuccessResponse();
            QueueSuccessResponse();
            SetupSessionKey();

            // Act - Two calls should both hit network (_repository has cache disabled)
            var result1 = _repository.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();
            var result2 = _repository.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();

            // Assert
            Assert.AreEqual(2, _mockHttpClient.RequestHistory.Count, "Should make two HTTP requests with cache disabled");
        }

        [Test]
        public void CreateChannel_ShouldCacheNewChannel()
        {
            // Arrange
            var repositoryWithCache = CreateRepositoryWithCache();
            QueueSuccessResponse("new_channel_url", "New Channel");
            SetupSessionKey();

            var createParams = new VcGroupChannelCreateParams
            {
                Name = "New Channel",
                UserIds = new List<string> { "user1", "user2" }
            };

            // Act - Create channel, then get it (should use cache)
            var created = repositoryWithCache.CreateChannelAsync(createParams).GetAwaiter().GetResult();
            var fetched = repositoryWithCache.GetChannelAsync("new_channel_url").GetAwaiter().GetResult();

            // Assert
            Assert.AreEqual(1, _mockHttpClient.RequestHistory.Count, "Should only make one HTTP request (create)");
            Assert.AreEqual("new_channel_url", fetched.ChannelUrl);
            Assert.AreEqual("New Channel", fetched.Name);
        }

        [Test]
        public void UpdateChannel_ShouldUpdateCache()
        {
            // Arrange
            var repositoryWithCache = CreateRepositoryWithCache();
            QueueSuccessResponse(name: "Old Name");
            QueueSuccessResponse(name: "Updated Name");
            SetupSessionKey();

            var updateParams = new VcGroupChannelUpdateParams { Name = "Updated Name" };

            // Act - Get (caches old name), update, then get again (uses updated cache)
            var original = repositoryWithCache.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();
            var updated = repositoryWithCache.UpdateChannelAsync(TestChannelUrl, updateParams).GetAwaiter().GetResult();
            var fetched = repositoryWithCache.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();

            // Assert
            Assert.AreEqual("Old Name", original.Name);
            Assert.AreEqual("Updated Name", updated.Name);
            Assert.AreEqual("Updated Name", fetched.Name, "Cache should be updated");
            Assert.AreEqual(2, _mockHttpClient.RequestHistory.Count, "Should make 2 HTTP requests (get, update)");
        }

        [Test]
        public void DeleteChannel_ShouldRemoveFromCache()
        {
            // Arrange
            var repositoryWithCache = CreateRepositoryWithCache();
            QueueSuccessResponse();
            _mockHttpClient.QueueResponse(new HttpResponse { StatusCode = 200, Body = "" }); // Delete response
            QueueErrorResponse(404, "Not found"); // Get after delete
            SetupSessionKey();

            // Expect error log from VcException.FromHttpResponse when 404 is returned
            TestLogHelper.ExpectHttpErrorFallback(404);

            // Act - Get (caches), delete, then try to get again
            var original = repositoryWithCache.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult();
            repositoryWithCache.DeleteChannelAsync(TestChannelUrl).GetAwaiter().GetResult();

            // Assert - Should throw 404 (cache was cleared, fetched from network)
            var ex = Assert.Throws<VcException>(() =>
                repositoryWithCache.GetChannelAsync(TestChannelUrl).GetAwaiter().GetResult());

            Assert.AreEqual(VcErrorCode.ErrNotFound, ex.ErrorCode);
            Assert.AreEqual(3, _mockHttpClient.RequestHistory.Count, "Should make 3 HTTP requests (get, delete, get)");
        }

        #endregion
    }
}