namespace VyinChatSdk
{
    public class VcBaseMessage
    {
        public long MessageId { get; set; }
        public string Message { get; set; }
        public string ChannelUrl { get; set; }
        public long CreatedAt { get; set; }

        /// <summary>
        /// Indicates whether the streaming message is complete.
        /// Only applicable for MEDI (Message Edit) events from streaming messages (e.g., AI responses).
        /// When true, this is the final version of the message.
        /// </summary>
        public bool Done { get; set; }

        /// <summary>
        /// Custom type for categorizing messages.
        /// Can be used to distinguish different message types (e.g., "text", "image", "ai_response").
        /// </summary>
        public string CustomType { get; set; }

        /// <summary>
        /// Additional custom data in JSON string format.
        /// Can be used to pass extra information with the message.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Request ID for tracking message requests.
        /// </summary>
        public string ReqId { get; set; }

        /// <summary>
        /// Sender information including role.
        /// </summary>
        public VcSender Sender { get; set; }
    }
}