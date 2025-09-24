using CloudCore.Common.Errors;
using CloudCore.Contracts.Requests;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CloudCore.Contracts.Responses.ItemResultResponses;

namespace CloudCore.Services.Implementations
{
    public class ItemRepository : IItemRepository
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly IZipArchiveService _zipArchiveService;
        private readonly IItemStorageService _itemStorageService;
        private readonly IValidationService _validationService;
        private readonly IItemRenameService _fileRenameService;
        private readonly IItemDataService _itemDataService;
        public ItemRepository(IDbContextFactory<CloudCoreDbContext> dbContextFactory, IZipArchiveService zipArchiveService, IItemStorageService itemStorageService, IValidationService validationService, IItemRenameService fileRenameService, IItemDataService itemDataService)
        {
            _dbContextFactory = dbContextFactory;
            _zipArchiveService = zipArchiveService;
            _itemStorageService = itemStorageService;
            _validationService = validationService;
            _fileRenameService = fileRenameService;
            _itemDataService = itemDataService;
        }

        public async Task<(Stream archiveStream, string fileName)> DownloadFolderAsync(int userId, int folderId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var folder = await context.Items
                .AsNoTracking()
                .Where(i => i.Id == folderId && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                .FirstOrDefaultAsync();

            if (folder == null)
                throw new FileNotFoundException("Folder not found");

            var archiveStream = await _zipArchiveService.CreateFolderArchiveAsync(userId, folderId, folder.Name);
            var fileName = $"{folder.Name}.zip";

            return (archiveStream, fileName);
        }

        public async Task<FileDownloadResult> DownloadFileAsync(int userId, int fileId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var item = await context.Items
                .AsNoTracking()
                .Where(i => i.Id == fileId && i.UserId == userId && i.IsDeleted == false && i.Type == "file")
                .FirstOrDefaultAsync();

            if (item == null)
                throw new FileNotFoundException("File not found");

            var fullPath = _itemStorageService.GetFileFullPath(userId, item.FilePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found");

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return new FileDownloadResult
            {
                Stream = fileStream,
                FileName = item.Name,
                MimeType = item.MimeType ?? "application/octet-stream"
            };

        }

        public async Task<(Stream archiveStream, string fileName)> DownloadMultipleItemsAsZipAsync(int userId, List<int> itemsId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var itemsValidation = await _validationService.ValidateItemIdsAsync(context, itemsId, userId);
            if (!itemsValidation.IsValid)
                throw new InvalidOperationException(itemsValidation.ErrorMessage);

            var items = await context.Items
                .AsNoTracking()
                .Where(i => itemsId.Contains(i.Id) && i.UserId == userId && i.IsDeleted == false)
                .ToListAsync();

            if (items == null || items.Count == 0)
                throw new FileNotFoundException("File not found");

            var totalSize = items.Where(i => i.Type == "file").Sum(i => i.FileSize ?? 0);
            var sizeValidation = _validationService.ValidateArchiveSize(totalSize, items.Count);
            if (!sizeValidation.IsValid)
                throw new InvalidOperationException(sizeValidation.ErrorMessage);

            var archiveStream = await _zipArchiveService.CreateMultipleItemArchiveAsync(userId, items);
            var fileName = $"selected_items_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

            return (archiveStream, fileName);
        }

        public async Task<RenameResult> RenameItemAsync(int userId, int itemId, string newName)
        {
            var itemName = _validationService.ValidateItemName(newName);
            if (!itemName.IsValid)
                return new RenameResult
                {
                    IsSuccess = false,
                    ErrorCode = itemName.ErrorCode,
                    Message = itemName.ErrorMessage
                };

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {

                var itemValidation = await _validationService.ValidateItemExistsAsync(context, itemId, userId);
                if (!itemValidation.IsValid)
                    return new RenameResult
                    {
                        IsSuccess = false,
                        ErrorCode = itemName.ErrorCode,
                        Message = itemName.ErrorMessage
                    };

                var item = await context.Items
                        .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == false)
                        .FirstOrDefaultAsync();

                var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(context, newName, userId, item.ParentId, itemId);
                if (!uniquenessValidation.IsValid)
                    return new RenameResult
                    {
                        IsSuccess = false,
                        ErrorCode = uniquenessValidation.ErrorCode,
                        Message = uniquenessValidation.ErrorMessage
                    };

                string successMessage;
                if (item.Type == "file")
                {
                    await _fileRenameService.RenameFileAsync(item, newName);
                    await context.SaveChangesAsync();
                    successMessage = "File renamed successfully";
                }
                else if (item.Type == "folder")
                {
                    await _fileRenameService.RenameFolderAsync(context, item, newName);
                    successMessage = "Folder renamed successfully";
                }
                else
                {
                    return new RenameResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.UNSUPPORTED_TYPE,
                        Message = "Unsupported item type"
                    };
                }
                await transaction.CommitAsync();

                return new RenameResult
                {
                    IsSuccess = true,
                    Message = successMessage,
                    ItemId = item.Id,
                    NewName = newName,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch(Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task<FolderSizeResult> GetFolderSizeAsync(int userId, int folderId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            // Verify the folder exists and belongs to the user
            var folder = await context.Items
                .AsNoTracking()
                .Where(i => i.Id == folderId && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                .FirstOrDefaultAsync();

            if (folder == null)
                throw new FileNotFoundException("File not found");

            // Calculate the total size recursively
            var (totalSize, fileCount) = await _zipArchiveService.CalculateArchiveSizeAsync(userId, folderId);

            return new FolderSizeResult
            {
                FolderId = folderId,
                TotalSize = totalSize,
                FileCount = fileCount,
                FormattedSize = _validationService.FormatFileSize(totalSize)
            };
        }

        public async Task<ActionResult<Dictionary<int, FolderSizeResult>>> GetMultipleFolderSizesAsync(int userId, List<int> folderIds)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var folders = await context.Items
                .AsNoTracking()
                .Where(i => folderIds.Contains(i.Id) && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                .ToListAsync();

            var results = new Dictionary<int, FolderSizeResult>();

            var tasks = folders.Select(async folder =>
            {
                var (totalSize, fileCount) = await _zipArchiveService.CalculateArchiveSizeAsync(userId, folder.Id);
                return new { folder.Id, totalSize, fileCount };
            });

            var taskResults = await Task.WhenAll(tasks);

            foreach (var result in taskResults)
            {
                results[result.Id] = new FolderSizeResult
                {
                    FolderId = result.Id,
                    TotalSize = result.totalSize,
                    FileCount = result.fileCount,
                    FormattedSize = _validationService.FormatFileSize(result.totalSize)
                };
            }

            return results;
        }

        public async Task<DeleteResult> DeleteItemAsync(int userId, int itemId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var item = await context.Items
                    .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == false)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return new DeleteResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                        Message = "File not found"
                    };
                }

                if (item.Type == "file")
                {
                    item.IsDeleted = true;
                    item.DeletedAt = DateTime.UtcNow;
                }
                else if (item.Type == "folder")
                {
                    item.IsDeleted = true;
                    item.DeletedAt = DateTime.UtcNow;
                    var childItems = await _itemDataService.GetAllChildItemsAsync(itemId, userId);
                    foreach (var childItem in childItems)
                    {
                        childItem.IsDeleted = true;
                        childItem.DeletedAt = DateTime.UtcNow;
                    }
                    context.UpdateRange(childItems);
                }
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new DeleteResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.DELETED_SUCCESSFULLY,
                    Message = "Item deleted successfully"
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<RestoreResult> RestoreItemAsync(int userId, int itemId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var item = await context.Items
                    .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == true)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return new RestoreResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.ITEM_NOT_FOUND,
                        Message = "File not found"
                    };
                }

                item.IsDeleted = false;
                item.DeletedAt = null;
                if (item.Type == "file")
                {
                    if (item.ParentId != null)
                    {
                        var parent = await _itemDataService.GetItemAsync(userId, (int)item.ParentId);
                        if (item.ParentId.HasValue)
                        {
                            if (parent != null && parent.IsDeleted == true)
                            {
                                return new RestoreResult
                                {
                                    IsSuccess = false,
                                    ErrorCode = ErrorCodes.PARENT_FOLDER_DELETED,
                                    Message = "Cannot restore file because its parent folder was also deleted."
                                };
                            }

                        }
                    }
                }
                else if (item.Type == "folder")
                {
                    item.IsDeleted = false;
                    item.DeletedAt = null;
                    var childItems = await _itemDataService.GetAllChildItemsAsync(itemId, userId);
                    foreach (var childItem in childItems)
                    {
                        childItem.IsDeleted = false;
                        childItem.DeletedAt = null;
                    }
                    context.UpdateRange(childItems);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new RestoreResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.RESTORED_SUCCESSFULLY,
                    Message = "Item restored successfully"
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<CreateFolderResult> CreateFolderAsync(int userId, FolderCreateRequest request)
        {
            var nameValidation = _validationService.ValidateItemName(request.Name);
            if (!nameValidation.IsValid)
                return new CreateFolderResult
                {
                    IsSuccess = false,
                    ErrorCode = nameValidation.ErrorCode,
                    Message = nameValidation.ErrorMessage
                };

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Validate parent folder exists if specified
                if (request.ParentId.HasValue)
                {
                    var parentValidation = await _validationService.ValidateItemExistsAsync(context, request.ParentId.Value, userId);
                    if (!parentValidation.IsValid)
                        return new CreateFolderResult
                        {
                            IsSuccess = false,
                            ErrorCode = parentValidation.ErrorCode,
                            Message = parentValidation.ErrorMessage
                        };
                }

                // Check if folder with same name already exists
                var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(context, request.Name, userId, request.ParentId, null);
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

                context.Items.Add(folder);
                await context.SaveChangesAsync();
                

                string folderPath = await _itemStorageService.GetFolderPathAsync(folder);
                Directory.CreateDirectory(folderPath);

                await transaction.CommitAsync();

                return new CreateFolderResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.CREATED_SUCCESSFULLY,
                    Message = "Folder created successfully",
                    FolderId = folder.Id,
                    FolderName = folder.Name
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<UploadResult> UploadFileAsync(int userId, IFormFile file, int? parentId = null)
        {
            var fileValidation = _validationService.ValidateFile(file);
            if (!fileValidation.IsValid)
                return new UploadResult
                {
                    IsSuccess = false,
                    ErrorCode = fileValidation.ErrorCode,
                    Message = fileValidation.ErrorMessage
                };


            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            string savedFilePath = null;
            try
            {
                if (parentId.HasValue)
                {
                    var parentValidation = await _validationService.ValidateItemExistsAsync(context, parentId.Value, userId);
                    if (!parentValidation.IsValid)
                    {
                        return new UploadResult
                        {
                            IsSuccess = false,
                            ErrorCode = parentValidation.ErrorCode,
                            Message = parentValidation.ErrorMessage
                        };
                    }
                }

                var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(context, file.FileName, userId, parentId, null);
                if (!uniquenessValidation.IsValid)
                {
                    return new UploadResult
                    {
                        IsSuccess = false,
                        ErrorCode = uniquenessValidation.ErrorCode,
                        Message = uniquenessValidation.ErrorMessage
                    };
                }

                savedFilePath = await _itemStorageService.SaveFileAsync(userId, file, parentId);

                var item = new Item
                {
                    Name = file.FileName,
                    Type = "file",
                    UserId = userId,
                    TeamspaceId = null,
                    ParentId = parentId,
                    FilePath = savedFilePath,
                    FileSize = file.Length,
                    MimeType = !string.IsNullOrEmpty(file.ContentType) ? file.ContentType : _itemStorageService.GetMimeType(file.FileName),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                context.Items.Add(item);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new UploadResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.UPLOADED_SUCCESSFULLY,
                    Message = "File uploaded successfully",
                    ItemId = item.Id,
                    FileName = item.Name
                };
            }
            catch(Exception)
            {
                await transaction.RollbackAsync();
                if (savedFilePath != null)
                {
                    try
                    {
                        var fullPath = _itemStorageService.GetFileFullPath(userId, savedFilePath);
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            Console.WriteLine($"[CLEANUP] Orphaned file '{fullPath}' deleted successfully.");
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"[CRITICAL] Failed to cleanup orphaned file '{savedFilePath}': {cleanupEx.Message}");
                    }
                }

                throw;
            }
        }
    }
}
