namespace VyinChatSdk.Internal.Domain.Log
{
    /// <summary>
    /// Internal module categories for logging classification
    /// </summary>
    internal enum LogCategory
    {
        Connection,  // Connection-related logs
        Http,        // HTTP client logs
        WebSocket,   // WebSocket client logs
        Collection,  // Data collection/caching
        Command,     // WebSocket command processing
        Message,     // Message operations
        Channel,     // Channel operations
        User         // User operations
    }
}
