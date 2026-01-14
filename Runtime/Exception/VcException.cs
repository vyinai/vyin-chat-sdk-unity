using System;

namespace VyinChatSdk
{
    /// <summary>
    /// Represents errors that occur during VyinChat SDK operations
    /// </summary>
    public class VcException : Exception
    {
        /// <summary>
        /// Gets the error code that identifies the type of error
        /// </summary>
        public VcErrorCode ErrorCode { get; }

        /// <summary>
        /// Gets additional information about the error
        /// </summary>
        public string Details { get; }

        public VcException(VcErrorCode errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public VcException(VcErrorCode errorCode, string message, string details)
            : base(message)
        {
            ErrorCode = errorCode;
            Details = details;
        }

        public VcException(VcErrorCode errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public VcException(VcErrorCode errorCode, string message, string details, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Details = details;
        }

        public override string ToString()
        {
            var result = $"[VcException] Code={ErrorCode}, Message={Message}";
            if (!string.IsNullOrEmpty(Details))
            {
                result += $", Details={Details}";
            }
            if (InnerException != null)
            {
                result += $", InnerException={InnerException.Message}";
            }
            return result;
        }
    }
}
