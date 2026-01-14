// -----------------------------------------------------------------------------
//
// LOGI Command Data Structures - Domain Layer
// Pure C# - NO Unity dependencies
//
// -----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace VyinChatSdk.Internal.Domain.Commands
{
    /// <summary>
    /// LOGI command response from server
    /// Format: LOGI{json}
    /// </summary>
    public class LogiCommand
    {
        /// <summary>
        /// Session key for authenticated communication
        /// JSON field: "key"
        /// </summary>
        [JsonProperty("key")]
        public string SessionKey { get; set; }

        /// <summary>
        /// Error flag indicating authentication failure
        /// JSON field: "error"
        /// </summary>
        [JsonProperty("error")]
        public bool? Error { get; set; }

        /// <summary>
        /// Encryption key (if encryption is enabled)
        /// JSON field: "ekey"
        /// </summary>
        [JsonProperty("ekey")]
        public string EKey { get; set; }

        /// <summary>
        /// Ping interval in seconds (default: 15)
        /// JSON field: "ping_interval"
        /// </summary>
        [JsonProperty("ping_interval")]
        public int PingInterval { get; set; } = 15;

        /// <summary>
        /// Pong timeout in seconds (default: 5)
        /// JSON field: "pong_timeout"
        /// </summary>
        [JsonProperty("pong_timeout")]
        public int PongTimeout { get; set; } = 5;

        /// <summary>
        /// Last connected timestamp
        /// JSON field: "login_ts"
        /// </summary>
        [JsonProperty("login_ts")]
        public long LastConnected { get; set; }

        /// <summary>
        /// Unread count information
        /// JSON field: "unread_count"
        /// </summary>
        [JsonProperty("unread_count")]
        public UnreadCountModel UnreadCount { get; set; }

        /// <summary>
        /// Check if LOGI response indicates success
        /// </summary>
        public bool IsSuccess()
        {
            return Error != true && !string.IsNullOrEmpty(SessionKey);
        }
    }

    /// <summary>
    /// Unread count model
    /// </summary>
    public class UnreadCountModel
    {
        /// <summary>
        /// Total unread count
        /// JSON field: "all"
        /// </summary>
        [JsonProperty("all")]
        public long All { get; set; }

        /// <summary>
        /// Timestamp of unread count
        /// JSON field: "ts"
        /// </summary>
        [JsonProperty("ts")]
        public long Ts { get; set; }
    }
}
