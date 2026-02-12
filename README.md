# VyinChat Unity SDK

Real-time messaging SDK for Unity.

## Requirements

- Unity 2022.3 or newer

## Installation

Add VyinChat SDK via Unity Package Manager:

1. **Window** > **Package Manager** > **+** > **Add package from git URL**
2. Enter:
   ```
   https://github.com/vyinai/vyin-chat-sdk-unity.git
   ```

## Quick Start

Add the namespace to your script:

```csharp
using VyinChatSdk;
```

### 1. Initialize SDK

```csharp
VyinChat.Init(new VcInitParams("YOUR_APP_ID"));
```

### 2. Connect User

```csharp
VyinChat.Connect(userId, authToken, (user, error) =>
{
    if (error != null)
    {
        Debug.LogError($"Connect failed: {error.Message}");
        return;
    }
    Debug.Log($"Connected as {user.UserId}");
});
```

### 3. Create or Get a Group Channel

```csharp
var channelParams = new VcGroupChannelCreateParams
{
    Name = "general-room",
    UserIds = new List<string> { userId },
    IsDistinct = true
};

VcGroupChannelModule.CreateGroupChannel(channelParams, (channel, error) =>
{
    if (error != null)
    {
        Debug.LogError($"Create channel failed: {error.Message}");
        return;
    }

    // Store channel reference for later use
    _currentChannel = channel;
    Debug.Log($"Channel ready: {channel.ChannelUrl}");
});
```

### 4. Send a Message

```csharp
var messageParams = new VcUserMessageCreateParams
{
    Message = "Hello everyone!"
};

_currentChannel.SendUserMessage(messageParams, (message, error) =>
{
    if (error != null)
    {
        Debug.LogError($"Send failed: {error.Message}");
        return;
    }
    Debug.Log($"Message sent: {message.MessageId}");
});
```

### 5. Receive Messages

```csharp
var handler = new VcGroupChannelHandler
{
    OnMessageReceived = (channel, message) =>
    {
        Debug.Log($"New message: {message.Message}");
    },
    OnMessageUpdated = (channel, message) =>
    {
        Debug.Log($"Message updated: {message.Message}");
    }
};

VcGroupChannel.AddGroupChannelHandler("my-handler", handler);

// Remove when no longer needed
VcGroupChannel.RemoveGroupChannelHandler("my-handler");
```

## License

Use of this SDK requires authorization from Vyin Chat.
