using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;

namespace CloudCore.Services.Implementations
{
    public class ItemManagerService : IItemManagerService
    {
        private readonly IItemStorageService _itemStorageService;
        private readonly ILogger<ItemManagerService> _logger;

        public ItemManagerService(IItemStorageService itemStorageService, ILogger<ItemManagerService> logger)
        {
            _itemStorageService = itemStorageService;
            _logger = logger;
        }

        public Task<ItemResultResponses.DeleteResult> DeleteItemPermanentlyAsync(int userId, int id)
        {
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<Item> PrepareItemsForRenaming(Item item, string newName, IAsyncEnumerable<Item> childItems = null, string folderPath = null)
        {
            if (item.Type == "file")
            {
                _logger.LogInformation("Starting rename operation for item {ItemId} of type {ItemType} to new name '{NewName}'.",
                item.Id, item.Type, newName);

                var newRelativePath = _itemStorageService.RenameItemPhysically(item, newName);

                _logger.LogInformation("Renaming file physically: {OldName} -> {NewName}", item.Name, newName);

                item.Name = Path.GetFileNameWithoutExtension(newName);
                item.FilePath = newRelativePath;

                yield return item;
                yield break;
            }
            if (item.Type == "folder")
            {
                _logger.LogInformation("Renaming folder physically: {OldName} -> {NewName}", item.Name, newName);

                var newFolderPath = _itemStorageService.GetNewFolderPath(folderPath, item.Name, newName);
                _logger.LogInformation($"New folder path is {newFolderPath}");
                var basePath = _itemStorageService.GetUserStoragePath(item.UserId);
                _logger.LogInformation($"Base path is {basePath}");

                if (childItems != null)
                {
                    await foreach (var childItem in childItems)
                    {
                        if (childItem.Type == "file")
                        {
                            childItem.FilePath = _itemStorageService.GetNewFilePath(childItem.FilePath, newFolderPath, basePath);
                        }
                        yield return childItem;
                    }
                }
                

                _itemStorageService.RenameItemPhysically(item, newName, folderPath);

                item.Name = newName;
                yield return item;

                _logger.LogInformation("Folder rename completed. Updated folder {ItemId} and its children.", item.Id);
                yield break;
            }
            _logger.LogError("Unsupported item type {ItemType} for renaming.", item.Type);
            throw new NotSupportedException($"Item type '{item.Type}' is not supported for renaming.");
        }

        public async IAsyncEnumerable<Item> PrepareItemsForMoving(Item item, int newParentId, string sourceFolderPath, string destinationFolderPath, IAsyncEnumerable<Item> childItems = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrWhiteSpace(destinationFolderPath))
                throw new ArgumentException("Destination folder path is required", nameof(destinationFolderPath));

            _logger.LogInformation("Starting move operation for item {ItemId} of type {ItemType} to parent {ParentId}",
                item.Id, item.Type, newParentId);

            var itemsToUpdate = new List<Item>();
            var basePath = _itemStorageService.GetUserStoragePath(item.UserId);

            if (item.Type == "file")
            {
                _logger.LogInformation("Moving file {FileName} (ID: {ItemId}) to parent {ParentId}",
                    item.Name, item.Id, newParentId);

                var oldFilePath = item.FilePath;
                var newRelativePath = _itemStorageService.MoveItemPhysically(item, destinationFolderPath);

                item.ParentId = newParentId;
                item.FilePath = newRelativePath;
                item.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("File move completed. OldPath={OldPath}, NewPath={NewPath}", oldFilePath, newRelativePath);

                yield return item;
                yield break;
            }

            if (item.Type == "folder")
            {
                if (string.IsNullOrWhiteSpace(sourceFolderPath))
                    throw new ArgumentException("Source folder path is required for folder items", nameof(sourceFolderPath));

                var newFolderPath = Path.Combine(destinationFolderPath, item.Name);

                if (childItems != null)
                {
                    await foreach (var childItem in childItems)
                    {
                        if (childItem.Type == "file")
                        {
                            var oldChildAbsolutePath = Path.Combine(basePath, childItem.FilePath);
                            var relativePathInFolder = Path.GetRelativePath(sourceFolderPath, oldChildAbsolutePath);
                            var newChildAbsolutePath = Path.Combine(newFolderPath, relativePathInFolder);
                            childItem.FilePath = Path.GetRelativePath(basePath, newChildAbsolutePath).Replace("\\", "/");
                        }
                        yield return childItem;
                    }
                }

                _itemStorageService.MoveItemPhysically(item, destinationFolderPath, sourceFolderPath);

                item.ParentId = newParentId;
                yield return item;

                _logger.LogInformation("Folder move completed. FolderId={ItemId}", item.Id);
                yield break;
            }
            else
            {
                _logger.LogError("Unsupported item type {ItemType} for moving", item.Type);
                throw new NotSupportedException($"Item type '{item.Type}' is not supported for moving.");
            }
        }

        public async IAsyncEnumerable<Item> PrepareItemsForSoftDeleteAsync(IAsyncEnumerable<Item> items)
        {
            var deletionTime = DateTime.UtcNow;

            await foreach (var item in items)
            {
                item.IsDeleted = true;
                item.DeletedAt = deletionTime;
                yield return item;
            }
        }

        public async Task<Item> ProcessUploadAsync(int userId, int? parentId, IFormFile file, string targetDirectory)
        {

            string savedRelativePath = await _itemStorageService.SaveFileAsync(userId, targetDirectory, file);

            var item = new Item
            {
                Name = Path.GetFileName(file.FileName),
                Type = "file",
                UserId = userId,
                ParentId = parentId,
                FilePath = savedRelativePath,
                FileSize = file.Length,
                MimeType = file.ContentType ?? _itemStorageService.GetMimeType(file.FileName),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            return item;
        }

        public async IAsyncEnumerable<Item> PrepareItemsForRestoreAsync(IAsyncEnumerable<Item> items)
        {
            await foreach (var item in items)
            {
                item.IsDeleted = false;
                item.DeletedAt = null;
                yield return item;
            }
        }


        
    }

}
