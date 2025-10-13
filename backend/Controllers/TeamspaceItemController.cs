using CloudCore.Common.Errors;
using CloudCore.Common.QueryParameters;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Mappers;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CloudCore.Controllers
{
    /// <summary>
    /// Controller for managing items within teamspaces
    /// </summary>
    [ApiController]
    [Route("user/{userId}/teamspaces/{teamspaceId}/items")]
    [Authorize]
    [Produces("application/json")]
    [Tags("Teamspace Items")]
    public class TeamspaceItemController : ControllerBase
    {
        private readonly ITeamspaceApplication _teamspaceApplication;
        private readonly IValidationService _validationService;
        private readonly ILogger<TeamspaceItemController> _logger;

        public TeamspaceItemController(
            ITeamspaceApplication teamspaceApplication,
            IValidationService validationService,
            ILogger<TeamspaceItemController> logger)
        {
            _teamspaceApplication = teamspaceApplication;
            _validationService = validationService;
            _logger = logger;
        }


        private async Task<ActionResult?> VerifyTeamspacePermission(int userId, int teamspaceId, string requiredPermission)
        {
            var hasPermission = await _teamspaceApplication.VerifyTeamspacePermissionAsync(
                userId,
                teamspaceId,
                requiredPermission);

            if (!hasPermission)
            {
                _logger.LogWarning("User {UserId} lacks {Permission} permission for teamspace {TeamspaceId}",
                    userId, requiredPermission, teamspaceId);

                return StatusCode(403, ApiResponse.Error(
                    $"You need {requiredPermission} permission for this operation",
                    ErrorCodes.INSUFFICIENT_PERMISSION));
            }

            return null;
        }

        /// <summary>
        /// Gets all items in a teamspace folder
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<ItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTeamspaceItems(
            [Required] int userId,
            [Required] int teamspaceId,
            int? parentId,
            [FromQuery] QueryParameters queryParams)
        {

            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "read");
            if (permissionResult != null) return permissionResult;

            _logger.LogInformation("Fetching teamspace items. TeamspaceId={TeamspaceId}, ParentId={ParentId}",
                teamspaceId, parentId);

            var result = await _teamspaceApplication.GetTeamspaceItemsAsync(
                userId,
                teamspaceId,
                parentId,
                queryParams.Page,
                queryParams.PageSize,
                queryParams.SortBy,
                queryParams.SortDir,
                queryParams.SearchQuery);

            return Ok(new PaginatedResponse<ItemResponse>
            {
                Data = result.Data.Select(i => i.ToResponseDto()),
                Pagination = result.Pagination
            });
        }

        /// <summary>
        /// Gets items in the teamspace trash
        /// </summary>
        [HttpGet("trash")]
        [ProducesResponseType(typeof(PaginatedResponse<ItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTeamspaceTrash(
            [Required] int userId,
            [Required] int teamspaceId,
            [FromQuery] QueryParameters queryParams)
        {


            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "read");
            if (permissionResult != null) return permissionResult;

            var result = await _teamspaceApplication.GetTeamspaceTrashAsync(
                userId,
                teamspaceId,
                queryParams.Page,
                queryParams.PageSize,
                queryParams.SortBy,
                queryParams.SortDir);

            return Ok(new PaginatedResponse<ItemResponse>
            {
                Data = result.Data.Select(i => i.ToResponseDto()),
                Pagination = result.Pagination
            });
        }

        /// <summary>
        /// Uploads a file to a teamspace
        /// </summary>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UploadFile(
            [Required] int userId,
            [Required] int teamspaceId,
            IFormFile file,
            [FromForm] int? parentId = null)
        {

            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "write");
            if (permissionResult != null) return permissionResult;

            _logger.LogInformation("Uploading file to teamspace. TeamspaceId={TeamspaceId}, FileName={FileName}",
                teamspaceId, file.FileName);

            var result = await _teamspaceApplication.UploadFileToTeamspaceAsync(
                userId,
                teamspaceId,
                file,
                parentId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Upload failed. Error={ErrorCode}, Message={Message}",
                    result.ErrorCode, result.Message);

                return result.ErrorCode switch
                {
                    ErrorCodes.STORAGE_LIMIT_EXCEEDED => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.NAME_ALREADY_EXISTS => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            return Ok(new
            {
                message = result.Message,
                itemId = result.ItemId,
                fileName = result.FileName,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Creates a folder in a teamspace
        /// </summary>
        [HttpPost("createfolder")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateFolder(
            [Required] int userId,
            [Required] int teamspaceId,
            [FromBody] FolderCreateRequest request)
        {

            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "write");
            if (permissionResult != null) return permissionResult;

            _logger.LogInformation("Creating folder in teamspace. TeamspaceId={TeamspaceId}, FolderName={FolderName}",
                teamspaceId, request.Name);

            var result = await _teamspaceApplication.CreateFolderInTeamspaceAsync(
                userId,
                teamspaceId,
                request);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCodes.NAME_ALREADY_EXISTS => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            return Ok(new
            {
                message = result.Message,
                folderId = result.FolderId,
                folderName = result.FolderName,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Renames an item in a teamspace
        /// </summary>
        [HttpPut("{itemId}/rename")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RenameItem(
            [Required] int userId,
            [Required] int teamspaceId,
            [Required] int itemId,
            [FromBody] string newName)
        {

            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "write");
            if (permissionResult != null) return permissionResult;

            var result = await _teamspaceApplication.RenameTeamspaceItemAsync(
                userId,
                teamspaceId,
                itemId,
                newName);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCodes.ITEM_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.NAME_ALREADY_EXISTS => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            return Ok(new
            {
                message = result.Message,
                itemId = result.ItemId,
                newName = result.NewName,
                timestamp = result.Timestamp
            });
        }

        /// <summary>
        /// Deletes an item in a teamspace (moves to trash)
        /// </summary>
        [HttpDelete("{itemId}/delete")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteItem(
            [Required] int userId,
            [Required] int teamspaceId,
            [Required] int itemId)
        {

            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "write");
            if (permissionResult != null) return permissionResult;

            var result = await _teamspaceApplication.SoftDeleteTeamspaceItemAsync(
                userId,
                teamspaceId,
                itemId);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCodes.ITEM_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            return Ok(result);
        }

        /// <summary>
        /// Restores an item from the teamspace trash
        /// </summary>
        [HttpPut("{itemId}/restore")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RestoreItem(
            [Required] int userId,
            [Required] int teamspaceId,
            [Required] int itemId)
        {


            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "write");
            if (permissionResult != null) return permissionResult;

            var result = await _teamspaceApplication.RestoreTeamspaceItemAsync(
                userId,
                teamspaceId,
                itemId);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCodes.ITEM_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            return Ok(result);
        }

        /// <summary>
        /// Downloads a file from a teamspace
        /// </summary>
        [HttpGet("{fileId}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadFile(
            [Required] int userId,
            [Required] int teamspaceId,
            [Required] int fileId)
        {


            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "read");
            if (permissionResult != null) return permissionResult;

            var fileResult = await _teamspaceApplication.DownloadTeamspaceFileAsync(
                userId,
                teamspaceId,
                fileId);

            return File(fileResult.Stream, fileResult.MimeType, fileResult.FileName, enableRangeProcessing: true);
        }

        /// <summary>
        /// Downloads a folder as a ZIP archive from a teamspace
        /// </summary>
        [HttpGet("{folderId}/downloadfolder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadFolder(
            [Required] int userId,
            [Required] int teamspaceId,
            [Required] int folderId)
        {


            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "read");
            if (permissionResult != null) return permissionResult;

            var (archiveStream, fileName) = await _teamspaceApplication.DownloadTeamspaceFolderAsync(
                userId,
                teamspaceId,
                folderId);

            return File(archiveStream, "application/zip", fileName);
        }

        /// <summary>
        /// Downloads multiple items as a ZIP archive from a teamspace
        /// </summary>
        [HttpPost("download/multiple")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DownloadMultipleItems(
            [Required] int userId,
            [Required] int teamspaceId,
            [FromBody] List<int> itemIds)
        {


            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "read");
            if (permissionResult != null) return permissionResult;

            var (archiveStream, fileName) = await _teamspaceApplication.DownloadMultipleTeamspaceItemsAsync(
                userId,
                teamspaceId,
                itemIds);

            return File(archiveStream, "application/zip", fileName);
        }

        /// <summary>
        /// Gets the breadcrumb path for a teamspace folder
        /// </summary>
        [HttpGet("folder/path/{folderId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFolderPath(
            [Required] int userId,
            [Required] int teamspaceId,
            [Required] int folderId)
        {


            var permissionResult = await VerifyTeamspacePermission(userId, teamspaceId, "read");
            if (permissionResult != null) return permissionResult;

            var path = await _teamspaceApplication.GetTeamspaceBreadcrumbPathAsync(
                userId,
                teamspaceId,
                folderId);

            return Ok(path);
        }
    }
}
