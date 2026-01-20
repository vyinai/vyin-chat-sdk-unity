using NUnit.Framework;
using VyinChatSdk;
using VyinChatSdk.Internal.Data.Mappers;

namespace VyinChatSdk.Tests.Editor.Internal.Data.Mappers
{
    public class ErrorCodeMapperTests
    {
        [Test]
        public void FromApiCode_ShouldMapLegacyCodesCorrectly()
        {
            // Assert
            // 400110 -> 400004 (ErrInputExceedLimit)
            Assert.AreEqual(VcErrorCode.ErrInputExceedLimit, ErrorCodeMapper.FromApiCode(400110));
            // 100000 -> 400011 (ErrParameterDecode)
            Assert.AreEqual(VcErrorCode.ErrParameterDecode, ErrorCodeMapper.FromApiCode(100000));
            // 7403101 -> 638004 (ErrAPPPermissionDeny)
            Assert.AreEqual(VcErrorCode.ErrAPPPermissionDeny, ErrorCodeMapper.FromApiCode(7403101));
            // 9251541 -> 307006 (ErrPollNotFound)
            Assert.AreEqual(VcErrorCode.ErrPollNotFound, ErrorCodeMapper.FromApiCode(9251541));
        }

        [Test]
        public void FromApiCode_ShouldMapExistingEnumCodesCorrectly()
        {
            // Assert
            Assert.AreEqual(VcErrorCode.ErrInvalidValue, ErrorCodeMapper.FromApiCode(400005));
            Assert.AreEqual(VcErrorCode.ErrChannelNotFound, ErrorCodeMapper.FromApiCode(279004));
        }

        [Test]
        public void FromApiCode_ShouldReturnUnknownForUndefinedCodes()
        {
            // Assert
            Assert.AreEqual(VcErrorCode.UnknownError, ErrorCodeMapper.FromApiCode(999999));
        }

        [Test]
        public void FromHttpStatusFallback_ShouldMapStandardCodes()
        {
            // Assert
            Assert.AreEqual(VcErrorCode.ErrInvalidValue, ErrorCodeMapper.FromHttpStatusFallback(400));
            Assert.AreEqual(VcErrorCode.ErrInvalidSession, ErrorCodeMapper.FromHttpStatusFallback(401));
            Assert.AreEqual(VcErrorCode.ErrNotFound, ErrorCodeMapper.FromHttpStatusFallback(404));
            Assert.AreEqual(VcErrorCode.ErrInternal, ErrorCodeMapper.FromHttpStatusFallback(500));
        }

        [Test]
        public void FromHttpStatusFallback_ShouldFallbackToNetworkError()
        {
            // Assert
            Assert.AreEqual(VcErrorCode.NetworkError, ErrorCodeMapper.FromHttpStatusFallback(418)); // I'm a teapot
        }

        [Test]
        public void FromWebSocketCloseCode_ShouldMapStandardCodes()
        {
            // Assert
            Assert.AreEqual(VcErrorCode.WebSocketConnectionClosed, ErrorCodeMapper.FromWebSocketCloseCode(1000));
            Assert.AreEqual(VcErrorCode.WebSocketConnectionFailed, ErrorCodeMapper.FromWebSocketCloseCode(1006));
        }
    }
}
