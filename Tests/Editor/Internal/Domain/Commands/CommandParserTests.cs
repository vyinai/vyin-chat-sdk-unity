// -----------------------------------------------------------------------------
//
// Command Parser Tests - Unit Tests
// Tests JSON parsing logic (no WebSocket connection required)
//
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VyinChatSdk.Internal.Domain.Commands;

namespace VyinChatSdk.Tests.Editor.Internal.Domain.Commands
{
    public class CommandParserTests
    {
        [Test]
        public void ExtractCommandType_ValidLOGI_ShouldReturnLOGI()
        {
            string message = "LOGI{\"key\":\"session_123\"}";

            CommandType? result = CommandParser.ExtractCommandType(message);

            Assert.AreEqual(CommandType.LOGI, result);
        }

        [Test]
        public void ExtractCommandType_InvalidCommand_ShouldReturnNull()
        {
            string message = "XXXX{\"data\":\"test\"}";

            CommandType? result = CommandParser.ExtractCommandType(message);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseLogiCommand_ValidSuccess_ShouldParseCorrectly()
        {
            string message = "LOGI{\"key\":\"session_abc123\",\"error\":false,\"ping_interval\":15,\"pong_timeout\":5}";

            LogiCommand result = CommandParser.ParseLogiCommand(message);

            Assert.IsNotNull(result);
            Assert.AreEqual("session_abc123", result.SessionKey);
            Assert.IsFalse(result.Error);
            Assert.IsTrue(result.IsSuccess());
        }

        [Test]
        public void ParseLogiCommand_ErrorResponse_ShouldParseError()
        {
            string message = "LOGI{\"error\":true}";

            LogiCommand result = CommandParser.ParseLogiCommand(message);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Error);
            Assert.IsFalse(result.IsSuccess());
        }

        [Test]
        public void ParseLogiCommand_InvalidJSON_ShouldReturnNull()
        {
            string message = "LOGI{invalid json}";

            LogiCommand result = CommandParser.ParseLogiCommand(message);

            Assert.IsNull(result);
        }

        [Test]
        public void ExtractCommandType_Mesg_ShouldReturnMESG()
        {
            string message = "MESG{\"req_id\":\"abc\"}";

            var result = CommandParser.ExtractCommandType(message);

            Assert.AreEqual(CommandType.MESG, result);
        }

        [Test]
        public void ExtractPayload_ShouldReturnPayloadPart()
        {
            string message = "MESG{\"req_id\":\"123\",\"text\":\"hi\"}";

            var payload = CommandParser.ExtractPayload(message);

            Assert.AreEqual("{\"req_id\":\"123\",\"text\":\"hi\"}", payload);
        }
    }
}
