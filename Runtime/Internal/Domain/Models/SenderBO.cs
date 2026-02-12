namespace VyinChatSdk.Internal.Domain.Models
{
    public enum RoleBO
    {
        None,
        Operator
    }

    public class SenderBO
    {
        public string UserId { get; set; }
        public string Nickname { get; set; }
        public string ProfileUrl { get; set; }
        public RoleBO Role { get; set; } = RoleBO.None;
    }
}
