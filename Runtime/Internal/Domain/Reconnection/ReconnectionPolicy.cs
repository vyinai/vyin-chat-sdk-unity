// -----------------------------------------------------------------------------
//
// Reconnection Policy
// Implements exponential backoff strategy for reconnection attempts
//
// -----------------------------------------------------------------------------

using System;

namespace VyinChatSdk.Internal.Domain.Reconnection
{
    /// <summary>
    /// Manages reconnection retry logic with exponential backoff.
    /// Formula: delay = min(initial_delay * (multiplier ^ attempt), max_delay)
    /// </summary>
    public class ReconnectionPolicy
    {
        /// <summary>
        /// Initial delay in seconds for the first retry attempt
        /// </summary>
        public float InitialDelay { get; }

        /// <summary>
        /// Multiplier for exponential backoff (typically 2.0 for doubling)
        /// </summary>
        public float BackoffMultiplier { get; }

        /// <summary>
        /// Maximum delay in seconds to cap the exponential growth
        /// </summary>
        public float MaxDelay { get; }

        /// <summary>
        /// Maximum number of retry attempts (-1 for unlimited)
        /// </summary>
        public int MaxRetries { get; }

        /// <summary>
        /// Current retry attempt count (0-based)
        /// </summary>
        public int CurrentAttempt { get; private set; }

        /// <summary>
        /// Last calculated delay value
        /// </summary>
        private float _currentDelay;

        /// <summary>
        /// Creates a new reconnection policy with default values
        /// </summary>
        /// <param name="initialDelay">Initial delay in seconds (default: 1.0)</param>
        /// <param name="backoffMultiplier">Exponential backoff multiplier (default: 2.0)</param>
        /// <param name="maxDelay">Maximum delay cap in seconds (default: 30.0)</param>
        /// <param name="maxRetries">Maximum retry attempts, -1 for unlimited (default: 3)</param>
        public ReconnectionPolicy(
            float initialDelay = 1.0f,
            float backoffMultiplier = 2.0f,
            float maxDelay = 30.0f,
            int maxRetries = 3)
        {
            if (initialDelay < 0)
                throw new ArgumentException("Initial delay cannot be negative", nameof(initialDelay));

            if (backoffMultiplier <= 0)
                throw new ArgumentException("Backoff multiplier must be positive", nameof(backoffMultiplier));

            if (maxDelay < 0)
                throw new ArgumentException("Max delay cannot be negative", nameof(maxDelay));

            InitialDelay = initialDelay;
            BackoffMultiplier = backoffMultiplier;
            MaxDelay = maxDelay;
            MaxRetries = maxRetries;
            CurrentAttempt = 0;
            _currentDelay = 0f;
        }

        /// <summary>
        /// Calculates and returns the next delay for reconnection attempt.
        /// Increments the current attempt counter.
        /// </summary>
        /// <returns>Delay in seconds before the next retry</returns>
        public float GetNextDelay()
        {
            // Formula: delay = min(initial_delay * (multiplier ^ attempt), max_delay)
            float delay = InitialDelay * (float)Math.Pow(BackoffMultiplier, CurrentAttempt);
            _currentDelay = Math.Min(delay, MaxDelay);

            CurrentAttempt++;

            return _currentDelay;
        }

        /// <summary>
        /// Returns the last calculated delay without incrementing attempt counter
        /// </summary>
        /// <returns>Current delay in seconds, or 0 if no attempts have been made</returns>
        public float GetCurrentDelay()
        {
            return _currentDelay;
        }

        /// <summary>
        /// Checks if another retry attempt should be made based on max retries
        /// </summary>
        /// <returns>True if retry should be attempted, false otherwise</returns>
        public bool ShouldRetry()
        {
            // If MaxRetries is -1 (unlimited), always return true
            if (MaxRetries == -1)
                return true;

            // Otherwise, check if current attempt is less than max retries
            return CurrentAttempt < MaxRetries;
        }

        /// <summary>
        /// Resets the retry counter to 0, preserving configuration
        /// </summary>
        public void Reset()
        {
            CurrentAttempt = 0;
            _currentDelay = 0f;
        }
    }
}
