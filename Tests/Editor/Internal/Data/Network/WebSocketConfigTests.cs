// -----------------------------------------------------------------------------
//
// WebSocket Config Tests
// Tests URL building logic (matches Swift SDK format)
//
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VyinChatSdk.Internal.Data.Network;

namespace VyinChatSdk.Tests.Editor.Internal.Data.Network
{
    public class WebSocketConfigTests
    {
        [Test]
        public void BuildWebSocketUrl_WithValidParams_ShouldGenerateCorrectUrl()
        {
            var config = new WebSocketConfig
            {
                ApplicationId = "adb53e88-4c35-469a-a888-9e49ef1641b2",
                UserId = "test_user_123",
                AccessToken = "test_token_456",
                EnvironmentDomain = "gamania.chat",
                SdkVersion = "0.1.0",
                AppVersion = "1.0.0"
            };

            string url = config.BuildWebSocketUrl();

            Assert.IsTrue(url.StartsWith("wss://adb53e88-4c35-469a-a888-9e49ef1641b2.gamania.chat/ws?"));
            Assert.IsTrue(url.Contains("user_id=test_user_123"));
            Assert.IsTrue(url.Contains("access_token=test_token_456"));
            Assert.IsTrue(url.Contains($"api-version={ApiVersionConfig.DefaultVersion}"));
        }

        [Test]
        public void BuildWebSocketUrl_ShouldUrlEncodeSpecialCharacters()
        {
            var config = new WebSocketConfig
            {
                ApplicationId = "test-app-id",
                UserId = "user@test.com",
                AccessToken = "token+with/special=chars",
                EnvironmentDomain = "gamania.chat"
            };

            string url = config.BuildWebSocketUrl();

            Assert.IsTrue(url.Contains("user_id=user%40test.com"));
            Assert.IsTrue(url.Contains("access_token=token%2Bwith%2Fspecial%3Dchars"));
        }

        [Test]
        public void BuildWebSocketUrl_WithoutAccessToken_ShouldNotIncludeToken()
        {
            var config = new WebSocketConfig
            {
                ApplicationId = "test-app-id",
                UserId = "test_user",
                EnvironmentDomain = "gamania.chat"
            };

            string url = config.BuildWebSocketUrl();

            Assert.IsTrue(url.StartsWith("wss://test-app-id.gamania.chat/ws?"));
            Assert.IsTrue(url.Contains("user_id=test_user"));
            Assert.IsFalse(url.Contains("access_token"));
            Assert.IsTrue(url.Contains($"api-version={ApiVersionConfig.DefaultVersion}"));
        }

        [Test]
        public void BuildWebSocketUrl_WithEmptyApplicationId_ShouldThrowException()
        {
            var config = new WebSocketConfig
            {
                ApplicationId = "",
                UserId = "test_user"
            };

            Assert.Throws<System.ArgumentException>(() => config.BuildWebSocketUrl());
        }
    }
}
