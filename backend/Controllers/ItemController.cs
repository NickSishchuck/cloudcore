using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using CloudCore.Domain.Entities;
using CloudCore.Contracts.Responses;
using CloudCore.Contracts.Requests;
using CloudCore.Mappers;
using CloudCore.Common.QueryParameters;
using CloudCore.Common.Errors;

namespace CloudCore.Controllers
{
    [ApiController]
    [Route("user/{userid}/mydrive")]
    [Authorize] // Require authentication for all endpoints
    public class ItemController : ControllerBase
    {
        private readonly IItemApplication _itemApplication;
        private readonly ILogger<ItemController> _logger;

        public ItemController(IValidationService validationService, IItemApplication itemApplication, ILogger<ItemController> logger)
        {
            _itemApplication = itemApplication;
            _logger = logger;
        }
        /// <summary>
        /// Retrieves all items for a specific user within a given parent directory.
        /// </summary>
        /// <param name="userId">User identifier from route</param>
        /// <param name="parentId">Parent directory ID (null for root level)</param>
        /// <returns>List of user items or NotFound if no items exist</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItemsAsync([Required] int userId, int? parentId, [FromQuery] QueryParameters queryParams)
        {

            _logger.LogInformation("Fetching items for User ID: {UserId}, Parent ID: {ParentId}, Page: {Page}, Page Size: {PageSize}, Search Query: {SearchQuery}.", userId, parentId, queryParams.Page, queryParams.PageSize, queryParams.SearchQuery);

            var result = await _itemApplication.GetItemsAsync(userId, parentId, queryParams.Page, queryParams.PageSize, queryParams.SortBy, queryParams.SortDir, searchQuery: queryParams.SearchQuery);

            _logger.LogInformation("Successfully fetched {ItemCount} items for User ID: {UserId}.", result.Data.Count(), userId);
            return Ok(new PaginatedResponse<ItemResponse>
            {
                Data = result.Data.Select(i => i.ToResponseDto()),
                Pagination = result.Pagination
            });
        }

        [HttpGet("folder/path/{folderId}")]
        public async Task<IActionResult> GetFolderPath([Required] int userId, int folderId)
        {

            _logger.LogInformation("Fetching folder path for User ID: {UserId}, Folder ID: {FolderId}", userId, folderId);
            string folderPath = await _itemApplication.GetBreadcrumbPathAsync(userId, folderId, "folder");


            return Ok(folderPath);
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

            _logger.LogInformation("User {UserId} initiated download for Folder ID: {FolderId}.", userId, folderId);
            var (archiveStream, fileName) = await _itemApplication.DownloadFolderAsync(userId, folderId);

            _logger.LogInformation("Successfully created archive '{FileName}' for User ID: {UserId}.", fileName, userId);
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

            _logger.LogInformation("User {UserId} initiated download for File ID: {FileId}.", userId, fileId);

            var fileResult = await _itemApplication.DownloadFileAsync(userId, fileId);

            _logger.LogInformation("Serving file '{FileName}' for User ID: {UserId}.", fileResult.FileName, userId);

            return File(fileResult.Stream, fileResult.MimeType, fileResult.FileName, enableRangeProcessing: true);
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

            _logger.LogInformation("User {UserId} initiated download for {ItemCount} items.", userId, itemsId.Count);
            var (archiveStream, fileName) = await _itemApplication.DownloadMultipleItemsAsZipAsync(userId, itemsId);
            _logger.LogInformation("Successfully created archive '{FileName}' with multiple items for User ID: {UserId}.", fileName, userId);


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

            _logger.LogInformation("User {UserId} attempting to rename Item ID: {ItemId} to '{NewName}'.", userId, itemId, newName);
            var result = await _itemApplication.RenameItemAsync(userId, itemId, newName);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to rename Item ID: {ItemId} for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode}).", itemId, userId, result.Message, result.ErrorCode);
                return result.ErrorCode switch
                {
                    "ITEM_NOT_FOUND" => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    "NAME_CONFLICT" => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("Successfully renamed Item ID: {ItemId} to '{NewName}' for User ID: {UserId}.", itemId, newName, userId);

            return Ok(new
            {
                message = result.Message,
                code = result.ErrorCode,
                itemId = result.ItemId,
                itemNewName = result.NewName,
                timestamp = result.Timestamp
            });

        }


        [HttpDelete("{itemId}/delete")]
        public async Task<IActionResult> DeleteItemAsync(int userId, int itemId)
        {


            _logger.LogInformation("User {UserId} attempting to delete Item ID: {ItemId}.", userId, itemId);

            var result = await _itemApplication.SoftDeleteItemAsync(userId, itemId);
            _logger.LogInformation("Item ID: {ItemId} successfully moved to trash for User ID: {UserId}.", itemId, userId);

            return Ok(result);

        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFileAsync([Required] int userId, IFormFile file, [FromForm] int? parentId = null)
        {


            _logger.LogInformation("User {UserId} attempting to upload file '{FileName}' to Parent ID: {ParentId}.", userId, file.FileName, parentId);

            var result = await _itemApplication.UploadFileAsync(userId, file, parentId);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to upload file '{FileName}' for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode}).", file.FileName, userId, result.Message, result.ErrorCode);
                return result.ErrorCode switch
                {
                    "PARENT_NOT_FOUND" => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode)),
                    "NAME_CONFLICT" => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully uploaded file '{FileName}' with new Item ID: {ItemId}.", userId, file.FileName, result.ItemId);

            return Ok(new
            {
                message = result.Message,
                itemId = result.ItemId,
                fileName = result.FileName,
                timestamp = DateTime.UtcNow
            });

        }


        // <summary>
        /// Creates a new folder for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user creating the folder.</param>
        /// <param name="request">The folder creation request containing folder name and optional parent ID.</param>
        /// <returns>
        /// Returns:
        /// - 200 OK if the folder is successfully created.
        /// - 400 Bad Request if the folder name is invalid or the parent folder does not exist.
        /// - 409 Conflict if a folder with the same name already exists under the same parent.
        /// </returns>
        [HttpPost("createfolder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateFolderAsync([Required] int userId, [FromBody] FolderCreateRequest request)
        {



            _logger.LogInformation("User {UserId} attempting to create folder '{FolderName}' in Parent ID: {ParentId}.", userId, request.Name, request.ParentId);

            var result = await _itemApplication.CreateFolderAsync(userId, request);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to create folder '{FolderName}' for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode}).", request.Name, userId, result.Message, result.ErrorCode);
                return result.ErrorCode switch
                {
                    "PARENT_NOT_FOUND" => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode)),
                    "NAME_CONFLICT" => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully created folder '{FolderName}' with new Folder ID: {FolderId}.", userId, request.Name, result.FolderId);

            return Ok(new
            {
                message = result.Message,
                code = result.ErrorCode,
                folderId = result.FolderId,
                folderName = result.FolderName,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("trash")]
        public async Task<ActionResult<IEnumerable<Item>>> GetDeletedItemsAsync([Required] int userId, int? parentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string? sortBy = "name", [FromQuery] string? sortDir = "asc")
        {

            _logger.LogInformation("Fetching trash items for User ID: {UserId}, Page: {Page}, PageSize: {PageSize}.", userId, page, pageSize);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 30;

            var result = await _itemApplication.GetItemsAsync(userId, parentId, page, pageSize, sortBy, sortDir, true);

            _logger.LogInformation("Successfully fetched {ItemCount} trash items for User ID: {UserId}.", result.Data.Count(), userId);

            return Ok(new PaginatedResponse<ItemResponse>
            {
                Data = result.Data.Select(i => i.ToResponseDto()),
                Pagination = result.Pagination
            });
        }

        [HttpPut("{itemId}/restore")]
        public async Task<IActionResult> RestoreItemAsync(int userId, int itemId)
        {

            _logger.LogInformation("User {UserId} attempting to restore Item ID: {ItemId}.", userId, itemId);
            var result = await _itemApplication.RestoreItemAsync(userId, itemId);

            if (!result.IsSuccess)
                _logger.LogWarning("Failed to restore Item ID: {ItemId} for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode}).", itemId, userId, result.Message, result.ErrorCode);
            else
                _logger.LogInformation("Item ID: {ItemId} successfully restored for User ID: {UserId}.", itemId, userId);

            return Ok(result);
        }

        [HttpPost("{itemId}/move/{targetId}")]
        public async Task<IActionResult> MoveItemAsync(int userId, int itemId, int targetId)
        {
            _logger.LogInformation("User {UserId} attempting to move Item ID: {ItemID} to Target ID: {TargetId}", userId, itemId, targetId);
            var result = await _itemApplication.MoveItemAsync(userId, itemId, targetId);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to move Item ID: {ItemId} for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode}).", itemId, userId, result.Message, result.ErrorCode);
                return result.ErrorCode switch
                {
                    ErrorCodes.ITEM_NOT_FOUND => NotFound(result),
                    ErrorCodes.FOLDER_NOT_FOUND => NotFound(result),
                    ErrorCodes.FILE_NOT_FOUND => NotFound(result),
                    ErrorCodes.INVALID_TARGET => BadRequest(result),
                    ErrorCodes.CIRCULAR_REFERENCE => BadRequest(result),
                    ErrorCodes.NAME_ALREADY_EXISTS => Conflict(result),
                    ErrorCodes.ACCESS_DENIED => StatusCode(StatusCodes.Status403Forbidden, result),
                    ErrorCodes.IO_ERROR => StatusCode(StatusCodes.Status409Conflict, result),
                    ErrorCodes.UNEXPECTED_ERROR => StatusCode(StatusCodes.Status500InternalServerError, result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }
    }
}