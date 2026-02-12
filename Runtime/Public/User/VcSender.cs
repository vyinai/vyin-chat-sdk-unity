namespace VyinChatSdk
{
    /// <summary>
    /// Defines the role of a user in a channel
    /// </summary>
    public enum VcRole
    {
        /// <summary>
        /// Regular user with no special permissions
        /// </summary>
        None,

        /// <summary>
        /// Channel operator with administrative permissions
        /// </summary>
        Operator
    }

    /// <summary>
    /// Represents the sender of a message, extending <see cref="VcUser"/> with role information
    /// </summary>
    public class VcSender : VcUser
    {
        /// <summary>
        /// Role of the sender in the channel
        /// </summary>
        public VcRole Role { get; set; } = VcRole.None;
    }
}
