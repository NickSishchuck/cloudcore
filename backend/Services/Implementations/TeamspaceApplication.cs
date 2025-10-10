using CloudCore.Common.Errors;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using static CloudCore.Contracts.Responses.ItemResultResponses;

namespace CloudCore.Services.Implementations
{
    public class TeamspaceApplication : ITeamspaceApplication
    {
        private readonly ITeamspaceService _teamspaceService;
        private readonly IItemRepository _itemRepository;
        private readonly IItemStorageService _itemStorageService;
        private readonly IZipArchiveService _zipArchiveService;
        private readonly IValidationService _validationService;
        private readonly IItemManagerService _itemManagerService;
        private readonly ILogger<TeamspaceApplication> _logger;
        private readonly IStorageTrackingService _storageTrackingService;

        public TeamspaceApplication(
            ITeamspaceService teamspaceService,
            IItemRepository itemRepository,
            IItemStorageService itemStorageService,
            IZipArchiveService zipArchiveService,
            IValidationService validationService,
            IItemManagerService itemManagerService,
            ILogger<TeamspaceApplication> logger,
            IStorageTrackingService storageTrackingService)
        {
            _teamspaceService = teamspaceService;
            _itemRepository = itemRepository;
            _itemStorageService = itemStorageService;
            _zipArchiveService = zipArchiveService;
            _validationService = validationService;
            _itemManagerService = itemManagerService;
            _logger = logger;
            _storageTrackingService = storageTrackingService;
        }

        #region Item Retrieval

        public async Task<PaginatedResponse<Item>> GetTeamspaceItemsAsync(
            int userId,
            int teamspaceId,
            int? parentId,
            int page,
            int pageSize,
            string? sortBy,
            string? sortDir,
            string? searchQuery = null)
        {
            _logger.LogInformation("Fetching teamspace items. TeamspaceId={TeamspaceId}, UserId={UserId}",
                teamspaceId, userId);

            var (items, totalCount) = await _itemRepository.GetItemsAsync(
                userId,
                parentId,
                page,
                pageSize,
                sortBy,
                sortDir,
                IsTrashFolder: false,
                searchQuery,
                teamspaceId);

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

        public async Task<PaginatedResponse<Item>> GetTeamspaceTrashAsync(
            int userId,
            int teamspaceId,
            int page,
            int pageSize,
            string? sortBy,
            string? sortDir)
        {
            _logger.LogInformation("Fetching teamspace trash. TeamspaceId={TeamspaceId}, UserId={UserId}",
                teamspaceId, userId);

            var (items, totalCount) = await _itemRepository.GetItemsAsync(
                userId,
                null,
                page,
                pageSize,
                sortBy,
                sortDir,
                IsTrashFolder: true,
                null,
                teamspaceId);

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

        public async Task<Item?> GetTeamspaceItemAsync(
            int userId,
            int teamspaceId,
            int itemId,
            string type)
        {
            _logger.LogInformation("Fetching teamspace item. TeamspaceId={TeamspaceId}, ItemId={ItemId}",
                teamspaceId, itemId);

            var item = await _itemRepository.GetItemAsync(userId, itemId, type);

            // Verify item belongs to this teamspace
            if (item?.TeamspaceId != teamspaceId)
            {
                _logger.LogWarning("Item {ItemId} does not belong to teamspace {TeamspaceId}",
                    itemId, teamspaceId);
                return null;
            }

            return item;
        }

        public async Task<string> GetTeamspaceBreadcrumbPathAsync(
            int userId,
            int teamspaceId,
            int folderId)
        {
            var folder = await GetTeamspaceItemAsync(userId, teamspaceId, folderId, "folder");

            if (folder == null)
            {
                _logger.LogWarning("Folder not found. TeamspaceId={TeamspaceId}, FolderId={FolderId}",
                    teamspaceId, folderId);
                throw new FileNotFoundException(ErrorCodes.FOLDER_NOT_FOUND);
            }

            return await _itemRepository.GetBreadcrumbPathAsync(folder);
        }

        #endregion

        #region File Operations

        public async Task<UploadResult> UploadFileToTeamspaceAsync(
                int userId,
                int teamspaceId,
                IFormFile file,
                int? parentId = null)
        {
            _logger.LogInformation("Uploading file to teamspace. TeamspaceId={TeamspaceId}, FileName={FileName}",
                teamspaceId, file.FileName);

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

            var canUpload = await _storageTrackingService.CanAddToTeamspaceStorageAsync(teamspaceId, file.Length);
            if (!canUpload)
            {
                var (usedMb, limitMb) = await _storageTrackingService.GetTeamspaceStorageInfoAsync(teamspaceId);
                long fileSizeMb = file.Length / (1024 * 1024);

                return new UploadResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.STORAGE_LIMIT_EXCEEDED,
                    Message = $"Upload would exceed teamspace storage limit ({usedMb + fileSizeMb}MB / {limitMb}MB)"
                };
            }

            Item? createdItem = null;
            string targetDirectory = string.Empty;

            if (parentId.HasValue)
            {
                var parentFolder = await _itemRepository.GetItemAsync(userId, parentId.Value, "folder");

                if (parentFolder == null || parentFolder.TeamspaceId != teamspaceId)
                {
                    return new UploadResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.FOLDER_NOT_FOUND,
                        Message = "Parent folder not found in this teamspace"
                    };
                }

                targetDirectory = await _itemRepository.GetFolderPathAsync(parentFolder);
            }

            // Check name uniqueness in teamspace
            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(
                file.FileName,
                "file",
                userId,
                parentId,
                excludeItemId: null,
                includeDeleted: true);

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
                createdItem = await _itemManagerService.ProcessUploadAsync(
                    userId,
                    parentId,
                    file,
                    targetDirectory);

                createdItem.TeamspaceId = teamspaceId;

                await _itemRepository.AddItemInTranscationAsync(createdItem);

                await _storageTrackingService.AddToTeamspaceStorageAsync(teamspaceId, file.Length);

                _logger.LogInformation("File uploaded to teamspace successfully. ItemId={ItemId}, TeamspaceId={TeamspaceId}",
                    createdItem.Id, teamspaceId);

                return new UploadResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.UPLOADED_SUCCESSFULLY,
                    Message = "File uploaded successfully",
                    ItemId = createdItem.Id,
                    FileName = createdItem.Name
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to teamspace {TeamspaceId}", teamspaceId);

                // Cleanup orphaned file
                if (createdItem != null && !string.IsNullOrEmpty(createdItem.FilePath))
                {
                    _itemStorageService.DeleteItemPhysically(createdItem);
                }
                throw;
            }
        }

        public async Task<CreateFolderResult> CreateFolderInTeamspaceAsync(
            int userId,
            int teamspaceId,
            FolderCreateRequest request)
        {
            _logger.LogInformation("Creating folder in teamspace. TeamspaceId={TeamspaceId}, FolderName={FolderName}",
                teamspaceId, request.Name);

            // Validate folder name
            var nameValidation = _validationService.ValidateItemName(request.Name);
            if (!nameValidation.IsValid)
            {
                return new CreateFolderResult
                {
                    IsSuccess = false,
                    ErrorCode = nameValidation.ErrorCode,
                    Message = nameValidation.ErrorMessage
                };
            }

            // Validate parent folder if specified
            if (request.ParentId.HasValue)
            {
                var parentFolder = await _itemRepository.GetItemAsync(userId, request.ParentId.Value, "folder");

                if (parentFolder == null || parentFolder.TeamspaceId != teamspaceId)
                {
                    return new CreateFolderResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.FOLDER_NOT_FOUND,
                        Message = "Parent folder not found in this teamspace"
                    };
                }
            }

            // Check name uniqueness
            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(
                request.Name,
                "folder",
                userId,
                request.ParentId,
                excludeItemId: null,
                includeDeleted: true);

            if (!uniquenessValidation.IsValid)
            {
                return new CreateFolderResult
                {
                    IsSuccess = false,
                    ErrorCode = uniquenessValidation.ErrorCode,
                    Message = uniquenessValidation.ErrorMessage
                };
            }

            var folder = new Item
            {
                Name = request.Name,
                Type = "folder",
                UserId = userId,
                ParentId = request.ParentId,
                TeamspaceId = teamspaceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            try
            {
                await _itemRepository.AddItemInTranscationAsync(folder);

                string relativeFolderPath = await _itemRepository.GetFolderPathAsync(folder);
                bool created = _itemStorageService.TryCreateFolder(userId, relativeFolderPath);

                if (!created)
                {
                    _logger.LogError("Failed to create physical folder. Path={Path}", relativeFolderPath);
                    await _itemRepository.DeleteItemPermanentlyAsync(folder);

                    return new CreateFolderResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.IO_ERROR,
                        Message = "Failed to create folder on disk"
                    };
                }

                _logger.LogInformation("Folder created in teamspace successfully. FolderId={FolderId}, TeamspaceId={TeamspaceId}",
                    folder.Id, teamspaceId);

                return new CreateFolderResult
                {
                    IsSuccess = true,
                    FolderId = folder.Id,
                    FolderName = folder.Name
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create folder in teamspace {TeamspaceId}", teamspaceId);
                return new CreateFolderResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An unexpected error occurred"
                };
            }
        }

        public async Task<RenameResult> RenameTeamspaceItemAsync(
            int userId,
            int teamspaceId,
            int itemId,
            string newName)
        {
            _logger.LogInformation("Renaming teamspace item. TeamspaceId={TeamspaceId}, ItemId={ItemId}, NewName={NewName}",
                teamspaceId, itemId, newName);

            // Validate new name
            var nameValidation = _validationService.ValidateItemName(newName);
            if (!nameValidation.IsValid)
            {
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = nameValidation.ErrorCode,
                    Message = nameValidation.ErrorMessage
                };
            }

            // Get the item
            var item = await GetTeamspaceItemAsync(userId, teamspaceId, itemId, null);
            if (item == null)
            {
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                    Message = "Item not found in teamspace"
                };
            }

            // Check uniqueness
            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(
                newName,
                item.Type,
                userId,
                item.ParentId,
                excludeItemId: itemId,
                includeDeleted: true);

            if (!uniquenessValidation.IsValid)
            {
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = uniquenessValidation.ErrorCode,
                    Message = uniquenessValidation.ErrorMessage
                };
            }

            List<Item> childItems = new();
            string? folderPath = null;

            if (item.Type == "folder")
            {
                await foreach (var child in _itemRepository.GetAllChildItemsAsync(userId, itemId))
                {
                    childItems.Add(child);
                }
                folderPath = await _itemRepository.GetFolderPathAsync(item);
                folderPath = Path.Combine(_itemStorageService.GetUserStoragePath(userId), folderPath);
            }

            var itemsToRename = _itemManagerService.PrepareItemsForRenaming(
                item,
                newName,
                childItems,
                folderPath);

            try
            {
                await _itemRepository.UpdateItemsInTransactionAsync(itemsToRename);

                _logger.LogInformation("Item renamed in teamspace successfully. ItemId={ItemId}, NewName={NewName}",
                    itemId, newName);

                return new RenameResult
                {
                    IsSuccess = true,
                    Message = "Item renamed successfully",
                    ItemId = item.Id,
                    NewName = newName,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rename item in teamspace {TeamspaceId}", teamspaceId);
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An unexpected error occurred"
                };
            }
        }
        public async Task<DeleteResult> SoftDeleteTeamspaceItemAsync(
                int userId,
                int teamspaceId,
                int itemId)
        {
            _logger.LogInformation("Soft deleting teamspace item. TeamspaceId={TeamspaceId}, ItemId={ItemId}",
                teamspaceId, itemId);

            var item = await GetTeamspaceItemAsync(userId, teamspaceId, itemId, null);
            if (item == null)
            {
                return new DeleteResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                    Message = "Item not found in teamspace"
                };
            }

            List<Item> childItems = new();
            
            if (item.Type == "folder")
            {
                await foreach(var child in _itemRepository.GetAllChildItemsAsync(userId, itemId))
                {
                    childItems.Add(child);
                }
            }

            var itemsToDelete = _itemManagerService.PrepareItemsForSoftDelete(item, childItems);

            try
            {
                await _itemRepository.UpdateItemsInTransactionAsync(itemsToDelete);

                var allItems = new List<Item>(itemsToDelete) { item };
                long totalBytes = allItems.Where(i => i.Type == "file").Sum(i => i.FileSize ?? 0);
                await _storageTrackingService.RemoveFromTeamspaceStorageAsync(teamspaceId, totalBytes);

                _logger.LogInformation("Item soft deleted in teamspace successfully. ItemId={ItemId}", itemId);

                return new DeleteResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.DELETED_SUCCESSFULLY,
                    Message = "Item moved to trash successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete item in teamspace {TeamspaceId}", teamspaceId);
                return new DeleteResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An unexpected error occurred"
                };
            }
        }

        public async Task<RestoreResult> RestoreTeamspaceItemAsync(
                int userId,
                int teamspaceId,
                int itemId)
        {
            _logger.LogInformation("Restoring teamspace item. TeamspaceId={TeamspaceId}, ItemId={ItemId}",
                teamspaceId, itemId);

            var item = await _itemRepository.GetDeletedItemAsync(userId, itemId);

            if (item == null || item.TeamspaceId != teamspaceId)
            {
                return new RestoreResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                    Message = "Item not found in teamspace trash"
                };
            }

            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(
                item.Name,
                item.Type,
                userId,
                item.ParentId,
                includeDeleted: false);

            if (!uniquenessValidation.IsValid)
            {
                return new RestoreResult
                {
                    IsSuccess = false,
                    ErrorCode = uniquenessValidation.ErrorCode,
                    Message = uniquenessValidation.ErrorMessage
                };
            }

            List<Item> itemsToRestore = new() { item };
            List<Item> descendants = new();
            if (item.Type == "folder")
            {
                await foreach (var desc in _itemRepository.GetAllChildItemsAsync(userId, itemId))
                {
                    descendants.Add(desc);
                }
                itemsToRestore.AddRange(descendants);
            }

            long totalBytes = itemsToRestore.Where(i => i.Type == "file").Sum(i => i.FileSize ?? 0);
            var canRestore = await _storageTrackingService.CanAddToTeamspaceStorageAsync(teamspaceId, totalBytes);

            if (!canRestore)
            {
                var (usedMb, limitMb) = await _storageTrackingService.GetTeamspaceStorageInfoAsync(teamspaceId);
                long restoreSizeMb = totalBytes / (1024 * 1024);

                return new RestoreResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.STORAGE_LIMIT_EXCEEDED,
                    Message = $"Restore would exceed teamspace storage limit ({usedMb + restoreSizeMb}MB / {limitMb}MB)"
                };
            }

            _itemManagerService.PrepareItemsForRestore(itemsToRestore);

            try
            {
                await _itemRepository.UpdateItemsInTransactionAsync(itemsToRestore);

                await _storageTrackingService.AddToTeamspaceStorageAsync(teamspaceId, totalBytes);

                _logger.LogInformation("Item restored in teamspace successfully. ItemId={ItemId}", itemId);

                return new RestoreResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.RESTORED_SUCCESSFULLY,
                    Message = "Item restored successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore item in teamspace {TeamspaceId}", teamspaceId);
                return new RestoreResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An unexpected error occurred"
                };
            }
        }
        #endregion

        #region Download Operations

        public async Task<FileDownloadResult> DownloadTeamspaceFileAsync(
            int userId,
            int teamspaceId,
            int fileId)
        {
            _logger.LogInformation("Downloading teamspace file. TeamspaceId={TeamspaceId}, FileId={FileId}",
                teamspaceId, fileId);

            var file = await GetTeamspaceItemAsync(userId, teamspaceId, fileId, "file");

            if (file == null || file.IsDeleted == true)
            {
                throw new FileNotFoundException(ErrorCodes.FILE_NOT_FOUND);
            }

            var fullPath = _itemStorageService.GetFileFullPath(userId, file.FilePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogError("Physical file missing. ItemId={ItemId}, Path={Path}", file.Id, fullPath);
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

        public async Task<(Stream archiveStream, string fileName)> DownloadTeamspaceFolderAsync(
            int userId,
            int teamspaceId,
            int folderId)
        {
            _logger.LogInformation("Downloading teamspace folder. TeamspaceId={TeamspaceId}, FolderId={FolderId}",
                teamspaceId, folderId);

            var folder = await GetTeamspaceItemAsync(userId, teamspaceId, folderId, "folder");

            if (folder == null || folder.IsDeleted == true)
            {
                throw new FileNotFoundException(ErrorCodes.FOLDER_NOT_FOUND);
            }

            var archiveStream = await _zipArchiveService.CreateFolderArchiveAsync(
                userId,
                folderId,
                folder.Name);

            return (archiveStream, $"{folder.Name}.zip");
        }

        public async Task<(Stream archiveStream, string fileName)> DownloadMultipleTeamspaceItemsAsync(
            int userId,
            int teamspaceId,
            List<int> itemIds)
        {
            _logger.LogInformation("Downloading multiple teamspace items. TeamspaceId={TeamspaceId}, Count={Count}",
                teamspaceId, itemIds.Count);

            var items = await _itemRepository.GetItemsByIdsForUserAsync(userId, itemIds);

            // Verify all items belong to the teamspace
            var teamspaceItems = items.Where(i => i.TeamspaceId == teamspaceId && i.IsDeleted == false).ToList();

            if (teamspaceItems.Count == 0)
            {
                throw new FileNotFoundException(ErrorCodes.ITEM_NOT_FOUND);
            }

            var archiveStream = await _zipArchiveService.CreateMultipleItemArchiveAsync(userId, teamspaceItems);
            var fileName = $"teamspace_items_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

            return (archiveStream, fileName);
        }

        #endregion

        #region Permission & Validation Helpers

        public async Task<bool> VerifyTeamspacePermissionAsync(
            int userId,
            int teamspaceId,
            string requiredPermission)
        {
            return await _teamspaceService.HasPermissionAsync(userId, teamspaceId, requiredPermission);
        }

        public async Task<bool> VerifyItemBelongsToTeamspaceAsync(int itemId, int teamspaceId)
        {
            var item = await _itemRepository.GetItemAsync(0, itemId, null);
            return item?.TeamspaceId == teamspaceId;
        }
        public async Task<bool> CheckStorageLimitAsync(int teamspaceId, long fileSizeBytes)
        {
            return await _storageTrackingService.CanAddToTeamspaceStorageAsync(teamspaceId, fileSizeBytes); //TODO: Clear the wrapper
        }

        #endregion
    }
}
