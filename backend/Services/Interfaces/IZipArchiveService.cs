using CloudCore.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudCore.Services.Interfaces
{
    /// <summary>
    /// Service for creating ZIP archives from folders and files
    /// Provides functionality to compress user files and folders into downloadable archives
    /// </summary>
    public interface IZipArchiveService
    {
        /// <summary>
        /// Creates a ZIP archive containing the specified folder and all its contents
        /// Recursively includes all subfolders and files while preserving directory structure
        /// </summary>
        /// <param name="userId">User ID for authorization and file path resolution</param>
        /// <param name="folderId">ID of the folder to archive</param>
        /// <param name="folderName">Name of the folder to use as root in the archive</param>
        /// <returns>Memory stream containing the ZIP archive data</returns>
        
        Task<FileStream> CreateFolderArchiveAsync(int userId, int folderId, string folderName);

        /// <summary>
        /// Creates a ZIP archive containing multiple selected items (files and folders)
        /// </summary>
        /// <param name="userId">User ID for authorization and file path resolution</param>
        /// <param name="itemsIds">List of item to include in the archive</param>
        /// <returns>Memory stream containing the ZIP archive data</returns>
        Task<FileStream> CreateMultipleItemArchiveAsync(int userId, List<Item> itemsIds);

        /// <summary>
        /// Recursively calculates the total size and file count of a folder and all its subfolders
        /// </summary>
        /// <param name="userId">User ID to filter items by ownership</param>
        /// <param name="folderId">Folder ID to calculate size for (null for root level)</param>
        /// <returns>Tuple containing total size in bytes and total file count</returns>
        /// <remarks>
        /// Creates a new database context for each call to ensure thread safety
        /// Recursively processes all subfolders to calculate complete hierarchy size
        /// Only counts files marked as "file" type and not soft-deleted
        /// Returns zero values if no items found in the specified folder
        /// </remarks>
        Task<(long totalSize, int fileCount)> CalculateArchiveSizeAsync(int userId, int? folderId);

        /// <summary>
        /// Calculates the total size and file count for a collection of mixed items (files and folders)
        /// </summary>
        /// <param name="userId">User ID for folder size calculations</param>
        /// <param name="items">Collection of items to process, can contain both files and folders</param>
        /// <returns>Tuple containing combined size in bytes and total file count</returns>
        /// <remarks>
        /// Processes files directly using their FileSize property with null-safety
        /// For folders, delegates to CalculateArchiveSizeAsync for recursive calculation
        /// Accumulates totals from all items regardless of their type
        /// Handles mixed collections efficiently without duplicate database queries
        /// </remarks>
        Task<(long totalSize, int fileCount)> CalculateMultipleItemsSizeAsync(int userId, List<Item> items);

    }
}
