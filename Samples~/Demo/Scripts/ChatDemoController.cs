using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VyinChatSdk;
using System.Collections.Generic;

/// <summary>
/// VyinChat SDK Demo - Demonstrates core SDK functionality.
///
/// SDK Usage Flow:
/// 1. Initialize SDK with VyinChat.Init()
/// 2. Connect to server with VyinChat.Connect()
/// 3. Create/Get channel with VcGroupChannelModule
/// 4. Send messages with VcGroupChannel.SendUserMessage()
/// 5. Receive messages via VcGroupChannel.AddGroupChannelHandler()
/// </summary>
public class ChatDemoController : MonoBehaviour
{
    #region Inspector Fields

    [Header("SDK Configuration")]
    [Tooltip("Environment (PROD, DEV, STG, TEST)")]
    [SerializeField] private Environment environment = Environment.PROD;

    [Tooltip("Your App ID (will auto-fill based on environment if empty)")]
    [SerializeField] private string appId = "";

    [Tooltip("User ID for testing")]
    [SerializeField] private string userId = "testuser1";

    [Tooltip("Auth Token (optional, leave empty if not needed)")]
    [SerializeField] private string authToken = "";

    [Header("Channel Configuration")]
    [Tooltip("Channel name to create")]
    [SerializeField] private string channelName = "Unity Test Channel";

    [Tooltip("Bot ID to invite to the channel")]
    [SerializeField] private string botId = "vyin_chat_openai";

    [Tooltip("Other users to invite to the channel (optional)")]
    [SerializeField] private List<string> inviteUserIds = new();

    [Header("Debug Options")]
    [Tooltip("When enabled, pending and sent messages are shown as separate entries")]
    [SerializeField] private bool showAckAsSeparateMessages;

    [Tooltip("When enabled, updated messages are shown as separate entries instead of replacing the original")]
    [SerializeField] private bool showUpdatesAsSeparateMessages;

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect scrollRect;

    #endregion

    #region Private Fields

    private const string HANDLER_ID = "ChatDemoHandler";
    private VcGroupChannel _currentChannel;
    private long _pendingMessageId = -1;
    private readonly Dictionary<long, (string message, string meta1, string meta2)> _messageCache = new();

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        SetupUI();
        InitializeAndConnect();
    }

    private void OnDestroy()
    {
        // Cleanup: Remove message handler
        VcGroupChannel.RemoveGroupChannelHandler(HANDLER_ID);
    }

    #endregion

    #region SDK Step 1: Initialize

    /// <summary>
    /// Step 1: Initialize SDK with app configuration.
    /// </summary>
    private void InitializeAndConnect()
    {
        var (domain, actualAppId) = GetEnvironmentConfig();
        LogInfo($"Environment: {environment}");
        LogInfo($"AppId: {actualAppId}");

        // Step 1: Initialize SDK
        var initParams = new VcInitParams(actualAppId, logLevel: VcLogLevel.Debug);
        VyinChat.Init(initParams);
        LogInfo("SDK Initialized");

        // Proceed to Step 2
        ConnectToServer(domain, actualAppId);
    }

    #endregion

    #region SDK Step 2: Connect

    /// <summary>
    /// Step 2: Connect to VyinChat server.
    /// </summary>
    private void ConnectToServer(string domain, string actualAppId)
    {
        var apiHost = $"https://{actualAppId}.{domain}";
        var wsHost = $"wss://{actualAppId}.{domain}";
        var token = string.IsNullOrEmpty(authToken) ? null : authToken;

        LogInfo($"Connecting as '{userId}'...");
        VyinChat.Connect(userId, token, apiHost, wsHost, OnConnected);
    }

    private void OnConnected(VcUser user, VcException error)
    {
        if (error != null)
        {
            LogError($"Connection failed: {error.Message}");
            return;
        }

        LogInfo($"Connected! Welcome, {user.UserId}!");

        // Proceed to Step 3
        CreateOrGetChannel();
    }

    #endregion

    #region SDK Step 3: Create/Get Channel

    /// <summary>
    /// Step 3: Create or get a group channel.
    /// </summary>
    private void CreateOrGetChannel()
    {
        LogSeparator();
        LogInfo($"Creating channel '{channelName}'...");

        // Build member list
        var members = new List<string> { userId };
        if (!string.IsNullOrEmpty(botId))
        {
            LogInfo($"Creating with botId: '{botId}'...");
            members.Add(botId);
        }
        members.AddRange(inviteUserIds);

        // Create channel params
        var createParams = new VcGroupChannelCreateParams
        {
            Name = channelName,
            UserIds = members,
            OperatorUserIds = new List<string> { userId },
            IsDistinct = true  // Reuse existing channel if members match
        };

        // Create channel
        VcGroupChannelModule.CreateGroupChannel(createParams, OnChannelCreated);
    }

    private void OnChannelCreated(VcGroupChannel channel, VcException error)
    {
        if (error != null)
        {
            LogError($"Failed to create channel: {error.Message}");
            return;
        }

        LogInfo($"Channel created!");

        // Get channel info
        VcGroupChannelModule.GetGroupChannel(channel.ChannelUrl, OnChannelRetrieved);
    }

    private void OnChannelRetrieved(VcGroupChannel channel, VcException error)
    {
        if (error != null)
        {
            LogError($"Failed to get channel: {error.Message}");
            return;
        }

        _currentChannel = channel;
        LogInfo($"Channel ready!");
        LogInfo($"  Name: {channel.Name}");
        LogInfo($"  URL: {channel.ChannelUrl}");

        // Now register message handler (Step 5)
        RegisterGroupChannelMessageHandler();

        LogSeparator();
        LogInfo("Ready to chat! Type a message above.");
    }

    #endregion

    #region SDK Step 4: Send Message

    /// <summary>
    /// Step 4: Send a user message.
    /// </summary>
    private void SendMessage(string text)
    {
        if (_currentChannel == null)
        {
            LogError("No channel available");
            return;
        }

        // Show pending state
        ShowPendingMessage(text);

        // Send message via SDK
        var messageParams = new VcUserMessageCreateParams { Message = text };
        _currentChannel.SendUserMessage(messageParams, OnMessageSent);
    }

    private void OnMessageSent(VcBaseMessage message, VcException error)
    {
        // Clear pending state
        if (_pendingMessageId > 0 && !showAckAsSeparateMessages)
        {
            _messageCache.Remove(_pendingMessageId);
        }
        _pendingMessageId = -1;

        if (error != null)
        {
            LogError($"Failed to send: {error.Message}");
            return;
        }

        if (message != null)
        {
            var displayName = GetDisplayName(message);
            UpdateMessageDisplay(message.MessageId, $"[{displayName}] {message.Message}", "  -> Sent");
        }
    }

    #endregion

    #region SDK Step 5: Receive Messages

    /// <summary>
    /// Step 5: Register handler via VcGroupChannel.AddGroupChannelHandler().
    /// </summary>
    private void RegisterGroupChannelMessageHandler()
    {
        var handler = new VcGroupChannelHandler
        {
            OnMessageReceived = (channel, msg) => HandleMessage(msg, isUpdate: false),
            OnMessageUpdated = (channel, msg) => HandleMessage(msg, isUpdate: true)
        };

        VcGroupChannel.AddGroupChannelHandler(HANDLER_ID, handler);
        LogInfo("GroupChannel handler registered");
    }

    private void HandleMessage(VcBaseMessage message, bool isUpdate)
    {
        // Skip own messages (already shown via OnMessageSent)
        if (message.Sender?.UserId == userId)
            return;

        var eventType = isUpdate ? "Updated" : "Received";

        // Debug log
        Debug.Log($"[Message {eventType}] '{message.Message}' | Type: '{message.CustomType}' | Done: {message.Done}");

        // Display in UI
        var displayName = GetDisplayName(message);
        var meta1 = $"  -> {eventType} | Type: '{message.CustomType}' | Done: {message.Done}";
        var meta2 = $"  -> Data: '{message.Data ?? ""}'";

        // Generate unique ID for separate display mode
        var displayId = (showUpdatesAsSeparateMessages && isUpdate)
            ? message.MessageId * 1000 + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000
            : message.MessageId;

        UpdateMessageDisplay(displayId, $"[{displayName}] {message.Message}", meta1, meta2);
    }

    #endregion

    #region UI Helpers

    private void SetupUI()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
    }

    private void OnSendButtonClicked()
    {
        if (inputField == null) return;

        var text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        inputField.text = "";
        inputField.ActivateInputField();

        if (_currentChannel == null)
        {
            LogInfo("No channel available. Creating one...");
            CreateOrGetChannel();
            return;
        }

        SendMessage(text);
    }

    private void ShowPendingMessage(string text)
    {
        _pendingMessageId = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        UpdateMessageDisplay(_pendingMessageId, $"[{userId}] {text}", "  -> Sending...");
    }

    private static string GetDisplayName(VcBaseMessage message)
    {
        if (message?.Sender == null) return "Unknown";
        return string.IsNullOrEmpty(message.Sender.Nickname)
            ? message.Sender.UserId
            : message.Sender.Nickname;
    }

    #endregion

    #region Logging

    private void LogInfo(string message)
    {
        var formatted = $"[Demo] {message}";
        Debug.Log(formatted);
        AppendToLog(formatted);
    }

    private void LogError(string message)
    {
        var formatted = $"[Demo] ERROR: {message}";
        Debug.LogError(formatted);
        AppendToLog(formatted);
    }

    private void LogSeparator()
    {
        AppendToLog("────────────────────────────────");
    }

    private void AppendToLog(string text)
    {
        if (logText != null)
        {
            logText.text += text + "\n";
            ScrollToBottom();
        }
    }

    private void UpdateMessageDisplay(long messageId, string message, string meta1 = null, string meta2 = null)
    {
        _messageCache[messageId] = (message, meta1, meta2);

        if (logText == null) return;

        var lines = new List<string>();
        foreach (var entry in _messageCache.Values)
        {
            lines.Add(entry.message);
            if (!string.IsNullOrEmpty(entry.meta1))
                lines.Add(entry.meta1);
            if (!string.IsNullOrEmpty(entry.meta2))
                lines.Add(entry.meta2);
        }

        logText.text = string.Join("\n", lines);
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            StartCoroutine(ScrollToBottomCoroutine());
        }
    }

    private System.Collections.IEnumerator ScrollToBottomCoroutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    #endregion

    #region Environment Configuration

    public enum Environment { PROD, DEV, STG, TEST }

    private (string domain, string appId) GetEnvironmentConfig()
    {
        var domain = environment switch
        {
            Environment.PROD => "gamania.chat",
            Environment.DEV => "dev.gim.beango.com",
            Environment.STG => "stg.gim.beango.com",
            Environment.TEST => "test.gim.beango.com",
            _ => "gamania.chat"
        };

        var resolvedAppId = string.IsNullOrEmpty(appId) ? GetDefaultAppId() : appId;

        return (domain, resolvedAppId);
    }

    private string GetDefaultAppId() => environment switch
    {
        Environment.PROD => "adb53e88-4c35-469a-a888-9e49ef1641b2",
        Environment.DEV => "b553fe2f-4975-4d22-934f-f4aa02167e19",
        Environment.STG => "9c839b9c-0be9-4e98-be4c-1f06345bdb7d",
        Environment.TEST => "1ba5d5e3-73ab-4b47-9b1d-ca1ce967fac2",
        _ => "adb53e88-4c35-469a-a888-9e49ef1641b2"
    };

    #endregion
}
