namespace VyinChatSdk
{
    public delegate void VcUserHandler(VcUser inUser, string error);

    public delegate void VcUserMessageHandler(VcBaseMessage inUserMessage, string error);

    public delegate void VcGroupChannelCallbackHandler(VcGroupChannel inGroupChannel, string error);
}