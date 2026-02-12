// -----------------------------------------------------------------------------
//
// Reconnection Manager
// Lightweight reconnection decision logic (Domain Layer)
//
// Design: Passive inquiry pattern - ConnectionManager asks for decisions
// Does NOT execute connections, only provides logic
//
// -----------------------------------------------------------------------------

using System;
using VyinChatSdk;

namespace VyinChatSdk.Internal.Domain.Reconnection
{
    /// <summary>
    /// Manages reconnection decision logic.
    /// Responsibilities:
    /// - Decide whether to attempt reconnection
    /// - Calculate retry delays using ReconnectionPolicy
    /// - Track retry state
    /// </summary>
    internal class ReconnectionManager
    {
        private readonly ReconnectionPolicy _policy;

        /// <summary>
        /// Current retry attempt count (0-based)
        /// </summary>
        public int CurrentAttempt => _policy.CurrentAttempt;

        /// <summary>
        /// Creates a new ReconnectionManager with the given policy
        /// </summary>
        /// <param name="policy">Reconnection policy for calculating delays and limits</param>
        public ReconnectionManager(ReconnectionPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        /// <summary>
        /// Decides whether reconnection should be attempted based on error type and retry limits
        /// </summary>
        /// <param name="error">The error that caused disconnection</param>
        /// <returns>True if reconnection should be attempted, false otherwise</returns>
        public bool ShouldAttemptReconnect(VcException error)
        {
            if (error == null)
            {
                return false;
            }

            // Check if error type is retriable
            if (!IsRetriableError(error.ErrorCode))
            {
                return false;
            }

            // Check if we've exceeded max retries
            if (!_policy.ShouldRetry())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the next retry delay from the policy
        /// Increments the retry attempt counter
        /// </summary>
        /// <returns>Delay in seconds before next retry attempt</returns>
        public float GetNextRetryDelay()
        {
            return _policy.GetNextDelay();
        }

        /// <summary>
        /// Called when connection succeeds
        /// Resets retry counter and state
        /// </summary>
        public void OnConnectionSuccess()
        {
            _policy.Reset();
        }

        /// <summary>
        /// Called when connection attempt fails
        /// Currently just tracks the failure, delay is calculated separately
        /// </summary>
        /// <param name="error">The error that caused the failure</param>
        public void OnConnectionFailed(VcException error)
        {
            // Currently just tracking, delay calculation happens in GetNextRetryDelay
            // Future: Could store last error for smarter reconnection logic
        }

        /// <summary>
        /// Determines if an error code represents a retriable error
        /// </summary>
        /// <param name="errorCode">The error code to check</param>
        /// <returns>True if the error is retriable (network/connection issues)</returns>
        private bool IsRetriableError(VcErrorCode errorCode)
        {
            switch (errorCode)
            {
                // Retriable: Network and connection errors
                case VcErrorCode.NetworkError:
                case VcErrorCode.NetworkRoutingError:
                case VcErrorCode.WebSocketConnectionClosed:
                case VcErrorCode.WebSocketConnectionFailed:
                case VcErrorCode.LoginTimeout:
                case VcErrorCode.AckTimeout:
                case VcErrorCode.RequestFailed:
                    return true;

                // Non-retriable: Authentication and validation errors
                // These errors should trigger token refresh flow, not auto-reconnect
                case VcErrorCode.ErrInvalidSession:
                case VcErrorCode.ErrInvalidSessionKeyValue:
                case VcErrorCode.InvalidParameter:
                case VcErrorCode.InvalidInitialization:
                case VcErrorCode.PassedInvalidAccessToken:
                    return false;

                // Default: Don't retry unknown errors
                default:
                    return false;
            }
        }
    }
}
