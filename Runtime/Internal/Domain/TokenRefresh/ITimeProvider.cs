// -----------------------------------------------------------------------------
//
// Time Provider Interface
// Abstraction for time to enable testing of timeout behavior
//
// -----------------------------------------------------------------------------

namespace VyinChatSdk.Internal.Domain.TokenRefresh
{
    /// <summary>
    /// Interface for time provider to enable testing
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Current time in seconds (similar to Unity's Time.realtimeSinceStartup)
        /// </summary>
        float CurrentTime { get; }
    }

    /// <summary>
    /// Default time provider using system time
    /// </summary>
    public class SystemTimeProvider : ITimeProvider
    {
        public float CurrentTime =>
            (float)System.DateTime.UtcNow.Subtract(System.DateTime.UnixEpoch).TotalSeconds;
    }
}
