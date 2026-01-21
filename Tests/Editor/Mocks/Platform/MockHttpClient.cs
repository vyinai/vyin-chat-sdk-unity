using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Data.Network;

namespace VyinChatSdk.Tests.Mocks.Platform
{
    /// <summary>
    /// Mock implementation of HTTP client for testing
    /// </summary>
    public class MockHttpClient : IHttpClient
    {
        private readonly Queue<HttpResponse> _responseQueue = new Queue<HttpResponse>();
        private HttpResponse _defaultResponse;
        private string _sessionKey;

        public List<(string method, string url, string body, Dictionary<string, string> headers)> RequestHistory { get; } = new();
        public string SessionKey => _sessionKey;

        /// <summary>
        /// Sets the session key that will be included in request headers
        /// </summary>
        public void SetSessionKey(string sessionKey)
        {
            _sessionKey = sessionKey;
        }

        /// <summary>
        /// Adds a response to the queue that will be returned for the next request
        /// </summary>
        public void QueueResponse(HttpResponse response)
        {
            _responseQueue.Enqueue(response);
        }

        /// <summary>
        /// Sets the default response to return when the queue is empty
        /// </summary>
        public void SetDefaultResponse(HttpResponse response)
        {
            _defaultResponse = response;
        }

        /// <summary>
        /// Clears all queued responses, request history, and resets the session key
        /// </summary>
        public void Reset()
        {
            _responseQueue.Clear();
            RequestHistory.Clear();
            _defaultResponse = null;
            _sessionKey = null;
        }

        public Task<HttpResponse> GetAsync(
            string url,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var allHeaders = MergeHeaders(headers);
            RequestHistory.Add(("GET", url, null, allHeaders));
            return Task.FromResult(GetNextResponse());
        }

        public Task<HttpResponse> PostAsync(
            string url,
            string body,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var allHeaders = MergeHeaders(headers);
            RequestHistory.Add(("POST", url, body, allHeaders));
            return Task.FromResult(GetNextResponse());
        }

        public Task<HttpResponse> PutAsync(
            string url,
            string body,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var allHeaders = MergeHeaders(headers);
            RequestHistory.Add(("PUT", url, body, allHeaders));
            return Task.FromResult(GetNextResponse());
        }

        public Task<HttpResponse> DeleteAsync(
            string url,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var allHeaders = MergeHeaders(headers);
            RequestHistory.Add(("DELETE", url, null, allHeaders));
            return Task.FromResult(GetNextResponse());
        }

        private Dictionary<string, string> MergeHeaders(Dictionary<string, string> headers)
        {
            var result = new Dictionary<string, string>();

            result[ApiVersionConfig.HeaderName] = ApiVersionConfig.DefaultVersion;

            // Add session key if set
            if (!string.IsNullOrEmpty(_sessionKey))
            {
                result["Session-Key"] = _sessionKey;
            }

            // Add custom headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    result[header.Key] = header.Value;
                }
            }

            return result;
        }

        private HttpResponse GetNextResponse()
        {
            if (_responseQueue.Count > 0)
            {
                return _responseQueue.Dequeue();
            }

            if (_defaultResponse != null)
            {
                return _defaultResponse;
            }

            // Default success response
            return new HttpResponse
            {
                StatusCode = 200,
                Body = "{}",
                Headers = new Dictionary<string, string>()
            };
        }
    }

    /// <summary>
    /// Provides helper methods to create common HTTP response objects for testing
    /// </summary>
    public static class MockHttpResponseBuilder
    {
        public static HttpResponse Success(string body = "{}")
        {
            return new HttpResponse
            {
                StatusCode = 200,
                Body = body,
                Headers = new Dictionary<string, string>()
            };
        }

        public static HttpResponse NotFound(string body = "{\"error\":\"Not found\"}")
        {
            return new HttpResponse
            {
                StatusCode = 404,
                Body = body,
                Error = "Not found"
            };
        }

        public static HttpResponse Unauthorized(string body = "{\"error\":\"Unauthorized\"}")
        {
            return new HttpResponse
            {
                StatusCode = 401,
                Body = body,
                Error = "Unauthorized"
            };
        }

        public static HttpResponse ServerError(string body = "{\"error\":\"Internal server error\"}")
        {
            return new HttpResponse
            {
                StatusCode = 500,
                Body = body,
                Error = "Internal server error"
            };
        }
    }
}
