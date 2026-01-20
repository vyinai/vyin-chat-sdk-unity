using System;
using NUnit.Framework;
using VyinChatSdk;

namespace VyinChatSdk.Tests.Runtime
{
    public class ErrorHandlingTests
    {
        [Test]
        public void VcException_ShouldInheritFromSystemException()
        {
            var exception = new VcException(VcErrorCode.UnknownError, "Test Error");
            Assert.IsInstanceOf<Exception>(exception);
        }

        [Test]
        public void VcException_Constructor_ShouldSetCodeAndMessage()
        {
            VcErrorCode code = VcErrorCode.UnknownError;
            string message = "Unknown Error";

            var exception = new VcException(code, message);

            Assert.AreEqual(code, exception.ErrorCode);
            Assert.AreEqual((int)code, exception.Code); // Check numeric value
            Assert.AreEqual(message, exception.Message);
        }

        [Test]
        public void VcException_ToString_ShouldReturnFormattedString()
        {
            VcErrorCode code = VcErrorCode.ErrBadRequest;
            string message = "Bad Request";
            var exception = new VcException(code, message);

            string result = exception.ToString();

            // Format: (Code) Message
            Assert.AreEqual("(400000) Bad Request", result);
        }

        [Test]
        public void VcErrorCode_ShouldDefineCorrectConstantValues_SDKInternal()
        {
            // SDK Internal (8xxxxx)
            Assert.AreEqual(800000, (int)VcErrorCode.UnknownError);
            Assert.AreEqual(800100, (int)VcErrorCode.InvalidInitialization);
            Assert.AreEqual(800101, (int)VcErrorCode.ConnectionRequired);
            Assert.AreEqual(800120, (int)VcErrorCode.NetworkError);
            Assert.AreEqual(800700, (int)VcErrorCode.LocalDatabaseError);
        }

        [Test]
        public void VcErrorCode_ShouldDefineCorrectConstantValues_Common()
        {
            // Common (4xxxxx - 5xxxxx)
            Assert.AreEqual(400000, (int)VcErrorCode.ErrBadRequest);
            Assert.AreEqual(400001, (int)VcErrorCode.ErrInvalidArgument);
            Assert.AreEqual(400003, (int)VcErrorCode.ErrUnauthorized);
            Assert.AreEqual(400011, (int)VcErrorCode.ErrParameterDecode);
            Assert.AreEqual(403000, (int)VcErrorCode.ErrForbidden);
            Assert.AreEqual(404000, (int)VcErrorCode.ErrNotFound);
            Assert.AreEqual(500000, (int)VcErrorCode.ErrInternal);
        }

        [Test]
        public void VcErrorCode_ShouldDefineCorrectConstantValues_Channel()
        {
            // Channel (279xxx)
            Assert.AreEqual(279000, (int)VcErrorCode.ErrChannelFreeze);
            Assert.AreEqual(279004, (int)VcErrorCode.ErrChannelNotFound);
        }

        [Test]
        public void VcErrorCode_ShouldDefineCorrectConstantValues_Message()
        {
            // Message (307xxx)
            Assert.AreEqual(307004, (int)VcErrorCode.ErrMessageNotFound);
            Assert.AreEqual(307001, (int)VcErrorCode.ErrSendNotAllowed);
        }

        [Test]
        public void VcErrorCode_ShouldDefineCorrectConstantValues_Application()
        {
            // Application (638xxx)
            Assert.AreEqual(638001, (int)VcErrorCode.ErrInvalidAppID);
            Assert.AreEqual(638002, (int)VcErrorCode.ErrUserNotFound);
        }
    }
}