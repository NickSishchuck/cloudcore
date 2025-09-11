using CloudCore.Models;
using CloudCore.Services.Interfaces;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CloudCore.Controllers
{
    [ApiController]
    [Route("user/{userid}/mydrive")]
    [Authorize] // Require authentication for all endpoints
    public class ItemController : ControllerBase
    {
        private readonly CloudCoreDbContext _context;
        private readonly IFileStorageService _fileStorageService;

        public ItemController(CloudCoreDbContext context, IFileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
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
        public async Task<ActionResult<IEnumerable<Item>>> GetItems(int userId, int? parentId)
        {
            var currentUserId = GetCurrentUserId();
            
            if (currentUserId != userId)
                return Forbid("You can only access your own files");

            var userFiles = await _context.Items
                .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == parentId)
                .ToListAsync();

            if (userFiles == null || !userFiles.Any())
                return NotFound("No files found for this user.");

            return Ok(userFiles);
        }

        /// <summary>
        /// Downloads a file by ID.
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="id">File identifier</param>
        /// <returns>File content or NotFound/BadRequest if file doesn't exist or path is invalid</returns>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadFile(int userId, int id)
        {
            var currentUserId = GetCurrentUserId();
            
            if (currentUserId != userId)
                return Forbid("You can only download your own files");

            var item = await _context.Items
                .Where(i => i.Id == id && i.UserId == userId && i.IsDeleted == false && i.Type == "file")
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound("File not found or is not a file.");

            if (string.IsNullOrEmpty(item.FilePath))
                return NotFound("File path is empty.");

            try
            {
                var fullPath = _fileStorageService.GetFileFullPath(userId, item.FilePath);

                if (!System.IO.File.Exists(fullPath))
                    return NotFound("File not found on disk.");

                return PhysicalFile(fullPath, item.MimeType ?? "application/octet-stream", item.Name, enableRangeProcessing: false);
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest("Invalid file path");
            }
        }
    }
}