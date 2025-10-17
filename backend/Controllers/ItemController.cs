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
using NaturalSort.Extension;
using static CloudCore.Contracts.Responses.ItemResultResponses;

namespace CloudCore.Controllers
{
    [ApiController]
    [Route("user/{userId}/mydrive")]
    [Authorize] // Require authentication for all endpoints
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class ItemController : ControllerBase
    {
        private readonly IItemApplication _itemApplication;
        private readonly ILogger<ItemController> _logger;

        public ItemController(IItemApplication itemApplication, ILogger<ItemController> logger)
        {
            _itemApplication = itemApplication;
            _logger = logger;
        }

        #region Get something
        /// <summary>
        /// Retrieves a paginated list of items within a specific directory.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="parentId">Parent directory ID (null for root level).</param>
        /// <param name="queryParams">Query parameters for pagination, sorting, and search.</param>
        /// <returns>Paginated list of items.</returns>
        /// <response code="200">Returns the paginated list of items.</response>
        /// <response code="401">Unauthorized - user must be authenticated.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<ItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Item>>> GetItemsAsync([FromRoute] int userId, [FromQuery] int? parentId, [FromQuery] QueryParameters queryParams)
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


        /// <summary>
        /// Retrieves the full breadcrumb path of a folder by its ID.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="folderId">The ID of the folder.</param>
        /// <returns>Full path string of the folder.</returns>
        /// <response code="200">Returns the folder path.</response>
        /// <response code="404">Folder not found.</response>
        [HttpGet("folder/path/{folderId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<string>> GetFolderPath([FromRoute] int userId, [FromRoute] int folderId)
        {

            _logger.LogInformation("Fetching folder path for User ID: {UserId}, Folder ID: {FolderId}", userId, folderId);
            string folderPath = await _itemApplication.GetBreadcrumbPathAsync(userId, folderId, "folder");


            return Ok(folderPath);
        }

        /// <summary>
        /// Retrieves all direct child folders within a parent folder.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="parentFolderId">Parent folder ID (null for root).</param>
        /// <returns>List of child folders.</returns>
        /// <response code="200">Returns list of folders.</response>
        [HttpGet("folders")]
        [ProducesResponseType(typeof(IEnumerable<ItemResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetСhildFoldersAsync([FromRoute] int userId, [FromQuery] int? parentFolderId = null)
        {
            _logger.LogInformation("Fetching child folders for User ID: {UserId}, Parent Folder ID: {FolderId}", userId, parentFolderId);

            var childrenAsyncEnumerable = _itemApplication.GetDirectChildrenAsync(userId, parentFolderId, "folder");

            if (childrenAsyncEnumerable == null)
            {
                return Ok(Enumerable.Empty<ItemResponse>());
            }

            var result = await childrenAsyncEnumerable
                .Where(item => item != null)
                .ToListAsync();

            var sorted = result
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase.WithNaturalSort())
                .Select(i => i.ToResponseDto());

            return Ok(sorted);
        }

        /// <summary>
        /// Retrieves an item by its name within a parent folder.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="name">Name of the item to search for.</param>
        /// <param name="parentId">Parent folder ID (null for root).</param>
        /// <returns>The matching item.</returns>
        /// <response code="200">Returns the item.</response>
        /// <response code="404">Item not found.</response>
        [HttpGet("get/name")]
        [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetItemByNameAsync([FromRoute] int userId, [FromQuery][Required] string name, [FromQuery] int? parentId)
        {
            var item = await _itemApplication.GetItemByNameAsync(userId, name, parentId);

            if (item == null)
                return NotFound(new { message = "Item not found." });

            return Ok(item.ToResponseDto());
        }


        /// <summary>
        /// Retrieves items from the recycle bin (trash).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="parentId">Parent folder ID (optional).</param>
        /// <param name="queryParams">Query parameters for pagination and sorting.</param>
        /// <returns>Paginated list of deleted items.</returns>
        /// <response code="200">Returns paginated trash items.</response>
        [HttpGet("trash")]
        [ProducesResponseType(typeof(PaginatedResponse<ItemResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Item>>> GetDeletedItemsAsync([FromRoute] int userId, int? parentId, [FromQuery] QueryParameters queryParams)
        {

            _logger.LogInformation("Fetching items for User ID: {UserId}, Parent ID: {ParentId}, Page: {Page}, Page Size: {PageSize}, Search Query: {SearchQuery}.", userId, parentId, queryParams.Page, queryParams.PageSize, queryParams.SearchQuery);

            var result = await _itemApplication.GetItemsAsync(userId, parentId, queryParams.Page, queryParams.PageSize, queryParams.SortBy, queryParams.SortDir, true, searchQuery: queryParams.SearchQuery);

            _logger.LogInformation("Successfully fetched {ItemCount} trash items for User ID: {UserId}.", result.Data.Count(), userId);

            return Ok(new PaginatedResponse<ItemResponse>
            {
                Data = result.Data.Select(i => i.ToResponseDto()),
                Pagination = result.Pagination
            });
        }

        #endregion

        #region Download something

        /// <summary>
        /// Downloads a folder as a ZIP archive.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the folder.</param>
        /// <param name="folderId">The ID of the folder to download.</param>
        /// <returns>ZIP file containing all folder contents.</returns>
        /// <response code="200">Returns the folder as a ZIP file.</response>
        /// <response code="404">Folder not found.</response>
        /// <remarks>
        /// <para>Sample request:</para>
        /// <para>    GET /user/123/mydrive/456/downloadfolder</para>
        /// <para>Response: ZIP file named "FolderName.zip"</para>
        /// </remarks>
        [HttpGet("{folderId}/downloadfolder")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/zip")]
        public async Task<IActionResult> DownloadFolderAsync([FromRoute] int userId, [Required] int folderId)
        {

            _logger.LogInformation("User {UserId} initiated download for Folder ID: {FolderId}.", userId, folderId);
            var (archiveStream, fileName) = await _itemApplication.DownloadFolderAsync(userId, folderId);

            _logger.LogInformation("Successfully created archive '{FileName}' for User ID: {UserId}.", fileName, userId);
            return File(archiveStream, "application/zip", fileName);
        }

        /// <summary>
        /// Downloads a file by its ID.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the file.</param>
        /// <param name="fileId">The ID of the file to download.</param>
        /// <returns>File stream with appropriate content type.</returns>
        /// <response code="200">Returns the file content.</response>
        /// <response code="404">File not found.</response>
        [HttpGet("{fileId}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadFileAsync([FromRoute] int userId, [FromRoute] int fileId)
        {

            _logger.LogInformation("User {UserId} initiated download for File ID: {FileId}.", userId, fileId);

            var fileResult = await _itemApplication.DownloadFileAsync(userId, fileId);

            _logger.LogInformation("Serving file '{FileName}' for User ID: {UserId}.", fileResult.FileName, userId);

            return File(fileResult.Stream, fileResult.MimeType, fileResult.FileName, enableRangeProcessing: true);
        }

        /// <summary>
        /// Downloads multiple items (files and folders) as a single ZIP archive.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="itemsId">List of item IDs to include in the archive.</param>
        /// <returns>ZIP file containing all selected items.</returns>
        /// <response code="200">Returns the archive file.</response>
        /// <response code="400">Invalid item IDs or validation failed.</response>
        /// <remarks>
        /// <para>Sample request:</para>
        /// <para>    POST /user/123/mydrive/download/multiple</para>
        /// <para>    [12, 45, 67, 89]</para>
        /// <para>Archive filename format: "selected_items_yyyyMMdd_HHmmss.zip"</para>
        /// </remarks>
        [HttpPost("download/multiple")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/zip")]
        public async Task<IActionResult> DownloadMultipleItemsAsZipAsync([FromRoute] int userId, [FromBody] List<int> itemsId)
        {

            _logger.LogInformation("User {UserId} initiated download for {ItemCount} items.", userId, itemsId.Count);
            var (archiveStream, fileName) = await _itemApplication.DownloadMultipleItemsAsZipAsync(userId, itemsId);
            _logger.LogInformation("Successfully created archive '{FileName}' with multiple items for User ID: {UserId}.", fileName, userId);


            return File(archiveStream, "application/zip", fileName);
        }
        #endregion

        #region Modify something
        /// <summary>
        /// Renames an item (file or folder).
        /// </summary>
        /// <param name="userId">The ID of the user who owns the item.</param>
        /// <param name="itemId">The ID of the item to rename.</param>
        /// <param name="newName">The new name for the item (max 250 characters).</param>
        /// <returns>Result of the rename operation.</returns>
        /// <response code="200">Item renamed successfully.</response>
        /// <response code="400">Invalid name or request.</response>
        /// <response code="404">Item not found.</response>
        /// <response code="409">Name conflict - item with this name already exists.</response>
        /// <remarks>
        /// <para>Sample request:</para>
        /// <para>    PUT /user/123/mydrive/456/rename</para>
        /// <para>    "New Document Name.pdf"</para>
        /// </remarks>
        [HttpPut("{itemId}/rename")]
        [ProducesResponseType(typeof(RenameResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RenameItemAsync([FromRoute] int userId, [FromRoute] int itemId, [StringLength(250)][FromBody] string newName)
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

        /// <summary>
        /// Moves an item to a different folder.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="itemId">The ID of the item to move.</param>
        /// <param name="targetId">Target folder ID (null for root).</param>
        /// <returns>Result of the move operation.</returns>
        /// <response code="200">Item moved successfully.</response>
        /// <response code="400">Invalid target or circular reference detected.</response>
        /// <response code="404">Item or target folder not found.</response>
        /// <response code="409">Name conflict or I/O error.</response>
        [HttpPost("{itemId}/move/{targetId?}")]
        [ProducesResponseType(typeof(MoveResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MoveResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MoveResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MoveResult), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MoveItemAsync([FromRoute] int userId, [FromRoute] int itemId, [FromQuery] int? targetId)
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


        /// <summary>
        /// Restores an item from the recycle bin.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="itemId">The ID of the item to restore.</param>
        /// <returns>Result of the restore operation.</returns>
        /// <response code="200">Item restored successfully.</response>
        /// <response code="400">Restoration failed (parent deleted, storage limit, etc.).</response>
        /// <response code="404">Item not found in recycle bin.</response>
        [HttpPut("{itemId}/restore")]
        [ProducesResponseType(typeof(RestoreResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestoreResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RestoreResult), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RestoreItemAsync([FromRoute] int userId, [FromRoute] int itemId)
        {

            _logger.LogInformation("User {UserId} attempting to restore Item ID: {ItemId}.", userId, itemId);
            var result = await _itemApplication.RestoreItemAsync(userId, itemId);

            if (!result.IsSuccess)
                _logger.LogWarning("Failed to restore Item ID: {ItemId} for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode}).", itemId, userId, result.Message, result.ErrorCode);
            else
                _logger.LogInformation("Item ID: {ItemId} successfully restored for User ID: {UserId}.", itemId, userId);

            return Ok(result);
        }


        #endregion

        #region Delete something

        /// <summary>
        /// Moves an item to the recycle bin (soft delete).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="itemId">The ID of the item to delete.</param>
        /// <returns>Result of the delete operation.</returns>
        /// <response code="200">Item moved to trash successfully.</response>
        /// <response code="404">Item not found.</response>
        [HttpDelete("{itemId}/delete")]
        [ProducesResponseType(typeof(DeleteResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(DeleteResult), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SoftDeleteItemAsync([FromRoute] int userId, [FromRoute] int itemId)
        {


            _logger.LogInformation("User {UserId} attempting to delete Item ID: {ItemId}.", userId, itemId);

            var result = await _itemApplication.SoftDeleteItemAsync(userId, itemId);
            _logger.LogInformation("Item ID: {ItemId} successfully moved to trash for User ID: {UserId}.", itemId, userId);

            return Ok(result);

        }

        /// <summary>
        /// Permanently deletes an item from both database and physical storage.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="itemId">The ID of the item to permanently delete.</param>
        /// <returns>Result of the permanent deletion.</returns>
        /// <response code="200">Item permanently deleted.</response>
        /// <response code="404">Item not found.</response>
        /// <remarks>
        /// Warning: This operation cannot be undone.
        /// </remarks>
        [HttpDelete("{itemId}/delete/permanently")]
        [ProducesResponseType(typeof(DeleteResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(DeleteResult), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePermanently([FromRoute] int userId, [FromRoute] int itemId)
        {
            var result = await _itemApplication.DeleteItemPermanentlyAsync(userId, itemId);

            _logger.LogInformation("Item ID: {ItemId} permanently deleted for User ID: {UserId}.", itemId, userId);

            return Ok(result);
        }

        #endregion

        #region Upload/Create something
        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="request">Folder creation request with name and optional parent folder ID.</param>
        /// <returns>Result with new folder details.</returns>
        /// <response code="200">Folder created successfully.</response>
        /// <response code="400">Invalid folder name or parent not found.</response>
        /// <response code="409">Folder with this name already exists.</response>
        /// <remarks>
        /// <para>Sample request:</para>
        /// <para>POST /user/123/mydrive/createfolder</para>    
        /// {
        ///     "name": "My Documents",
        ///     "parentId": 456
        /// }
        ///
        /// </remarks>
        [HttpPost("createfolder")]
        [ProducesResponseType(typeof(CreateFolderResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateFolderAsync([FromRoute] int userId, [FromBody] FolderCreateRequest request)
        {

            _logger.LogInformation("User {UserId} attempting to create folder '{FolderName}' in Parent ID: {ParentId}.", userId, request.Name, request.ParentId);

            var result = await _itemApplication.CreateFolderAsync(userId, request);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to create folder '{FolderName}' for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode}).", request.Name, userId, result.Message, result.ErrorCode);
                return result.ErrorCode switch
                {
                    ErrorCodes.PARENT_NOT_FOUND => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.NAME_CONFLICT => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
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

        /// <summary>
        /// Uploads a file to user's storage.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="file">The file to upload.</param>
        /// <param name="parentId">Optional parent folder ID (null for root).</param>
        /// <returns>Result with uploaded file details.</returns>
        /// <response code="200">File uploaded successfully.</response>
        /// <response code="400">Invalid file, parent not found, or storage limit exceeded.</response>
        /// <response code="409">File with this name already exists.</response>
        /// <remarks>
        /// Accepts multipart/form-data with file and optional parentId parameter.
        /// </remarks>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(UploadResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFileAsync([FromRoute] int userId, [Required] IFormFile file, [FromForm] int? parentId = null)
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
        #endregion
    }
}