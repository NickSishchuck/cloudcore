namespace CloudCore.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<TeamspaceLimits> GetTeamspaceLimitsAsync(int userId);
        Task<bool> CanCreateTeamspaceAsync(int userId);
    }

    public class TeamspaceLimits
    {
        public long StorageLimitMb { get; set; }
        public int MemberLimit { get; set; }
        public int MaxTeamspaces { get; set; }
    }
}


