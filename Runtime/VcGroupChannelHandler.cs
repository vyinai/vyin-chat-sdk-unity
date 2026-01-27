using System;

namespace VyinChatSdk
{
    /// <summary>
    /// Handler for receiving group channel events such as new messages and message updates
    /// </summary>
    public class VcGroupChannelHandler
    {
        /// <summary>
        /// Invoked when a new message is received in the channel
        /// </summary>
        public Action<VcGroupChannel, VcBaseMessage> OnMessageReceived;

        /// <summary>
        /// Invoked when an existing message is updated (e.g., streaming AI responses)
        /// </summary>
        public Action<VcGroupChannel, VcBaseMessage> OnMessageUpdated;
    }
}