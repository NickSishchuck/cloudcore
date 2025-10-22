namespace CloudCore.Contracts.Responses
{
    public class TeamspaceResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int AdminUserId { get; set; }
        public string AdminUsername { get; set; } = null!;
        public long StorageLimitMb { get; set; }
        public long StorageUsedMb { get; set; }
        public int MemberLimit { get; set; }
        public int MemberCount { get; set; }

        // Current user's role in this teamspace
        public string UserPermission { get; set; } = null!;
        public bool IsAdmin { get; set; }
    }
}