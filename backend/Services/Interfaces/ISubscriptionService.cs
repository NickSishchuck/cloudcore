using CloudCore.Common.Models;

namespace CloudCore.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<TeamspaceLimits> GetTeamspaceLimitsAsync(int userId);
        Task<bool> CanCreateTeamspaceAsync(int userId);
    }
}