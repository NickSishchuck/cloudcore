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

                var newRelativePath = _itemStorageService.RenameItemPhysicaly(item, newName);

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

                _itemStorageService.RenameItemPhysicaly(item, newName, childItems, folderPath);

                _logger.LogInformation("Folder rename completed. Updated {ChildCount} child items.", childItems?.Count ?? 0);

                item.Name = newName;

                var itemsToUpdate = new List<Item>(childItems) { item };

                return itemsToUpdate;
            }
            _logger.LogError("Unsupported item type {ItemType} for renaming.", item.Type);
            throw new NotSupportedException($"Item type '{item.Type}' is not supported for renaming.");
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
