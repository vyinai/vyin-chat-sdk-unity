namespace VyinChatSdk
{
    public enum VcRole
    {
        None,
        Operator
    }

    public class VcSender : VcUser
    {
        public VcRole Role { get; set; } = VcRole.None;
    }
}
