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
        
        public ItemController(IDbContextFactory<CloudCoreDbContext> contextFactory, IFileStorageService fileStorageService, IZipArchiveService zipArchiveService, IFileRenameService fileRenameService)
        {
            _contextFactory = contextFactory;
            _fileStorageService = fileStorageService;
            _zipArchiveService = zipArchiveService;
            _fileRenameService = fileRenameService;
        }

        /// <summary>
        /// Gets the current user ID from JWT token
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        /// <summary>
        /// Retrieves all items for a specific user within a given parent directory.
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="parentId">Parent directory ID (null for root level)</param>
        /// <returns>List of user items or NotFound if no items exist</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItemsAsync(int userId, int? parentId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId != userId)
                return StatusCode(403, new
                {
                    message = "You can only download your own files",
                    errorCode = "ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var userFiles = await context.Items
                    .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == parentId)
                    .ToListAsync();

                if (userFiles == null || userFiles.Count == 0)
                    return NotFound("No files found for this user.");

                return Ok(userFiles);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{folderId}/downloadfolder")]
        public async Task<IActionResult> DownloadFolderAsync(int userId, int folderId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId != userId)
                return StatusCode(403, new
                {
                    message = "You can only download your own files",
                    errorCode = "ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var folder = await context.Items
                    .Where(i => i.Id == folderId && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                    .FirstOrDefaultAsync();

                if (folder == null)
                    return NotFound("Folder not found");

                var archiveStream = await _zipArchiveService.CreateFolderArchiveAsync(userId, folderId, folder.Name);
                var fileName = $"{folder.Name}.zip";
                return File(archiveStream, "application/zip", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Downloads a file by ID.
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="fileId">File identifier</param>
        /// <returns>File content or NotFound/BadRequest if file doesn't exist or path is invalid</returns>
        [HttpGet("{fileId}/download")]
        public async Task<IActionResult> DownloadFileAsync(int userId, int fileId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId != userId)
                return StatusCode(403, new
                {
                    message = "You can only download your own files",
                    errorCode = "ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var item = await context.Items
                    .Where(i => i.Id == fileId && i.UserId == userId && i.IsDeleted == false && i.Type == "file")
                    .FirstOrDefaultAsync();

                if (item == null)
                    return NotFound("File not found or is not a file.");

                if (string.IsNullOrEmpty(item.FilePath))
                    return NotFound("File path is empty.");

                var fullPath = _fileStorageService.GetFileFullPath(userId, item.FilePath);

                if (!System.IO.File.Exists(fullPath))
                    return NotFound("File not found on disk.");

                return PhysicalFile(fullPath, item.MimeType ?? "application/octet-stream", item.Name, enableRangeProcessing: true);
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest("Invalid file path");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
        /// <example>
        /// POST /user/2/mydrive/download/multiple
        /// Content-Type: application/json
        /// Body: [1, 3, 5, 7]
        /// 
        /// Response: ZIP file download with multiple selected items
        /// </example>
        [HttpPost("download/multiple")]
        public async Task<IActionResult> DownloadMultipleItemsAsZipAsync(int userId, [FromBody] List<int> itemsId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId)
                return StatusCode(403, new
                {
                    message = "You can only download your own files",
                    errorCode = "ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var items = await context.Items
                    .Where(i => itemsId.Contains(i.Id) && i.UserId == userId && i.IsDeleted == false)
                    .ToListAsync();

                if (items == null || items.Count == 0)
                    return NotFound("File or files not found");

                var archiveStream = await _zipArchiveService.CreateMultipleItemArchiveAsync(userId, items);
                var fileName = $"selected_items_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

                return File(archiveStream, "application/zip", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return BadRequest();
            }
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
        public async Task<IActionResult> RenameItem(int userId, int itemId, [FromBody] string newName)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(newName))
            {
                return BadRequest(new
                {
                    message = "Item name cannot be empty",
                    errorCode = "INVALID_NAME",
                    timestamp = DateTime.UtcNow
                });
            }

            // Authorization check
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId)
            {
                return StatusCode(403, new
                {
                    message = "You can only rename your own files",
                    errorCode = "ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });
            }

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var item = await context.Items
                    .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == false)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(new
                    {
                        message = "Item not found",
                        errorCode = "ITEM_NOT_FOUND",
                        timestamp = DateTime.UtcNow
                    });
                }

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

                return BadRequest(new
                {
                    message = "Unsupported item type",
                    errorCode = "UNSUPPORTED_TYPE",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    errorCode = "INVALID_OPERATION",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new
                {
                    message = "Access denied to file system",
                    errorCode = "FILE_SYSTEM_ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new
                {
                    message = "Directory not found",
                    errorCode = "DIRECTORY_NOT_FOUND",
                    detail = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (IOException ex)
            {
                return StatusCode(409, new
                {
                    message = "File system conflict occurred",
                    errorCode = "FILE_SYSTEM_CONFLICT",
                    detail = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred",
                    errorCode = "INTERNAL_ERROR",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Gets the total size and file count for a directory
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="folderId">Folder identifier to calculate size for</param>
        /// <returns>Object containing total size in bytes and file count</returns>
        [HttpGet("{folderId}/size")]
        public async Task<ActionResult<object>> GetFolderSizeAsync(int userId, int folderId)
        {
            var currentUserId = GetCurrentUserId();
            
            if (currentUserId != userId)
                return StatusCode(403, new
                {
                    message = "You can only access your own files",
                    errorCode = "ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });

            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Verify the folder exists and belongs to the user
                var folder = await context.Items
                    .Where(i => i.Id == folderId && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                    .FirstOrDefaultAsync();

                if (folder == null)
                    return NotFound("Folder not found");

                // Calculate the total size recursively
                var (totalSize, fileCount) = await _zipArchiveService.CalculateArchiveSizeAsync(userId, folderId);

                return Ok(new
                {
                    folderId = folderId,
                    totalSize = totalSize,
                    fileCount = fileCount,
                    formattedSize = FormatFileSize(totalSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Gets sizes for multiple folders in a single request
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="folderIds">List of folder IDs to calculate sizes for</param>
        /// <returns>Dictionary of folder sizes</returns>
        [HttpPost("sizes")]
        public async Task<ActionResult<Dictionary<int, object>>> GetMultipleFolderSizesAsync(
            int userId, 
            [FromBody] List<int> folderIds)
        {
            var currentUserId = GetCurrentUserId();
            
            if (currentUserId != userId)
                return StatusCode(403, new
                {
                    message = "You can only access your own files",
                    errorCode = "ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });

            try
            {
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
                        formattedSize = FormatFileSize(totalSize)
                    };
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Helper method to format file sizes
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 Bytes";
            
            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
    }
}