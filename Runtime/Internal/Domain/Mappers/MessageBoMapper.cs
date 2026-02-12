using VyinChatSdk.Internal.Domain.Models;

namespace VyinChatSdk.Internal.Domain.Mappers
{
    public static class MessageBoMapper
    {
        public static VcBaseMessage ToPublicModel(MessageBO bo)
        {
            if (bo == null)
                return null;

            return new VcBaseMessage
            {
                MessageId = bo.MessageId,
                Message = bo.Message,
                ChannelUrl = bo.ChannelUrl,
                CreatedAt = bo.CreatedAt,
                Done = bo.Done,
                CustomType = bo.CustomType,
                Data = bo.Data,
                ReqId = bo.ReqId,
                Sender = ToPublicSender(bo.Sender)
            };
        }

        public static VcSender ToPublicSender(SenderBO bo)
        {
            if (bo == null)
                return null;

            return new VcSender
            {
                UserId = bo.UserId,
                Nickname = bo.Nickname,
                ProfileUrl = bo.ProfileUrl,
                Role = ToPublicRole(bo.Role)
            };
        }

        internal static VcRole ToPublicRole(RoleBO role)
        {
            return role switch
            {
                RoleBO.Operator => VcRole.Operator,
                _ => VcRole.None
            };
        }

        public static MessageBO ToBusinessObject(VcBaseMessage model)
        {
            if (model == null)
                return null;

            return new MessageBO
            {
                MessageId = model.MessageId,
                Message = model.Message,
                ChannelUrl = model.ChannelUrl,
                CreatedAt = model.CreatedAt,
                Done = model.Done,
                CustomType = model.CustomType,
                Data = model.Data,
                ReqId = model.ReqId,
                Sender = ToSenderBO(model.Sender)
            };
        }

        public static SenderBO ToSenderBO(VcSender model)
        {
            if (model == null)
                return null;

            return new SenderBO
            {
                UserId = model.UserId,
                Nickname = model.Nickname,
                ProfileUrl = model.ProfileUrl,
                Role = ToRoleBO(model.Role)
            };
        }

        internal static RoleBO ToRoleBO(VcRole role)
        {
            return role switch
            {
                VcRole.Operator => RoleBO.Operator,
                _ => RoleBO.None
            };
        }
    }
}
