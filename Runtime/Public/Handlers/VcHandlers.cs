namespace VyinChatSdk
{
    /// <summary>
    /// Callback handler for user-related operations
    /// </summary>
    /// <param name="user">The user object if the operation succeeded, null otherwise</param>
    /// <param name="error">The exception if the operation failed, null otherwise</param>
    public delegate void VcUserHandler(VcUser user, VcException error);

    /// <summary>
    /// Callback handler for message-related operations
    /// </summary>
    /// <param name="message">The message object if the operation succeeded, null otherwise</param>
    /// <param name="error">The exception if the operation failed, null otherwise</param>
    public delegate void VcUserMessageHandler(VcUserMessage message, VcException error);

    /// <summary>
    /// Callback handler for group channel operations
    /// </summary>
    /// <param name="channel">The group channel object if the operation succeeded, null otherwise</param>
    /// <param name="error">The exception if the operation failed, null otherwise</param>
    public delegate void VcGroupChannelCallbackHandler(VcGroupChannel channel, VcException error);
}
