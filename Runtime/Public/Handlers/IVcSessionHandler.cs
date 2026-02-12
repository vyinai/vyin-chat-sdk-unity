// -----------------------------------------------------------------------------
// IVcSessionHandler - Session callback interface for token refresh
// -----------------------------------------------------------------------------

using System;

namespace VyinChatSdk
{
    /// <summary>
    /// Session callback handler for token refresh events.
    /// Implement this interface to handle token refresh lifecycle.
    /// </summary>
    public interface IVcSessionHandler
    {
        /// <summary>
        /// Called when SDK needs a new token.
        /// App should fetch a new token and call success(newToken) when done,
        /// or call fail() if token refresh failed.
        /// </summary>
        /// <param name="success">Call with new token when refresh succeeds. Pass null if app decides not to refresh.</param>
        /// <param name="fail">Call when token refresh fails.</param>
        void OnSessionTokenRequired(Action<string> success, Action fail);

        /// <summary>
        /// Called when token refresh succeeded.
        /// </summary>
        void OnSessionRefreshed();

        /// <summary>
        /// Called when session cannot be recovered.
        /// </summary>
        void OnSessionClosed();

        /// <summary>
        /// Called when an error occurred during refresh.
        /// </summary>
        /// <param name="error">The error that occurred</param>
        void OnSessionError(VcException error);
    }
}
