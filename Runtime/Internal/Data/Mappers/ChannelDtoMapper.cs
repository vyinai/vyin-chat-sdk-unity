using VyinChatSdk.Internal.Data.DTOs;
using VyinChatSdk.Internal.Domain.Models;

namespace VyinChatSdk.Internal.Data.Mappers
{
    /// <summary>
    /// Mapper for converting between ChannelDTO and ChannelBO
    /// Data Layer responsibility: DTO ↔ Business Object conversion
    /// </summary>
    public static class ChannelDtoMapper
    {
        /// <summary>
        /// Convert ChannelDTO to ChannelBO (Business Object)
        /// </summary>
        public static ChannelBO ToBusinessObject(ChannelDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new ChannelBO
            {
                ChannelUrl = dto.channel_url,
                Name = dto.name,
                CoverUrl = dto.cover_url,
                CustomType = dto.custom_type,
                IsDistinct = dto.is_distinct,
                IsPublic = dto.is_public,
                MemberCount = dto.member_count,
                CreatedAt = dto.created_at,
                MyRole = MessageDtoMapper.ParseRole(dto.my_role)
            };
        }

        /// <summary>
        /// Convert ChannelBO to ChannelDTO
        /// </summary>
        public static ChannelDTO ToDto(ChannelBO bo)
        {
            if (bo == null)
            {
                return null;
            }

            return new ChannelDTO
            {
                channel_url = bo.ChannelUrl,
                name = bo.Name,
                cover_url = bo.CoverUrl,
                custom_type = bo.CustomType,
                is_distinct = bo.IsDistinct,
                is_public = bo.IsPublic,
                member_count = bo.MemberCount,
                created_at = bo.CreatedAt
            };
        }
    }
}
