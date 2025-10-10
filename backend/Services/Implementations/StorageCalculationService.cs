using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;

namespace CloudCore.Services.Implementations
{
    public class StorageCalculationService : IStorageCalculationService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<StorageCalculationService> _logger;

        public StorageCalculationService(
            IItemRepository itemRepository,
            ILogger<StorageCalculationService> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public async Task<long> GetUserTotalStorageAsync(int userId)
        {
            _logger.LogInformation("Calculating total storage for user {UserId}", userId);

            // Calculate root-level storage (parentId = null gets everything)
            var (totalSize, fileCount) = await _itemRepository.CalculateArchiveSizeAsync(userId, null);

            _logger.LogInformation(
                "User {UserId} storage: {Size} bytes across {FileCount} files",
                userId, totalSize, fileCount);

            return totalSize;
        }

        public async Task<(long totalSize, int fileCount)> CalculateFolderSizeAsync(int userId, int? folderId)
        {
            _logger.LogInformation(
                "Calculating folder size for user {UserId}, folder {FolderId}",
                userId, folderId);

            return await _itemRepository.CalculateArchiveSizeAsync(userId, folderId);
        }

        public async Task<(long totalSize, int fileCount)> CalculateMultipleItemsSizeAsync(
            int userId,
            IAsyncEnumerable<Item> items)
        {

            long totalSize = 0;
            int fileCount = 0;

            await foreach (var item in items)
            {
                if (item.IsDeleted == false)
                    continue;

                if (item.Type == "file")
                {
                    totalSize += item.FileSize ?? 0;
                    fileCount++;
                }
                else if (item.Type == "folder")
                {
                    var (folderSize, folderFileCount) = await _itemRepository.CalculateArchiveSizeAsync(userId, item.Id);
                    totalSize += folderSize;
                    fileCount += folderFileCount;
                }
            }

            return (totalSize, fileCount);
        }
    }
}
