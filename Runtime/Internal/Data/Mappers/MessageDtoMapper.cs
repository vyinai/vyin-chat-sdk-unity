using System;
using Newtonsoft.Json;
using VyinChatSdk.Internal.Data.DTOs;
using VyinChatSdk.Internal.Domain.Models;

namespace VyinChatSdk.Internal.Data.Mappers
{
    public static class MessageDtoMapper
    {
        public static MessageBO ToBusinessObject(MessageDTO dto)
        {
            if (dto == null)
                return null;

            return new MessageBO
            {
                MessageId = dto.message_id != 0 ? dto.message_id : dto.msg_id,
                Message = dto.message,
                ChannelUrl = dto.channel_url,
                CreatedAt = dto.created_at != 0 ? dto.created_at : dto.ts,
                Done = dto.done,
                CustomType = dto.custom_type ?? "",
                Data = dto.data ?? "",
                ReqId = dto.request_id ?? dto.req_id ?? "",
                Sender = ToSenderBO(dto.user)
            };
        }

        public static SenderBO ToSenderBO(SenderDTO dto)
        {
            if (dto == null)
                return null;

            return new SenderBO
            {
                UserId = dto.user_id ?? dto.guest_id ?? "",
                Nickname = dto.nickname ?? "",
                ProfileUrl = dto.profile_url ?? "",
                Role = ParseRole(dto.role)
            };
        }

        public static MessageDTO ParseFromJson(string json, string fallbackChannelUrl = null, string fallbackMessage = null)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            var dto = JsonConvert.DeserializeObject<MessageDTO>(json);
            if (dto == null)
                return null;

            if (string.IsNullOrEmpty(dto.channel_url))
                dto.channel_url = fallbackChannelUrl;

            if (string.IsNullOrEmpty(dto.message))
                dto.message = fallbackMessage;

            if (dto.created_at == 0 && dto.ts == 0)
                dto.created_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return dto;
        }

        private static RoleBO ParseRole(string role)
        {
            if (string.IsNullOrEmpty(role))
                return RoleBO.None;

            return role.ToLowerInvariant() switch
            {
                "operator" => RoleBO.Operator,
                _ => RoleBO.None
            };
        }
    }
}
