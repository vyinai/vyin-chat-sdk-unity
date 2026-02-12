namespace VyinChatSdk.Internal.Domain.Commands
{
    public interface ICommandProtocol
    {
        /// <summary>Create a serialized command with a generated req_id.</summary>
        (string reqId, string serialized) BuildCommand(CommandType commandType, object payload);
    }
}

