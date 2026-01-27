using System.Collections.Generic;

namespace VyinChatSdk
{
    /// <summary>
    /// Parameters for creating a new group channel
    /// </summary>
    public class VcGroupChannelCreateParams
    {
        /// <summary>
        /// Name of the group channel
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of user IDs to be assigned as channel operators
        /// </summary>
        public List<string> OperatorUserIds { get; set; }

        /// <summary>
        /// List of user IDs to be added as channel members
        /// </summary>
        public List<string> UserIds { get; set; }

        /// <summary>
        /// If true, returns an existing channel with the same members instead of creating a new one
        /// </summary>
        public bool IsDistinct { get; set; }

        /// <summary>
        /// URL of the channel's cover image
        /// </summary>
        public string CoverUrl { get; set; }

        /// <summary>
        /// Custom type for categorizing the channel
        /// </summary>
        public string CustomType { get; set; }

        /// <summary>
        /// Custom data associated with the channel as key-value pairs
        /// </summary>
        public Dictionary<string, string> Data { get; set; }
    }
}