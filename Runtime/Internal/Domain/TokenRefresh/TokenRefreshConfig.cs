// -----------------------------------------------------------------------------
//
// Token Refresh Config
// Configuration for token refresh behavior
//
// -----------------------------------------------------------------------------

using System;

namespace VyinChatSdk.Internal.Domain.TokenRefresh
{
    /// <summary>
    /// Configuration for token refresh behavior
    /// </summary>
    public class TokenRefreshConfig
    {
        private const float MinTimeoutSeconds = 60f;
        private const float MaxTimeoutSeconds = 1800f;
        private const float DefaultProactiveRefreshSeconds = 300f; // 5 minutes

        /// <summary>
        /// Timeout in seconds for app to provide new token (default: 60s)
        /// </summary>
        public float TimeoutSeconds { get; }

        /// <summary>
        /// Proactive refresh threshold in seconds before token expiry (default: 300s = 5 minutes)
        /// </summary>
        public float ProactiveRefreshSeconds { get; }

        /// <summary>
        /// Creates a new TokenRefreshConfig with default values
        /// </summary>
        public TokenRefreshConfig() : this(60f, DefaultProactiveRefreshSeconds)
        {
        }

        /// <summary>
        /// Creates a new TokenRefreshConfig with custom timeout
        /// </summary>
        public TokenRefreshConfig(float timeoutSeconds) : this(timeoutSeconds, DefaultProactiveRefreshSeconds)
        {
        }

        /// <summary>
        /// Creates a new TokenRefreshConfig with custom timeout and proactive refresh threshold
        /// </summary>
        public TokenRefreshConfig(float timeoutSeconds, float proactiveRefreshSeconds)
        {
            TimeoutSeconds = Math.Max(MinTimeoutSeconds, Math.Min(timeoutSeconds, MaxTimeoutSeconds));
            ProactiveRefreshSeconds = Math.Max(0f, proactiveRefreshSeconds);
        }
    }
}
