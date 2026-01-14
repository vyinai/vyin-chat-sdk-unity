using System.Collections.Generic;

namespace VyinChatSdk
{
    public class VcGroupChannelCreateParams
    {
        public string Name { get; set; }
        public List<string> OperatorUserIds { get; set; }
        public List<string> UserIds { get; set; }
        public bool IsDistinct { get; set; }
        public string CoverUrl { get; set; }
        public string CustomType { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}