using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Logger = VyinChatSdk.Internal.Domain.Log.Logger;

namespace VyinChatSdk.Internal.Platform
{
    /// <summary>
    /// Dispatcher to execute callbacks on Unity's main thread.
    /// Solves the issue where callbacks from native Android/iOS are executed on background threads,
    /// which causes crashes when updating UI.
    /// </summary>
    internal class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static readonly List<Action> _updateCallbacks = new List<Action>();
        private static readonly object _lock = new object();
        private static int? _mainThreadId;
        private static bool? _isTestEnvironment;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var _ = Instance;
        }

        static MainThreadDispatcher()
        {
            Logger.SetInstance(Unity.UnityLoggerImpl.Instance);
            Application.quitting += OnApplicationQuit;
        }

        public static MainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("VyinChatMainThreadDispatcher");
                            _instance = go.AddComponent<MainThreadDispatcher>();

                            // Capture main thread ID when instance is created on main thread
                            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

                            // DontDestroyOnLoad only works in PlayMode
                            if (Application.isPlaying)
                            {
                                DontDestroyOnLoad(go);
                            }

                            Logger.Debug(message: "MainThreadDispatcher initialized");
                        }
                    }
                }
                return _instance;
            }
        }

        private static void OnApplicationQuit()
        {
            _instance = null;
        }

        void OnDestroy()
        {
            lock (_lock)
            {
                _executionQueue.Clear();
                _updateCallbacks.Clear();
            }
            if (_instance == this)
            {
                _instance = null;
                _mainThreadId = null;
                _isTestEnvironment = null;
            }
        }

        void Update()
        {
            // Process queued actions - copy queue outside lock to minimize lock duration
            Queue<Action> actionsToExecute = null;
            lock (_lock)
            {
                if (_executionQueue.Count > 0)
                {
                    actionsToExecute = new Queue<Action>(_executionQueue);
                    _executionQueue.Clear();
                }
            }

            // Execute actions outside lock
            if (actionsToExecute != null)
            {
                while (actionsToExecute.Count > 0)
                {
                    var action = actionsToExecute.Dequeue();
                    ExecuteActionSafely(action, "action");
                }
            }

            // Process update callbacks (e.g., WebSocket message dispatch)
            List<Action> callbacksCopy;
            lock (_lock)
            {
                callbacksCopy = new List<Action>(_updateCallbacks);
            }

            foreach (var callback in callbacksCopy)
            {
                ExecuteActionSafely(callback, "update callback");
            }
        }

        /// <summary>
        /// Execute action with exception handling
        /// Uses Logger if available, falls back to Debug.LogError
        /// </summary>
        private static void ExecuteActionSafely(Action action, string actionType)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                try
                {
                    Logger.Error(message: $"MainThreadDispatcher error executing {actionType}: {e}", exception: e);
                }
                catch
                {
                    // Fallback to Unity Debug.LogError if Logger is not initialized
                    UnityEngine.Debug.LogError($"MainThreadDispatcher error executing {actionType}: {e}");
                }
            }
        }

        /// <summary>
        /// Enqueue action to be executed on Unity's main thread in the next Update()
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;

            // Check if we're on the main thread by comparing thread IDs
            // This avoids calling Application.isPlaying from background threads
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            bool isMainThread = _mainThreadId.HasValue && currentThreadId == _mainThreadId.Value;

            // If we're on a background thread, queue immediately
            if (_mainThreadId.HasValue && !isMainThread)
            {
                lock (_lock)
                {
                    _executionQueue.Enqueue(action);
                }
                return;
            }

            // From here, we're either on the main thread or haven't initialized yet
            // Safe to check Application.isPlaying
            bool isPlaying = UnityEngine.Application.isPlaying;

            // EditMode: Execute synchronously (no Update() cycle)
            if (!isPlaying)
            {
                ExecuteActionWithFallbackLogging(action);
                return;
            }

            // PlayMode: Ensure instance exists
            var _ = Instance;

            // In tests on main thread: Execute synchronously for LogAssert
            // Cache the test environment check as it's relatively expensive
            if (!_isTestEnvironment.HasValue)
            {
                _isTestEnvironment = System.AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => a.FullName.StartsWith("nunit.framework"));
            }

            if (isMainThread && _isTestEnvironment.Value)
            {
                ExecuteActionWithFallbackLogging(action);
                return;
            }

            // Queue for execution in Update()
            lock (_lock)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Execute action with fallback to Debug.LogError if Logger is not available
        /// Used in EditMode where Logger might not be initialized
        /// </summary>
        private static void ExecuteActionWithFallbackLogging(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"MainThreadDispatcher error executing action: {e}");
            }
        }

        /// <summary>
        /// Register a callback to be executed every Update cycle
        /// Used for WebSocket message dispatching
        /// </summary>
        public static void RegisterUpdateCallback(Action callback)
        {
            if (callback == null) return;

            var _ = Instance;

            lock (_lock)
            {
                if (!_updateCallbacks.Contains(callback))
                {
                    _updateCallbacks.Add(callback);
                }
            }
        }

        /// <summary>
        /// Unregister an update callback
        /// </summary>
        public static void UnregisterUpdateCallback(Action callback)
        {
            if (callback == null) return;

            lock (_lock)
            {
                _updateCallbacks.Remove(callback);
            }
        }

#if UNITY_INCLUDE_TESTS
        /// <summary>
        /// Clear all pending actions in the queue (for testing purposes only)
        /// </summary>
        internal static void ClearQueue()
        {
            var _ = Instance;
            lock (_lock)
            {
                _executionQueue.Clear();
            }
        }
#endif
    }
}
