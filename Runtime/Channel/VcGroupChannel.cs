using System;
using System.Collections.Generic;
using UnityEngine;
using VyinChatSdk.Internal.Platform;
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

        public static void AddGroupChannelHandler(string handlerId, VcGroupChannelHandler handler)
        {
            if (_handlers.ContainsKey(handlerId))
            {
                Logger.Warning(TAG, $"Handler already exists: {handlerId}");
                return;
            }

            _handlers[handlerId] = handler;

            if (Application.isEditor)
            {
                Logger.Debug(TAG, "Simulate AddGroupChannelHandler in Editor");
                return;
            }

            try
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        AddAndroidGroupChannelHandler(handlerId, handler);
                        break;

                    case RuntimePlatform.IPhonePlayer:
                        AddIOSGroupChannelHandler();
                        break;

                    default:
                        Logger.Warning(TAG, "Platform not supported");
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error(TAG, $"Failed to add group channel handler: {e}", e);
            }
        }

        private static void AddAndroidGroupChannelHandler(string handlerId, VcGroupChannelHandler handler)
        {
            using var androidBridge =
                new AndroidJavaClass("com.gamania.gim.unitybridge.UnityBridge");

            var proxy = new AndroidGroupChannelHandlerProxy(handler);
            androidBridge.CallStatic("addChannelHandler", handlerId, proxy);
        }

        private static void AddIOSGroupChannelHandler()
        {
            Internal.ChatSDKWrapper.OnMessageReceived += OnIOSMessageReceived;
        }

        private static void OnIOSMessageReceived(string eventType, Internal.ChatSDKWrapper.ReceivedMessage message)
        {
            try
            {
                Logger.Debug(TAG, $"iOS message event: {eventType}");
                // TODO: map iOS message to channel if needed
                var vcMessage = From(message);

                if (eventType == "onMessageReceived")
                {
                    TriggerMessageReceived(null, vcMessage);
                }
                else if (eventType == "onMessageUpdated")
                {
                    TriggerMessageUpdated(null, vcMessage);
                }
            }
            catch (Exception e)
            {
                Logger.Error(TAG, "Error handling iOS message: " + e, e);
            }
        }

        private static VcBaseMessage From(Internal.ChatSDKWrapper.ReceivedMessage raw)
        {
            if (raw == null) return null;

            return new VcBaseMessage
            {
                MessageId = raw.messageId,
                Message = raw.message,
                ChannelUrl = raw.channelUrl,
                SenderId = raw.senderId,
                SenderNickname = raw.senderNickname,
                CreatedAt = raw.createdAt
            };
        }

        public static void RemoveGroupChannelHandler(string handlerId)
        {
            _handlers.Remove(handlerId);

            if (Application.isEditor)
            {
                Logger.Debug(TAG, "Simulate RemoveGroupChannelHandler in Editor");
                return;
            }

            try
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        RemoveAndroidChannelHandler(handlerId);
                        break;

                    case RuntimePlatform.IPhonePlayer:
                        RemoveIOSChannelHandler();
                        break;

                    default:
                        Logger.Warning(TAG, "Platform not supported");
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error(TAG, $"Failed to remove group channel handler: {e}", e);
            }
        }

        private static void RemoveAndroidChannelHandler(string handlerId)
        {
            using var androidBridge =
                new AndroidJavaClass("com.gamania.gim.unitybridge.UnityBridge");

            androidBridge.CallStatic("removeChannelHandler", handlerId);
        }

        private static void RemoveIOSChannelHandler()
        {
            Internal.ChatSDKWrapper.OnMessageReceived -= OnIOSMessageReceived;
        }

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

        private class AndroidGroupChannelHandlerProxy : AndroidJavaProxy
        {
            private readonly VcGroupChannelHandler handler;

            public AndroidGroupChannelHandlerProxy(VcGroupChannelHandler handler)
                : base("com.gamania.gim.unitybridge.UnityChannelHandler")
            {
                this.handler = handler;
            }

            public void onMessageReceived(AndroidJavaObject baseChannel, AndroidJavaObject baseMessage)
            {
                try
                {
                    var groupChannel = baseChannel.ToVcGroupChannel();
                    var message = baseMessage.ToVcBaseMessage();
                    Logger.Debug(TAG, "onMessageReceived: groupChannel=" + groupChannel + ", message=" + message);

                    MainThreadDispatcher.Enqueue(() =>
                    {
                        try
                        {
                            handler.OnMessageReceived(groupChannel, message);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(TAG, $"Error in handler.OnMessageReceived: {e.Message}", e);
                        }
                    });
                }
                catch (Exception e)
                {
                    Logger.Error(TAG, $"Error in onMessageReceived: {e.Message}", e);
                }
            }

            public void onMessageUpdated(AndroidJavaObject baseChannel, AndroidJavaObject baseMessage)
            {
                try
                {
                    var groupChannel = baseChannel.ToVcGroupChannel();
                    var message = baseMessage.ToVcBaseMessage();
                    Logger.Debug(TAG, "onMessageUpdated: groupChannel=" + groupChannel + ", message=" + message);

                    MainThreadDispatcher.Enqueue(() =>
                    {
                        try
                        {
                            handler.OnMessageUpdated(groupChannel, message);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(TAG, $"Error in handler.OnMessageUpdated: {e.Message}", e);
                        }
                    });
                }
                catch (Exception e)
                {
                    Logger.Error(TAG, $"Error in onMessageUpdated: {e.Message}", e);
                }
            }
        }
    }
}