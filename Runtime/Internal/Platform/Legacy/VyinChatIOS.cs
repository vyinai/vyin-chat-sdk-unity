using UnityEngine;
using VyinChatSdk;
using VyinChatSdk.Internal.Platform;

namespace VyinChatSdk.Internal
{
    internal class VyinChatIOS : IVyinChat
    {
        public VyinChatIOS()
        {
        }

        public void Init(VcInitParams initParams)
        {
            try
            {
                ChatSDKWrapper.Initialize(initParams.AppId);
                Debug.Log("[VyinChat] iOS ChatSDK initialized with AppId=" + initParams.AppId);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error calling iOS Initialize: " + e);
            }
        }

        public void Connect(string userId, string authToken, string apiHost, string wsHost, VcUserHandler callback)
        {
            try
            {
                ChatSDKWrapper.Connect(userId, authToken, (result, error) =>
                {
                    VcUser user = null;
                    if (!string.IsNullOrEmpty(result))
                    {
                        user = new VcUser { UserId = userId };
                    }

                    MainThreadDispatcher.Enqueue(() =>
                    {
                        callback?.Invoke(user, error);
                    });
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error calling iOS Connect: " + e);
                callback?.Invoke(null, e.Message);
            }
        }

        public void SendMessage(string channelUrl, string message, VcUserMessageHandler callback)
        {
            Debug.Log($"[VyinChatIOS] SendMessage channelUrl:{channelUrl}, message:{message}");

            try
            {
                ChatSDKWrapper.SendMessage(channelUrl, message, (result, error) =>
                {
                    VcBaseMessage baseMessage = null;
                    if (!string.IsNullOrEmpty(result) && string.IsNullOrEmpty(error))
                    {
                        // Parse the result to create VcBaseMessage
                        // For now, create a simple message object
                        baseMessage = new VcBaseMessage
                        {
                            ChannelUrl = channelUrl,
                            Message = message,
                            CreatedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };
                    }

                    MainThreadDispatcher.Enqueue(() =>
                    {
                        callback?.Invoke(baseMessage, error);
                    });
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VyinChatIOS] Error calling iOS SendMessage: {e}");
                callback?.Invoke(null, e.Message);
            }
        }
    }
}