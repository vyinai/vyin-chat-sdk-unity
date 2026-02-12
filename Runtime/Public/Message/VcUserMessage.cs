namespace VyinChatSdk
{
    /// <summary>
    /// Represents a user message in a channel.
    /// </summary>
    public class VcUserMessage : VcBaseMessage
    {
        public static VcUserMessage FromBase(VcBaseMessage message)
        {
            if (message == null) return null;
            if (message is VcUserMessage userMessage) return userMessage;

            return new VcUserMessage
            {
                MessageId = message.MessageId,
                Message = message.Message,
                ChannelUrl = message.ChannelUrl,
                CreatedAt = message.CreatedAt,
                Done = message.Done,
                CustomType = message.CustomType,
                Data = message.Data,
                ReqId = message.ReqId,
                Sender = message.Sender,
                SendingStatus = message.SendingStatus,
                ErrorCode = message.ErrorCode
            };
        }
    }
}
