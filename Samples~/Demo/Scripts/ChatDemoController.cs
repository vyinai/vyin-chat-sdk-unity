using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VyinChatSdk;
using System.Collections.Generic;

public class ChatDemoController : MonoBehaviour
{
    // MARK: UI Elements
    [Header("SDK Configuration")]
    [Tooltip("Environment (PROD, DEV, STG, TEST)")]
    [SerializeField] private Environment environment = Environment.PROD;

    [Tooltip("Your App ID (will auto-fill based on environment if empty)")]
    [SerializeField] private string appId = "";

    [Tooltip("User ID for testing")]
    [SerializeField] private string userId = "testuser1";

    [Tooltip("Auth Token (optional, leave empty if not needed)")]
    [SerializeField] private string authToken = "";

    // Environment enum
    public enum Environment
    {
        PROD,
        DEV,
        STG,
        TEST
    }

    [Header("Channel Configuration")]
    [Tooltip("Channel name to create")]
    [SerializeField] private string channelName = "Unity Test Channel";

    [Tooltip("Other users to invite to the channel")]
    [SerializeField] private List<string> inviteUserIds = new() { "testuser2" };

    [Header("UI Elements")]
    public TMP_InputField inputField;
    public Button sendButton;
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect;

    private string currentChannelUrl;
    private VcGroupChannel _vcGroupChannel;
    private static readonly string UNIQUE_HANDLER_ID = "UNIQUE_HANDLER_ID";

    void Start()
    {
        try
        {
            // 根據環境設定配置
            AppendLogText($"[ChatDemo] Environment: {environment}");
            string domain = GetDomainForEnvironment(environment);
            string actualAppId = GetAppIdForEnvironment(environment, appId);
            AppendLogText($"[ChatDemo] Domain: {domain}");
            AppendLogText($"[ChatDemo] AppId: {actualAppId}");
            AppendLogText("──────────────────────────────");

            // 初始化 VyinChat SDK
            AppendLogText("[ChatDemo] Initializing VyinChat...");
            AppendLogText("──────────────────────────────");
            VcInitParams initParams = new VcInitParams(actualAppId, logLevel: VcLogLevel.Debug);
            VyinChat.Init(initParams);

            // 連線 VyinChat
            AppendLogText($"[ChatDemo] Connecting as '{userId}'...");
            AppendLogText("──────────────────────────────");
            string token = string.IsNullOrEmpty(authToken) ? null : authToken;
            string apiHost = $"https://{actualAppId}.{domain}";
            string wsHost = $"wss://{actualAppId}.{domain}";
            AppendLogText($"[ChatDemo] API Host: {apiHost}");
            AppendLogText($"[ChatDemo] WS Host: {wsHost}");
            VyinChat.Connect(userId, token, apiHost, wsHost, (inUser, inError) =>
            {
                if (!string.IsNullOrEmpty(inError))
                {
                    AppendLogText("[ChatDemo] Connection failed: " + inError);
                    return;
                }
                AppendLogText($"[ChatDemo] Connected! Welcome, {inUser.UserId}!");

                // 連接成功後，建立 AI ChatBot 聊天室
                CreateAIChatBotChannel();
            });

            // 訂閱訊息事件
            VcGroupChannelHandler groupChannelHandler = new()
            {
                OnMessageReceived = HandleIncomingMessage,
                OnMessageUpdated = HandleIncomingMessage
            };

            VcGroupChannel.AddGroupChannelHandler(UNIQUE_HANDLER_ID, groupChannelHandler);

            // Button 綁定
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClick);
            }
        }
        catch (System.Exception e)
        {
            LogErrorWithDebug("[ChatDemo] Start error", e);
        }
    }

    private void HandleIncomingMessage(VcGroupChannel _, VcBaseMessage message)
    {
        if (message.Sender?.UserId == userId)
            return;

        string displayName = GetDisplayName(message);
        string displayText = $"[{displayName}] {message.Message}";
        AddOrUpdateMessage(message.MessageId, displayText);
    }

    void OnDestroy()
    {
        try
        {
            // 取消訂閱
            VcGroupChannel.RemoveGroupChannelHandler(UNIQUE_HANDLER_ID);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ChatDemoController OnDestroy error: {e}");
        }
    }

    private void CreateAIChatBotChannel()
    {
        AppendLogText("──────────────────────────────");
        AppendLogText($"[ChatDemo] Creating channel '{channelName}'...");

        // 將當前用戶和邀請用戶合併
        List<string> allMembers = new() { userId };
        allMembers.AddRange(inviteUserIds);

        VcGroupChannelCreateParams channelCreateParams = new()
        {
            Name = channelName,
            UserIds = allMembers,
            OperatorUserIds = new() { userId, "shuyu", "andy-console" },
            IsDistinct = true
        };

        void channelCreateCallback(VcGroupChannel channel, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                AppendLogText("[ChatDemo] Failed to create channel: " + error);
                return;
            }

            currentChannelUrl = channel.ChannelUrl;
            AppendLogText($"[ChatDemo] Channel created: '{channel.Name}', ChannelUrl: '{currentChannelUrl}'");

            VcGroupChannelModule.GetGroupChannel(currentChannelUrl, (retrievedChannel, getError) =>
            {
                if (!string.IsNullOrEmpty(getError))
                {
                    AppendLogText($"[ChatDemo] GetChannel failed: {getError}");
                    return;
                }

                _vcGroupChannel = retrievedChannel;
                AppendLogText($"[ChatDemo] GetChannel success!");
                AppendLogText($"  - Channel URL: {retrievedChannel.ChannelUrl}");
                AppendLogText($"  - Name: {retrievedChannel.Name}");
                AppendLogText($"  - Members: {retrievedChannel.MemberCount}");
                AppendLogText("──────────────────────────────");
                AppendLogText("AI ChatBot connected!");
                AppendLogText("Please enter a message above to start chatting.");
            });
        }

        VcGroupChannelModule.CreateGroupChannel(channelCreateParams, channelCreateCallback);
    }

    // MARK: UI Event Handlers
    private void OnSendClick()
    {
        try
        {
            if (inputField == null) return;

            string msg = inputField.text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            inputField.text = "";
            inputField.ActivateInputField(); // Keep input field focused

            if (string.IsNullOrEmpty(currentChannelUrl))
            {
                AppendLogText("[ChatDemo] No channel available. Creating one...");
                CreateAIChatBotChannel();
                return;
            }

            SendMessageInternal(msg);
        }
        catch (System.Exception e)
        {
            LogErrorWithDebug("[ChatDemo] OnSendClick error", e);
        }
    }

    private void SendMessageInternal(string message)
    {
        _vcGroupChannel.SendUserMessage(new VcUserMessageCreateParams { Message = message }, (sentMessage, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                AppendLogText($"[ChatDemo] Failed to send: {error}");
                return;
            }

            try
            {
                if (sentMessage != null)
                {
                    string displayName = GetDisplayName(sentMessage);
                    AddOrUpdateMessage(sentMessage.MessageId, $"[{displayName}] {sentMessage.Message}");
                }
            }
            catch (System.Exception e)
            {
                LogErrorWithDebug("[ChatDemo] Error handling sent message", e);
            }
        });
    }

    private void LogErrorWithDebug(string message, System.Exception e)
    {
        AppendLogText($"{message}: {e.Message}");
        Debug.LogError($"{message}: {e}");
    }

    public static string GetDisplayName(VcBaseMessage message)
    {
        if (message?.Sender == null) return string.Empty;

        return string.IsNullOrEmpty(message.Sender.Nickname)
            ? message.Sender.UserId
            : message.Sender.Nickname;
    }

    // MARK: Helper Methods

    private static string GetDomainForEnvironment(Environment env) => env switch
    {
        Environment.PROD => "gamania.chat",
        Environment.DEV => "dev.gim.beango.com",
        Environment.STG => "stg.gim.beango.com",
        Environment.TEST => "test.gim.beango.com",
        _ => "gamania.chat"
    };

    private static string GetAppIdForEnvironment(Environment env, string customAppId)
    {
        if (!string.IsNullOrEmpty(customAppId))
            return customAppId;

        return env switch
        {
            Environment.PROD => "adb53e88-4c35-469a-a888-9e49ef1641b2",
            Environment.DEV => "b553fe2f-4975-4d22-934f-f4aa02167e19",
            Environment.STG => "9c839b9c-0be9-4e98-be4c-1f06345bdb7d",
            Environment.TEST => "1ba5d5e3-73ab-4b47-9b1d-ca1ce967fac2",
            _ => "adb53e88-4c35-469a-a888-9e49ef1641b2"
        };
    }

    private void AppendLogText(string message)
    {
        try
        {
            Debug.Log(message);
            if (logText != null)
            {
                logText.text += message + "\n";
                ScrollToBottom();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Log error: {e}");
        }
    }

    private readonly Dictionary<long, string> _messageCache = new();

    private void AddOrUpdateMessage(long messageId, string message)
    {
        try
        {
            _messageCache[messageId] = message;

            if (logText != null)
            {
                logText.text = string.Join("\n", _messageCache.Values);
                ScrollToBottom();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Log error: {e}");
        }
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

}