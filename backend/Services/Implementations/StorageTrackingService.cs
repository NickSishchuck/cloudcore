using CloudCore.Common.Errors;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudCore.Services.Implementations
{
    public class StorageTrackingService : IStorageTrackingService
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<StorageTrackingService> _logger;

        private const long BYTES_PER_MB = 1024 * 1024;

        public StorageTrackingService(
            IDbContextFactory<CloudCoreDbContext> dbContextFactory,
            ISubscriptionService subscriptionService,
            ILogger<StorageTrackingService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        #region Personal Storage

        public async Task AddToPersonalStorageAsync(int userId, long fileSizeBytes)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User {UserId} not found when adding storage", userId);
                    throw new InvalidOperationException($"User {userId} not found");
                }

                long fileSizeMb = fileSizeBytes / BYTES_PER_MB;
                user.PersonalStorageUsedMb = (user.PersonalStorageUsedMb ?? 0) + fileSizeMb;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Added {SizeMb}MB to user {UserId} storage. New total: {TotalMb}MB",
                    fileSizeMb, userId, user.PersonalStorageUsedMb);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to add storage for user {UserId}", userId);
                throw;
            }
        }

        public async Task RemoveFromPersonalStorageAsync(int userId, long fileSizeBytes)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found when removing storage", userId);
                    return;
                }

                long fileSizeMb = fileSizeBytes / BYTES_PER_MB;
                user.PersonalStorageUsedMb = Math.Max(0, (user.PersonalStorageUsedMb ?? 0) - fileSizeMb);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Removed {SizeMb}MB from user {UserId} storage. New total: {TotalMb}MB",
                    fileSizeMb, userId, user.PersonalStorageUsedMb);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to remove storage for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> CanAddToPersonalStorageAsync(int userId, long fileSizeBytes)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when checking storage limit", userId);
                return false;
            }

            // Get storage limit based on subscription
            var limit = await GetPersonalStorageLimitAsync(userId);

            long fileSizeMb = fileSizeBytes / BYTES_PER_MB;
            long currentUsage = user.PersonalStorageUsedMb ?? 0;
            long newTotal = currentUsage + fileSizeMb;

            bool canAdd = newTotal <= limit;

            _logger.LogInformation(
                "Storage check for user {UserId}: Current={CurrentMb}MB, Adding={AddMb}MB, Limit={LimitMb}MB, CanAdd={CanAdd}",
                userId, currentUsage, fileSizeMb, limit, canAdd);

            return canAdd;
        }

        public async Task<(long usedMb, long limitMb)> GetPersonalStorageInfoAsync(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when getting storage info", userId);
                return (0, 0);
            }

            var limit = await GetPersonalStorageLimitAsync(userId);
            return (user.PersonalStorageUsedMb ?? 0, limit);
        }

        private async Task<long> GetPersonalStorageLimitAsync(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return 0;

            return user.SubscriptionPlan switch
            {
                "free" => 10240,      // 10 GB
                "premium" => 20480,   // 20 GB
                "enterprise" => 51200, // 50 GB
                _ => 10240
            };
        }

        #endregion

        #region Teamspace Storage

        public async Task AddToTeamspaceStorageAsync(int teamspaceId, long fileSizeBytes)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var teamspace = await context.Teamspaces.FindAsync(teamspaceId);
                if (teamspace == null)
                {
                    _logger.LogError("Teamspace {TeamspaceId} not found when adding storage", teamspaceId);
                    throw new InvalidOperationException($"Teamspace {teamspaceId} not found");
                }

                long fileSizeMb = fileSizeBytes / BYTES_PER_MB;
                teamspace.StorageUsedMb = (teamspace.StorageUsedMb ?? 0) + fileSizeMb;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Added {SizeMb}MB to teamspace {TeamspaceId} storage. New total: {TotalMb}MB",
                    fileSizeMb, teamspaceId, teamspace.StorageUsedMb);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to add storage for teamspace {TeamspaceId}", teamspaceId);
                throw;
            }
        }

        public async Task RemoveFromTeamspaceStorageAsync(int teamspaceId, long fileSizeBytes)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var teamspace = await context.Teamspaces.FindAsync(teamspaceId);
                if (teamspace == null)
                {
                    _logger.LogWarning("Teamspace {TeamspaceId} not found when removing storage", teamspaceId);
                    return;
                }

                long fileSizeMb = fileSizeBytes / BYTES_PER_MB;
                teamspace.StorageUsedMb = Math.Max(0, (teamspace.StorageUsedMb ?? 0) - fileSizeMb);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Removed {SizeMb}MB from teamspace {TeamspaceId} storage. New total: {TotalMb}MB",
                    fileSizeMb, teamspaceId, teamspace.StorageUsedMb);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to remove storage for teamspace {TeamspaceId}", teamspaceId);
                throw;
            }
        }

        public async Task<bool> CanAddToTeamspaceStorageAsync(int teamspaceId, long fileSizeBytes)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var teamspace = await context.Teamspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == teamspaceId);

            if (teamspace == null)
            {
                _logger.LogWarning("Teamspace {TeamspaceId} not found when checking storage limit", teamspaceId);
                return false;
            }

            long fileSizeMb = fileSizeBytes / BYTES_PER_MB;
            long currentUsage = teamspace.StorageUsedMb ?? 0;
            long newTotal = currentUsage + fileSizeMb;

            bool canAdd = newTotal <= teamspace.StorageLimitMb;

            _logger.LogInformation(
                "Storage check for teamspace {TeamspaceId}: Current={CurrentMb}MB, Adding={AddMb}MB, Limit={LimitMb}MB, CanAdd={CanAdd}",
                teamspaceId, currentUsage, fileSizeMb, teamspace.StorageLimitMb, canAdd);

            return canAdd;
        }

        public async Task<(long usedMb, long limitMb)> GetTeamspaceStorageInfoAsync(int teamspaceId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var teamspace = await context.Teamspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == teamspaceId);

            if (teamspace == null)
            {
                _logger.LogWarning("Teamspace {TeamspaceId} not found when getting storage info", teamspaceId);
                return (0, 0);
            }

            return (teamspace.StorageUsedMb ?? 0, teamspace.StorageLimitMb);
        }

        #endregion

        #region Batch Operations

        public async Task UpdateStorageForItemsAsync(int userId, List<Item> items, bool isAdding)
        {
            // Calculate total size of all files
            long totalBytes = items
                .Where(i => i.Type == "file" && i.FileSize.HasValue)
                .Sum(i => i.FileSize!.Value);

            if (totalBytes == 0)
            {
                _logger.LogInformation("No storage to update for user {UserId} (no files in batch)", userId);
                return;
            }

            // Determine if items belong to a teamspace
            var firstItem = items.FirstOrDefault();
            if (firstItem?.TeamspaceId.HasValue == true)
            {
                // Teamspace items
                if (isAdding)
                    await AddToTeamspaceStorageAsync(firstItem.TeamspaceId.Value, totalBytes);
                else
                    await RemoveFromTeamspaceStorageAsync(firstItem.TeamspaceId.Value, totalBytes);
            }
            else
            {
                // Personal items
                if (isAdding)
                    await AddToPersonalStorageAsync(userId, totalBytes);
                else
                    await RemoveFromPersonalStorageAsync(userId, totalBytes);
            }
        }

        public async Task RecalculatePersonalStorageAsync(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for storage recalculation", userId);
                    return;
                }

                var actualUsageBytes = await context.Items
                    .Where(i => i.UserId == userId &&
                                i.TeamspaceId == null &&
                                i.Type == "file" &&
                                i.IsDeleted == false)
                    .SumAsync(i => i.FileSize ?? 0);

                long actualUsageMb = actualUsageBytes / BYTES_PER_MB;

                _logger.LogInformation(
                    "Recalculating storage for user {UserId}. Old: {OldMb}MB, New: {NewMb}MB",
                    userId, user.PersonalStorageUsedMb ?? 0, actualUsageMb);

                user.PersonalStorageUsedMb = actualUsageMb;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to recalculate storage for user {UserId}", userId);
                throw;
            }
        }

        public async Task RecalculateTeamspaceStorageAsync(int teamspaceId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var teamspace = await context.Teamspaces.FindAsync(teamspaceId);
                if (teamspace == null)
                {
                    _logger.LogWarning("Teamspace {TeamspaceId} not found for storage recalculation", teamspaceId);
                    return;
                }

                var actualUsageBytes = await context.Items
                    .Where(i => i.TeamspaceId == teamspaceId &&
                                i.Type == "file" &&
                                i.IsDeleted == false)
                    .SumAsync(i => i.FileSize ?? 0);

                long actualUsageMb = actualUsageBytes / BYTES_PER_MB;

                _logger.LogInformation(
                    "Recalculating storage for teamspace {TeamspaceId}. Old: {OldMb}MB, New: {NewMb}MB",
                    teamspaceId, teamspace.StorageUsedMb ?? 0, actualUsageMb);

                teamspace.StorageUsedMb = actualUsageMb;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to recalculate storage for teamspace {TeamspaceId}", teamspaceId);
                throw;
            }
        }

        #endregion
    }
}
