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
        public ItemController(IDbContextFactory<CloudCoreDbContext> contextFactory, IFileStorageService fileStorageService, IZipArchiveService zipArchiveService)
        {
            _contextFactory = contextFactory;
            _fileStorageService = fileStorageService;
            _zipArchiveService = zipArchiveService;
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
                return StatusCode(403 , new
                {
                    message = "You can only download your own files",
                    errorCode = "ACCESS_DENIED",
                    timestamp = DateTime.UtcNow
                });
            try
            {
                using var _context = _contextFactory.CreateDbContext(); 
                var userFiles = await _context.Items
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
                using var _context = _contextFactory.CreateDbContext();
                var folder = await _context.Items
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
                using var _context = _contextFactory.CreateDbContext();
                var item = await _context.Items
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
        [HttpPost ("download/multiple")]
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
                using var _context = _contextFactory.CreateDbContext();
                var items = await _context.Items
                    .Where(i => itemsId.Contains(i.Id) && i.UserId == userId && i.IsDeleted == false)
                    .ToListAsync();

                if (items == null || items.Count == 0)
                    return NotFound("File or files not found");

                var archiveStream = await _zipArchiveService.CreateMultipleItemArchiveAsync(userId, items);
                var fileName = $"selected_items_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

                return File(archiveStream, "application/zip", fileName);
            }
            catch(InvalidOperationException ex)
            { 
                return BadRequest(ex.Message); 
            }
            catch(Exception)
            {
                return BadRequest();
            }
        }

        


    }
}