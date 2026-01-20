using NUnit.Framework;
using VyinChatSdk.Internal.Domain.Commands;

namespace VyinChatSdk.Tests.Editor.Internal.Domain.Commands
{
    public class CommandTypeExtensionsTests
    {
        [TestCase(CommandType.LOGI)]
        [TestCase(CommandType.MESG)]
        [TestCase(CommandType.FILE)]
        public void IsAckRequired_ShouldReturnTrue_ForAckCommands(CommandType commandType)
        {
            Assert.IsTrue(commandType.IsAckRequired());
        }

        [TestCase(CommandType.PING)]
        [TestCase(CommandType.PONG)]
        [TestCase(CommandType.EROR)]
        public void IsAckRequired_ShouldReturnFalse_ForNonAckCommands(CommandType commandType)
        {
            Assert.IsFalse(commandType.IsAckRequired());
        }
    }
}

