using System;
using Newtonsoft.Json;
using VyinChatSdk.Internal.Data.Mappers;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Domain.Log;

namespace VyinChatSdk
{
    /// <summary>
    /// Represents errors that occur during VyinChat SDK operations
    /// </summary>
    public class VcException : Exception
    {
        /// <summary>
        /// Gets the error code that identifies the type of error.
        /// See <see cref="VcErrorCode"/> for standard error codes.
        /// </summary>
        public VcErrorCode ErrorCode { get; }

        /// <summary>
        /// Gets the numeric value of the error code.
        /// </summary>
        public int Code => (int)ErrorCode;

        /// <summary>
        /// Gets additional information about the error (Optional)
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
            // Format: (Code) Message
            return $"({Code}) {Message}";
        }

        /// <summary>
        /// Creates a VcException from an HTTP response, parsing the body for specific error codes if available.
        /// </summary>
        internal static VcException FromHttpResponse(HttpResponse response)
        {
            // Prefer API error code from response body
            if (!string.IsNullOrEmpty(response.Body))
            {
                try
                {
                    var errorDto = JsonConvert.DeserializeObject<ApiErrorDto>(response.Body);
                    if (errorDto != null && errorDto.Code != 0)
                    {
                        var message = !string.IsNullOrEmpty(errorDto.Message)
                            ? errorDto.Message
                            : $"Server returned error code: {errorDto.Code}";

                        // Map legacy or raw codes to VcErrorCode
                        var vcErrorCode = ErrorCodeMapper.FromApiCode(errorDto.Code);

                        var exception = new VcException(vcErrorCode, message, response.Body);
                        Logger.Error(LogCategory.Http,
                            $"HTTP error code mapped: apiCode={errorDto.Code}, vcCode={(int)vcErrorCode}",
                            exception);
                        return exception;
                    }
                }
                catch
                {
                    // JSON parse failed; fall back to HTTP status mapping
                }
            }

            var fallbackErrorCode = ErrorCodeMapper.FromHttpStatusFallback(response.StatusCode);
            var fallbackMessage = $"HTTP {response.StatusCode}: {response.Error ?? "Unknown Error"}";
            
            // Customize default messages for common HTTP codes if no body
            if (string.IsNullOrEmpty(response.Error))
            {
                fallbackMessage = response.StatusCode switch
                {
                    400 => "Invalid request parameters",
                    401 => "Invalid or missing session key",
                    403 => "Invalid session key",
                    404 => "Resource not found",
                    500 => "Internal server error",
                    _ => fallbackMessage
                };
            }

            var fallbackException = new VcException(fallbackErrorCode, fallbackMessage, response.Body);
            Logger.Error(LogCategory.Http,
                $"HTTP error fallback: status={response.StatusCode}, vcCode={(int)fallbackErrorCode}",
                fallbackException);
            return fallbackException;
        }

        internal static VcException FromWebSocketCloseCode(ushort closeCode, string message = null)
        {
            var errorCode = ErrorCodeMapper.FromWebSocketCloseCode(closeCode);
            var errorMessage = string.IsNullOrEmpty(message)
                ? $"WebSocket closed with code: {closeCode}"
                : message;
            return new VcException(errorCode, errorMessage);
        }

        private class ApiErrorDto
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}
