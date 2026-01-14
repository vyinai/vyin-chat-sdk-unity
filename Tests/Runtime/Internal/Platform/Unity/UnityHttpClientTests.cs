// Tests/Runtime/Internal/Platform/Unity/UnityHttpClientTests.cs
// Unit tests for UnityHttpClient

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Platform.Unity.Network;

namespace VyinChatSdk.Tests.Runtime.Internal.Platform.Unity
{
    [TestFixture]
    public class UnityHttpClientTests
    {
        private UnityHttpClient _httpClient;
        private const string TestUrl = "https://api.example.com/test";
        private const string TestSessionKey = "test-session-key-12345";

        [SetUp]
        public void SetUp()
        {
            _httpClient = new UnityHttpClient();
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient = null;
        }

        #region GetAsync Tests

        [UnityTest]
        public IEnumerator GetAsync_ShouldReturnData_WhenSuccess()
        {
            // Arrange
            var url = "https://httpbin.org/get";

            // Act
            Task<HttpResponse> task = _httpClient.GetAsync(url);
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsFalse(task.IsFaulted, "Request should not fault");
            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.AreEqual(200, response.StatusCode, "Status code should be 200");
            Assert.IsTrue(response.IsSuccess, "Response should be successful");
            Assert.IsNotEmpty(response.Body, "Response body should not be empty");
        }

        #endregion

        #region PostAsync Tests

        [UnityTest]
        public IEnumerator PostAsync_ShouldSendBody_Correctly()
        {
            // Arrange
            var url = "https://httpbin.org/post";
            var body = "{\"test\":\"data\",\"value\":123}";

            // Act
            Task<HttpResponse> task = _httpClient.PostAsync(url, body);
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsFalse(task.IsFaulted, "Request should not fault");
            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.AreEqual(200, response.StatusCode, "Status code should be 200");
            Assert.IsTrue(response.IsSuccess, "Response should be successful");

            // httpbin.org echoes back the posted data in the response
            Assert.IsTrue(response.Body.Contains("test"), "Response should contain posted data");
            Assert.IsTrue(response.Body.Contains("data"), "Response should contain posted data");
        }

        #endregion

        #region Session Key Tests

        [UnityTest]
        public IEnumerator SetSessionKey_ShouldIncludeInHeaders()
        {
            // Arrange
            var url = "https://httpbin.org/headers";
            _httpClient.SetSessionKey(TestSessionKey);

            // Act
            Task<HttpResponse> task = _httpClient.GetAsync(url);
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsFalse(task.IsFaulted, "Request should not fault");
            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.AreEqual(200, response.StatusCode, "Status code should be 200");

            // httpbin.org/headers returns all request headers in response body as JSON
            // Headers might be lowercase in response, so check case-insensitively
            var bodyLower = response.Body.ToLower();
            Assert.IsTrue(bodyLower.Contains("session-key"),
                $"Response should show Session-Key header was sent. Response body: {response.Body}");
            Assert.IsTrue(response.Body.Contains(TestSessionKey),
                $"Response should show session key value was sent. Response body: {response.Body}");
        }

        [UnityTest]
        public IEnumerator Request_ShouldIncludeSessionKey_InAuthHeader()
        {
            // Arrange
            // httpbin.org/post accepts POST and echoes back request headers
            var url = "https://httpbin.org/post";
            _httpClient.SetSessionKey(TestSessionKey);

            // Act
            Task<HttpResponse> task = _httpClient.PostAsync(url, "{\"test\":\"data\"}");
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsFalse(task.IsFaulted, "Request should not fault");
            var response = task.Result;

            // Verify session key is in headers (httpbin echoes back request headers in JSON)
            var bodyLower = response.Body.ToLower();
            Assert.IsTrue(bodyLower.Contains("session-key"),
                $"Session-Key header should be present. Response body: {response.Body}");
            Assert.IsTrue(response.Body.Contains(TestSessionKey),
                $"Session key value should match. Response body: {response.Body}");
        }

        #endregion

        #region Error Handling Tests

        [UnityTest]
        public IEnumerator Request_ShouldReturnError_When401()
        {
            // Arrange
            var url = "https://httpbin.org/status/401";

            // Act
            Task<HttpResponse> task = _httpClient.GetAsync(url);
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);

            // Assert
            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.AreEqual(401, response.StatusCode, "Status code should be 401");
            Assert.IsFalse(response.IsSuccess, "Response should not be successful");
        }

        [UnityTest]
        public IEnumerator Request_ShouldReturnError_When403_InvalidSessionKey()
        {
            // Arrange
            var url = "https://httpbin.org/status/403";
            _httpClient.SetSessionKey("invalid-session-key");

            // Act
            Task<HttpResponse> task = _httpClient.GetAsync(url);
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);

            // Assert
            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.AreEqual(403, response.StatusCode, "Status code should be 403");
            Assert.IsFalse(response.IsSuccess, "Response should not be successful");
        }

        [UnityTest]
        public IEnumerator Request_ShouldReturnError_When500()
        {
            // Arrange
            var url = "https://httpbin.org/status/500";

            // Act
            Task<HttpResponse> task = _httpClient.GetAsync(url);
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);

            // Assert
            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.AreEqual(500, response.StatusCode, "Status code should be 500");
            Assert.IsFalse(response.IsSuccess, "Response should not be successful");
        }

        [UnityTest]
        public IEnumerator Request_ShouldThrow_OnTimeout()
        {
            // Arrange
            var url = "https://httpbin.org/delay/10"; // 10 second delay
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(2)); // Timeout after 2 seconds

            // Act
            Task<HttpResponse> task = _httpClient.GetAsync(url, null, cancellationTokenSource.Token);
            yield return new UnityEngine.WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.IsFaulted || task.IsCanceled, "Request should be faulted or canceled on timeout");
        }

        #endregion
    }
}
