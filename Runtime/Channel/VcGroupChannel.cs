using System;
using System.Collections.Generic;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk
{
    public class VcGroupChannel
    {
        private const string TAG = "VcGroupChannel";

        public string ChannelUrl { get; set; }
        public string Name { get; set; }
        public long CreatedAt { get; set; }
        public VcBaseMessage LastMessage { get; set; }
        public List<VcUser> Members { get; set; }
        public int MemberCount { get; set; }
        public string Data { get; set; }
        public string CoverUrl { get; set; }
        public string CustomType { get; set; }


        private static readonly Dictionary<string, VcGroupChannelHandler> _handlers = new();

        /// <summary>
        /// Add a group channel handler to receive message events.
        /// The handler will receive OnMessageReceived for new MESG commands
        /// and OnMessageUpdated for MEDI commands (streaming AI messages).
        /// </summary>
        /// <param name="handlerId">Unique identifier for this handler</param>
        /// <param name="handler">Handler containing callback functions</param>
        public static void AddGroupChannelHandler(string handlerId, VcGroupChannelHandler handler)
        {
            if (_handlers.ContainsKey(handlerId))
            {
                Logger.Warning(TAG, $"Handler already exists: {handlerId}");
                return;
            }

            _handlers[handlerId] = handler;
            Logger.Debug(TAG, $"Added group channel handler: {handlerId}");
        }

        /// <summary>
        /// Remove a group channel handler.
        /// </summary>
        /// <param name="handlerId">Unique identifier of the handler to remove</param>
        public static void RemoveGroupChannelHandler(string handlerId)
        {
            if (_handlers.Remove(handlerId))
            {
                Logger.Debug(TAG, $"Removed group channel handler: {handlerId}");
            }
            else
            {
                Logger.Warning(TAG, $"Handler not found: {handlerId}");
            }
        }

        /// <summary>
        /// Trigger message received event for all registered handlers.
        /// Called internally when a new message is received via WebSocket.
        /// </summary>
        /// <param name="channel">The channel where the message was received (may be null)</param>
        /// <param name="message">The received message</param>
        public static void TriggerMessageReceived(VcGroupChannel channel, VcBaseMessage message)
        {
            foreach (var handler in _handlers.Values)
            {
                try
                {
                    handler.OnMessageReceived?.Invoke(channel, message);
                }
                catch (Exception e)
                {
                    Logger.Error(TAG, $"Error in handler: {e.Message}", e);
                }
            }
        }

        /// <summary>
        /// Trigger message updated event for all registered handlers.
        /// Called internally when a message is updated via WebSocket.
        /// </summary>
        /// <param name="channel">The channel where the message was updated (may be null)</param>
        /// <param name="message">The updated message</param>
        public static void TriggerMessageUpdated(VcGroupChannel channel, VcBaseMessage message)
        {
            foreach (var handler in _handlers.Values)
            {
                try
                {
                    handler.OnMessageUpdated?.Invoke(channel, message);
                }
                catch (Exception e)
                {
                    Logger.Error(TAG, $"Error in handler: {e.Message}", e);
                }
            }
        }
    }
}
