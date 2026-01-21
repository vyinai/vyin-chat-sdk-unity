using NUnit.Framework;
using VyinChatSdk;
using VyinChatSdk.Internal.Data.Mappers;

namespace VyinChatSdk.Tests.Editor.Internal.Data.Mappers
{
    public class ErrorCodeMapperTests
    {
        [Test]
        public void FromApiCode_ShouldMapDefinedEnumCodesCorrectly()
        {
            Assert.AreEqual(VcErrorCode.ErrInvalidValue, ErrorCodeMapper.FromApiCode(400005));
            Assert.AreEqual(VcErrorCode.ErrChannelNotFound, ErrorCodeMapper.FromApiCode(279004));
            Assert.AreEqual(VcErrorCode.ErrUserNotFound, ErrorCodeMapper.FromApiCode(638002));
        }

        [Test]
        public void FromApiCode_ShouldReturnUnknownForUndefinedCodes()
        {
            Assert.AreEqual(VcErrorCode.UnknownError, ErrorCodeMapper.FromApiCode(999999));
        }

        [Test]
        public void FromHttpStatusFallback_ShouldMapStandardCodes()
        {
            Assert.AreEqual(VcErrorCode.ErrInvalidValue, ErrorCodeMapper.FromHttpStatusFallback(400));
            Assert.AreEqual(VcErrorCode.ErrInvalidSession, ErrorCodeMapper.FromHttpStatusFallback(401));
            Assert.AreEqual(VcErrorCode.ErrNotFound, ErrorCodeMapper.FromHttpStatusFallback(404));
            Assert.AreEqual(VcErrorCode.ErrInternal, ErrorCodeMapper.FromHttpStatusFallback(500));
        }

        [Test]
        public void FromHttpStatusFallback_ShouldFallbackToNetworkError()
        {
            Assert.AreEqual(VcErrorCode.NetworkError, ErrorCodeMapper.FromHttpStatusFallback(418)); // I'm a teapot
        }
    }
}
