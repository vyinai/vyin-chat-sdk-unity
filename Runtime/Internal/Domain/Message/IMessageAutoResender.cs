using System;

namespace VyinChatSdk.Internal.Domain.Message
{
    /// <summary>
    /// Interface for auto-resend message queue management.
    /// </summary>
    public interface IMessageAutoResender : IDisposable
    {
        /// <summary>
        /// Number of pending messages in queue.
        /// </summary>
        int PendingCount { get; }

        /// <summary>
        /// Whether auto-resend is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Register a message for auto-resend.
        /// </summary>
        /// <param name="message">Message to queue.</param>
        /// <returns>True if registered, false if rejected (disabled or capacity reached).</returns>
        bool Register(PendingMessage message);

        /// <summary>
        /// Try to dequeue the next pending message (FIFO).
        /// </summary>
        /// <param name="message">Dequeued message if available.</param>
        /// <returns>True if message was dequeued.</returns>
        bool TryDequeue(out PendingMessage message);

        /// <summary>
        /// Remove a message from the queue (e.g., after successful send).
        /// </summary>
        /// <param name="requestId">Request ID of the message to remove.</param>
        /// <returns>True if message was found and removed.</returns>
        bool Unregister(string requestId);

        /// <summary>
        /// Set enabled state.
        /// When disabled, clears pending queue and rejects new registrations.
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Remove expired messages from queue (TTL cleanup).
        /// Invokes OnFailed callback for each expired message.
        /// </summary>
        void CleanupExpired();

        /// <summary>
        /// Called when connection is established.
        /// Triggers resend loop.
        /// </summary>
        void OnConnected();

        /// <summary>
        /// Called when connection is lost.
        /// Stops resend loop.
        /// </summary>
        void OnDisconnected();

        /// <summary>
        /// Called when token is refreshed.
        /// May trigger resend for pending messages.
        /// </summary>
        void OnTokenRefreshed();
    }
}
