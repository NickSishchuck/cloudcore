using System.IO;
using CloudCore.Common.Models;
using CloudCore.Contracts.Responses;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Linq;
using Sprache;
using NaturalSort.Extension;

namespace CloudCore.Services.Implementations
{
    public class DbRepository : IItemRepository, ISubscriptionService
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly ILogger<DbRepository> _logger;

        public DbRepository(IDbContextFactory<CloudCoreDbContext> dbContextFactory, ILogger<DbRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async IAsyncEnumerable<Item> GetAllChildItemsAsync(int userId, int parentId, int maxDepth = 10000)
        {
            var context = _dbContextFactory.CreateDbContext();
            try
            {
            var userIdParam = new MySqlParameter("@UserId", userId);
            var parentIdParam = new MySqlParameter("@ParentId", parentId);
            var maxDepthParam = new MySqlParameter("@MaxDepth", maxDepth);

            var sql = @" WITH RECURSIVE ItemsHierarchy AS (SELECT id, name, type, parent_id, user_id, teamspace_id, file_path, file_size, mime_type, created_at, updated_at, deleted_at, access_level, is_deleted, 1 as level
                FROM items
                WHERE user_id = @UserId AND parent_id = @ParentId
                UNION ALL

                SELECT i.id, i.name, i.type, i.parent_id, i.user_id, i.teamspace_id, i.file_path, i.file_size, i.mime_type, i.created_at, i.updated_at, i.deleted_at, i.access_level, i.is_deleted, ih.level + 1
                FROM items i
                INNER JOIN ItemsHierarchy ih ON i.parent_id = ih.id
                WHERE i.user_id = @UserId AND ih.type = 'folder' AND ih.level < @MaxDepth)

                SELECT id, name, type, parent_id, user_id, teamspace_id, file_path, file_size, mime_type, created_at, updated_at, deleted_at, access_level, is_deleted
                FROM ItemsHierarchy 
                ORDER BY Level, Type DESC, Name;"
            ;

            await foreach (var item in context.Items.FromSqlRaw(sql, userIdParam, parentIdParam, maxDepthParam)
                                                    .AsNoTracking()
                                                    .AsAsyncEnumerable())
            {
                    yield return item;
            }

            }
            finally
            {
                await context.DisposeAsync();
            }
        }

        public async IAsyncEnumerable<Item> GetDirectChildrenAsync(int userId, int? parentId, string? itemType = null, bool includeDeleted = false)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var query = context.Items
                .AsNoTracking()
                .Where(i => i.UserId == userId && i.ParentId == parentId);

            if(!string.IsNullOrEmpty(itemType))
            {
                query = query.Where(i => i.Type == itemType);
            }

            if (includeDeleted == false)
            {
                query = query.Where(i => i.IsDeleted == false);
            }

            await foreach (var item in query.AsAsyncEnumerable())
            {
                yield return item;
            }
        }

        public async Task<(IEnumerable<Item> Items, int TotalCount)> GetItemsAsync(int userId, int? parentId, int page, int pageSize, string? sortBy, string? sortDir, bool isTrashFolder = false, string? searchQuery = null, int? teamspaceId = null)
        {
            _logger.LogInformation("Fetching items. UserId={UserId}, ParentId={ParentId}, Page={Page}, PageSize={PageSize}, SortBy={SortBy}, SortDir={SortDir}, IsTrashFolder={IsTrashFolder}", userId, parentId, page, pageSize, sortBy, sortDir, isTrashFolder);

            using var context = _dbContextFactory.CreateDbContext();

            var query = context.Items
                .AsNoTracking()
                .Where(i => i.UserId == userId);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                _logger.LogInformation("Searching items for UserId={UserId}, Query={SearchQuery}", userId, searchQuery);
                query = query.Where(i => EF.Functions.Like(i.Name.ToLower(), $"%{searchQuery.ToLower()}%"));
            }
            if (teamspaceId.HasValue)
            {
                query = query.Where(i => i.TeamspaceId == teamspaceId);
            }


            if (isTrashFolder == true)
            {
                _logger.LogInformation("Querying trash folder items for UserId={UserId}", userId);
                query = query.Where(i => i.IsDeleted == true && (i.ParentId == null || context.Items
                    .AsNoTracking()
                    .Any(p => p.Id == i.ParentId && p.IsDeleted == false)));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    query = query.Where(i => i.IsDeleted == false && i.ParentId == parentId);
                }
                else
                {
                    query = query.Where(i => i.IsDeleted == false);
                }
            }


            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var allItems = await query.ToListAsync();
            var totalCount = allItems.Count;

            IOrderedEnumerable<Item> orderedItems = allItems.OrderBy(i => i.Type == "folder" ? 0 : 1);

            switch ((sortBy ?? "name").ToLowerInvariant())
            {
                case "size":
                case "filesize":
                    orderedItems = desc
                        ? orderedItems.ThenByDescending(i => i.FileSize ?? 0)
                        : orderedItems.ThenBy(i => i.FileSize ?? 0);
                    break;

                case "modified":
                case "updatedat":
                    orderedItems = desc
                        ? orderedItems.ThenByDescending(i => i.UpdatedAt)
                        : orderedItems.ThenBy(i => i.UpdatedAt);
                    break;

                case "created":
                case "createdat":
                    orderedItems = desc
                        ? orderedItems.ThenByDescending(i => i.CreatedAt)
                        : orderedItems.ThenBy(i => i.CreatedAt);
                    break;

                case "name":
                default:
                    orderedItems = desc
                        ? orderedItems.ThenByDescending(i => i.Name, StringComparison.OrdinalIgnoreCase.WithNaturalSort())
                        : orderedItems.ThenBy(i => i.Name, StringComparison.OrdinalIgnoreCase.WithNaturalSort());
                    break;
            }


            var items = orderedItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogInformation("Fetched {Count} items. UserId={UserId}, ParentId={ParentId}, TotalCount={TotalCount}", items.Count, userId, parentId, totalCount);

            return (items, totalCount);
        }

        public async Task<Item?> GetItemAsync(int userId, int itemId, string? itemType)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var query = context.Items
                .AsNoTracking()
                .Where(i => i.Id == itemId && i.UserId == userId);
            if (!string.IsNullOrWhiteSpace(itemType))
            {
                query = query.Where(i => i.Type == itemType);
            }

            _logger.LogInformation("Fetching item. UserId={UserId}, ItemId={ItemId}", userId, itemId);

            var item = await query.FirstOrDefaultAsync();
            if (item == null)
                _logger.LogWarning("Item not found. UserId={UserId}, ItemId={ItemId}, ItemType={ItemType}", userId, itemId, itemType);
            else
                _logger.LogInformation("Item retrieved successfully. ItemId={ItemId}, Name={Name}, Type={Type}", item.Id, item.Name, item.Type);

            return item;
        }

        public async Task<Item?> GetItemByNameAsync(int userId, string name, int? parentId, int? teamspaceId = null)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var query = context.Items
                .AsNoTracking()
                .Where(i => i.UserId == userId && i.Name.ToLower() == name.ToLower());

            if (parentId.HasValue)
                query = query.Where(i => i.ParentId == parentId.Value);
            else
                query = query.Where(i => i.ParentId == null);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<Item?> GetDeletedItemAsync(int userId, int itemId)
        {
            _logger.LogInformation("Fetching deleted item. UserId={UserId}, ItemId={ItemId}", userId, itemId);
            using var context = _dbContextFactory.CreateDbContext();

            var item = await context.Items
                .AsNoTracking()
                .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == true)
                .FirstOrDefaultAsync();
            if (item == null)
                _logger.LogWarning("Item not found. UserId={UserId}, ItemId={ItemId}", userId, itemId);
            else
                _logger.LogInformation("Item retrieved successfully. ItemId={ItemId}, Name={Name}", item.Id, item.Name);

            return item;
        }

        public async IAsyncEnumerable<Item> GetItemsByIdsForUserAsync(int userId, List<int> itemsIds)
        {
            if (itemsIds == null || itemsIds.Count == 0)
            {
                _logger.LogWarning("GetItemsByIdsForUserAsync called with empty or null IDs for UserId: {UserId}", userId);
                yield break;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync();

            await foreach (var item in context.Items
                .AsNoTracking()
                .Where(i => i.UserId == userId && i.IsDeleted == false && itemsIds.Contains(i.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }
        }

        public async Task<IEnumerable<Item>> GetDeletedItemsByIdsAsync(List<int> itemsIds)
        {
            if (itemsIds == null || itemsIds.Count == 0)
            {
                _logger.LogWarning("GetDeletedItemsByIdsAsync called with an empty or null list of IDs.");
                return Enumerable.Empty<Item>();
            }

            _logger.LogInformation("Fetching {ItemCount} deleted items by IDs.", itemsIds.Count);

            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var items = await context.Items
                .AsNoTracking()
                .Where(i => i.IsDeleted == true && itemsIds.Contains(i.Id))
                .ToListAsync();

            _logger.LogInformation("Found {FoundCount} out of {RequestedCount} items.", items.Count, itemsIds.Count);

            return items;

        }

        public async Task<string> GetFolderPathAsync(Item folder)
        {
            _logger.LogInformation("Building folder path. FolderId={FolderId}, Name={FolderName}", folder.Id, folder.Name);

            using var context = _dbContextFactory.CreateDbContext();
            var sql = @"
                WITH RECURSIVE FolderHierarchy AS (
                    SELECT id, name, parent_id, 0 as level
                    FROM items 
                    WHERE id = @folderId
            
                    UNION ALL
            
                    SELECT p.id, p.name, p.parent_id, fh.level + 1
                    FROM items p
                    INNER JOIN FolderHierarchy fh ON p.id = fh.parent_id
                )
                SELECT name 
                FROM FolderHierarchy 
                ORDER BY level DESC";

            var folderIdParam = new MySqlParameter("@folderId", folder.Id);

            var pathParts = await context.Database
                .SqlQueryRaw<string>(sql, folderIdParam)
                .ToListAsync();

            var path = Path.Combine(pathParts.ToArray());
            _logger.LogInformation("Folder path built: {Path}", path);
            // WITHOUT USERPATH!!!
            return path;
        }

        public async Task<string> GetBreadcrumbPathAsync(Item folder)
        {
            var fullPath = await GetFolderPathAsync(folder);

            _logger.LogInformation("Building breadcrumb path for Folder ID: {FolderId}", folder.Id);
            var breadcrumb = fullPath.Split(Path.DirectorySeparatorChar).ToList();
            _logger.LogInformation("Builded breadcrumb path for Folder ID: {FolderId}, Path: {Path}", folder.Id, breadcrumb);
            return fullPath;
        }


        public async Task<bool> IsNameUniqueAsync(string name, int userId, string itemType, int? parentId, int? excludeItemId = null)
        {
            _logger.LogInformation(
            "Checking name uniqueness. Name: {Name}, Type: {ItemType}, UserId: {UserId}, ParentId: {ParentId}, ExcludeItemId: {ExcludeItemId}",
            name, itemType, userId, parentId, excludeItemId);

            var context = _dbContextFactory.CreateDbContext();
            var query = context.Items
                .AsNoTracking()
                .Where(i => i.Name == name && i.Type == itemType && i.UserId == userId && i.ParentId == parentId && i.IsDeleted == false);

            if (excludeItemId.HasValue)
            {
                query = query.Where(i => i.Id != excludeItemId.Value);
            }

            var isDuplicate = await query.AnyAsync();

            if (isDuplicate)
            {
                _logger.LogWarning(
                    "Name uniqueness check failed: An item of type {ItemType} with name '{Name}' already exists for UserId {UserId} in ParentId {ParentId}.",
                    itemType, name, userId, parentId);
                return false;
            }
            else
            {
                _logger.LogInformation(
                    "Name uniqueness check successful: Name '{Name}' is available for {ItemType} for UserId {UserId}.",
                    name, itemType, userId);
                return true;
            }
        }

        public async Task<bool> ItemExistsAsync(int itemId, int userId, string? itemType)
        {
            _logger.LogInformation("Checking existence for ItemId: {ItemId}, UserId: {UserId}, Type: {ItemType}", itemId, userId, itemType);

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var query = context.Items
                .AsNoTracking()
                .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == false);

            if (!string.IsNullOrEmpty(itemType))
            {
                query = query.Where(i => i.Type == itemType);
            }

            var exists = await query.AnyAsync();

            _logger.LogInformation("Item {ItemId} existence check result: {Exists}", itemId, exists);
            return exists;

        }

        public async Task<int> CountExistingItemsAsync(List<int> itemIds, int userId)
        {
            int providedCount = itemIds?.Count ?? 0;
            _logger.LogInformation("Counting existing items for UserId: {UserId}. Provided IDs count: {ProvidedCount}", userId, providedCount);

            if (providedCount == 0)
            {
                _logger.LogWarning("No item IDs provided to count for UserId: {UserId}", userId);
                return 0;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var foundCount = await context.Items
                .AsNoTracking()
                .CountAsync(i => itemIds.Contains(i.Id) && i.UserId == userId && i.IsDeleted == false);

            _logger.LogInformation("Found {FoundCount} existing items out of {ProvidedCount} provided for UserId: {UserId}", foundCount, providedCount, userId);
            return foundCount;
        }

        public async Task<bool> DoesItemExistByNameAsync(string name, string itemType, int userId, int? parentId, int? excludeItemId = null, bool includeDeleted = false)
        {

            _logger.LogInformation("Checking for item by name. Name: '{Name}', Type: {ItemType}, UserId: {UserId}, ParentId: {ParentId}, IncludeDeleted: {IncludeDeleted}", name, itemType, userId, parentId, includeDeleted);

            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var query = context.Items
                .AsNoTracking()
                .Where(i => i.Name == name && i.Type == itemType && i.UserId == userId && i.ParentId == parentId);

            if (!includeDeleted)
            {
                query = query.Where(i => i.IsDeleted == false);
            }

            if (excludeItemId.HasValue)
            {
                query = query.Where(i => i.Id != excludeItemId.Value);
            }

            var exists = await query.AnyAsync();

            if (exists)
                _logger.LogWarning("Duplicate found for name '{Name}' with IncludeDeleted={IncludeDeleted}", name, includeDeleted);
            else
                _logger.LogInformation("No duplicate found for name '{Name}' with IncludeDeleted={IncludeDeleted}", name, includeDeleted);

            return exists;
        }

        public async Task<(long totalSize, int fileCount)> CalculateArchiveSizeAsync(int userId, int? folderId)
        {
            long totalSize = 0;
            int fileCount = 0;

            await foreach (var item in folderId.HasValue
                ? GetAllChildItemsAsync(userId, folderId.Value)
                : _dbContextFactory.CreateDbContext().Items
                    .AsNoTracking()
                    .Where(i => i.UserId == userId && i.IsDeleted == false && i.ParentId == folderId)
                    .AsAsyncEnumerable())
            {
                if (item.Type == "file")
                {
                    totalSize += item.FileSize ?? 0;
                    fileCount++;
                }
            }

            return (totalSize, fileCount);
        }
        public async Task AddItemInTranscationAsync(Item item)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            _logger.LogInformation("Starting transaction to add ItemName={ItemName}.", item.Name);
            try
            {
                context.Add(item);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully. Item added.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed. Failed to add item");
                throw;
            }
        }

        public async Task UpdateItemsInTransactionAsync(IAsyncEnumerable<Item> items, int batchSize = 500)
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            _logger.LogInformation("Starting transaction to update items lazily.");
            try
            {
                var batch = new List<Item>(batchSize);
                int totalProcessed = 0;

                await foreach (var item in items)
                {
                    batch.Add(item);

                    if (batch.Count >= batchSize)
                    {
                        context.UpdateRange(batch);
                        await context.SaveChangesAsync();
                        totalProcessed += batch.Count;
                        _logger.LogDebug("Saved batch of {Count} items, total: {Total}", batch.Count, totalProcessed);
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                {
                    context.UpdateRange(batch);
                    await context.SaveChangesAsync();
                    totalProcessed += batch.Count;
                    _logger.LogDebug("Saved final batch of {Count} items", batch.Count);
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully. Total items updated: {Total}", totalProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed. Rolling back changes.");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteItemPermanentlyAsync(Item item)
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            _logger.LogInformation("Starting transaction to delete ItemID={ItemId}.", item.Id);
            try
            {
                context.Remove(item);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully. Deleted ItemID={ItemId}.", item.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed. Rolled back changes for ItemID={ItemId}.", item.Id);
                throw;
            }
        }

        public async Task<List<int>> GetExpiredItemIdsAsync(DateTime thresholdDate)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Items
                .AsNoTracking()
                .Where(i => i.IsDeleted == true && i.DeletedAt <= thresholdDate)
                .Select(i => i.Id)
                .ToListAsync();
        }

        public async Task<int> DeleteItemsByIdsAsync(List<int> itemIds)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Items
                .Where(i => itemIds.Contains(i.Id))
                .ExecuteDeleteAsync();
        }


        public async Task<bool> IsFolderSubFolderAsync(int userId, int parentFolderId, int childFolderId)
        {
            _logger.LogInformation("Checking if folder {ChildFolderId} is subfolder of {ParentFolderId}", childFolderId, parentFolderId);

            using var context = _dbContextFactory.CreateDbContext();

            var userIdParam = new MySqlParameter("@UserId", userId);
            var parentIdParam = new MySqlParameter("@ParentFolderId", parentFolderId);
            var childIdParam = new MySqlParameter("@ChildFolderId", childFolderId);

            var sql = @"
        WITH RECURSIVE ItemsHierarchy AS (
            SELECT id
            FROM items
            WHERE user_id = @UserId AND parent_id = @ParentFolderId AND type = 'folder'
            
            UNION ALL
            
            SELECT i.id
            FROM items i
            INNER JOIN ItemsHierarchy ih ON i.parent_id = ih.id
            WHERE i.user_id = @UserId AND i.type = 'folder'
        )
        SELECT EXISTS(SELECT 1 FROM ItemsHierarchy WHERE id = @ChildFolderId) AS Value";

            var result = await context.Database
                .SqlQueryRaw<int>(sql, userIdParam, parentIdParam, childIdParam)
                .FirstOrDefaultAsync();

            return result == 1;
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
