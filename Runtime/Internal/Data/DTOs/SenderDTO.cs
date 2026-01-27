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
}
