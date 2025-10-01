using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloudCore.Services.Implementations
{
    public class TrashCleanupService : ITrashCleanupService
    {

        private const int BATCH_SIZE = 500;
        private const int RETENTION_DAYS = -30;

        private readonly IItemRepository _itemDataService;
        private readonly IItemStorageService _itemStorageService;
        private readonly ILogger<TrashCleanupService> _logger;

        public TrashCleanupService(IItemStorageService itemStorageService, ILogger<TrashCleanupService> logger, IItemRepository itemDataService)
        {
            _itemDataService = itemDataService;
            _itemStorageService = itemStorageService;
            _logger = logger;
        }



        public async Task<int> CleanupExpiredItemsAsync()
        {
            _logger.LogInformation("Starting trash cleanup job for items older than {RetentionDays} days.", -RETENTION_DAYS);
            var totalDeletedCount = 0;
            var thresholdDate = DateTime.UtcNow.AddDays(RETENTION_DAYS);

            var expiredItemIds = await _itemDataService.GetExpiredItemIdsAsync(thresholdDate);

            if (expiredItemIds.Count == 0)
            {
                _logger.LogInformation("No expired items found to clean up.");
                return 0;
            }

            _logger.LogInformation("Found {TotalCount} expired items to process.", expiredItemIds.Count);

            foreach (var batchOfIds in expiredItemIds.Chunk(BATCH_SIZE))
            {
                totalDeletedCount += await ProcessBatchAsync(batchOfIds.ToList());
            }

            _logger.LogInformation("Trash cleanup job finished. Permanently deleted {TotalCount} items.", totalDeletedCount);
            return totalDeletedCount;
        }

        private async Task<int> ProcessBatchAsync(List<int> batchIds)
        {
            _logger.LogInformation("Processing a batch of {BatchSize} items.", batchIds.Count);

            var itemsToDelete = await _itemDataService.GetDeletedItemsByIdsAsync(batchIds);

            var orderedItemsToDelete = itemsToDelete
                .OrderBy(i => i.Type == "folder")
                .ToList();

            await DeletePhysicalItemsAsync(orderedItemsToDelete);

            try
            {
                var deletedDbCount = await _itemDataService.DeleteItemsByIdsAsync(batchIds);
                _logger.LogInformation("Successfully deleted {DbCount} records from DB for this batch.", deletedDbCount);
                return deletedDbCount;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "CRITICAL: Failed to delete records from DB for batch of IDs {BatchIDs} after physical items were deleted. Manual cleanup may be required.", string.Join(", ", batchIds));
                return 0;
            }
        }
        private async Task DeletePhysicalItemsAsync(List<Item> items)
        {
            foreach (var item in items)
            {
                try
                {
                    string? itemPath = null;
                    if (item.Type == "folder")
                    {
                        itemPath = await _itemDataService.GetFolderPathAsync(item);
                    }

                    _itemStorageService.DeleteItemPhysicaly(item, itemPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete physical item ID {ItemId} ({ItemType}). This DB record will still be targeted for deletion.", item.Id, item.Type);
                }
            }
        }
    }
}
