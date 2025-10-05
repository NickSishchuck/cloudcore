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
        private readonly IItemRepository _itemDataService;
        private readonly ILogger<ZipArchiveService> _logger;
        private readonly IStorageCalculationService _storageCalculationService;


        public ZipArchiveService(IItemStorageService fileStorage, IValidationService validationService, IItemRepository itemDataService, ILogger<ZipArchiveService> logger, IStorageCalculationService storageCalculationService)
        {
            _fileStorageService = fileStorage;
            _validationService = validationService;
            _itemDataService = itemDataService;
            _logger = logger;
            _storageCalculationService = storageCalculationService;
        }

        public async Task<FileStream> CreateFolderArchiveAsync(int userId, int folderId, string folderName)
        {
            _logger.LogInformation("Starting CreateFolderArchive for UserId: {UserId}, FolderId: {FolderId}, FolderName: '{FolderName}'", userId, folderId, folderName);

            await ValidateArchive(userId, folderId); // Checks if archive will be valid

            var tempFilePath = Path.GetTempFileName() + ".zip";
            _logger.LogInformation("Creating temporary archive at: {TempPath}", tempFilePath);

            var allDescendants = await _itemDataService.GetAllChildItemsAsync(userId, folderId);
            var allNotDeletedDescendants = allDescendants.Where(i => i.IsDeleted == false);
            var itemsByParent = allNotDeletedDescendants.ToLookup(item => item.ParentId);

            using (var fileSteam = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            using (var zipArchive = new ZipArchive(fileSteam, ZipArchiveMode.Create, true))
            {
                _logger.LogInformation("Starting to build zip archive recursively.");
                AddChildrenToZip(zipArchive, itemsByParent, folderId, folderName);
            }

            _logger.LogInformation("Temporary archive created successfully. Returning stream.");
            return new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
        }
        private void AddChildrenToZip(ZipArchive archive, ILookup<int?, Item> itemsByParent, int? parentId, string currentPath)
        {
            if (!itemsByParent.Contains(parentId))
            {
                return;
            }

            foreach (var item in itemsByParent[parentId])
            {
                var entryPath = Path.Combine(currentPath, item.Name).Replace('\\', '/');

                if (item.Type == "folder")
                {
                    _logger.LogDebug("Creating directory entry in archive: '{EntryPath}'", entryPath);
                    archive.CreateEntry(entryPath + "/");
                    AddChildrenToZip(archive, itemsByParent, item.Id, entryPath);
                }
                else if (item.Type == "file")
                {
                    _logger.LogDebug("Adding file entry to archive: '{EntryPath}'", entryPath);
                    AddFileToZipAsync(archive, item, entryPath).GetAwaiter().GetResult();
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

        public async Task<FileStream> CreateMultipleItemArchiveAsync(int userId, List<Item> itemsIds)
        {
            _logger.LogInformation("Starting CreateMultipleItemArchive for UserId: {UserId} with {ItemCount} root items.", userId, itemsIds.Count);
            await ValidateMultipleItemsArchive(userId, itemsIds); // Checks if archive will be valid

            var tempFilePath = Path.GetTempFileName() + ".zip";
            _logger.LogInformation("Creating temporary archive for multiple items at {TempPath}", tempFilePath);

            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
            {
                foreach (var item in itemsIds) // For each item in list
                {
                    if (item.Type == "folder") // If item is folder
                    {
                        _logger.LogInformation("Processing folder '{ItemName}' (ID: {ItemId}) for multi-item archive.", item.Name, item.Id);
                        var descendants = await _itemDataService.GetAllChildItemsAsync(userId, item.Id);
                        var itemsByParent = descendants.ToLookup(i => i.ParentId);
                        AddChildrenToZip(zipArchive, itemsByParent, item.Id, item.Name);
                    }
                    else if (item.Type == "file")
                    {
                        _logger.LogInformation("Adding root file '{ItemName}' (ID: {ItemId}) to multi-item archive.", item.Name, item.Id);
                        await AddFileToZipAsync(zipArchive, item, item.Name);
                    }
                }
                _logger.LogInformation("Multi-item archive created successfully. Returning stream.");
                return new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            }
        }

        public async Task<(long totalSize, int fileCount)> CalculateMultipleItemsSizeAsync(int userId, List<Item> items)
        {
            return await _storageCalculationService.CalculateMultipleItemsSizeAsync(userId, items);
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
            var (totalSize, fileCount) = await _storageCalculationService.CalculateMultipleItemsSizeAsync(userId, items);
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

