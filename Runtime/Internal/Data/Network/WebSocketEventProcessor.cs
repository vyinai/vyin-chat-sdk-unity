// -----------------------------------------------------------------------------
//
// WebSocket Event Processor
// Processes WebSocket events and dispatches commands
// Helper utility to simplify command distribution
//
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VyinChatSdk.Internal.Domain.Commands;
using VyinChatSdk.Internal.Domain.Log;

namespace VyinChatSdk.Internal.Data.Network
{
    /// <summary>
    /// Processes WebSocket events and dispatches commands
    /// Helper utility to eliminate large if-else/switch statements
    /// </summary>
    internal class WebSocketEventProcessor
    {
        private readonly Dictionary<CommandType, List<Action<string>>> _commandHandlers
            = new Dictionary<CommandType, List<Action<string>>>();
        private Action<CommandType, string> _defaultHandler;

        /// <summary>
        /// Register a handler for a specific command type
        /// Multiple handlers can be registered for the same command type
        /// </summary>
        public void RegisterHandler(CommandType commandType, Action<string> handler)
        {
            if (!_commandHandlers.ContainsKey(commandType))
            {
                _commandHandlers[commandType] = new List<Action<string>>();
            }
            _commandHandlers[commandType].Add(handler);
            Logger.Debug(LogCategory.Command, $"Registered handler for {commandType}");
        }

        /// <summary>
        /// Set default handler for unregistered commands
        /// </summary>
        public void SetDefaultHandler(Action<CommandType, string> handler)
        {
            _defaultHandler = handler;
        }

        /// <summary>
        /// Process incoming WebSocket command
        /// Dispatches to registered handlers or default handler
        /// </summary>
        public void ProcessCommand(CommandType commandType, string payload)
        {
            Logger.Verbose(LogCategory.Command,
                $"Processing command: {commandType}, payload length: {payload?.Length ?? 0}");

            if (!_commandHandlers.TryGetValue(commandType, out var handlers) || handlers.Count == 0)
            {
                Logger.Debug(LogCategory.Command,
                    $"No registered handler for {commandType}, using default");
                _defaultHandler?.Invoke(commandType, payload);
                return;
            }

            Logger.Debug(LogCategory.Command,
                $"Dispatching {commandType} to {handlers.Count} handler(s)");

            foreach (var handler in handlers)
            {
                SafeInvokeHandler(commandType, handler, payload);
            }
        }

        private void SafeInvokeHandler(CommandType commandType, Action<string> handler, string payload)
        {
            try
            {
                handler.Invoke(payload);
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.Command,
                    $"Handler exception for {commandType}", ex);
            }
        }

        /// <summary>
        /// Clear all registered handlers
        /// </summary>
        public void ClearHandlers()
        {
            _commandHandlers.Clear();
            _defaultHandler = null;
            Logger.Debug(LogCategory.Command, "Cleared all command handlers");
        }
    }
}
