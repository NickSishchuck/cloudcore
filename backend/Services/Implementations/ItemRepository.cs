using System.IO;
using CloudCore.Contracts.Responses;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Sprache;

namespace CloudCore.Services.Implementations
{
    public class ItemRepository : IItemRepository
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly ILogger<ItemRepository> _logger;

        public ItemRepository(IDbContextFactory<CloudCoreDbContext> dbContextFactory, ILogger<ItemRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<List<Item>> GetAllChildItemsAsync(int parentId, int userId, int maxDepth = 10000)
        {
            _logger.LogInformation("Fetching all child items. UserId={UserId}, ParentId={ParentId}, MaxDepth={MaxDepth}", userId, parentId, maxDepth);
            using var context = _dbContextFactory.CreateDbContext();
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

            var result = await context.Items
                .FromSqlRaw(sql, userIdParam, parentIdParam, maxDepthParam)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} child items for ParentId={ParentId}, UserId={UserId}", result.Count, parentId, userId);
            return result;
        }

        public async Task<(IEnumerable<Item> Items, int TotalCount)> GetItemsAsync(int userId, int? parentId, int page, int pageSize, string? sortBy, string? sortDir, bool isTrashFolder = false)
        {
            _logger.LogInformation("Fetching items. UserId={UserId}, ParentId={ParentId}, Page={Page}, PageSize={PageSize}, SortBy={SortBy}, SortDir={SortDir}, IsTrashFolder={IsTrashFolder}", userId, parentId, page, pageSize, sortBy, sortDir, isTrashFolder);

            using var context = _dbContextFactory.CreateDbContext();
            var query = context.Items
                .AsNoTracking()
                .Where(i => i.UserId == userId);

            if (isTrashFolder == true)
            {
                _logger.LogInformation("Querying trash folder items for UserId={UserId}", userId);
                query = query.Where(i => i.IsDeleted == true && (i.ParentId == null || context.Items
                    .AsNoTracking()
                    .Any(p => p.Id == i.ParentId && p.IsDeleted == false)));
            }
            else

                query = query.Where(i => i.IsDeleted == false && i.ParentId == parentId);


            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = query.OrderBy(i => i.Type == "folder" ? 0 : 1);

            switch ((sortBy ?? "name").ToLowerInvariant())
            {
                case "size":
                case "filesize":
                    query = desc
                        ? query.OrderByDescending(i => i.FileSize ?? 0)
                        : query.OrderBy(i => i.FileSize ?? 0);
                    break;

                case "modified":
                case "updatedat":
                    query = desc
                        ? query.OrderByDescending(i => i.UpdatedAt)
                        : query.OrderBy(i => i.UpdatedAt);
                    break;

                case "name":
                default:
                    query = desc
                        ? query.OrderByDescending(i => i.Name)
                        : query.OrderBy(i => i.Name);
                    break;
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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

        public async Task<IEnumerable<Item>> GetItemsByIdsForUserAsync(int userId, List<int> itemsIds)
        {
            if (itemsIds == null || itemsIds.Count == 0)
            {
                _logger.LogWarning("GetItemsByIdsForUserAsync called with an empty or null list of IDs for UserId: {UserId}", userId);
                return Enumerable.Empty<Item>();
            }

            _logger.LogInformation("Fetching {ItemCount} items by IDs for UserId: {UserId}", itemsIds.Count, userId);

            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var items = await context.Items
                .AsNoTracking()
                .Where(i => i.UserId == userId && i.IsDeleted == false && itemsIds.Contains(i.Id))
                .ToListAsync();

            _logger.LogInformation("Found {FoundCount} out of {RequestedCount} items for UserId: {UserId}", items.Count, itemsIds.Count, userId);

            return items;
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

        public async Task<bool> ItemExistsAsync(int itemId, int userId, string itemType)
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
            using var _context = _dbContextFactory.CreateDbContext();

            if (folderId.HasValue)
            {
                var allChildItems = await GetAllChildItemsAsync(folderId.Value, userId);
                var files = allChildItems.Where(item => item.Type == "file" && item.IsDeleted == false); //FIX: Filter deleted items
                long totalSize = files.Sum(f => f.FileSize ?? 0);
                int fileCount = files.Count();
                return (totalSize, fileCount);
            }
            else
            {
                var items = await _context.Items
                    .AsNoTracking()
                    .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == folderId)
                    .ToListAsync();

                long totalSize = 0;
                int fileCount = 0;
                foreach (var item in items)
                {
                    if (item.Type == "file")
                    {
                        // FIX: Use null-coalescing instead of cast
                        totalSize += item.FileSize ?? 0;
                        fileCount++;
                    }
                }

                var subfolders = items.Where(i => i.Type == "folder").ToList();

                foreach (var folder in subfolders)
                {
                    var (subSize, subCount) = await CalculateArchiveSizeAsync(userId, folder.Id);
                    totalSize += subSize;
                    fileCount += subCount;
                }

                return (totalSize, fileCount);
            }
        }
        public async Task AddItemInTranscationAsync(Item item)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            _logger.LogInformation("Starting transaction to add ItemName={ItemName}.", item.Name);
            try
            {
                context.Update(item);
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

        public async Task UpdateItemsInTransactionAsync(List<Item> items)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            _logger.LogInformation("Starting transaction to update {ItemCount} items.", items.Count);
            try
            {
                context.UpdateRange(items);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully. Updated {ItemCount} items.", items.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed. Rolled back changes for {ItemCount} items.", items.Count);
                throw;
            }

        }

        public async Task DeleteItemPermanentlyAsync(Item item)
        {
            using var context = _dbContextFactory.CreateDbContext();
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
    }
}
