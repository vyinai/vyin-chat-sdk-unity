using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VyinChatSdk.Internal.Data.Network;
using VyinChatSdk.Internal.Domain.Commands;

namespace VyinChatSdk.Tests.Runtime.Internal.Domain.Commands
{
    /// <summary>
    /// ACK flow behavior tests using integrated API.
    /// Uses UnityTest to support async operations without deadlocks.
    /// </summary>
    public class AckFlowTests
    {
        [UnityTest]
        public IEnumerator SendCommand_ShouldReceiveACK_WithinTimeout()
        {
            var ws = CreateFakeClient();
            var task = ws.SendCommandAsync(CommandType.MESG, new { text = "hi" }, TimeSpan.FromSeconds(2));

            var reqId = ExtractReqId(ws.LastSent);
            ws.SimulateIncoming($"MESG{{\"req_id\":\"{reqId}\"}}");

            yield return new WaitUntil(() => task.IsCompleted);
            
            var result = task.Result;
            Assert.IsNotNull(result, "Should return ACK payload");
            Assert.IsTrue(result.Contains(reqId), "ACK payload should contain req_id");
        }

        [UnityTest]
        public IEnumerator SendCommand_ShouldTimeout_WhenNoACK()
        {
            var ws = CreateFakeClient();
            var task = ws.SendCommandAsync(CommandType.MESG, new { text = "hi" }, TimeSpan.FromMilliseconds(100));

            yield return new WaitUntil(() => task.IsCompleted);

            var result = task.Result;
            Assert.IsNull(result, "Timeout should return null");
            Assert.AreEqual(1, ws.SendCount, "Message should be sent once");
        }

        [UnityTest]
        public IEnumerator SendCommand_ShouldMatchACK_ByReqId()
        {
            var ws = CreateFakeClient();
            var task = ws.SendCommandAsync(CommandType.MESG, new { text = "hi" }, TimeSpan.FromSeconds(2));

            var reqId = ExtractReqId(ws.LastSent);
            ws.SimulateIncoming("MESG{\"req_id\":\"other\"}");
            Assert.IsFalse(task.IsCompleted, "Task should remain pending after wrong req_id");

            ws.SimulateIncoming($"MESG{{\"req_id\":\"{reqId}\"}}");

            yield return new WaitUntil(() => task.IsCompleted);
            
            var result = task.Result;
            Assert.IsNotNull(result, "Task should complete with ACK payload after matching req_id");
        }

        [UnityTest]
        public IEnumerator MultipleCommands_ShouldTrackSessions_Independently()
        {
            var ws = CreateFakeClient();
            var t1 = ws.SendCommandAsync(CommandType.MESG, new { text = "m1" }, TimeSpan.FromSeconds(2));
            var r1 = ExtractReqId(ws.LastSent);
            var t2 = ws.SendCommandAsync(CommandType.MESG, new { text = "m2" }, TimeSpan.FromSeconds(2));
            var r2 = ExtractReqId(ws.LastSent);

            ws.SimulateIncoming($"MESG{{\"req_id\":\"{r2}\"}}");

            yield return new WaitUntil(() => t2.IsCompleted);
            Assert.IsNotNull(t2.Result, "r2 should complete success");
            Assert.IsFalse(t1.IsCompleted, "r1 should remain pending while r2 is completed");

            ws.SimulateIncoming($"MESG{{\"req_id\":\"{r1}\"}}");

            yield return new WaitUntil(() => t1.IsCompleted);
            Assert.IsNotNull(t1.Result, "r1 should complete success");
        }

        [UnityTest]
        public IEnumerator ACK_WithWrongReqId_ShouldNotMatch()
        {
            var ws = CreateFakeClient();
            var task = ws.SendCommandAsync(CommandType.MESG, new { text = "hi" }, TimeSpan.FromMilliseconds(200));

            ws.SimulateIncoming("MESG{\"req_id\":\"wrong\"}");

            Assert.IsFalse(task.IsCompleted, "Task should stay pending until timeout");
            
            yield return new WaitUntil(() => task.IsCompleted);
            
            var result = task.Result;
            Assert.IsNull(result, "Eventually times out with null result");
        }

        [UnityTest]
        public IEnumerator Duplicate_ACK_ShouldNotTriggerTwice()
        {
            var ws = CreateFakeClient();
            var task = ws.SendCommandAsync(CommandType.MESG, new { text = "hi" }, TimeSpan.FromSeconds(2));
            var reqId = ExtractReqId(ws.LastSent);

            ws.SimulateIncoming($"MESG{{\"req_id\":\"{reqId}\"}}");

            yield return new WaitUntil(() => task.IsCompleted);
            Assert.IsTrue(task.IsCompleted, "Task should complete after first MESG ACK");

            var result = task.Result;
            Assert.IsNotNull(result, "Should have ACK payload");

            // Simulate duplicate - should not throw or cause issues
            ws.SimulateIncoming($"MESG{{\"req_id\":\"{reqId}\"}}");
        }

        [UnityTest]
        public IEnumerator NonAckCommand_ShouldNotWaitAck()
        {
            var ws = CreateFakeClient();
            var task = ws.SendCommandAsync(CommandType.NOOP, new { }, TimeSpan.FromSeconds(2));

            Assert.IsTrue(task.IsCompleted, "Non-ACK command should complete immediately");
            
            yield return new WaitUntil(() => task.IsCompleted);
            
            var result = task.Result;
            Assert.IsNull(result, "Non-ACK command should return null result");
            Assert.AreEqual(1, ws.SendCount, "Message should be sent once");
        }

        private static FakeWebSocketClient CreateFakeClient()
        {
            return new FakeWebSocketClient();
        }

        private static string ExtractReqId(string serializedCommand)
        {
            var payload = CommandParser.ExtractPayload(serializedCommand);
            if (string.IsNullOrEmpty(payload)) return null;

            const string key = "\"req_id\":\"";
            var start = payload.IndexOf(key, StringComparison.Ordinal);
            if (start < 0) return null;
            start += key.Length;
            var end = payload.IndexOf('"', start);
            if (end < 0 || end <= start) return null;
            return payload.Substring(start, end - start);
        }

        private class FakeWebSocketClient : IWebSocketClient
        {
            public event Action OnConnected { add { } remove { } }
            public event Action OnDisconnected { add { } remove { } }
            public event Action<CommandType, string> OnCommandReceived;
            public event Action<string> OnError { add { } remove { } }
            public event Action<string> OnAuthenticated { add { } remove { } }

            public bool IsConnected => true;
            public string SessionKey { get; private set; }
            public string LastSent { get; private set; }
            public int SendCount { get; private set; }

            private readonly ICommandProtocol _commandProtocol = new CommandProtocol();
            private readonly System.Collections.Generic.Dictionary<string, PendingAck> _pendingAcks =
                new System.Collections.Generic.Dictionary<string, PendingAck>();
            private readonly object _lock = new object();

            public void Connect(WebSocketConfig config) { }
            public void Disconnect() { }
            public void Update() { }

            public async System.Threading.Tasks.Task<string> SendCommandAsync(
                CommandType commandType,
                object payload,
                TimeSpan? ackTimeout = null,
                System.Threading.CancellationToken cancellationToken = default)
            {
                var (reqId, serialized) = _commandProtocol.BuildCommand(commandType, payload);
                LastSent = serialized;
                SendCount++;

                if (!commandType.IsAckRequired())
                {
                    return null;
                }

                var tcs = new System.Threading.Tasks.TaskCompletionSource<string>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
                var timeoutCts = new System.Threading.CancellationTokenSource();
                
                lock (_lock)
                {
                    _pendingAcks[reqId] = new PendingAck(tcs, timeoutCts);
                }

                timeoutCts.CancelAfter(ackTimeout ?? TimeSpan.FromSeconds(5));
                timeoutCts.Token.Register(() =>
                {
                    CompletePendingAck(reqId, null, cancelTimeout: false);
                });

                return await tcs.Task;
            }

            public void SimulateIncoming(string message)
            {
                var commandType = CommandParser.ExtractCommandType(message);
                if (commandType == CommandType.MESG)
                {
                    var payload = CommandParser.ExtractPayload(message);
                    var reqId = ExtractReqIdFromPayload(payload);
                    if (!string.IsNullOrEmpty(reqId))
                    {
                        // Check if this is an ACK response (has req_id and matches pending request)
                        CompletePendingAck(reqId, payload, cancelTimeout: true);
                    }
                }
                else if (commandType != null)
                {
                    OnCommandReceived?.Invoke(commandType.Value, CommandParser.ExtractPayload(message));
                }
            }

            private bool CompletePendingAck(string reqId, string ackPayload, bool cancelTimeout)
            {
                PendingAck ack;
                lock (_lock)
                {
                    if (!_pendingAcks.TryGetValue(reqId, out ack))
                    {
                        return false;
                    }
                    _pendingAcks.Remove(reqId);
                }

                if (cancelTimeout)
                {
                    ack.TimeoutCts.Cancel();
                }

                ack.Tcs.TrySetResult(ackPayload);
                ack.Dispose();
                return true;
            }

            private static string ExtractReqIdFromPayload(string payload)
            {
                if (string.IsNullOrEmpty(payload)) return null;
                const string key = "\"req_id\":\"";
                var start = payload.IndexOf(key, StringComparison.Ordinal);
                if (start < 0) return null;
                start += key.Length;
                var end = payload.IndexOf('"', start);
                if (end < 0 || end <= start) return null;
                return payload.Substring(start, end - start);
            }

            private sealed class PendingAck : IDisposable
            {
                public System.Threading.Tasks.TaskCompletionSource<string> Tcs { get; }
                public System.Threading.CancellationTokenSource TimeoutCts { get; }

                public PendingAck(System.Threading.Tasks.TaskCompletionSource<string> tcs, System.Threading.CancellationTokenSource timeoutCts)
                {
                    Tcs = tcs;
                    TimeoutCts = timeoutCts;
                }

                public void Dispose()
                {
                    TimeoutCts.Dispose();
                }
            }
        }
    }
}
