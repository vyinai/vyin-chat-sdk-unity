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
                Name = bo.Name
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
                Name = model.Name
            };
        }
    }
}
