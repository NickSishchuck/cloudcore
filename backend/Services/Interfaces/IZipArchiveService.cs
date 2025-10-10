using CloudCore.Domain.Entities;
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
        Task<FileStream> CreateMultipleItemArchiveAsync(int userId, IAsyncEnumerable<Item> itemsIds);


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
        Task<(long totalSize, int fileCount)> CalculateMultipleItemsSizeAsync(int userId, IAsyncEnumerable<Item> items);

    }
}
