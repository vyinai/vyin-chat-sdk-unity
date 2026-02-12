namespace VyinChatSdk
{
    /// <summary>
    /// Internal extension methods for VcErrorCode related to message reliability.
    /// External users should use VcBaseMessage.IsResendable property instead.
    /// </summary>
    internal static class VcErrorCodeExtensions
    {
        /// <summary>
        /// Check if error code is auto-resendable (network/connection issues).
        /// These errors will trigger automatic retry on reconnection.
        /// </summary>
        internal static bool IsAutoResendable(this VcErrorCode code)
        {
            return code switch
            {
                VcErrorCode.ConnectionRequired => true,
                VcErrorCode.WebSocketConnectionClosed => true,
                VcErrorCode.WebSocketConnectionFailed => true,
                VcErrorCode.NetworkError => true,
                VcErrorCode.RequestFailed => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if error code is user-resendable (can be manually retried by user).
        /// Used internally by VcBaseMessage.IsResendable property.
        /// </summary>
        internal static bool IsResendable(this VcErrorCode code)
        {
            if (code.IsAutoResendable())
                return true;

            return code switch
            {
                VcErrorCode.AckTimeout => true,
                VcErrorCode.PendingError => true,
                _ => false
            };
        }
    }
}
