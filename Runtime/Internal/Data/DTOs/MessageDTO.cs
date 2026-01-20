namespace VyinChatSdk.Internal.Data.DTOs
{
    public class SenderDTO
    {
        public string user_id { get; set; }
        public string guest_id { get; set; }
        public string nickname { get; set; }
        public string name { get; set; }  // alias of nickname
        public string profile_url { get; set; }
        public string image { get; set; }  // alias of profile_url
        public string role { get; set; }
    }

    public class MessageDTO
    {
        public long message_id { get; set; }
        public long msg_id { get; set; }  // alias of message_id
        public string message { get; set; }
        public string channel_url { get; set; }
        public long created_at { get; set; }
        public long ts { get; set; }  // alias of created_at
        public bool done { get; set; }
        public string custom_type { get; set; }
        public string data { get; set; }
        public string request_id { get; set; }
        public string req_id { get; set; }  // alias of request_id
        public SenderDTO user { get; set; }
    }
}
