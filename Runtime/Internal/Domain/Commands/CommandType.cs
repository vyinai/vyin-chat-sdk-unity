// -----------------------------------------------------------------------------
//
// Command Type Enum - Domain Layer
// Pure C# - NO Unity dependencies
//
// -----------------------------------------------------------------------------

namespace VyinChatSdk.Internal.Domain.Commands
{
    /// <summary>
    /// Command types for WebSocket protocol
    /// Based on server command specification
    /// </summary>
    public enum CommandType
    {
        EROR,  // Error
        LOGI,  // Login
        MESG,  // Message
        FILE,  // File
        EXIT,  // Exit
        READ,  // Read
        MEDI,  // Message Edit
        FEDI,  // File Edit
        ENTR,  // Enter
        BRDM,  // Broadcast Message
        ADMM,  // Admin Message
        AEDI,  // Admin Edit
        TPST,  // Typing Start
        TPEN,  // Typing End
        MTIO,  // Message Timeout
        SYEV,  // System Event
        USEV,  // User Event
        DELM,  // Delete Message
        LEAV,  // Leave
        UNRD,  // Unread
        DLVR,  // Delivered
        NOOP,  // No Operation
        MRCT,  // Message Reaction
        PING,  // Ping
        PONG,  // Pong
        JOIN,  // Join
        MTHD,  // Message Thread
        EXPR,  // Expression
        MCNT,  // Message Count
        NONE,  // None
        CUEV,  // Custom Event
        PEDI,  // Poll Edit
        VOTE,  // Vote
        SUMM,  // Summary
        MREV,  // Message Revoke
        FREV   // File Revoke
    }

    /// <summary>
    /// Extension methods for CommandType
    /// </summary>
    public static class CommandTypeExtensions
    {
        /// <summary>
        /// Check if this command type requires acknowledgement
        /// </summary>
        public static bool IsAckRequired(this CommandType commandType)
        {
            switch (commandType)
            {
                case CommandType.LOGI:
                case CommandType.MESG:
                case CommandType.FILE:
                case CommandType.EXIT:
                case CommandType.READ:
                case CommandType.MEDI:
                case CommandType.FEDI:
                case CommandType.ENTR:
                case CommandType.PEDI:
                case CommandType.VOTE:
                case CommandType.SUMM:
                case CommandType.MREV:
                case CommandType.FREV:
                    return true;
                default:
                    return false;
            }
        }
    }
}
