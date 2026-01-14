using System;

namespace VyinChatSdk
{
    public class VcGroupChannelHandler
    {
        public Action<VcGroupChannel, VcBaseMessage> OnMessageReceived;

        public Action<VcGroupChannel, VcBaseMessage> OnMessageUpdated;
    }
}