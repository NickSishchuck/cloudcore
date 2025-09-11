using System.IO.Compression;
using CloudCore.Models;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudCore.Services
{
    public class ZipArchiveService : IZipArchiveService
    {
        private readonly CloudCoreDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        public ZipArchiveService(CloudCoreDbContext context, IFileStorageService fileStorage)
        {
            _context = context;
            _fileStorageService = fileStorage;
        }
        public async Task<MemoryStream> CreateFolderArchiveAsync(int userId, int folderId, string folderName)
        {
            
            MemoryStream memoryStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                await AddFolderToZipRecursivelyAsync(zipArchive, userId, folderId, folderName);
            }
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<MemoryStream> CreateMultipleItemArchiveAsync(int userId, List<Item> itemsIds)
        {
            MemoryStream memoryStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var item in itemsIds)
                {
                    if (item.Type == "folder")
                    {
                        zipArchive.CreateEntry($"{item.Name}/");
                        await AddFolderToZipRecursivelyAsync(zipArchive, userId, item.Id, item.Name);
                    }
                    else if(item.Type == "file")
                        await AddFileToZipAsync(zipArchive, item, item.Name, userId);
                }
            }
            memoryStream.Position = 0;
            return memoryStream;
        }

        
        /// <summary>
        /// Recursively adds folder contents to the ZIP archive while preserving hierarchy
        /// Processes all files and subfolders within the specified parent folder
        /// </summary>
        /// <param name="zipArchive">ZIP archive instance to add entries to</param>
        /// <param name="userId">User ID for database queries and file access</param>
        /// <param name="parentId">Parent folder ID to process (null for root level)</param>
        /// <param name="currentPath">Current path within the archive hierarchy</param>
        /// <remarks>
        /// Creates directory entries for folders and file entries for documents
        /// Uses forward slashes for cross-platform compatibility in archive paths
        /// Silently skips files that cannot be accessed due to permissions or missing files
        /// </remarks>
        private async Task AddFolderToZipRecursivelyAsync(ZipArchive zipArchive, int userId, int? folderId, string folderName, string currentPath = "")
        {
            // Get all items in the folder
            var items = await _context.Items
                .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == folderId)
                .ToListAsync();

            foreach (var item in items)
            {
                // Get a item path to make an hierarchy
                // -> Documents
                //    -> item1.html
                //    -> Folder1
                //      -> item2.html
                var itemPath = string.IsNullOrEmpty(currentPath) ? item.Name : $"{currentPath}/{item.Name}";

                if (item.Type == "folder")
                {
                    zipArchive.CreateEntry($"{itemPath}/");

                    await AddFolderToZipRecursivelyAsync(zipArchive, userId, item.Id, itemPath);
                }
                if (item.Type == "file")
                    await AddFileToZipAsync(zipArchive, item, itemPath, userId);

            }
        }

        /// <summary>
        /// Adds a single file to the ZIP archive with optimal compression
        /// Copies file content from disk to the archive entry stream
        /// </summary>
        /// <param name="zipArchive">ZIP archive instance to add the file to</param>
        /// <param name="item">File item metadata from database</param>
        /// <param name="itemPath">Path where file should be placed in the archive</param>
        /// <param name="userId">User ID for file path resolution</param>
        /// <remarks>
        /// Uses optimal compression level for best balance of size and performance
        /// Silently handles and logs file access errors without failing the entire operation
        /// Skips files with empty file paths to prevent errors
        /// </remarks>
        private async Task AddFileToZipAsync(ZipArchive zipArchive, Item item, string itemPath, int userId)
        {
            if (string.IsNullOrEmpty(item.FilePath))
                return;

            try
            {
                var fullPath = _fileStorageService.GetFileFullPath(userId, item.FilePath);

                if (File.Exists(fullPath))
                {
                    var entry = zipArchive.CreateEntry(itemPath, CompressionLevel.Optimal);

                    using var entryStream = entry.Open();
                    using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                    await fileStream.CopyToAsync(entryStream);
                }
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (FileNotFoundException)
            {

            }
            catch (Exception)
            {

            }
        }


    }
}
