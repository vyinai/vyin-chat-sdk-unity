using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VyinChatSdk;
using System;
using System.Reflection;
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
public class ChatDemoController : MonoBehaviour, IVcSessionHandler
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
    [Tooltip("Enable automatic message resend on reconnection")]
    [SerializeField] private bool enableAutoResend = true;

    [Tooltip("When enabled, pending and sent messages are shown as separate entries")]
    [SerializeField] private bool showAckAsSeparateMessages;

    [Tooltip("When enabled, updated messages are shown as separate entries instead of replacing the original")]
    [SerializeField] private bool showUpdatesAsSeparateMessages;

    [Header("Token Refresh Testing")]
    [Tooltip("New token to provide when SDK requests refresh")]
    [SerializeField] private string refreshToken = "demo-refresh-token";

    [Tooltip("Simulate token refresh failure")]
    [SerializeField] private bool simulateRefreshFailure;

#if UNITY_EDITOR
    [Header("Debug")]
    [Tooltip("Trigger EXPR once in Editor")]
    [SerializeField] private bool triggerExprOnce;
#endif

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI connectionStatusText;

    #endregion

    #region Private Fields

    private const string HANDLER_ID = "ChatDemoHandler";
    private VcGroupChannel _currentChannel;
    private readonly Dictionary<string, (string message, string meta1, string meta2)> _messageCache = new();

    // Token refresh state
    private Action<string> _tokenRefreshSuccess;
    private Action _tokenRefreshFail;
    private bool _isWaitingForToken;

#if UNITY_EDITOR
    private bool _triggerExprConsumed;
#endif
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        SetupUI();
        InitializeAndConnect();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (triggerExprOnce && !_triggerExprConsumed)
        {
            _triggerExprConsumed = true;
            triggerExprOnce = false;
            TriggerExprInternal();
            return;
        }

        if (!triggerExprOnce)
        {
            _triggerExprConsumed = false;
        }
    }
#endif

    private void OnDestroy()
    {
        // Cleanup handlers
        VcGroupChannel.RemoveGroupChannelHandler(HANDLER_ID);
        VyinChat.RemoveConnectionHandler(HANDLER_ID);
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

        // Enable auto-resend for message reliability testing
        VyinChat.SetEnableMessageAutoResend(enableAutoResend);
        LogInfo($"Auto-resend: {(enableAutoResend ? "enabled" : "disabled")}");

        // Set session handler for token refresh
        VyinChat.SetSessionHandler(this);
        LogInfo("Session handler registered");

        // Register connection handler
        RegisterConnectionHandler();

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
        UpdateConnectionStatusDisplay("Connecting...", ConnectingColor);
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
    private void SendChatMessage(string text)
    {
        if (_currentChannel == null)
        {
            LogError("No channel available");
            return;
        }

        // Send message via SDK
        var messageParams = new VcUserMessageCreateParams { Message = text };
        VcUserMessage pending = null;
        pending = _currentChannel.SendUserMessage(messageParams, (msg, err) => OnMessageSent(msg, err, pending?.ReqId, text));
        if (pending != null)
        {
            ShowPendingMessage(pending.ReqId, text);
        }
    }

    private void OnMessageSent(VcUserMessage message, VcException error, string pendingId, string originalText)
    {
        if (error != null)
        {
            LogError($"Failed to send: {error.Message}");
            if (!string.IsNullOrEmpty(pendingId))
                UpdateMessageDisplay(pendingId, $"[{userId}] {originalText}", $"  -> {VcSendingStatus.Failed}");
            return;
        }

        if (message != null)
        {
            var displayName = GetDisplayName(message);
            if (!showAckAsSeparateMessages && !string.IsNullOrEmpty(pendingId))
            {
                _messageCache.Remove(pendingId);
            }
            UpdateMessageDisplay(message.MessageId.ToString(), $"[{displayName}] {message.Message}", $"  -> {message.SendingStatus}");
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
            ? $"{message.MessageId}-{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : message.MessageId.ToString();

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

        UpdateConnectionStatusDisplay("Disconnected", DisconnectedColor);
    }

    private static readonly Color ConnectedColor = new(0.2f, 0.8f, 0.4f); // Soft green
    private static readonly Color DisconnectedColor = new(0.9f, 0.3f, 0.3f); // Soft red
    private static readonly Color ConnectingColor = new(0.3f, 0.6f, 0.9f); // Soft blue
    private static readonly Color ReconnectingColor = new(1f, 0.7f, 0.2f); // Amber

    private void RegisterConnectionHandler()
    {
        var handler = new VcConnectionHandler
        {
            OnConnected = _ => UpdateConnectionStatusDisplay("Connected", ConnectedColor),
            OnDisconnected = _ => UpdateConnectionStatusDisplay("Disconnected", DisconnectedColor),
            OnReconnectStarted = () => UpdateConnectionStatusDisplay("Reconnecting...", ReconnectingColor),
            OnReconnectSucceeded = () => UpdateConnectionStatusDisplay("Connected", ConnectedColor),
            OnReconnectFailed = () => UpdateConnectionStatusDisplay("Reconnect Failed", DisconnectedColor)
        };
        VyinChat.AddConnectionHandler(HANDLER_ID, handler);
    }

    private void UpdateConnectionStatusDisplay(string text, Color color)
    {
        if (connectionStatusText == null) return;
        connectionStatusText.text = text;
        connectionStatusText.color = color;
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

        SendChatMessage(text);
    }

    private void ShowPendingMessage(string pendingId, string text)
    {
        if (showAckAsSeparateMessages)
        {
            UpdateMessageDisplay($"{pendingId}-none", $"[{userId}] {text}", $"  -> {VcSendingStatus.None}");
        }
        UpdateMessageDisplay(pendingId, $"[{userId}] {text}", $"  -> {VcSendingStatus.Pending}");
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

    private void UpdateMessageDisplay(string messageId, string message, string meta1 = null, string meta2 = null)
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

    #region IVcSessionHandler Implementation

    /// <summary>
    /// Called when SDK needs a new token.
    /// In production, fetch from your auth server.
    /// </summary>
    public void OnSessionTokenRequired(Action<string> success, Action fail)
    {
        _tokenRefreshSuccess = success;
        _tokenRefreshFail = fail;
        _isWaitingForToken = true;

        LogInfo("🔑 SDK requests new token!");
        LogInfo("   Use Inspector to provide token or simulate failure.");

        // Auto-provide token if not simulating failure
        if (!simulateRefreshFailure && !string.IsNullOrEmpty(refreshToken))
        {
            LogInfo($"   Auto-providing token: {refreshToken.Substring(0, System.Math.Min(20, refreshToken.Length))}...");
            ProvideRefreshToken();
        }
    }

    /// <summary>
    /// Called when token refresh succeeded.
    /// </summary>
    public void OnSessionRefreshed()
    {
        LogInfo("✅ Session refreshed successfully!");
        _isWaitingForToken = false;
    }

    /// <summary>
    /// Called when session cannot be recovered.
    /// In production, navigate to login screen.
    /// </summary>
    public void OnSessionClosed()
    {
        LogError("❌ Session closed - would navigate to login");
        _isWaitingForToken = false;
    }

    /// <summary>
    /// Called when an error occurred during refresh.
    /// </summary>
    public void OnSessionError(VcException error)
    {
        LogError($"⚠️ Session error: {error.ErrorCode} - {error.Message}");
        _isWaitingForToken = false;
    }

    /// <summary>
    /// Provide token to SDK (call from Inspector button or code)
    /// </summary>
    public void ProvideRefreshToken()
    {
        if (!_isWaitingForToken)
        {
            LogInfo("Not waiting for token");
            return;
        }

        LogInfo($"Providing token to SDK...");
        _isWaitingForToken = false;
        _tokenRefreshSuccess?.Invoke(refreshToken);
    }

    /// <summary>
    /// Fail token refresh (call from Inspector button or code)
    /// </summary>
    public void FailTokenRefresh()
    {
        if (!_isWaitingForToken)
        {
            LogInfo("Not waiting for token");
            return;
        }

        LogInfo("Failing token refresh...");
        _isWaitingForToken = false;
        _tokenRefreshFail?.Invoke();
    }

    #endregion

#if UNITY_EDITOR
    private void TriggerExprInternal()
    {
        try
        {
            var sdkAssembly = typeof(VyinChat).Assembly;
            var mainType = sdkAssembly.GetType("VyinChatSdk.Internal.Platform.Unity.VyinChatMain");
            if (mainType == null)
            {
                LogError("VyinChatMain type not found");
                return;
            }

            var instanceProp = mainType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            var mainInstance = instanceProp?.GetValue(null);
            if (mainInstance == null)
            {
                LogError("VyinChatMain instance not available");
                return;
            }

            var cmField = mainType.GetField("_connectionManager", BindingFlags.Instance | BindingFlags.NonPublic);
            var connectionManager = cmField?.GetValue(mainInstance);
            if (connectionManager == null)
            {
                LogError("ConnectionManager not available");
                return;
            }

            var handleExpr = connectionManager.GetType().GetMethod(
                "HandleExprCommand",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (handleExpr == null)
            {
                LogError("HandleExprCommand not found");
                return;
            }

            handleExpr.Invoke(connectionManager, new object[] { "{}" });
            LogInfo("Debug EXPR triggered");
        }
        catch (Exception ex)
        {
            LogError($"Debug EXPR failed: {ex.Message}");
        }
    }
#endif

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
