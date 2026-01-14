// ChatSDKWrapper.cs
// C# Wrapper for ChatSDK iOS Native Bridge
// This file should be placed in Assets/Plugins/iOS/

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VyinChatSdk;
using VyinChatSdk.Internal.Platform;

namespace VyinChatSdk.Internal
{
    public class ChatSDKWrapper
    {
        #region iOS Native Bridge

#if UNITY_IOS && !UNITY_EDITOR

    // API callback registration
    [DllImport("__Internal")]
    private static extern void ChatSDK_SetCallback(CallbackDelegate callback);

    // Message event callback registration (for receiving messages)
    [DllImport("__Internal")]
    private static extern void ChatSDK_SetMessageCallback(MessageCallbackDelegate callback);

    // Configuration
    [DllImport("__Internal")]
    private static extern void ChatSDK_SetConfiguration(string appId, string domain);

    [DllImport("__Internal")]
    private static extern void ChatSDK_ResetConfiguration();

    [DllImport("__Internal")]
    private static extern void ChatSDK_Initialize(string appId);

    [DllImport("__Internal")]
    private static extern void ChatSDK_Connect(string userId, string authToken, int callbackId);

    [DllImport("__Internal")]
    private static extern void ChatSDK_CreateGroupChannel(string channelName, string userIdsJson, string operatorUserIdsJson, bool isDistinct, int callbackId);

    [DllImport("__Internal")]
    private static extern void ChatSDK_InviteUsers(string channelUrl, string userIdsJson, int callbackId);

    [DllImport("__Internal")]
    private static extern void ChatSDK_SendMessage(string channelUrl, string message, int callbackId);

#endif

        #endregion

        #region Callback Handling

        // Delegate for API callbacks
        private delegate void CallbackDelegate(int callbackId, string result, string error);

        // Delegate for message events (eventType, messageJson)
        private delegate void MessageCallbackDelegate(string eventType, string messageJson);

        // Dictionary to store pending callbacks
        private static System.Collections.Generic.Dictionary<int, Action<string, string>> callbacks =
            new System.Collections.Generic.Dictionary<int, Action<string, string>>();

        private static int nextCallbackId = 0;

        // Event fired when a message is received
        public static event Action<string, ReceivedMessage> OnMessageReceived;

        // Register a callback and return its ID
        private static int RegisterCallback(Action<string, string> callback)
        {
            int callbackId = nextCallbackId++;
            callbacks[callbackId] = callback;
            return callbackId;
        }

        [AOT.MonoPInvokeCallback(typeof(CallbackDelegate))]
        private static void OnNativeCallback(int callbackId, string result, string error)
        {
            Debug.Log($"[ChatSDK] Callback {callbackId}: result={result}, error={error}");

            if (callbacks.TryGetValue(callbackId, out var callback))
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    callback?.Invoke(result, error);
                });
                callbacks.Remove(callbackId);
            }
        }

        // Handle native message events (called from iOS)
        [AOT.MonoPInvokeCallback(typeof(MessageCallbackDelegate))]
        private static void OnNativeMessageCallback(string eventType, string messageJson)
        {
            Debug.Log($"[ChatSDK] Message Event: {eventType}, data: {messageJson}");

            if (eventType == "onMessageReceived" || eventType == "onMessageUpdated")
            {
                try
                {
                    var message = JsonUtility.FromJson<ReceivedMessage>(messageJson);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        OnMessageReceived?.Invoke(eventType, message);
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ChatSDK] Failed to parse message: {ex.Message}");
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set custom configuration for ChatSDK
        /// Call this BEFORE Initialize() to override default settings
        /// </summary>
        /// <param name="appId">Application ID (optional, pass null to keep current)</param>
        /// <param name="domain">Environment domain (e.g., "dev.gim.beango.com", "stg.gim.beango.com", "gamania.chat")</param>
        public static void SetConfiguration(string appId, string domain)
        {
#if UNITY_IOS && !UNITY_EDITOR
        ChatSDK_SetConfiguration(appId, domain);
        Debug.Log($"[ChatSDK] Configuration set - appId: {appId}, domain: {domain}");
#else
            Debug.LogWarning("[ChatSDK] SetConfiguration only supported on iOS device");
#endif
        }

        /// <summary>
        /// Reset configuration to default values (PROD environment)
        /// </summary>
        public static void ResetConfiguration()
        {
#if UNITY_IOS && !UNITY_EDITOR
        ChatSDK_ResetConfiguration();
        Debug.Log("[ChatSDK] Configuration reset to defaults");
#else
            Debug.LogWarning("[ChatSDK] ResetConfiguration only supported on iOS device");
#endif
        }

        /// <summary>
        /// Initialize ChatSDK (call once at game start)
        /// </summary>
        public static void Initialize(string appId)
        {
#if UNITY_IOS && !UNITY_EDITOR
        // Register API callback
        ChatSDK_SetCallback(OnNativeCallback);
        // Register message event callback
        ChatSDK_SetMessageCallback(OnNativeMessageCallback);
        // Initialize SDK
        ChatSDK_Initialize(appId);
        Debug.Log($"[ChatSDK] Initialized with appId: {appId}");
#else
            Debug.LogWarning("[ChatSDK] Only supported on iOS device");
#endif
        }

        /// <summary>
        /// Connect to ChatSDK
        /// </summary>
        public static void Connect(string userId, string authToken, Action<string, string> callback)
        {
#if UNITY_IOS && !UNITY_EDITOR
        int callbackId = RegisterCallback(callback);
        ChatSDK_Connect(userId, authToken, callbackId);
#else
            Debug.LogWarning("[ChatSDK] Only supported on iOS device");
            callback?.Invoke(null, "Not supported on this platform");
#endif
        }

        /// <summary>
        /// Create a group channel (low-level method)
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="userIds">Array of user IDs</param>
        /// <param name="operatorUserIds">Array of operator user IDs</param>
        /// <param name="isDistinct">Whether to create a distinct channel</param>
        /// <param name="callback">Callback function (result, error)</param>
        private static void CreateGroupChannel(string channelName, string[] userIds, string[] operatorUserIds, bool isDistinct, Action<string, string> callback)
        {
#if UNITY_IOS && !UNITY_EDITOR
        // Convert to JSON array
        string userIdsJson = "[\"" + string.Join("\",\"", userIds) + "\"]";
        string operatorUserIdsJson = (operatorUserIds != null && operatorUserIds.Length > 0)
            ? "[\"" + string.Join("\",\"", operatorUserIds) + "\"]"
            : null;

        int callbackId = RegisterCallback(callback);
        ChatSDK_CreateGroupChannel(channelName, userIdsJson, operatorUserIdsJson, isDistinct, callbackId);
#else
            Debug.LogWarning("[ChatSDK] Only supported on iOS device");
            callback?.Invoke(null, "Not supported on this platform");
#endif
        }

        /// <summary>
        /// Create a group channel
        /// </summary>
        /// <param name="channelCreateParams">Channel creation parameters</param>
        /// <param name="callback">Callback function (VcGroupChannel, error)</param>
        public static void CreateGroupChannel(VcGroupChannelCreateParams channelCreateParams, VcGroupChannelCallbackHandler callback)
        {
            if (channelCreateParams == null)
            {
                callback?.Invoke(null, "channelCreateParams is null");
                return;
            }

            string channelName = channelCreateParams.Name;
            string[] userIds = (channelCreateParams.UserIds ?? new System.Collections.Generic.List<string>()).ToArray();
            string[] operatorUserIds = (channelCreateParams.OperatorUserIds ?? new System.Collections.Generic.List<string>()).ToArray();
            bool isDistinct = channelCreateParams.IsDistinct;

            // Internal handler to convert string result to VcGroupChannel
            void handler(string result, string error)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    callback?.Invoke(null, error);
                    Debug.LogError("[ChatSDK] Error calling CreateGroupChannel: " + error);
                    return;
                }

                // Parse JSON result to extract channelUrl and name
                try
                {
                    // Simple JSON parsing (assuming format: {"channelUrl":"xxx","name":"yyy"})
                    var channelUrl = ExtractJsonValue(result, "channelUrl");
                    var name = ExtractJsonValue(result, "name");

                    var groupChannel = new VcGroupChannel
                    {
                        ChannelUrl = channelUrl,
                        Name = name
                    };

                    callback?.Invoke(groupChannel, null);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[ChatSDK] Failed to parse channel result: " + e);
                    callback?.Invoke(null, e.Message);
                }
            }

            CreateGroupChannel(channelName, userIds, operatorUserIds, isDistinct, handler);
        }

        // Simple JSON value extractor (for basic key-value pairs)
        private static string ExtractJsonValue(string json, string key)
        {
            string searchKey = $"\"{key}\":\"";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) return null;

            startIndex += searchKey.Length;
            int endIndex = json.IndexOf("\"", startIndex);
            if (endIndex == -1) return null;

            return json.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Invite users to a channel
        /// </summary>
        /// <param name="channelUrl">Channel URL</param>
        /// <param name="userIds">Array of user IDs to invite</param>
        /// <param name="callback">Callback function (result, error)</param>
        public static void InviteUsers(string channelUrl, string[] userIds, Action<string, string> callback)
        {
#if UNITY_IOS && !UNITY_EDITOR
        string userIdsJson = "[\"" + string.Join("\",\"", userIds) + "\"]";

        int callbackId = RegisterCallback(callback);
        ChatSDK_InviteUsers(channelUrl, userIdsJson, callbackId);
#else
            Debug.LogWarning("[ChatSDK] Only supported on iOS device");
            callback?.Invoke(null, "Not supported on this platform");
#endif
        }

        /// <summary>
        /// Send a message to a channel
        /// </summary>
        /// <param name="channelUrl">Channel URL</param>
        /// <param name="message">Message content</param>
        /// <param name="callback">Callback function (result, error)</param>
        public static void SendMessage(string channelUrl, string message, Action<string, string> callback)
        {
#if UNITY_IOS && !UNITY_EDITOR
        int callbackId = RegisterCallback(callback);
        ChatSDK_SendMessage(channelUrl, message, callbackId);
#else
            Debug.LogWarning("[ChatSDK] Only supported on iOS device");
            callback?.Invoke(null, "Not supported on this platform");
#endif
        }

        #endregion

        #region Data Models

        /// <summary>
        /// Received message data structure
        /// </summary>
        [Serializable]
        public class ReceivedMessage
        {
            public long messageId;
            public string message;
            public string channelUrl;
            public string senderId;
            public string senderNickname;
            public long createdAt;
        }

        #endregion
    }
}

// ===== Usage Example =====
/*
public class GameChatExample : MonoBehaviour
{
    void Start()
    {
        // 1. Initialize
        ChatSDKWrapper.Initialize("YOUR_APP_ID");

        // 2. Subscribe to message events
        ChatSDKWrapper.OnMessageReceived += HandleMessageReceived;

        // 3. Connect
        ChatSDKWrapper.Connect("user123", null, (result, error) => {
            if (error != null) {
                Debug.LogError($"Connect failed: {error}");
                return;
            }
            Debug.Log($"Connected: {result}");

            // 4. Create channel
            CreateChannel();
        });
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        ChatSDKWrapper.OnMessageReceived -= HandleMessageReceived;
    }

    void HandleMessageReceived(string eventType, ChatSDKWrapper.ReceivedMessage message)
    {
        Debug.Log($"Message received from {message.senderNickname}: {message.message}");
        // Update UI or process message here
    }

    void CreateChannel()
    {
        var channelParams = new VcGroupChannelCreateParams
        {
            Name = "Game Chat",
            UserIds = new System.Collections.Generic.List<string> { "user456", "user789" },
            IsDistinct = true  // Ensure unique channel for same members
        };

        ChatSDKWrapper.CreateGroupChannel(channelParams, (channel, error) => {
            if (error != null) {
                Debug.LogError($"Create channel failed: {error}");
                return;
            }
            Debug.Log($"Channel created: {channel.Name}, URL: {channel.ChannelUrl}");
        });
    }

    void InviteToChannel(string channelUrl)
    {
        string[] newUsers = new string[] { "user999" };
        ChatSDKWrapper.InviteUsers(channelUrl, newUsers, (result, error) => {
            if (error != null) {
                Debug.LogError($"Invite failed: {error}");
                return;
            }
            Debug.Log($"Users invited: {result}");
        });
    }

    void SendChatMessage(string channelUrl)
    {
        ChatSDKWrapper.SendMessage(channelUrl, "Hello from Unity!", (result, error) => {
            if (error != null) {
                Debug.LogError($"Send message failed: {error}");
                return;
            }
            Debug.Log($"Message sent: {result}");
        });
    }
}
*/
