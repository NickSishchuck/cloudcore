namespace CloudCore.Contracts.Responses
{
    public class TeamspaceMemberResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PermissionLevel { get; set; } = null!;
        public int? InvitedBy { get; set; }
        public string? InvitedByUsername { get; set; }
        public bool IsAdmin { get; set; }
    }
}
