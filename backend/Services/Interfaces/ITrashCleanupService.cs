namespace CloudCore.Services.Interfaces
{
    /// <summary>
    /// Defines a service for periodically cleaning up soft-deleted items from the trash.
    /// This service is typically invoked by a background job or scheduled task.
    /// </summary>
    public interface ITrashCleanupService
    {
        /// <summary>
        /// Permanently deletes items from the trash that have exceeded their retention period (e.g., older than 30 days).
        /// This operation is irreversible and includes deleting the physical files from storage.
        /// </summary>
        /// <returns>
        /// A Task that resolves to an integer representing the total number of items (both files and folders)
        /// that were permanently deleted during the cleanup process.
        /// </returns>
        Task<int> CleanupExpiredItemsAsync();
    }
}