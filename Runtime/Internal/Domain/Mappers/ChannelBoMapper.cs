using VyinChatSdk.Internal.Domain.Models;

namespace VyinChatSdk.Internal.Domain.Mappers
{
    /// <summary>
    /// Mapper for converting between ChannelBO and VcGroupChannel (Public API Model)
    /// Domain Layer responsibility: Business Object ↔ Public Model conversion
    /// Used by UseCases to convert internal BO to public-facing models
    /// </summary>
    public static class ChannelBoMapper
    {
        /// <summary>
        /// Convert ChannelBO to VcGroupChannel (Public API Model)
        /// </summary>
        public static VcGroupChannel ToPublicModel(ChannelBO bo)
        {
            if (bo == null)
            {
                return null;
            }

            return new VcGroupChannel
            {
                ChannelUrl = bo.ChannelUrl,
                Name = bo.Name,
                CoverUrl = bo.CoverUrl,
                CustomType = bo.CustomType,
                IsDistinct = bo.IsDistinct,
                IsPublic = bo.IsPublic,
                MemberCount = bo.MemberCount,
                CreatedAt = bo.CreatedAt,
                MyRole = MessageBoMapper.ToPublicRole(bo.MyRole)
            };
        }

        /// <summary>
        /// Convert VcGroupChannel to ChannelBO
        /// Used for input parameters (e.g., update operations)
        /// </summary>
        public static ChannelBO ToBusinessObject(VcGroupChannel model)
        {
            if (model == null)
            {
                return null;
            }

            return new ChannelBO
            {
                ChannelUrl = model.ChannelUrl,
                Name = model.Name,
                CoverUrl = model.CoverUrl,
                CustomType = model.CustomType,
                IsDistinct = model.IsDistinct,
                IsPublic = model.IsPublic,
                MemberCount = model.MemberCount,
                CreatedAt = model.CreatedAt,
                MyRole = MessageBoMapper.ToRoleBO(model.MyRole)
            };
        }
    }
}
