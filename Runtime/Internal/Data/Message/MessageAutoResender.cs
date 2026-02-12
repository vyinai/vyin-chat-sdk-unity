using System;
using System.Collections.Generic;
using VyinChatSdk.Internal.Domain.Log;
using VyinChatSdk.Internal.Domain.Message;

namespace VyinChatSdk.Internal.Data.Message
{
    /// <summary>
    /// Manages pending message queue for auto-resend on reconnection.
    /// Thread-safe implementation with FIFO ordering.
    /// </summary>
    internal class MessageAutoResender : IMessageAutoResender
    {
        private readonly Queue<PendingMessage> _pendingQueue;
        private readonly object _lock = new object();
        private readonly int _maxCapacity;

        private bool _isEnabled = false;
        private bool _isOnline = false;
        private bool _isDisposed = false;

        /// <summary>Create a new auto-resender with specified capacity.</summary>
        public MessageAutoResender(int maxCapacity = 1000)
        {
            _maxCapacity = maxCapacity;
            _pendingQueue = new Queue<PendingMessage>(Math.Min(maxCapacity, 100));
        }

        /// <inheritdoc/>
        public int PendingCount
        {
            get
            {
                lock (_lock)
                {
                    return _pendingQueue.Count;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsEnabled => _isEnabled;

        /// <inheritdoc/>
        public bool Register(PendingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            lock (_lock)
            {
                if (!_isEnabled)
                {
                    Logger.Debug(LogCategory.Message, $"[AutoResender] Rejected (disabled): {message.RequestId}");
                    return false;
                }

                if (_pendingQueue.Count >= _maxCapacity)
                {
                    Logger.Warning(LogCategory.Message, $"[AutoResender] Queue full ({_maxCapacity}), rejected: {message.RequestId}");
                    return false;
                }

                _pendingQueue.Enqueue(message);
                Logger.Debug(LogCategory.Message, $"[AutoResender] Registered: {message.RequestId}, Queue: {_pendingQueue.Count}");
                return true;
            }
        }

        /// <inheritdoc/>
        public bool TryDequeue(out PendingMessage message)
        {
            lock (_lock)
            {
                if (_pendingQueue.Count == 0)
                {
                    message = null;
                    return false;
                }

                message = _pendingQueue.Dequeue();
                Logger.Debug(LogCategory.Message, $"[AutoResender] Dequeued: {message.RequestId}, Remaining: {_pendingQueue.Count}");
                return true;
            }
        }

        /// <inheritdoc/>
        public bool Unregister(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
                return false;

            lock (_lock)
            {
                var count = _pendingQueue.Count;
                var found = false;

                for (int i = 0; i < count; i++)
                {
                    var msg = _pendingQueue.Dequeue();
                    if (msg.RequestId == requestId)
                    {
                        found = true;
                        Logger.Debug(LogCategory.Message, $"[AutoResender] Unregistered: {requestId}, Remaining: {_pendingQueue.Count}");
                        // Don't re-enqueue the found message
                    }
                    else
                    {
                        _pendingQueue.Enqueue(msg);
                    }
                }

                return found;
            }
        }

        /// <inheritdoc/>
        public void SetEnabled(bool enabled)
        {
            lock (_lock)
            {
                if (_isEnabled == enabled) return;

                _isEnabled = enabled;
                Logger.Info(LogCategory.Message, $"[AutoResender] Enabled: {enabled}");

                if (!enabled)
                {
                    // Clear queue when disabled
                    var count = _pendingQueue.Count;
                    _pendingQueue.Clear();
                    if (count > 0)
                    {
                        Logger.Info(LogCategory.Message, $"[AutoResender] Cleared {count} pending messages");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void CleanupExpired()
        {
            var expiredMessages = new List<PendingMessage>();

            lock (_lock)
            {
                var count = _pendingQueue.Count;
                for (int i = 0; i < count; i++)
                {
                    var msg = _pendingQueue.Dequeue();
                    if (msg.IsExpired())
                    {
                        expiredMessages.Add(msg);
                    }
                    else
                    {
                        _pendingQueue.Enqueue(msg);
                    }
                }
            }

            // Invoke callbacks outside lock
            foreach (var msg in expiredMessages)
            {
                Logger.Info(LogCategory.Message, $"[AutoResender] Expired (TTL): {msg.RequestId}");
                msg.MarkAsFailed(VcErrorCode.PendingError);
                try
                {
                    msg.OnFailed?.Invoke(msg, new VcException(VcErrorCode.PendingError, "Message expired (TTL exceeded)"));
                }
                catch (Exception ex)
                {
                    Logger.Error(LogCategory.Message, $"[AutoResender] Callback error: {ex.Message}");
                }
            }
        }

        /// <inheritdoc/>
        public void OnConnected()
        {
            lock (_lock)
            {
                _isOnline = true;
            }

            Logger.Info(LogCategory.Message, $"[AutoResender] Connected, pending: {PendingCount}");
            // Note: Actual resend loop is handled by the integration layer
        }

        /// <inheritdoc/>
        public void OnDisconnected()
        {
            lock (_lock)
            {
                _isOnline = false;
            }

            Logger.Info(LogCategory.Message, "[AutoResender] Disconnected");
        }

        /// <inheritdoc/>
        public void OnTokenRefreshed()
        {
            Logger.Info(LogCategory.Message, $"[AutoResender] Token refreshed, pending: {PendingCount}");
            // Note: Actual resend trigger is handled by the integration layer
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_lock)
            {
                _isDisposed = true;
                _pendingQueue.Clear();
            }
        }
    }
}
