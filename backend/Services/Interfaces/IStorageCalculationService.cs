using CloudCore.Domain.Entities;

namespace CloudCore.Services.Interfaces
{
    /// <summary>
    /// Service for calculating storage usage across the application.
    /// </summary>
    public interface IStorageCalculationService
    {
        /// <summary>
        /// Calculates the total storage used by a user across all their items.
        /// </summary>
        /// <param name="userId">The user ID to calculate storage for</param>
        /// <returns>Total storage in bytes</returns>
        Task<long> GetUserTotalStorageAsync(int userId);

        /// <summary>
        /// Calculates the size of a specific folder and all its contents.
        /// </summary>
        /// <param name="userId">The user ID who owns the folder</param>
        /// <param name="folderId">The folder ID to calculate (null for root)</param>
        /// <returns>Tuple of (total size in bytes, file count)</returns>
        Task<(long totalSize, int fileCount)> CalculateFolderSizeAsync(int userId, int? folderId);

        /// <summary>
        /// Calculates the size of multiple mixed items (files and folders).
        /// </summary>
        /// <param name="userId">The user ID who owns the items</param>
        /// <param name="items">Collection of items to calculate size for</param>
        /// <returns>Tuple of (total size in bytes, file count)</returns>
        Task<(long totalSize, int fileCount)> CalculateMultipleItemsSizeAsync(int userId, IAsyncEnumerable<Item> items);
    }
}