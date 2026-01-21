namespace VyinChatSdk
{
    public delegate void VcUserHandler(VcUser inUser, VcException error);

    public delegate void VcUserMessageHandler(VcBaseMessage inUserMessage, VcException error);

    public delegate void VcGroupChannelCallbackHandler(VcGroupChannel inGroupChannel, VcException error);
}