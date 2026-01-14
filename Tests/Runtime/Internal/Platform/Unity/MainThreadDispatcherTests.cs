using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VyinChatSdk.Internal.Platform;

namespace VyinChatSdk.Tests.Runtime.Platform.Unity
{
    public class MainThreadDispatcherTests
    {
        private const float DefaultTimeout = 1f;
        private const float LongTimeout = 2f;
        private readonly List<Action> _registeredCallbacks = new List<Action>();

        [SetUp]
        public void SetUp()
        {
            MainThreadDispatcher.ClearQueue();
            _registeredCallbacks.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure all registered callbacks are unregistered
            foreach (var callback in _registeredCallbacks)
            {
                MainThreadDispatcher.UnregisterUpdateCallback(callback);
            }
            _registeredCallbacks.Clear();
        }

        /// <summary>
        /// Helper method to wait for a condition with timeout
        /// </summary>
        private IEnumerator WaitForCondition(Func<bool> condition, float timeout = DefaultTimeout)
        {
            while (!condition() && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Helper method to register a callback with automatic cleanup
        /// </summary>
        private void RegisterCallbackWithCleanup(Action callback)
        {
            _registeredCallbacks.Add(callback);
            MainThreadDispatcher.RegisterUpdateCallback(callback);
        }

        [UnityTest]
        public IEnumerator Enqueue_ExecutesActionOnMainThread()
        {
            bool actionExecuted = false;
            int? callbackThreadId = null;
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;

            MainThreadDispatcher.Enqueue(() =>
            {
                actionExecuted = true;
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            });

            yield return null;

            Assert.IsTrue(actionExecuted, "Action was not executed");
            Assert.AreEqual(mainThreadId, callbackThreadId, "Action was not executed on main thread");
        }

        [UnityTest]
        public IEnumerator Enqueue_ExecutesMultipleActionsInOrder()
        {
            var executionOrder = new List<int>();

            MainThreadDispatcher.Enqueue(() => executionOrder.Add(1));
            MainThreadDispatcher.Enqueue(() => executionOrder.Add(2));
            MainThreadDispatcher.Enqueue(() => executionOrder.Add(3));

            yield return null;

            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual(1, executionOrder[0]);
            Assert.AreEqual(2, executionOrder[1]);
            Assert.AreEqual(3, executionOrder[2]);
        }

        [UnityTest]
        public IEnumerator Enqueue_HandlesExceptionGracefully()
        {
            bool secondActionExecuted = false;

            MainThreadDispatcher.Enqueue(() => throw new Exception("Test exception"));
            MainThreadDispatcher.Enqueue(() => secondActionExecuted = true);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Test exception.*"));

            yield return null;

            Assert.IsTrue(secondActionExecuted, "Second action should still execute after first action throws");
        }

        [UnityTest]
        public IEnumerator Enqueue_FromBackgroundThread_ExecutesOnMainThread()
        {
            bool actionExecuted = false;
            int? callbackThreadId = null;
            int? backgroundThreadId = null;
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;

            Task.Run(() =>
            {
                backgroundThreadId = Thread.CurrentThread.ManagedThreadId;
                MainThreadDispatcher.Enqueue(() =>
                {
                    actionExecuted = true;
                    callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                });
            });

            yield return WaitForCondition(() => actionExecuted);

            Assert.IsTrue(actionExecuted, "Action was not executed");
            Assert.AreNotEqual(backgroundThreadId, callbackThreadId, "Background thread and callback thread should be different");
            Assert.AreEqual(mainThreadId, callbackThreadId, "Callback should execute on main thread");
        }

        [UnityTest]
        public IEnumerator Enqueue_NullAction_DoesNotThrow()
        {
            MainThreadDispatcher.Enqueue(null);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Enqueue_MultipleBackgroundThreads_AllExecuteOnMainThread()
        {
            const int expectedCount = 5;
            int executionCount = 0;
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var threadIds = new List<int>();
            object lockObj = new object();

            for (int i = 0; i < expectedCount; i++)
            {
                Task.Run(() =>
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        lock (lockObj)
                        {
                            executionCount++;
                            threadIds.Add(Thread.CurrentThread.ManagedThreadId);
                        }
                    });
                });
            }

            yield return WaitForCondition(() => executionCount >= expectedCount, LongTimeout);

            Assert.AreEqual(expectedCount, executionCount, $"All {expectedCount} actions should execute");
            foreach (var threadId in threadIds)
            {
                Assert.AreEqual(mainThreadId, threadId, "All actions should execute on main thread");
            }
        }

        [UnityTest]
        public IEnumerator Instance_CreatesSingletonCorrectly()
        {
            var instance1 = MainThreadDispatcher.Instance;
            var instance2 = MainThreadDispatcher.Instance;

            yield return null;

            Assert.AreSame(instance1, instance2, "Should return same instance");

            var dispatchers = GameObject.FindObjectsOfType<MainThreadDispatcher>();
            Assert.AreEqual(1, dispatchers.Length, "Should only create one MainThreadDispatcher GameObject");
            Assert.AreEqual("VyinChatMainThreadDispatcher", instance1.gameObject.name, "GameObject should have correct name");
        }

        [UnityTest]
        public IEnumerator RegisterUpdateCallback_ExecutesEveryFrame()
        {
            int executionCount = 0;
            Action callback = () => executionCount++;

            RegisterCallbackWithCleanup(callback);

            // Wait for multiple frames
            yield return null;
            yield return null;
            yield return null;

            // Callback should execute at least 3 times
            Assert.GreaterOrEqual(executionCount, 3, "Callback should execute every frame");
        }

        [UnityTest]
        public IEnumerator RegisterUpdateCallback_WithNull_DoesNotThrow()
        {
            MainThreadDispatcher.RegisterUpdateCallback(null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RegisterUpdateCallback_SameCallbackTwice_OnlyRegistersOnce()
        {
            int executionCount = 0;
            Action callback = () => executionCount++;

            RegisterCallbackWithCleanup(callback);
            MainThreadDispatcher.RegisterUpdateCallback(callback); // Register again (not tracked for cleanup)

            yield return null;

            // Should only execute once per frame, not twice
            Assert.AreEqual(1, executionCount, "Same callback should only be registered once");
        }

        [UnityTest]
        public IEnumerator UnregisterUpdateCallback_StopsExecution()
        {
            int executionCount = 0;
            Action callback = () => executionCount++;

            MainThreadDispatcher.RegisterUpdateCallback(callback);
            yield return null;
            int countAfterFirstFrame = executionCount;

            MainThreadDispatcher.UnregisterUpdateCallback(callback);
            yield return null;
            yield return null;

            // Count should not increase after unregister
            Assert.AreEqual(countAfterFirstFrame, executionCount, "Callback should not execute after unregister");
        }

        [UnityTest]
        public IEnumerator UnregisterUpdateCallback_WithNull_DoesNotThrow()
        {
            MainThreadDispatcher.UnregisterUpdateCallback(null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UpdateCallback_HandlesExceptionGracefully()
        {
            bool secondCallbackExecuted = false;
            Action throwingCallback = () => throw new Exception("Test callback exception");
            Action normalCallback = () => secondCallbackExecuted = true;

            RegisterCallbackWithCleanup(throwingCallback);
            RegisterCallbackWithCleanup(normalCallback);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Test callback exception.*"));

            yield return null;

            Assert.IsTrue(secondCallbackExecuted, "Second callback should still execute after first throws");
        }

        [UnityTest]
        public IEnumerator Enqueue_LargeNumberOfActions_ExecutesAllCorrectly()
        {
            const int actionCount = 100;
            int executionCount = 0;
            object lockObj = new object();

            for (int i = 0; i < actionCount; i++)
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    lock (lockObj)
                    {
                        executionCount++;
                    }
                });
            }

            yield return null;

            Assert.AreEqual(actionCount, executionCount, $"All {actionCount} actions should execute");
        }

        [UnityTest]
        public IEnumerator Enqueue_MixedMainAndBackgroundThreads_AllExecuteCorrectly()
        {
            const int actionsPerType = 5;
            const int totalActions = actionsPerType * 2;
            int mainThreadActions = 0;
            int backgroundThreadActions = 0;
            object lockObj = new object();

            // Enqueue from main thread
            for (int i = 0; i < actionsPerType; i++)
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    lock (lockObj)
                    {
                        mainThreadActions++;
                    }
                });
            }

            // Enqueue from background threads
            for (int i = 0; i < actionsPerType; i++)
            {
                Task.Run(() =>
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        lock (lockObj)
                        {
                            backgroundThreadActions++;
                        }
                    });
                });
            }

            yield return WaitForCondition(() => (mainThreadActions + backgroundThreadActions) >= totalActions, LongTimeout);

            Assert.AreEqual(actionsPerType, mainThreadActions, "All main thread actions should execute");
            Assert.AreEqual(actionsPerType, backgroundThreadActions, "All background thread actions should execute");
        }
    }
}
