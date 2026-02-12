using System;

namespace VyinChatSdk.Internal.Data.Mappers
{
    internal static class ErrorCodeMapper
    {
        /// <summary>
        /// Maps an integer error code (from JSON body) to VcErrorCode.
        /// </summary>
        public static VcErrorCode FromApiCode(int code)
        {
            // Cast directly if defined in Enum
            if (Enum.IsDefined(typeof(VcErrorCode), code))
            {
                return (VcErrorCode)code;
            }

            // Fallback to Unknown if unrecognized
            return VcErrorCode.UnknownError;
        }

        /// <summary>
        /// Maps HTTP status code to VcErrorCode when detailed API error is missing.
        /// </summary>
        public static VcErrorCode FromHttpStatusFallback(int statusCode)
        {
            return statusCode switch
            {
                400 => VcErrorCode.ErrInvalidValue,       // Bad Request
                401 => VcErrorCode.ErrInvalidSession,     // Unauthorized
                403 => VcErrorCode.ErrForbidden,          // Forbidden
                404 => VcErrorCode.ErrNotFound,           // Not Found (generic)
                412 => VcErrorCode.ErrPreconditionFailed, // Precondition Failed
                429 => VcErrorCode.ErrServerBusy,         // Too Many Requests
                500 => VcErrorCode.ErrInternal,           // Internal Server Error
                503 => VcErrorCode.ErrOtherService,       // Service Unavailable
                504 => VcErrorCode.ErrHttpRequestTimeout, // Gateway Timeout
                _ => VcErrorCode.NetworkError             // Default fallback for other HTTP errors
            };
        }
    }
}
