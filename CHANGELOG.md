# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.4.1] - 2026-02-26

### Fixed
- Fixed several stability issues related to reconnection and lifecycle management
- Improved error log output to include full exception stack trace for easier debugging

## [0.4.0] - 2026-02-12

### Added
- Connection state management and auto-reconnection: `VcConnectionState` tracks connection state, `VcConnectionHandler` provides callbacks (`OnConnected`, `OnDisconnected`, `OnReconnectStarted`, `OnReconnectSucceeded`, `OnReconnectFailed`) with exponential backoff retry strategy
- `VcBackgroundDisconnectionConfig` for background disconnection settings, supporting app lifecycle tracking and customizable disconnect delay
- `IVcSessionHandler` token refresh lifecycle interface (`OnSessionTokenRequired`, `OnSessionRefreshed`, `OnSessionClosed`, `OnSessionError`) with proactive JWT expiry detection
- Message auto-resend mechanism: automatically resends failed messages on failure or reconnection. Disabled by default
- `VcSendingStatus` for tracking message sending state (`Pending`, `Succeeded`, `Failed`, `Canceled`)
- `VcGroupChannel` new properties: `IsDistinct`, `IsPublic`, `MyRole`
- `VcBaseMessage` new properties: `SendingStatus`, `ErrorCode`, `IsResendable`
- `VyinChat.CurrentUser` for accessing the currently connected user
- `VyinChat.GetConnectionState()` for querying current connection state

### Changed
- **BREAKING**: `SendUserMessage` now returns `VcUserMessage` (previously `void`) as a pending message object
- **BREAKING**: `SendUserMessageAsync` return type changed from `Task<VcBaseMessage>` to `Task<VcUserMessage>`
- **BREAKING**: `VcUserMessageHandler` callback parameter changed from `VcBaseMessage` to `VcUserMessage`

## [0.3.2] - 2026-01-16

### Added
- Logger with log level filtering (configurable via `VcInitParams.LogLevel`)

### Fixed
- Improved MainThreadDispatcher cleanup and Unity Editor lifecycle handling

## [0.3.1] - 2026-01-16

### Added
- Structured error handling with `VcException` and `VcErrorCode`: HTTP and WebSocket errors now provide typed error codes instead of raw strings
- API versioning support with `ApiVersionConfig`

### Fixed
- WebSocket sender parsing now supports field aliases (`name`/`image`)

## [0.3.0] - 2026-01-16

### Added
- Added `Done` flag to `VcBaseMessage`: enables detection of whether an AI streaming response has completed
- Extended message parsing fields: added support for additional message metadata such as `custom_type` and `data`
- Introduced message send & receive capabilities: provides the `groupChannel.SendUserMessage` API, callback-based responses, and structured error handling
- Full Unity Editor support with a pure C# Unity SDK: ensures stable operation across mobile platforms (Android / iOS) and PC Editor environments

### Changed
- NativeWebSocket is now bundled within the SDK, no longer requires separate Package Manager import
- Distribution changed to GitHub URL (not tgz), install via Unity Package Manager
- Android: SDK is now pure C#, no longer depends on Android Native Library. `baseProjectTemplate.gradle` and `mainTemplate.gradle` settings can be removed

## [0.2.0] - 2026-01-09

### Fixed
- Main thread execution: all Android / iOS callbacks now guaranteed to execute on Unity main thread

### Added
- Partial Unity Editor testing support: Init, Connect, CreateChannel can be tested directly in Editor without building to device

### Note
- Unity Editor testing requires [NativeWebSocket](https://github.com/endel/NativeWebSocket.git#upm) imported via Package Manager

## [0.1.1] - 2025-12-18

### Fixed
- Repackaged SDK for Unity 2022.3 LTS compatibility

## [0.1.0] - 2025-12-18

### Added
- VyinChat Unity SDK initial release (Wrapper)
- SDK initialization
- User connection
- Create group channel
- Send messages
- Receive channel messages

### Note
- This version is a quick POC implementation using Native SDK wrapper
- SDK depends on native runtime environments (Android / iOS), cannot run in Unity Editor desktop environment. Build to actual device for testing
