using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloudCore.Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IDbContextFactory<CloudCoreDbContext> dbContextFactory,
            ILogger<SubscriptionService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        // public async Task<TeamspaceLimits> GetPrivateLimits(int userId)
        // {
        //     using var context = _dbContextFactory.CreateDbContext();
        //
        //     var user = await context.Users
        //     .AsNoTracking()
        //     .FirstOrDefaultAsync(u => u.Id == userId);
        //
        //     if (user == null)
        //         throw new InvalidOperationException("User not found"); //FIXME: Use the error handler
        //
        //     return user.SubscriptionPlan switch
        //     {
        //         "free" => new PrivateLimits
        //         {
        //             StorageLimitMb = 10240    // 10 GB
        //         },
        //         "premium" => new PrivateLimits
        //         {
        //             StorageLimitMb = 20480    // 20 GB
        //         },
        //         "enterprise" => new PrivateLimits
        //         {
        //             StorageLimitMb = 51200    // 50 GB
        //         },
        //         _ => throw new InvalidOperationException("Invalid subscription plan") //FIXME: use the error handler
        //     };
        // }
        //
        //  public async Task<bool> IsExceedingPrivateLimit(int userId)
        // {
        //     using var context = _dbContextFactory.CreateDbContext();
        //
        //     var user = await context.Users
        //       .AsNoTracking()
        //       .FirstOrDefaultAsync(u => u.Id == userId);
        //
        //     if (userId == null)
        //         return false;
        //
        //     //TODO: 
        // }

        public async Task<TeamspaceLimits> GetTeamspaceLimitsAsync(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User not found"); //FIXME: Use the erro handler

            return user.SubscriptionPlan switch
            {
                "free" => new TeamspaceLimits
                {
                    StorageLimitMb = 5120,        // 5 GB
                    MemberLimit = 5,               // 5 members
                    MaxTeamspaces = 2              // 2 teamspaces max
                },
                "premium" => new TeamspaceLimits
                {
                    StorageLimitMb = 51200,       // 50 GB
                    MemberLimit = 25,              // 25 members
                    MaxTeamspaces = 10             // 10 teamspaces max
                },
                "enterprise" => new TeamspaceLimits
                {
                    StorageLimitMb = 512000,      // 500 GB
                    MemberLimit = 100,             // 100 members
                    MaxTeamspaces = -1             // Unlimited
                },
                _ => throw new InvalidOperationException("Invalid subscription plan") //FIXME: Use the error handler
            };
        }

        public async Task<bool> CanCreateTeamspaceAsync(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var user = await context.Users
                .AsNoTracking()
                .Include(u => u.Teamspaces)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            var limits = await GetTeamspaceLimitsAsync(userId);

            // Check if unlimited (-1) or under limit
            if (limits.MaxTeamspaces == -1)
                return true;

            return user.TeamspacesOwned < limits.MaxTeamspaces;
        }
    }
}
