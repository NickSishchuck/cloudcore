using System.IO;
using System.IO.Compression;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace CloudCore.Services.Implementations
{
    public class ZipArchiveService : IZipArchiveService
    {

        private readonly IItemStorageService _fileStorageService;
        private readonly IValidationService _validationService;
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<ZipArchiveService> _logger;
        private readonly IStorageCalculationService _storageCalculationService;


        public ZipArchiveService(IItemStorageService fileStorage, IValidationService validationService, IItemRepository itemDataService, ILogger<ZipArchiveService> logger, IStorageCalculationService storageCalculationService)
        {
            _fileStorageService = fileStorage;
            _validationService = validationService;
            _itemRepository = itemDataService;
            _logger = logger;
            _storageCalculationService = storageCalculationService;
        }

        public async Task<FileStream> CreateFolderArchiveAsync(int userId, int folderId, string folderName)
        {
            _logger.LogInformation("Starting CreateFolderArchive for UserId: {UserId}, FolderId: {FolderId}, FolderName: '{FolderName}'", userId, folderId, folderName);

            await ValidateArchive(userId, folderId); // Checks if archive will be valid

            var tempFilePath = Path.GetTempFileName() + ".zip";
            _logger.LogInformation("Creating temporary archive at: {TempPath}", tempFilePath);


            using (var fileSteam = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            using (var zipArchive = new ZipArchive(fileSteam, ZipArchiveMode.Create, true))
            {
                _logger.LogInformation("Starting to build zip archive recursively.");
                await AddChildrenToZipAsync(zipArchive, userId, folderId, string.Empty);
            }

            _logger.LogInformation("Temporary archive created successfully. Returning stream.");
            return new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
        }
        private async Task AddChildrenToZipAsync(ZipArchive archive, int userId, int? parentId, string currentPath)
        {
            _logger.LogDebug("Processing children for ParentId: {ParentId}, Path: '{Path}'", parentId, currentPath);

            // Get all child items of the current parent
            await foreach (var item in _itemRepository.GetDirectChildrenAsync(userId, parentId))
            {

                var entryPath = Path.Combine(currentPath, item.Name).Replace('\\', '/');

                if (item.Type == "folder")
                {
                    _logger.LogDebug("Creating directory entry in archive: '{EntryPath}'", entryPath);
                    archive.CreateEntry(entryPath + "/");

                    // Recurse into the folder to add its children
                    await AddChildrenToZipAsync(archive, userId, item.Id, entryPath);
                }
                else if (item.Type == "file")
                {
                    _logger.LogDebug("Adding file entry to archive: '{EntryPath}'", entryPath);
                    await AddFileToZipAsync(archive, item, entryPath);
                }
            }
        }



        private async Task AddFileToZipAsync(ZipArchive zipArchive, Item item, string entryPath)
        {
            if (string.IsNullOrEmpty(item.FilePath))
            {
                _logger.LogWarning("Skipping item ID {ItemId} because its FilePath is empty.", item.Id);
                return;
            }

            var fullPath = _fileStorageService.GetFileFullPath(item.UserId, item.FilePath);

            if (File.Exists(fullPath))
            {
                try
                {
                    var entry = zipArchive.CreateEntry(entryPath, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await fileStream.CopyToAsync(entryStream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add file {FullPath} to archive as entry {EntryPath}", fullPath, entryPath);
                }
            }
            else
            {
                _logger.LogWarning("File not found on disk, skipping: {FullPath}", fullPath);
            }
        }

        public async Task<FileStream> CreateMultipleItemArchiveAsync(int userId, IAsyncEnumerable<Item> items)
        {
            await ValidateMultipleItemsArchive(userId, items);

            var tempFilePath = Path.GetTempFileName() + ".zip";
            _logger.LogInformation("Creating temporary archive for multiple items at {TempPath}", tempFilePath);

            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
            {
                await foreach (var item in items)
                {
                    if (item.Type == "folder")
                    {
                        _logger.LogInformation("Processing folder '{ItemName}' (ID: {ItemId}) for multi-item archive.", item.Name, item.Id);

                        // Create folder entry at root level
                        zipArchive.CreateEntry($"{item.Name}/");

                        // Add folder contents recursively using the SAME method as single folder download
                        await AddChildrenToZipAsync(zipArchive, userId, item.Id, item.Name);
                    }
                    else if (item.Type == "file")
                    {
                        _logger.LogInformation("Adding root file '{ItemName}' (ID: {ItemId}) to multi-item archive.", item.Name, item.Id);
                        await AddFileToZipAsync(zipArchive, item, item.Name);
                    }
                }
            }

            _logger.LogInformation("Multi-item archive created successfully. Returning stream.");
            return new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
        }

        public async Task<(long totalSize, int fileCount)> CalculateMultipleItemsSizeAsync(int userId, IAsyncEnumerable<Item> items)
        {
            return await _storageCalculationService.CalculateMultipleItemsSizeAsync(userId, items);
        }

        /// <summary>
        /// Validates that multiple items meet archive size and file count constraints
        /// </summary>
        /// <param name="userId">User ID for item validation</param>
        /// <param name="items">IAsyncEnumerable<Item> collection of items to validate for archiving</param>
        /// <returns>Task that completes successfully if validation passes</returns>
        /// <exception cref="InvalidOperationException">Thrown when size exceeds 2000MB or file count exceeds 10000</exception>
        private async Task ValidateMultipleItemsArchive(int userId, IAsyncEnumerable<Item> items)
        {
            long totalSize = 0;
            int fileCount = 0;

            await foreach (var item in items)
            {
                if (item.IsDeleted == false && item.Type == "file")
                {
                    totalSize += item.FileSize ?? 0;
                    fileCount++;
                }
            }

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
            var (totalSize, fileCount) = await _storageCalculationService.CalculateFolderSizeAsync(userId, folderId);
            var validationResult = _validationService.ValidateArchiveSize(totalSize, fileCount);
            if (!validationResult.IsValid)
                throw new InvalidOperationException(validationResult.ErrorMessage);
        }
    }

}

