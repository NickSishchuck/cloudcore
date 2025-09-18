using CloudCore.Models;
using CloudCore.Services.Interfaces;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IO.Compression;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using MySqlConnector;

namespace CloudCore.Controllers
{
    [ApiController]
    [Route("user/{userid}/mydrive")]
    [Authorize] // Require authentication for all endpoints
    public class ItemController : ControllerBase
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _contextFactory;
        private readonly IFileStorageService _fileStorageService;
        private readonly IZipArchiveService _zipArchiveService;
        private readonly IFileRenameService _fileRenameService;
        private readonly IValidationService _validationService;
        private readonly IItemRepository _itemRepository;

        public ItemController(IDbContextFactory<CloudCoreDbContext> contextFactory, IFileStorageService fileStorageService, IZipArchiveService zipArchiveService, IFileRenameService fileRenameService, IValidationService validationService, IItemRepository itemRepository)
        {
            _contextFactory = contextFactory;
            _fileStorageService = fileStorageService;
            _zipArchiveService = zipArchiveService;
            _fileRenameService = fileRenameService;
            _validationService = validationService;
            _itemRepository = itemRepository;
        }

        /// <summary>
        /// Gets the current user ID from JWT token
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        private ActionResult? VerifyUser(int userId)
        {
            var currentUserId = GetCurrentUserId();

            var authValidation = _validationService.ValidateUserAuthorization(currentUserId, userId);
            if (!authValidation.IsValid)
                return StatusCode(403, ApiResponse.Error(authValidation.ErrorMessage, authValidation.ErrorCode));

            return null;
        }
        /// <summary>
        /// Retrieves all items for a specific user within a given parent directory.
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="parentId">Parent directory ID (null for root level)</param>
        /// <returns>List of user items or NotFound if no items exist</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItemsAsync([Required] int userId, int? parentId)
        {
            var authResult = VerifyUser(userId);
            if (authResult != null)
                return authResult;


            using var context = _contextFactory.CreateDbContext();
            var userFiles = await context.Items
                .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == parentId)
                .ToListAsync();

            if (userFiles == null || userFiles.Count == 0)
                return NotFound(ApiResponse.Error("No files found for this user", "ITEM_NOT_FOUND"));

            return Ok(userFiles);
        }

        /// <summary>
        /// Downloads a folder as a ZIP archive for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the folder</param>
        /// <param name="folderId">The ID of the folder to download</param>
        /// <returns>
        /// A ZIP file containing all contents of the specified folder if successful,
        /// or an appropriate error response if the folder is not found, access is denied,
        /// or an error occurs during archive creation.
        /// </returns>
        /// <response code="200">Returns the folder as a ZIP file download</response>
        /// <response code="403">Access denied - user can only download their own folders</response>
        /// <response code="404">Folder not found or does not belong to the user</response>
        /// <response code="400">Bad request - invalid operation or error during archive creation</response>
        /// <remarks>
        /// <example>
        /// GET /user/123/mydrive/456/downloadfolder
        /// 
        /// Response: ZIP file download named "MyFolder.zip"
        [HttpGet("{folderId}/downloadfolder")]
        public async Task<IActionResult> DownloadFolderAsync([Required] int userId, [Required] int folderId)
        {
            var authResult = VerifyUser(userId);
            if (authResult != null)
                return authResult;

            using var context = _contextFactory.CreateDbContext();
            var folder = await context.Items
                .Where(i => i.Id == folderId && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                .FirstOrDefaultAsync();

            if (folder == null)
                return NotFound(ApiResponse.Error("Folder not found", "ITEM_NOT_FOUND"));

            var archiveStream = await _zipArchiveService.CreateFolderArchiveAsync(userId, folderId, folder.Name);
            var fileName = $"{folder.Name}.zip";
            return File(archiveStream, "application/zip", fileName);


        }

        /// <summary>
        /// Downloads a file by ID.
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="fileId">File identifier</param>
        /// <returns>File content or NotFound/BadRequest if file doesn't exist or path is invalid</returns>
        [HttpGet("{fileId}/download")]
        public async Task<IActionResult> DownloadFileAsync([Required] int userId, [Required] int fileId)
        {
            var authResult = VerifyUser(userId);
            if (authResult != null)
                return authResult;

            using var context = _contextFactory.CreateDbContext();
            var item = await context.Items
                .Where(i => i.Id == fileId && i.UserId == userId && i.IsDeleted == false && i.Type == "file")
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(ApiResponse.Error("File not found", "ITEM_NOT_FOUND"));

            var fullPath = _fileStorageService.GetFileFullPath(userId, item.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound(ApiResponse.Error("File not found", "ITEM_NOT_FOUND"));

            return PhysicalFile(fullPath, item.MimeType ?? "application/octet-stream", item.Name, enableRangeProcessing: true);
        }

        /// <summary>
        /// Downloads multiple selected items (files and folders) as a single ZIP archive
        /// Processes user authorization, validates item ownership, and creates compressed archive
        /// </summary>
        /// <param name="userId">User identifier from route parameter for authorization validation</param>
        /// <param name="itemsId">List of item IDs to include in the ZIP archive from request body</param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// This endpoint allows users to download multiple files and folders in a single ZIP archive.
        /// The method validates user permissions, queries the database for accessible items,
        /// and uses the ZIP archive service to create a compressed file.
        /// Archive filename format: "selected_items_yyyyMMdd_HHmmss.zip"
        /// Only processes items that belong to the authenticated user and are not soft-deleted.
        /// </remarks>
        [HttpPost("download/multiple")]
        public async Task<IActionResult> DownloadMultipleItemsAsZipAsync([Required] int userId, [FromBody] List<int> itemsId)
        {
            var authResult = VerifyUser(userId);
            if (authResult != null)
                return authResult;

            using var context = _contextFactory.CreateDbContext();

            var itemsValidation = await _validationService.ValidateItemIdsAsync(context, itemsId, userId);
            if (!itemsValidation.IsValid)
                return BadRequest(ApiResponse.Error(itemsValidation.ErrorMessage, itemsValidation.ErrorCode));


            var items = await context.Items
                 .Where(i => itemsId.Contains(i.Id) && i.UserId == userId && i.IsDeleted == false)
                 .ToListAsync();

            if (items == null || items.Count == 0)
                return NotFound(ApiResponse.Error("File not found", "ITEM_NOT_FOUND"));

            var totalSize = items.Where(i => i.Type == "file").Sum(i => i.FileSize ?? 0);
            var sizeValidation = _validationService.ValidateArchiveSize(totalSize, items.Count);
            if (!sizeValidation.IsValid)
                return BadRequest(ApiResponse.Error(sizeValidation.ErrorMessage, sizeValidation.ErrorCode));

            var archiveStream = await _zipArchiveService.CreateMultipleItemArchiveAsync(userId, items);
            var fileName = $"selected_items_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

            return File(archiveStream, "application/zip", fileName);
        }

        /// <summary>
        /// Renames an item (file or folder) for a specific user, with authorization checks.
        /// Updates the database and file system paths accordingly.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the item</param>
        /// <param name="itemId">The ID of the item to rename</param>
        /// <param name="newName">The new name for the item</param>
        /// <returns>An action result indicating success or failure with appropriate HTTP status codes</returns>
        [HttpPut("{itemId}/rename")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RenameItemAsync([Required] int userId, [Required] int itemId, [StringLength(250)][FromBody] string newName)
        {

            var authResult = VerifyUser(userId);
            if (authResult != null)
                return authResult;

            var itemName = _validationService.ValidateItemName(newName);
            if (!itemName.IsValid)
                return StatusCode(409, ApiResponse.Error(itemName.ErrorMessage, itemName.ErrorCode));

            using var context = _contextFactory.CreateDbContext();

            var itemValidation = await _validationService.ValidateItemExistsAsync(context, itemId, userId);
            if (!itemValidation.IsValid)
                return NotFound(ApiResponse.Error(itemValidation.ErrorMessage, itemValidation.ErrorCode));

            var item = await context.Items
                .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == false)
                .FirstOrDefaultAsync();

            var uniquenessValidation = await _validationService.ValidateNameUniquenessAsync(context, newName, userId, item.ParentId, itemId);
            if (!uniquenessValidation.IsValid)
                return Conflict(ApiResponse.Error(uniquenessValidation.ErrorMessage, uniquenessValidation.ErrorCode));

            if (item.Type == "file")
            {
                _fileRenameService.RenameFile(item, newName, out string newRelativePath);
                await context.SaveChangesAsync();

                return Ok(new
                {
                    message = "File renamed successfully",
                    itemId = item.Id,
                    itemNewName = newName,
                    timestamp = DateTime.UtcNow
                });
            }

            if (item.Type == "folder")
            {
                await _fileRenameService.RenameFolder(context, item, newName);

                return Ok(new
                {
                    message = "Folder renamed successfully",
                    itemId = item.Id,
                    itemNewName = newName,
                    timestamp = DateTime.UtcNow
                });
            }

            return BadRequest(ApiResponse.Error("Unsupported item type", "UNSUPPORTED_TYPE"));

        }

        /// <summary>
        /// Gets the total size and file count for a directory
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="folderId">Folder identifier to calculate size for</param>
        /// <returns>Object containing total size in bytes and file count</returns>
        [HttpGet("{folderId}/size")]
        public async Task<ActionResult<object>> GetFolderSizeAsync([Required] int userId, [Required] int folderId)
        {
            var authResult = VerifyUser(userId);
            if (authResult != null)
                return authResult;

            using var context = _contextFactory.CreateDbContext();

            // Verify the folder exists and belongs to the user
            var folder = await context.Items
                .Where(i => i.Id == folderId && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                .FirstOrDefaultAsync();

            if (folder == null)
                return NotFound(ApiResponse.Error("File not found", "ITEM_NOT_FOUND"));

            // Calculate the total size recursively
            var (totalSize, fileCount) = await _zipArchiveService.CalculateArchiveSizeAsync(userId, folderId);

            return Ok(new
            {
                folderId = folderId,
                totalSize = totalSize,
                fileCount = fileCount,
                formattedSize = _validationService.FormatFileSize(totalSize)
            });

        }

        /// <summary>
        /// Gets sizes for multiple folders in a single request
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="folderIds">List of folder IDs to calculate sizes for</param>
        /// <returns>Dictionary of folder sizes</returns>
        [HttpPost("sizes")]
        public async Task<ActionResult<Dictionary<int, object>>> GetMultipleFolderSizesAsync([Required] int userId, [FromBody] List<int> folderIds)
        {
            var authResult = VerifyUser(userId);
            if (authResult != null)
                return authResult;

            using var context = _contextFactory.CreateDbContext();

            // Verify all folders exist and belong to the user
            var folders = await context.Items
                .Where(i => folderIds.Contains(i.Id) && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                .ToListAsync();

            var results = new Dictionary<int, object>();

            foreach (var folder in folders)
            {
                var (totalSize, fileCount) = await _zipArchiveService.CalculateArchiveSizeAsync(userId, folder.Id);

                results[folder.Id] = new
                {
                    folderId = folder.Id,
                    totalSize = totalSize,
                    fileCount = fileCount,
                    formattedSize = _validationService.FormatFileSize(totalSize)
                };
            }

            return Ok(ApiResponse.Ok());

        }

        [HttpDelete("{itemId}/delete")]
        public async Task<IActionResult> DeleteItemAsync(int userId, int itemId)
        {
            var authResult = VerifyUser(userId);
            if (authResult != null)
                return authResult;

            using var context = _contextFactory.CreateDbContext();

            // Get item 
            var item = await context.Items
                .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == false)
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(ApiResponse.Error("File not found", "ITEM_NOT_FOUND"));

            if (item.Type == "file")
                item.IsDeleted = true;

            if (item.Type == "folder")
            {
                item.IsDeleted = true;
                var childItems = await _itemRepository.GetAllChildItemsAsync(itemId, userId);
                foreach (var childItem in childItems)
                    childItem.IsDeleted = true;
                context.UpdateRange(childItems);

            }
            await context.SaveChangesAsync();
            return Ok(ApiResponse.Ok());

        }


    }
}