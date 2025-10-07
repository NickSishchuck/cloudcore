using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;
using static CloudCore.Contracts.Responses.ItemResultResponses;

namespace CloudCore.Services.Interfaces
{
    /// <summary>
    /// Application service orchestrating teamspace item operations.
    /// Handles business logic, validation, and coordination between services for teamspace file management.
    /// </summary>
    public interface ITeamspaceApplication
    {
        #region Item Retrieval

        /// <summary>
        /// Retrieves a paginated list of items within a teamspace
        /// </summary>
        /// <param name="userId">User requesting access (must have at least read permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="parentId">Parent folder ID (null for root)</param>
        /// <param name="page">Page number for pagination</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="sortBy">Field to sort by</param>
        /// <param name="sortDir">Sort direction (asc/desc)</param>
        /// <param name="searchQuery">Optional search query</param>
        /// <returns>Paginated response containing items and metadata</returns>
        Task<PaginatedResponse<Item>> GetTeamspaceItemsAsync(
            int userId,
            int teamspaceId,
            int? parentId,
            int page,
            int pageSize,
            string? sortBy,
            string? sortDir,
            string? searchQuery = null);

        /// <summary>
        /// Gets items in the teamspace trash
        /// </summary>
        Task<PaginatedResponse<Item>> GetTeamspaceTrashAsync(
            int userId,
            int teamspaceId,
            int page,
            int pageSize,
            string? sortBy,
            string? sortDir);

        /// <summary>
        /// Gets a specific item from a teamspace
        /// </summary>
        Task<Item?> GetTeamspaceItemAsync(
            int userId,
            int teamspaceId,
            int itemId,
            string type);

        /// <summary>
        /// Gets the breadcrumb path for a teamspace folder
        /// </summary>
        Task<string> GetTeamspaceBreadcrumbPathAsync(
            int userId,
            int teamspaceId,
            int folderId);

        #endregion

        #region File Operations

        /// <summary>
        /// Uploads a file to a teamspace
        /// </summary>
        /// <param name="userId">User uploading the file (must have write permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="file">File to upload</param>
        /// <param name="parentId">Optional parent folder ID</param>
        /// <returns>Upload result with file details</returns>
        Task<UploadResult> UploadFileToTeamspaceAsync(
            int userId,
            int teamspaceId,
            IFormFile file,
            int? parentId = null);

        /// <summary>
        /// Creates a folder in a teamspace
        /// </summary>
        /// <param name="userId">User creating the folder (must have write permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="request">Folder creation request</param>
        /// <returns>Folder creation result</returns>
        Task<CreateFolderResult> CreateFolderInTeamspaceAsync(
            int userId,
            int teamspaceId,
            FolderCreateRequest request);

        /// <summary>
        /// Renames an item in a teamspace
        /// </summary>
        /// <param name="userId">User renaming the item (must have write permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="itemId">ID of the item to rename</param>
        /// <param name="newName">New name for the item</param>
        /// <returns>Rename result</returns>
        Task<RenameResult> RenameTeamspaceItemAsync(
            int userId,
            int teamspaceId,
            int itemId,
            string newName);

        /// <summary>
        /// Soft deletes an item in a teamspace (moves to trash)
        /// </summary>
        /// <param name="userId">User deleting the item (must have write permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="itemId">ID of the item to delete</param>
        /// <returns>Delete result</returns>
        Task<DeleteResult> SoftDeleteTeamspaceItemAsync(
            int userId,
            int teamspaceId,
            int itemId);

        /// <summary>
        /// Restores an item from the teamspace trash
        /// </summary>
        /// <param name="userId">User restoring the item (must have write permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="itemId">ID of the item to restore</param>
        /// <returns>Restore result</returns>
        Task<RestoreResult> RestoreTeamspaceItemAsync(
            int userId,
            int teamspaceId,
            int itemId);

        #endregion

        #region Download Operations

        /// <summary>
        /// Downloads a file from a teamspace
        /// </summary>
        /// <param name="userId">User downloading the file (must have read permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="fileId">ID of the file to download</param>
        /// <returns>File download result with stream</returns>
        Task<FileDownloadResult> DownloadTeamspaceFileAsync(
            int userId,
            int teamspaceId,
            int fileId);

        /// <summary>
        /// Downloads a folder as a ZIP archive from a teamspace
        /// </summary>
        /// <param name="userId">User downloading the folder (must have read permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="folderId">ID of the folder to download</param>
        /// <returns>Archive stream and filename</returns>
        Task<(Stream archiveStream, string fileName)> DownloadTeamspaceFolderAsync(
            int userId,
            int teamspaceId,
            int folderId);

        /// <summary>
        /// Downloads multiple items as a ZIP archive from a teamspace
        /// </summary>
        /// <param name="userId">User downloading the items (must have read permission)</param>
        /// <param name="teamspaceId">ID of the teamspace</param>
        /// <param name="itemIds">List of item IDs to download</param>
        /// <returns>Archive stream and filename</returns>
        Task<(Stream archiveStream, string fileName)> DownloadMultipleTeamspaceItemsAsync(
            int userId,
            int teamspaceId,
            List<int> itemIds);

        #endregion

        #region Permission & Validation Helpers

        /// <summary>
        /// Verifies that a user has the required permission level for a teamspace
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <param name="teamspaceId">Teamspace ID</param>
        /// <param name="requiredPermission">Required permission level (read/write/admin)</param>
        /// <returns>True if user has permission, false otherwise</returns>
        Task<bool> VerifyTeamspacePermissionAsync(
            int userId,
            int teamspaceId,
            string requiredPermission);

        /// <summary>
        /// Verifies that an item belongs to the specified teamspace
        /// </summary>
        /// <param name="itemId">Item ID to verify</param>
        /// <param name="teamspaceId">Expected teamspace ID</param>
        /// <returns>True if item belongs to teamspace, false otherwise</returns>
        Task<bool> VerifyItemBelongsToTeamspaceAsync(
            int itemId,
            int teamspaceId);

        /// <summary>
        /// Checks if uploading a file would exceed the teamspace storage limit
        /// </summary>
        /// <param name="teamspaceId">Teamspace ID</param>
        /// <param name="fileSizeBytes">Size of the file to upload in bytes</param>
        /// <returns>True if within limit, false if would exceed</returns>
        Task<bool> CheckStorageLimitAsync(
            int teamspaceId,
            long fileSizeBytes);

        #endregion
    }
}
