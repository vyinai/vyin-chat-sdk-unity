namespace VyinChatSdk
{
    /// <summary>
    /// Parameters for creating a user message
    /// Used when sending messages to a channel
    /// </summary>
    public class VcUserMessageCreateParams
    {
        /// <summary>
        /// Message text content
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Custom data attached to the message (JSON string)
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Custom message type for categorization
        /// </summary>
        public string CustomType { get; set; }
    }
}
