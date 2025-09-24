using CloudCore.Data.Context;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudCore.Services.Implementations
{
    public class TrashCleanupService : ITrashCleanupService
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly IItemStorageService _itemStorageService;
        private readonly ILogger<TrashCleanupService> _logger;

        public TrashCleanupService(IDbContextFactory<CloudCoreDbContext> dbContextFactory, IItemStorageService itemStorageService, ILogger<TrashCleanupService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _itemStorageService = itemStorageService;
            _logger = logger;
        }



        public async Task<int> CleanupExpiredItemsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();

            var thresholdDate = DateTime.UtcNow.AddDays(-30);

            var itemsToDelete = await context.Items
                .Where(i => i.IsDeleted == true && i.DeletedAt <= thresholdDate)
                .OrderBy(i => i.Type == "folder") 
                .ToListAsync();

            if (itemsToDelete.Count == 0)
            {
                _logger.LogInformation("No expired items to clean up.");
                return 0;
            }
            _logger.LogInformation($"Found {itemsToDelete.Count} items to permanently delete.");

            int deletedCount = 0;
            foreach (var item in itemsToDelete)
            {
                try
                {
                    if (item.Type == "file")
                    {
                        var filePath = _itemStorageService.GetFileFullPath(item.UserId, item.FilePath);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            _logger.LogInformation($"Deleted file: {filePath}");
                        }
                    }
                    else if (item.Type == "folder")
                    {
                        var folderPath = await _itemStorageService.GetFolderPathAsync(item);
                        if (Directory.Exists(folderPath))
                        {
                            Directory.Delete(folderPath, recursive: true);
                            _logger.LogInformation($"Deleted folder: {folderPath}");
                        }
                    }

                    context.Items.Remove(item);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to delete item ID {item.Id}. Skipping.");
                }
            }

            if (deletedCount > 0)
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Permanently deleted {deletedCount} items from the database.", deletedCount);
            }
            return deletedCount;

        }
    }
}
