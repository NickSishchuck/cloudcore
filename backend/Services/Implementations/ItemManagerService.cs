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

        public List<Item> PrepareItemsForRenaming(Item item, string newName, List<Item> childItems, string folderPath = null)
        {
            if (item.Type == "file")
            {
                _logger.LogInformation("Starting rename operation for item {ItemId} of type {ItemType} to new name '{NewName}'.",
                item.Id, item.Type, newName);

                var newRelativePath = _itemStorageService.RenameItemPhysically(item, newName);

                _logger.LogInformation("Renaming file physically: {OldName} -> {NewName}", item.Name, newName);

                item.Name = Path.GetFileNameWithoutExtension(newName);
                item.FilePath = newRelativePath;

                _logger.LogInformation("File rename completed. New path: {NewPath}", newRelativePath);
                List<Item> itemsToUpdate = new List<Item> { item };
                return itemsToUpdate;
            }
            if (item.Type == "folder")
            {
                _logger.LogInformation("Renaming folder physically: {OldName} -> {NewName}", item.Name, newName);

                var newFolderPath = _itemStorageService.GetNewFolderPath(folderPath, item.Name, newName);
                _logger.LogInformation($"New folder path is {newFolderPath}");
                var basePath = _itemStorageService.GetUserStoragePath(item.UserId);
                _logger.LogInformation($"Base path is {basePath}");
                foreach (var childItem in childItems)
                {
                    if (childItem.Type == "file")
                    {
                        childItem.FilePath = _itemStorageService.GetNewFilePath(childItem.FilePath, newFolderPath, basePath);
                    }
                }

                _itemStorageService.RenameItemPhysically(item, newName, childItems, folderPath);

                _logger.LogInformation("Folder rename completed. Updated {ChildCount} child items.", childItems?.Count ?? 0);

                item.Name = newName;

                var itemsToUpdate = new List<Item>(childItems) { item };

                return itemsToUpdate;
            }
            _logger.LogError("Unsupported item type {ItemType} for renaming.", item.Type);
            throw new NotSupportedException($"Item type '{item.Type}' is not supported for renaming.");
        }

        public List<Item> PrepareItemsForMoving(Item item, int newParentId, string sourceFolderPath, string destinationFolderPath, List<Item> childItems = null)
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

                // Physically move the file
                var newRelativePath = _itemStorageService.MoveItemPhysically(item, destinationFolderPath);

                _logger.LogInformation("File physically moved: {OldPath} -> {NewPath}", oldFilePath, newRelativePath);

                // Update metadata
                item.ParentId = newParentId;
                item.FilePath = newRelativePath;
                item.UpdatedAt = DateTime.UtcNow;

                itemsToUpdate.Add(item);

                _logger.LogInformation("File move completed. New parent: {ParentId}, New path: {NewPath}",
                    newParentId, newRelativePath);
            }
            else if (item.Type == "folder")
            {
                if (string.IsNullOrWhiteSpace(sourceFolderPath))
                {
                    throw new ArgumentException("Source folder path is required for folder items", nameof(sourceFolderPath));
                }

                _logger.LogInformation("Moving folder {FolderName} (ID: {ItemId}) with {ChildCount} child items",
                    item.Name, item.Id, childItems?.Count ?? 0);

                // Get the new absolute path for the folder
                var newFolderPath = Path.Combine(destinationFolderPath, item.Name);

                _logger.LogInformation("Folder paths - Old: {OldPath}, New: {NewPath}",
                    sourceFolderPath, newFolderPath);

                // Update paths for all child items before physical move
                if (childItems != null && childItems.Any())
                {
                    foreach (var childItem in childItems)
                    {
                        if (childItem.Type == "file")
                        {
                            var oldChildAbsolutePath = Path.Combine(basePath, childItem.FilePath);

                            // Get the relative path of the child item within the source folder
                            var relativePathInFolder = Path.GetRelativePath(sourceFolderPath, oldChildAbsolutePath);

                            // Build the new absolute path for the child item
                            var newChildAbsolutePath = Path.Combine(newFolderPath, relativePathInFolder);

                            // Convert to relative path for storage
                            childItem.FilePath = Path.GetRelativePath(basePath, newChildAbsolutePath)
                                .Replace("\\", "/");

                            _logger.LogDebug("Updated child file path: {FileName} -> {NewPath}",
                                childItem.Name, childItem.FilePath);
                        }
                    }

                    itemsToUpdate.AddRange(childItems);
                }

                // Physically move the folder
                _itemStorageService.MoveItemPhysically(item, destinationFolderPath, sourceFolderPath);

                // Update metadata for the folder itself
                item.ParentId = newParentId;
                itemsToUpdate.Add(item);

                _logger.LogInformation("Folder move completed. Updated {TotalCount} items (1 folder + {ChildCount} children)",
                    itemsToUpdate.Count, childItems?.Count ?? 0);
            }
            else
            {
                _logger.LogError("Unsupported item type {ItemType} for moving", item.Type);
                throw new NotSupportedException($"Item type '{item.Type}' is not supported for moving.");
            }

            _logger.LogInformation("Move preparation completed. Total items to update: {Count}", itemsToUpdate.Count);
            return itemsToUpdate;
        }

        public List<Item> PrepareItemsForSoftDelete(Item item, List<Item> childItems = null)
        {
            var deletionTime = DateTime.UtcNow;

            if (item.Type == "file")
            {
                item.IsDeleted = true;
                item.DeletedAt = deletionTime;
                return new List<Item> { item };
            }
            else if (item.Type == "folder")
            {
                item.IsDeleted = true;
                item.DeletedAt = deletionTime;

                foreach (var childItem in childItems)
                {
                    childItem.IsDeleted = true;
                    childItem.DeletedAt = deletionTime;
                }
                childItems.Add(item);
                return childItems;
            }
            _logger.LogError("Unsupported item type {ItemType} for soft deleting.", item.Type);
            throw new NotSupportedException($"Item type '{item.Type}' is not supported soft deleting.");
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

        public void PrepareItemsForRestore(List<Item> items)
        {
            foreach (var item in items)
            {
                item.IsDeleted = false;
                item.DeletedAt = null;
            }
        }


        
    }

}
