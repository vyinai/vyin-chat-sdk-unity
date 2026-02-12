using System;
using System.Threading.Tasks;
using VyinChatSdk.Internal.Domain.Log;

namespace VyinChatSdk.Internal.Platform.Unity
{
    /// <summary>
    /// Helper for executing async operations with callback pattern.
    /// Ensures callbacks are always invoked on the main thread.
    /// </summary>
    internal static class AsyncCallbackHelper
    {
        /// <summary>
        /// Executes an async operation and invokes callback with result or error.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="asyncOperation">The async operation to execute.</param>
        /// <param name="callback">Callback invoked with result or error.</param>
        /// <param name="tag">Log tag for error logging.</param>
        /// <param name="operationName">Operation name for error logging.</param>
        public static async Task ExecuteAsync<T>(
            Func<Task<T>> asyncOperation,
            Action<T, VcException> callback,
            string tag,
            string operationName)
        {
            void InvokeCallback(T result, VcException error)
            {
                if (error != null)
                {
                    Logger.Error(tag, $"{operationName} failed", error);
                }
                MainThreadDispatcher.Enqueue(() => callback?.Invoke(result, error));
            }

            try
            {
                var result = await asyncOperation();
                InvokeCallback(result, null);
            }
            catch (VcException vcEx)
            {
                InvokeCallback(default, vcEx);
            }
            catch (Exception ex)
            {
                var fallback = new VcException(VcErrorCode.UnknownError, $"Unexpected error: {ex.Message}", ex);
                InvokeCallback(default, fallback);
            }
        }
    }
}
