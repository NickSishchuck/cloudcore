using System.IO;
using System.IO.Compression;
using CloudCore.Models;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace CloudCore.Services
{
    public class ZipArchiveService : IZipArchiveService
    {

        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly IFileStorageService _fileStorageService;
        private readonly IValidationService _validationService;
        private readonly IItemRepository _itemRepository;


        public ZipArchiveService(IDbContextFactory<CloudCoreDbContext> dbContextFactory, IFileStorageService fileStorage, IValidationService validationService, IItemRepository itemRepository)
        {
            _dbContextFactory = dbContextFactory;
            _fileStorageService = fileStorage;
            _validationService = validationService;
            _itemRepository = itemRepository;
        }

        public async Task<FileStream> CreateFolderArchiveAsync(int userId, int folderId, string folderName)
        {
            await ValidateArchive(userId, folderId); // Checks if archive will be valid

            var tempFilePath = Path.GetTempFileName() + ".zip";

            var fileSteam = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);

            try
            {
                using (var zipArchive = new ZipArchive(fileSteam, ZipArchiveMode.Create, true))
                    await AddFolderToZipRecursivelyAsync(zipArchive, userId, folderId, folderName);
            }
            finally
            {
                fileSteam?.Dispose();
            }

            return new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
        }

        public async Task<FileStream> CreateMultipleItemArchiveAsync(int userId, List<Item> itemsIds)
        {
            using var _context = _dbContextFactory.CreateDbContext();

            await ValidateMultipleItemsArchive(userId, itemsIds); // Checks if archive will be valid

            var tempFilePath = Path.GetTempFileName() + ".zip";

            var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);

            try
            {
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                {
                    foreach (var item in itemsIds) // For each item in list
                    {
                        if (item.Type == "folder") // If item is folder
                        {
                            zipArchive.CreateEntry($"{item.Name}/"); // Create a root folder
                            await AddFolderToZipRecursivelyAsync(zipArchive, userId, item.Id, item.Name);
                        }
                        else if (item.Type == "file") // If item is file
                            await AddFileToZipAsync(zipArchive, item, item.Name, userId);
                    }
                }
            }
            finally
            {
                fileStream?.Dispose(); // Close file stream
            }

            return new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
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
            using var _context = _dbContextFactory.CreateDbContext();

            // Get all items in the folder
            var items = await _context.Items
                .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == folderId)
                .ToListAsync();

            foreach (var item in items)
            {
                // Get an item path to make a hierarchy
                // -> Documents
                //    -> item1.html
                //    -> Folder1
                //      -> item2.html
                var itemPath = string.IsNullOrEmpty(currentPath) ? item.Name : $"{currentPath}/{item.Name}";

                if (item.Type == "folder")
                {
                    zipArchive.CreateEntry($"{itemPath}/");

                    await AddFolderToZipRecursivelyAsync(zipArchive, userId, item.Id, item.Name, itemPath);
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
                throw;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<(long totalSize, int fileCount)> CalculateArchiveSizeAsync(int userId, int? folderId)
        {
            using var _context = _dbContextFactory.CreateDbContext();

            if (folderId.HasValue)
            {
                var allChildItems = await _itemRepository.GetAllChildItemsAsync(folderId.Value, userId);

                var files = allChildItems.Where(item => item.Type == "file");

                long totalSize = files.Sum(f => f.FileSize ?? 0);
                int fileCount = files.Count();

                return (totalSize, fileCount);
            }
            else
            {
                var items = await _context.Items
                .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == folderId)
                .ToListAsync();

                long totalSize = 0;
                int fileCount = 0;
                foreach (var item in items)
                {
                    if (item.Type == "file")
                    {
                        totalSize += (long)item.FileSize;
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

        public async Task<(long totalSize, int fileCount)> CalculateMultipleItemsSizeAsync(int userId, List<Item> items)
        {
            long totalSize = 0;
            int fileCount = 0;

            foreach (var item in items)
            {
                if (item.Type == "file")
                {
                    totalSize += item.FileSize ?? 0;
                    fileCount++;
                }
                else if (item.Type == "folder")
                {
                    var (folderSize, folderFileCount) = await CalculateArchiveSizeAsync(userId, item.Id);
                    totalSize += folderSize;
                    fileCount += folderFileCount;
                }
            }

            return (totalSize, fileCount);
        }
        /// <summary>
        /// Validates that multiple items meet archive size and file count constraints
        /// </summary>
        /// <param name="userId">User ID for item validation</param>
        /// <param name="items">Collection of items to validate for archiving</param>
        /// <returns>Task that completes successfully if validation passes</returns>
        /// <exception cref="InvalidOperationException">Thrown when size exceeds 2000MB or file count exceeds 10000</exception>
        private async Task ValidateMultipleItemsArchive(int userId, List<Item> items)
        {
            var (totalSize, fileCount) = await CalculateMultipleItemsSizeAsync(userId, items);
            var validationResult = _validationService.ValidateArchiveSize(totalSize, fileCount);
            if (!validationResult.IsValid)
                throw new InvalidOperationException(validationResult.ErrorMessage);
        }
        /// <summary>
        /// Validates that a single folder meets archive size and file count constraints
        /// </summary>
        /// <param name="userId">User ID for folder validation</param>
        /// <param name="folderId">Folder ID to validate (null for root level)</param>
        /// <returns>Task that completes successfully if validation passes</returns>
        /// <exception cref="InvalidOperationException">Thrown when size exceeds 2000MB or file count exceeds 10000</exception>
        private async Task ValidateArchive(int userId, int? folderId)
        {
            var (totalSize, fileCount) = await CalculateArchiveSizeAsync(userId, folderId);
            var validationResult = _validationService.ValidateArchiveSize(totalSize, fileCount);
            if (!validationResult.IsValid)
                throw new InvalidOperationException(validationResult.ErrorMessage);
        }
    }

}

