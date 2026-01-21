using System.Collections.Generic;

namespace VyinChatSdk.Internal.Data.Network
{
    /// <summary>
    /// HTTP Response wrapper
    /// </summary>
    public class HttpResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
        public string Error { get; set; }

        public HttpResponse()
        {
            Headers = new Dictionary<string, string>();
        }
    }
}
