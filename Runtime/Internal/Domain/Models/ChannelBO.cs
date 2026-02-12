namespace VyinChatSdk.Internal.Domain.Models
{
    /// <summary>
    /// Channel Business Object (Domain Layer)
    /// Pure C# business entity, no external dependencies
    /// </summary>
    public class ChannelBO
    {
        public string ChannelUrl { get; set; }
        public string Name { get; set; }
        public string CoverUrl { get; set; }
        public string CustomType { get; set; }
        public bool IsDistinct { get; set; }
        public bool IsPublic { get; set; }
        public int MemberCount { get; set; }
        public long CreatedAt { get; set; }
        public RoleBO MyRole { get; set; } = RoleBO.None;
    }
}
