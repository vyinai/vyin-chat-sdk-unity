using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VyinChatSdk.Internal.Domain.Commands
{
    /// <summary>
    /// Basic command protocol: generates req_id and serializes payload with command prefix.
    /// </summary>
    public class CommandProtocol : ICommandProtocol
    {
        public (string reqId, string serialized) BuildCommand(CommandType commandType, object payload)
        {
            if (commandType == CommandType.NONE)
            {
                throw new ArgumentException("CommandType cannot be NONE.", nameof(commandType));
            }

            var reqId = GenerateReqId();
            var jsonBody = BuildJsonWithReqId(payload, reqId);
            var serialized = $"{commandType}{jsonBody}";

            return (reqId, serialized);
        }

        private static string GenerateReqId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static string BuildJsonWithReqId(object payload, string reqId)
        {
            JObject body = payload == null
                ? new JObject()
                : JObject.FromObject(payload);

            body["req_id"] = reqId;
            return body.ToString(Formatting.None);
        }
    }
}

