namespace VyinChatSdk.Internal.Domain.Models
{
    public class MessageBO
    {
        public long MessageId { get; set; }
        public string Message { get; set; }
        public string ChannelUrl { get; set; }
        public long CreatedAt { get; set; }
        public bool Done { get; set; }
        public string CustomType { get; set; }
        public string Data { get; set; }
        public string ReqId { get; set; }
        public SenderBO Sender { get; set; }
    }
}
