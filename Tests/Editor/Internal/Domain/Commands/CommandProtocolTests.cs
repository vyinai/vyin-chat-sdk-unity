using VyinChatSdk.Internal.Domain.Commands;
using NUnit.Framework;

namespace VyinChatSdk.Tests.Editor.Internal.Domain.Commands
{
    public class CommandProtocolTests
    {
        [Test]
        public void SendCommand_ShouldGenerateUniqueReqId()
        {
            var protocol = new CommandProtocol();

            var c1 = protocol.BuildCommand(CommandType.MESG, new { text = "hi" });
            var c2 = protocol.BuildCommand(CommandType.MESG, new { text = "hi" });

            Assert.AreNotEqual(c1.reqId, c2.reqId);
        }

        [Test]
        public void SendCommand_ShouldSerialize_Correctly()
        {
            var protocol = new CommandProtocol();

            var cmd = protocol.BuildCommand(CommandType.MESG, new { text = "hello" });

            Assert.IsTrue(cmd.serialized.StartsWith("MESG{"));
            Assert.IsTrue(cmd.serialized.Contains("\"req_id\""));
            Assert.IsTrue(cmd.serialized.Contains("\"text\":\"hello\""));
            Assert.IsFalse(string.IsNullOrWhiteSpace(cmd.reqId));
        }

        [Test]
        public void SendCommand_ShouldIncludeReqId_AndPreservePayload()
        {
            var protocol = new CommandProtocol();
            var payload = new { foo = 1, bar = "baz" };

            var cmd = protocol.BuildCommand(CommandType.FILE, payload);

            Assert.IsTrue(cmd.serialized.StartsWith("FILE{"));
            StringAssert.Contains("\"foo\":1", cmd.serialized);
            StringAssert.Contains("\"bar\":\"baz\"", cmd.serialized);
            StringAssert.Contains("\"req_id\"", cmd.serialized);
            Assert.IsFalse(string.IsNullOrWhiteSpace(cmd.reqId));
        }
    }
}

