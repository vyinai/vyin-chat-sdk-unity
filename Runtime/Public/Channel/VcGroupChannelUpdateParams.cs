namespace VyinChatSdk
{
    /// <summary>
    /// Parameters for updating a group channel
    /// Used when modifying channel properties
    /// </summary>
    public class VcGroupChannelUpdateParams
    {
        /// <summary>
        /// Channel name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Channel cover image URL
        /// </summary>
        public string CoverUrl { get; set; }

        /// <summary>
        /// Custom data attached to the channel (JSON string)
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Custom channel type for categorization
        /// </summary>
        public string CustomType { get; set; }
    }
}
