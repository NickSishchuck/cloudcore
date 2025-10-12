using CloudCore.Common.Errors;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using static CloudCore.Contracts.Responses.ItemResultResponses;

namespace CloudCore.Services.Implementations
{
    public class ItemApplication : IItemApplication
    {
        private readonly IZipArchiveService _zipArchiveService;
        private readonly IItemStorageService _itemStorageService;
        private readonly IValidationService _validationService;
        private readonly IItemRepository _itemRepository;
        private readonly IItemManagerService _itemManagerService;
        private readonly ILogger<ItemApplication> _logger;
        private readonly IStorageTrackingService _storageTrackingService;
        public ItemApplication(IZipArchiveService zipArchiveService, IItemStorageService itemStorageService, IValidationService validationService, IItemRepository itemRepository, ILogger<ItemApplication> logger, IItemManagerService itemManagerService, IStorageTrackingService storageTrackingService)
        {
            _zipArchiveService = zipArchiveService;
            _itemStorageService = itemStorageService;
            _validationService = validationService;
            _itemRepository = itemRepository;
            _logger = logger;
            _itemManagerService = itemManagerService;
            _storageTrackingService = storageTrackingService;
        }

        private IAsyncEnumerable<Item> CreateItemStream(int userId, Item item)
        {
            if (item.Type == "folder")
            {
                return _itemRepository.GetAllChildItemsAsync(userId, item.Id).Prepend(item);
            }
            else
            {
                return AsyncEnumerable.Repeat(item, 1);
            }
        }

        private async Task ProcessItemStreamsAsync(int userId, Item rootItem, Func<IAsyncEnumerable<Item>, IAsyncEnumerable<Item>> prepare, Func<int, IAsyncEnumerable<Item>, bool, Task> storageAction, bool isAdding)
        {
            var streamForDb = CreateItemStream(userId, rootItem);
            var preparedForDb = prepare(streamForDb);

            await _itemRepository.UpdateItemsInTransactionAsync(preparedForDb);

            var streamForStorage = CreateItemStream(userId, rootItem);
            await storageAction(userId, streamForStorage, isAdding);
        }

        public async Task<PaginatedResponse<Item>> GetItemsAsync(int userId, int? parentId, int page, int pageSize, string? sortBy, string? sortDir, bool isTrashFolder = false, string? searchQuery = null, int? teamspaceId = null)
        {
            var (items, totalCount) = await _itemRepository.GetItemsAsync(userId, parentId, page, pageSize, sortBy, sortDir, isTrashFolder, searchQuery);
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            return new PaginatedResponse<Item>
            {
                Data = items,
                Pagination = new PaginationMetadata
                {
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    HasNext = page < totalPages,
                    HasPrevious = page > 1
                }
            };
        }


        public async Task<Item?> GetItemAsync(int userId, int itemId, string type, int? teamspaceId = null)
        {
            return await _itemRepository.GetItemAsync(userId, itemId, type);
        }

        public async Task<string> GetBreadcrumbPathAsync(int userId, int folderId, string type)
        {
            var folder = await GetItemAsync(userId, folderId, type);
            if (folder == null)
            {
                _logger.LogWarning("Folder not found for User Id: {UserId}, Folder Id: {FolderId}", userId, folderId);
                throw new FileNotFoundException(ErrorCodes.FOLDER_NOT_FOUND);
            }

            string folderPath = await _itemRepository.GetBreadcrumbPathAsync(folder);
            if (string.IsNullOrEmpty(folderPath))
                return folder.Name;

            return folderPath;
        }

        public async Task<(Stream archiveStream, string fileName)> DownloadFolderAsync(int userId, int folderId)
        {
            var folder = await _itemRepository.GetItemAsync(userId, folderId, "folder");
            if (folder == null || folder.IsDeleted == true)
            {
                _logger.LogWarning("DownloadFolder failed: Folder with ID {FolderId} not found for user {UserId}", folderId, userId);
                throw new FileNotFoundException(ErrorCodes.FOLDER_NOT_FOUND);
            }

            var archiveStream = await _zipArchiveService.CreateFolderArchiveAsync(userId, folderId, folder.Name);
            var fileName = $"{folder.Name}.zip";

            return (archiveStream, fileName);
        }

        public async Task<FileDownloadResult> DownloadFileAsync(int userId, int fileId)
        {
            var file = await _itemRepository.GetItemAsync(userId, fileId, "file");
            if (file == null || file.IsDeleted == true)
            {
                _logger.LogWarning("DownloadFile failed: File with ID {FilerId} not found for user {UserId}", fileId, userId);
                throw new FileNotFoundException(ErrorCodes.FOLDER_NOT_FOUND);
            }

            var fullPath = _itemStorageService.GetFileFullPath(userId, file.FilePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogError("Physical file is missing on disk for Item ID {ItemId}. Path: {Path}", file.Id, fullPath);
                throw new FileNotFoundException(ErrorCodes.FILE_NOT_FOUND);
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return new FileDownloadResult
            {
                Stream = fileStream,
                FileName = file.Name,
                MimeType = file.MimeType ?? "application/octet-stream"
            };

        }

        public async Task<(Stream archiveStream, string fileName)> DownloadMultipleItemsAsZipAsync(int userId, List<int> itemsIds)
        {

            var itemsValidation = await _validationService.ValidateItemIdsAsync(itemsIds, userId);
            if (!itemsValidation.IsValid)
                throw new InvalidOperationException(itemsValidation.ErrorMessage);

            var itemsAsync = _itemRepository.GetItemsByIdsForUserAsync(userId, itemsIds);

            long totalSize = 0;
            int itemCount = 0;
            await foreach (var item in itemsAsync)
            {
                if (item.Type == "file")
                {
                    totalSize += item.FileSize ?? 0;
                    itemCount++;
                }
            }
            var sizeValidation = _validationService.ValidateArchiveSize(totalSize, itemCount);
            if (!sizeValidation.IsValid)
                throw new InvalidOperationException(sizeValidation.ErrorMessage);

            var archiveStream = await _zipArchiveService.CreateMultipleItemArchiveAsync(userId, _itemRepository.GetItemsByIdsForUserAsync(userId, itemsIds));
            var fileName = $"selected_items_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

            return (archiveStream, fileName);
        }


        public async Task<RestoreResult> RestoreItemAsync(int userId, int itemId)
        {
            _logger.LogInformation("Restore request for ItemId: {ItemId}, UserId: {UserId}", itemId, userId);

            var itemToRestore = await _itemRepository.GetDeletedItemAsync(userId, itemId);
            if (itemToRestore == null)
                return new RestoreResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                    Message = "Item not found in recycle bin."
                };

            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(itemToRestore.Name, itemToRestore.Type, userId, itemToRestore.ParentId);
            if (!uniquenessValidation.IsValid)
                return new RestoreResult
                {
                    IsSuccess = false,
                    ErrorCode = uniquenessValidation.ErrorCode,
                    Message = uniquenessValidation.ErrorMessage
                };

            if (itemToRestore.Type == "file" && itemToRestore.ParentId.HasValue)
            {
                var parentExists = await _validationService.ValidateItemExistsAsync(itemToRestore.ParentId.Value, userId, "folder");
                if (!parentExists.IsValid)
                    return new RestoreResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.PARENT_FOLDER_DELETED,
                        Message = "Cannot restore item because its parent folder was also deleted."
                    };
            }

            var streamForSize = CreateItemStream(userId, itemToRestore);
            long totalBytes = 0;
            await foreach (var item in streamForSize)
            {
                if (item.Type == "file")
                    totalBytes += item.FileSize ?? 0;
            }

            var canRestore = await _storageTrackingService.CanAddToPersonalStorageAsync(userId, totalBytes);
            if (!canRestore)
            {
                _logger.LogWarning("Storage limit exceeded for restore. UserId={UserId}, ItemId={ItemId}", userId, itemId);
                return new RestoreResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.STORAGE_LIMIT_EXCEEDED,
                    Message = "Cannot restore item, not enough storage."
                };
            }


            try
            {
                await ProcessItemStreamsAsync(userId, itemToRestore, _itemManagerService.PrepareItemsForRestoreAsync, _storageTrackingService.UpdateStorageForItemsAsync, isAdding: true);

                _logger.LogInformation("Item {ItemId} and its children restored successfully.", itemId);
                return new RestoreResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.RESTORED_SUCCESSFULLY,
                    Message = "Item restored successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore item {ItemId} in a transaction.", itemId);
                return new RestoreResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = ex.Message
                };
                throw;
            }
        }

        public async Task<DeleteResult> SoftDeleteItemAsync(int userId, int itemId)
        {
            _logger.LogInformation("Delete request received. UserId={UserId}, ItemId={ItemId}", userId, itemId);
            var item = await _itemRepository.GetItemAsync(userId, itemId, null);

            if (item == null)
            {
                _logger.LogWarning("Delete failed: item not found. UserId={UserId}, ItemId={ItemId}", userId, itemId);
                return new DeleteResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                    Message = "File not found"
                };
            }

            _logger.LogInformation("Item retrieved for deletion. ItemId={ItemId}, Type={ItemType}, Name={ItemName}", item.Id, item.Type, item.Name);

            try
            {
                await ProcessItemStreamsAsync(userId, item, _itemManagerService.PrepareItemsForSoftDeleteAsync, _storageTrackingService.UpdateStorageForItemsAsync, isAdding: false);

                _logger.LogInformation("Item deleted successfully. ItemId={ItemId}, Type={ItemType}, Name={ItemName}",
                    item.Id, item.Type, item.Name);

                return new DeleteResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.DELETED_SUCCESSFULLY,
                    Message = "Item deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete operation failed. UserId={UserId}, ItemId={ItemId}", userId, itemId);
                return new DeleteResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = ex.Message
                };
                throw;
            }
        }

        public async Task<RenameResult> RenameItemAsync(int userId, int itemId, string newName)
        {
            _logger.LogInformation("Rename request received. UserId={UserId}, ItemId={ItemId}, NewName={NewName}",
        userId, itemId, newName);

            var itemNameValidation = _validationService.ValidateItemName(newName);
            if (!itemNameValidation.IsValid)
            {
                _logger.LogWarning("Item name validation failed. ErrorCode={ErrorCode}, Message={Message}", itemNameValidation.ErrorCode, itemNameValidation.ErrorMessage);
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = itemNameValidation.ErrorCode,
                    Message = itemNameValidation.ErrorMessage
                };
            }

            var itemExistsValidation = await _validationService.ValidateItemExistsAsync(itemId, userId);
            if (!itemExistsValidation.IsValid)
            {
                _logger.LogWarning("Item existence validation failed. UserId={UserId}, ItemId={ItemId}, ErrorCode={ErrorCode}", userId, itemId, itemExistsValidation.ErrorCode);
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = itemExistsValidation.ErrorCode,
                    Message = itemExistsValidation.ErrorMessage
                };
            }

            var item = await _itemRepository.GetItemAsync(userId, itemId, null);
            _logger.LogInformation("Item retrieved successfully. ItemId={ItemId}, CurrentName={CurrentName}, ParentId={ParentId}", item.Id, item.Name, item.ParentId);

            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(newName, item.Type, userId, item.ParentId, itemId, true);
            if (!uniquenessValidation.IsValid)
            {
                _logger.LogWarning("Uniqueness validation failed. ItemId={ItemId}, NewName={NewName}, ErrorCode={ErrorCode}", item.Id, newName, uniquenessValidation.ErrorCode);
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = uniquenessValidation.ErrorCode,
                    Message = uniquenessValidation.ErrorMessage
                };
            }

            IAsyncEnumerable<Item> itemsToSoftDelete;
            var folderPath = String.Empty;
            if (item.Type == "folder")
            {
                itemsToSoftDelete = _itemRepository.GetAllChildItemsAsync(userId, itemId)
                                                  .Prepend(item);

                folderPath = await _itemRepository.GetFolderPathAsync(item);
                folderPath = Path.Combine(_itemStorageService.GetUserStoragePath(userId), folderPath);
                _logger.LogInformation("Folder Path is {FolderPath}", folderPath);
            }
            else
            {
                itemsToSoftDelete = AsyncEnumerable.Repeat(item, 1);
            }

            var preparedItemsAsync = _itemManagerService.PrepareItemsForRenaming(item, newName, itemsToSoftDelete, folderPath);

            try
            {
                await _itemRepository.UpdateItemsInTransactionAsync(preparedItemsAsync);
                _logger.LogInformation("Item renamed successfully in DB. ItemId={ItemId}, NewName={NewName}", item.Id, newName);

                return new RenameResult
                {
                    IsSuccess = true,
                    Message = "Item renamed succesfully.",
                    ItemId = item.Id,
                    NewName = newName,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rename operation failed. UserId={UserId}, ItemId={ItemId}, NewName={NewName}", userId, itemId, newName);
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An unexpected error occured."
                };
                throw;
            }

        }

        public async Task<UploadResult> UploadFileAsync(int userId, IFormFile file, int? parentId = null, int? teamspaceId = null)
        {
            var fileValidation = _validationService.ValidateFile(file);
            if (!fileValidation.IsValid)
            {
                return new UploadResult
                {
                    IsSuccess = false,
                    ErrorCode = fileValidation.ErrorCode,
                    Message = fileValidation.ErrorMessage
                };
            }

            var canUpload = await _storageTrackingService.CanAddToPersonalStorageAsync(userId, file.Length);
            if (!canUpload)
            {
                var (usedMb, limitMb) = await _storageTrackingService.GetPersonalStorageInfoAsync(userId);
                long fileSizeMb = file.Length / (1024 * 1024);

                _logger.LogWarning("Storage limit exceeded for user {UserId}. Used: {UsedMb}MB, Limit: {LimitMb}MB, Attempting: {FileMb}MB",
                    userId, usedMb, limitMb, fileSizeMb);

                return new UploadResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.STORAGE_LIMIT_EXCEEDED,
                    Message = $"Upload would exceed your storage limit ({usedMb + fileSizeMb}MB / {limitMb}MB)"
                };
            }
            Item createdItem = null;

            string targetDirectory = String.Empty;

            if (parentId.HasValue)
            {
                var parentFolder = await _itemRepository.GetItemAsync(userId, parentId.Value, "folder");

                if (parentFolder == null)
                {
                    _logger.LogWarning("Upload failed: Parent folder with ID {ParentId} not found for user {UserId}", parentId.Value, userId);
                    return new UploadResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                        Message = "The destination folder does not exist."
                    };
                }
                var parentFolderPath = await _itemRepository.GetFolderPathAsync(parentFolder);
                targetDirectory = parentFolderPath;
            }

            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(file.FileName, "file", userId, parentId, null, true);
            if (!uniquenessValidation.IsValid)
            {
                return new UploadResult
                {
                    IsSuccess = false,
                    ErrorCode = uniquenessValidation.ErrorCode,
                    Message = uniquenessValidation.ErrorMessage
                };
            }

            try
            {

                createdItem = await _itemManagerService.ProcessUploadAsync(userId, parentId, file, targetDirectory);

                await _itemRepository.AddItemInTranscationAsync(createdItem);
                _logger.LogInformation("Item added successfully in DB.");

                await _storageTrackingService.AddToPersonalStorageAsync(userId, file.Length);
                _logger.LogInformation("Personal storage updated for user {UserId} (+{SizeMb}MB)",
                    userId, file.Length / (1024 * 1024));

                return new UploadResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.UPLOADED_SUCCESSFULLY,
                    Message = "File uploaded successfully",
                    ItemId = createdItem.Id,
                    FileName = createdItem.Name
                };
            }
            catch (Exception)
            {
                if (createdItem != null && !string.IsNullOrEmpty(createdItem.FilePath))
                {
                    _logger.LogInformation("Attempting to delete orphaned file at {FilePath}", createdItem.FilePath);
                    _itemStorageService.DeleteItemPhysically(createdItem);
                }
                throw;
            }
        }

        public async Task<CreateFolderResult> CreateFolderAsync(int userId, FolderCreateRequest request, int? teamspaceId = null)
        {
            var nameValidation = _validationService.ValidateItemName(request.Name);
            if (!nameValidation.IsValid)
                return new CreateFolderResult
                {
                    IsSuccess = false,
                    ErrorCode = nameValidation.ErrorCode,
                    Message = nameValidation.ErrorMessage
                };

            // Validate parent folder exists if specified
            if (request.ParentId.HasValue)
            {
                var parentValidation = await _validationService.ValidateItemExistsAsync(request.ParentId.Value, userId);
                if (!parentValidation.IsValid)
                    return new CreateFolderResult
                    {
                        IsSuccess = false,
                        ErrorCode = parentValidation.ErrorCode,
                        Message = parentValidation.ErrorMessage
                    };
            }

            // Check if folder with same name already exists
            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(request.Name, "folder", userId, request.ParentId, null, true);
            if (!uniquenessValidation.IsValid)
                return new CreateFolderResult
                {
                    IsSuccess = false,
                    ErrorCode = uniquenessValidation.ErrorCode,
                    Message = uniquenessValidation.ErrorMessage
                };

            var folder = new Item
            {
                Name = request.Name,
                Type = "folder",
                UserId = userId,
                ParentId = request.ParentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            try
            {

                await _itemRepository.AddItemInTranscationAsync(folder);

                string relativeFolderPath = await _itemRepository.GetFolderPathAsync(folder);
                bool result = _itemStorageService.TryCreateFolder(userId, relativeFolderPath);
                if (!result)
                {
                    _logger.LogError("Failed to create physical folder on disk at path: {Path}. Rolling back database entry.", relativeFolderPath);

                    await _itemRepository.DeleteItemPermanentlyAsync(folder);
                    return new CreateFolderResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.IO_ERROR,
                        Message = "Failed to create folder on disk."
                    };
                }
                _logger.LogInformation("Folder '{FolderName}' (ID: {FolderId}) created successfully.", folder.Name, folder.Id);
                return new CreateFolderResult
                {
                    IsSuccess = true,
                    FolderId = folder.Id,
                    FolderName = folder.Name
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating folder '{FolderName}'.", request.Name);
                return new CreateFolderResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An unexpected error occurred."
                };
            }
        }

        public async Task<MoveResult> MoveItemAsync(int userId, int itemId, int? targetId)
        {
            var item = await _itemRepository.GetItemAsync(userId, itemId, null);
            if (item == null)
            {
                _logger.LogWarning("MoveItem failed: Item with ID {ItemId} not found for user {UserId}", itemId, userId);
                return new MoveResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                    Message = "Item to move not found."
                };
            }

            Item targetItem = null;
            bool isMovingToRoot = targetId == null || targetId == 0;

            if (!isMovingToRoot)
            {
                targetItem = await _itemRepository.GetItemAsync(userId, (int)targetId, null);
                if (targetItem == null)
                {
                    _logger.LogWarning("MoveItem failed: Target item with ID {TargetId} not found for user {UserId}", targetId, userId);
                    return new MoveResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                        Message = "Target folder not found."
                    };
                }
                if (targetItem.Type != "folder")
                {
                    _logger.LogWarning("MoveItem failed: Target item with ID {TargetId} is not a folder for user {UserId}", targetId, userId);
                    return new MoveResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.INVALID_TARGET,
                        Message = "Target item is not a folder."
                    };
                }
            }

            if (item.Type == "folder" && !isMovingToRoot)
            {
                var circularValidation = await _validationService.ValidateIsFolderSubFolder(userId, itemId, (int)targetId);
                if (!circularValidation.IsValid)
                {
                    _logger.LogWarning("MoveItem failed: Circular validation failed for ItemId {ItemId} and TargetId {TargetId} for user {UserId}", itemId, targetId, userId);
                    return new MoveResult
                    {
                        IsSuccess = false,
                        ErrorCode = circularValidation.ErrorCode,
                        Message = circularValidation.ErrorMessage
                    };
                }
            }

            int? actualTargetId = isMovingToRoot ? null : targetId;
            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(item.Name, item.Type, userId, actualTargetId, itemId, true);
            if (!uniquenessValidation.IsValid)
            {
                _logger.LogWarning("MoveItem failed: Uniqueness validation failed for ItemId {ItemId} and TargetId {TargetId} for user {UserId}", itemId, targetId, userId);
                return new MoveResult
                {
                    IsSuccess = false,
                    ErrorCode = uniquenessValidation.ErrorCode,
                    Message = uniquenessValidation.ErrorMessage
                };
            }

            try
            {
                IAsyncEnumerable<Item> childItemsAsync = CreateItemStream(userId, item);
                var basePath = _itemStorageService.GetUserStoragePath(userId);

                string sourceFolderPath = null;
                if (item.Type == "folder")
                {
                    var sourceFolderPathRelative = await _itemRepository.GetFolderPathAsync(item);
                    sourceFolderPath = Path.Combine(basePath, sourceFolderPathRelative);
                    _logger.LogInformation("Source folder path: {Path}", sourceFolderPath);
                }

                string destinationFolderPath;
                if (isMovingToRoot)
                {
                    destinationFolderPath = basePath;
                }
                else
                {
                    var targetRelativePath = await _itemRepository.GetFolderPathAsync(targetItem);
                    destinationFolderPath = Path.Combine(basePath, targetRelativePath);
                }
                _logger.LogInformation("Destination folder path: {Path}", destinationFolderPath);

                var preparedItemsAsync = _itemManagerService.PrepareItemsForMoving(item, actualTargetId, sourceFolderPath, destinationFolderPath, childItemsAsync);
                await _itemRepository.UpdateItemsInTransactionAsync(preparedItemsAsync);

                var itemsForCount = _itemRepository.GetAllChildItemsAsync(userId, itemId).Prepend(item);
                var count = await itemsForCount.CountAsync() - 1;

                return new MoveResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.MOVED_SUCCESSFULLY,
                    Message = $"Item '{item.Name}' moved successfully",
                    ItemId = item.Id,
                    UpdatedItemsCount = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during move. UserId={UserId}, ItemId={ItemId}", userId, itemId);
                return new MoveResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An unexpected error occurred. Please try again later."
                };
            }
        }


    }
}
