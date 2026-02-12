using VyinChatSdk.Internal.Data.DTOs;
using VyinChatSdk.Internal.Domain.Models;

namespace VyinChatSdk.Internal.Data.Mappers
{
    /// <summary>
    /// Mapper for converting between MessageDTO and MessageBO
    /// Data Layer responsibility: DTO ↔ Business Object conversion only
    /// JSON parsing is done by the caller (Repository, VyinChatMain)
    /// </summary>
    internal static class MessageDtoMapper
    {
        /// <summary>
        /// Convert MessageDTO to MessageBO (Business Object)
        /// </summary>
        internal static MessageBO ToBusinessObject(MessageDTO dto)
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

        /// <summary>
        /// Convert SenderDTO to SenderBO
        /// </summary>
        internal static SenderBO ToSenderBO(SenderDTO dto)
        {
            if (dto == null)
                return null;

            return new SenderBO
            {
                UserId = dto.user_id ?? dto.guest_id ?? "",
                Nickname = dto.name ?? dto.nickname ?? "",
                ProfileUrl = dto.image ?? dto.profile_url ?? "",
                Role = ParseRole(dto.role)
            };
        }

        internal static RoleBO ParseRole(string role)
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
