// -----------------------------------------------------------------------------
//
// Token Refresh Manager
//
// -----------------------------------------------------------------------------

using System;
using System.Text;
using VyinChatSdk;

namespace VyinChatSdk.Internal.Domain.TokenRefresh
{
    /// <summary>
    /// Manages token refresh flow
    /// </summary>
    public class TokenRefreshManager
    {
        private readonly TokenRefreshConfig _config;
        private readonly ITimeProvider _timeProvider;

        private bool _isRefreshing;
        private float _refreshStartTime;

        #region Properties

        /// <summary>
        /// Whether token refresh is in progress (dedup check)
        /// </summary>
        public bool IsRefreshing => _isRefreshing;

        #endregion

        #region Events

        /// <summary>
        /// Fired when SDK needs a new token from the app.
        /// App calls provideToken(newToken) on success, or provideToken(null) on failure.
        /// </summary>
        public event Action<Action<string>> OnTokenRefreshRequired;

        /// <summary>
        /// Fired when refresh completed successfully.
        /// </summary>
        public event Action OnSessionRefreshed;

        /// <summary>
        /// Fired when an error occurs during refresh.
        /// </summary>
        public event Action<VcException> OnSessionDidHaveError;

        /// <summary>
        /// Fired when App provides a new token.
        /// ConnectionManager should subscribe to this and call ReconnectWithToken.
        /// </summary>
        public event Action<string> OnNewTokenReceived;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates TokenRefreshManager with default time provider
        /// </summary>
        public TokenRefreshManager(TokenRefreshConfig config)
            : this(config, new SystemTimeProvider())
        {
        }

        /// <summary>
        /// Creates TokenRefreshManager with custom time provider (for testing)
        /// </summary>
        public TokenRefreshManager(TokenRefreshConfig config, ITimeProvider timeProvider)
        {
            _config = config ?? new TokenRefreshConfig();
            _timeProvider = timeProvider ?? new SystemTimeProvider();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Request token refresh. Dedup: ignored if already refreshing.
        /// </summary>
        public void RequestRefresh()
        {
            // Dedup: ignore if already refreshing
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            _refreshStartTime = _timeProvider.CurrentTime;

            // Fire event to notify app
            OnTokenRefreshRequired?.Invoke(OnTokenProvided);
        }

        /// <summary>
        /// Check if error code should trigger token refresh
        /// </summary>
        public bool IsTokenRefreshTrigger(VcException error)
        {
            if (error == null) return false;

            return error.ErrorCode == VcErrorCode.ErrInvalidSessionKeyValue ||
                   error.ErrorCode == VcErrorCode.ErrInvalidSession;
        }

        /// <summary>
        /// Check if JWT token is expired by parsing the exp claim
        /// </summary>
        public bool IsTokenExpired(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return true;
            }

            try
            {
                long? exp = ParseJwtExpiration(token);
                if (!exp.HasValue)
                {
                    return true; // No exp claim, treat as expired
                }

                float currentUnixTime = _timeProvider.CurrentTime;
                return currentUnixTime >= exp.Value;
            }
            catch
            {
                return true; // Parse error, treat as expired
            }
        }

        /// <summary>
        /// Check if token should be refreshed proactively (before it expires)
        /// Returns true if token expires within ProactiveRefreshSeconds (default 5 minutes)
        /// </summary>
        public bool ShouldRefreshProactively(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return true;
            }

            try
            {
                long? exp = ParseJwtExpiration(token);
                if (!exp.HasValue)
                {
                    return true; // No exp claim, refresh to be safe
                }

                float currentUnixTime = _timeProvider.CurrentTime;
                float timeUntilExpiry = exp.Value - currentUnixTime;
                
                // Refresh if token expires within threshold
                return timeUntilExpiry <= _config.ProactiveRefreshSeconds;
            }
            catch
            {
                return true; // Parse error, refresh to be safe
            }
        }

        /// <summary>
        /// Complete refresh successfully.
        /// </summary>
        public void CompleteRefresh()
        {
            if (!_isRefreshing)
            {
                return;
            }

            _isRefreshing = false;
            OnSessionRefreshed?.Invoke();
        }

        /// <summary>
        /// Fail refresh with specific error.
        /// </summary>
        public void FailRefresh(VcException error)
        {
            if (!_isRefreshing)
            {
                return;
            }

            _isRefreshing = false;
            OnSessionDidHaveError?.Invoke(error);
        }

        /// <summary>
        /// Update timeout timer. Should be called each frame.
        /// </summary>
        public void Update()
        {
            if (!_isRefreshing)
            {
                return;
            }

            float elapsed = _timeProvider.CurrentTime - _refreshStartTime;
            if (elapsed >= _config.TimeoutSeconds)
            {
                HandleTimeout();
            }
        }

        /// <summary>
        /// Reset state
        /// </summary>
        public void Reset()
        {
            _isRefreshing = false;
        }

        #endregion

        #region Private Methods

        private void OnTokenProvided(string token)
        {
            if (!_isRefreshing)
            {
                return;
            }

            // Null or empty token means app decided not to refresh
            if (string.IsNullOrEmpty(token))
            {
                _isRefreshing = false;
                var error = new VcException(
                    VcErrorCode.SessionKeyRefreshFailed,
                    "App failed to provide token"
                );
                OnSessionDidHaveError?.Invoke(error);
                return;
            }

            // Token provided - notify ConnectionManager to reconnect with new token
            // ConnectionManager will call CompleteRefresh() after successful reconnection
            OnNewTokenReceived?.Invoke(token);
        }

        private void HandleTimeout()
        {
            _isRefreshing = false;
            var error = new VcException(
                VcErrorCode.SessionKeyRefreshFailed,
                $"Token refresh timeout after {_config.TimeoutSeconds} seconds"
            );
            OnSessionDidHaveError?.Invoke(error);
        }

        /// <summary>
        /// Parse JWT and extract exp claim (Unix timestamp)
        /// </summary>
        private long? ParseJwtExpiration(string token)
        {
            // JWT format: header.payload.signature
            string[] parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            // Decode payload (second part)
            string payload = parts[1];
            
            // Add padding for Base64
            int padding = 4 - (payload.Length % 4);
            if (padding < 4)
            {
                payload += new string('=', padding);
            }
            
            // Replace URL-safe characters
            payload = payload.Replace('-', '+').Replace('_', '/');
            
            byte[] bytes = Convert.FromBase64String(payload);
            string json = Encoding.UTF8.GetString(bytes);
            
            // Simple JSON parsing for "exp" field
            return ExtractExpFromJson(json);
        }

        /// <summary>
        /// Extract exp value from JSON payload (simple parsing without external dependencies)
        /// </summary>
        private long? ExtractExpFromJson(string json)
        {
            // Look for "exp": or "exp" :
            int expIndex = json.IndexOf("\"exp\"", StringComparison.Ordinal);
            if (expIndex < 0)
            {
                return null;
            }

            // Find the colon after "exp"
            int colonIndex = json.IndexOf(':', expIndex + 5);
            if (colonIndex < 0)
            {
                return null;
            }

            // Find the start of the number
            int start = colonIndex + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t'))
            {
                start++;
            }

            // Find the end of the number
            int end = start;
            while (end < json.Length && char.IsDigit(json[end]))
            {
                end++;
            }

            if (end <= start)
            {
                return null;
            }

            string expValue = json.Substring(start, end - start);
            if (long.TryParse(expValue, out long exp))
            {
                return exp;
            }

            return null;
        }

        #endregion
    }
}
