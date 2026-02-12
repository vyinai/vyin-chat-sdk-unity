namespace VyinChatSdk.Internal.Domain.Message
{
    internal static class VcSendingStatusExtensions
    {
        public static bool IsTerminal(this VcSendingStatus status)
        {
            return status == VcSendingStatus.Succeeded || status == VcSendingStatus.Canceled;
        }

        public static bool CanTransitionTo(this VcSendingStatus current, VcSendingStatus target)
        {
            if (current == target) return false;

            return current switch
            {
                VcSendingStatus.None => target == VcSendingStatus.Pending || target == VcSendingStatus.Canceled,
                VcSendingStatus.Pending => target == VcSendingStatus.Succeeded || target == VcSendingStatus.Failed || target == VcSendingStatus.Canceled,
                VcSendingStatus.Failed => target == VcSendingStatus.Pending || target == VcSendingStatus.Canceled, // Retry or cancel
                VcSendingStatus.Succeeded => false, // Terminal state
                VcSendingStatus.Canceled => false, // Terminal state
                _ => false
            };
        }
    }
}
