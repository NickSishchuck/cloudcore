using System.ComponentModel.DataAnnotations;
using CloudCore.Common.Errors;
using CloudCore.Common.QueryParameters;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using static CloudCore.Contracts.Responses.ItemResultResponses;

namespace CloudCore.Services.Interfaces
{
    /// <summary>
    /// Defines the application service for managing user items (files and folders).
    /// This interface orchestrates validation, business logic, and data persistence
    /// to fulfill high-level use cases initiated by the user.
    /// </summary>
    public interface IItemApplication
    {

        #region Get something
        /// <summary>
        /// Retrieves a paginated list of items for a user within a specific parent folder or context.
        /// </summary>
        /// <param name="userId">The ID of the user requesting items.</param>
        /// <param name="parentId">The ID of the parent directory (null for root-level).</param>
        /// <param name="page">The current page number for pagination.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="sortBy">Field name to sort by (e.g., "name").</param>
        /// <param name="sortDir">Sort direction ("asc" or "desc").</param>
        /// <param name="isTrashFolder">Whether to return deleted items from the trash.</param>
        /// <param name="searchQuery">Optional search query for item filtering.</param>
        /// <param name="teamspaceId">Optional teamspace context.</param>
        /// <returns>PaginatedResponse containing the items and pagination metadata.</returns>
        Task<PaginatedResponse<Item>> GetItemsAsync(int userId, int? parentId, int page, int pageSize, string? sortBy, string? sortDir, bool isTrashFolder = false, string? searchQuery = null, int? teamspaceId = null);

        /// <summary>
        /// Retrieves a single item by its ID, user, and type.
        /// </summary>
        /// <param name="userId">The ID of the user owning the item.</param>
        /// <param name="itemId">The ID of the item to retrieve.</param>
        /// <param name="type">The item type (e.g. "file", "folder").</param>
        /// <param name="teamspaceId">Optional teamspace context.</param>
        /// <returns>The item if found, otherwise null.</returns>
        Task<Item?> GetItemAsync(int userId, int itemId, string type, int? teamspaceId = null);

        /// <summary>
        /// Retrieves all immediate child items (folders or files) for a user and parent.
        /// </summary>
        /// <param name="userId">The ID of the owner user.</param>
        /// <param name="parentId">ID of the parent folder.</param>
        /// <param name="itemType">Filter by item type ("folder", "file", etc.).</param>
        /// <param name="includeDeleted">Include items marked as deleted.</param>
        /// <returns>Async stream of matching items.</returns>
        IAsyncEnumerable<Item?> GetDirectChildrenAsync(int userId, int? parentId, string? itemType = null, bool includeDeleted = false);

        /// <summary>
        /// Retrieves a single item based on its name and parent folder.
        /// </summary>
        /// <param name="userId">The ID of the owner user.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="parentId">Parent folder ID (if any).</param>
        /// <param name="teamspaceId">Optional teamspace context.</param>
        /// <returns>The item if found, otherwise null.</returns>
        Task<Item?> GetItemByNameAsync(int userId, string name, int? parentId, int? teamspaceId = null);

        /// <summary>
        /// Gets the full breadcrumb path for a folder by user and folder ID.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="folderId">Folder ID.</param>
        /// <param name="type">Item type (should be "folder").</param>
        /// <returns>Breadcrumb-style string path for the folder.</returns>
        Task<string> GetBreadcrumbPathAsync(int userId, int folderId, string type);

        #endregion

        #region Download something

        /// <summary>
        /// Creates and downloads a zipped archive of a folder and its content.
        /// </summary>
        /// <param name="userId">ID of the user requesting the download.</param>
        /// <param name="folderId">ID of the folder.</param>
        /// <returns>Tuple containing the ZIP stream and suggested file name.</returns>
        Task<(Stream archiveStream, string fileName)> DownloadFolderAsync(int userId, int folderId);

        /// <summary>
        /// Downloads a physical file as a stream by user and file ID.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="fileId">File ID.</param>
        /// <returns>FileDownloadResult containing the stream and file metadata.</returns>
        Task<FileDownloadResult> DownloadFileAsync(int userId, int fileId);

        /// <summary>
        /// Downloads a ZIP archive containing multiple selected items (files and folders).
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="itemsIds">IDs of the items to include in the archive.</param>
        /// <returns>Tuple with archive stream and file name.</returns>
        Task<(Stream archiveStream, string fileName)> DownloadMultipleItemsAsZipAsync(int userId, List<int> itemsIds);

        #endregion

        #region Modify something
        /// <summary>
        /// Orchestrates the renaming of an existing item.
        /// </summary>
        /// <param name="userId">The ID of the user performing the rename.</param>
        /// <param name="itemId">The ID of the item to rename.</param>
        /// <param name="newName">The desired new name for the item.</param>
        /// <returns>A <see cref="RenameResult"/> indicating the outcome of the operation.</returns>
        Task<RenameResult> RenameItemAsync(int userId, int itemId, string newName);

        /// <summary>
        /// Moves a file or folder to a new parent folder.
        /// Validates permissions and performs all necessary updates.
        /// </summary>
        /// <param name="userId">The ID of the user performing the move.</param>
        /// <param name="itemId">ID of the item being moved.</param>
        /// <param name="targetId">ID of the target destination folder.</param>
        /// <returns>A result indicating success or failure with user-friendly messages.</returns>
        Task<MoveResult> MoveItemAsync(int userId, int itemId, int? targetId);


        /// <summary>
        /// Restores a previously deleted item (file or folder) for a user.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <param name="itemId">ID of the item.</param>
        /// <returns>RestoreResult with operation outcome.</returns>
        Task<RestoreResult> RestoreItemAsync(int userId, int itemId);

        #endregion

        #region Delete something

        /// <summary>
        /// Moves an item to the trash (soft delete). The item can be restored later.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <param name="itemId">ID of the item.</param>
        /// <returns>DeleteResult with deletion status.</returns>
        Task<DeleteResult> SoftDeleteItemAsync(int userId, int itemId);

        /// <summary>
        /// Permanently deletes an item (file or folder) from both the database and physical storage.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the item.</param>
        /// <param name="itemId">The ID of the item to be permanently deleted.</param>
        /// <returns>
        /// A <see cref="DeleteResult"/> indicating the success or failure of the operation.
        Task<DeleteResult> DeleteItemPermanentlyAsync(int userId, int itemId);


        #endregion

        #region Upload/Create something
        /// <summary>
        /// Orchestrates the entire process of uploading a new file.
        /// </summary>
        /// <param name="userId">The ID of the user uploading the file.</param>
        /// <param name="file">The <see cref="IFormFile"/> object from the request.</param>
        /// <param name="parentId">Optional. The ID of the parent folder to upload into. If null, uploads to the root.</param>
        /// <param name="teamspaceId">Optional teamspace id.</param>
        /// <returns>An <see cref="UploadResult"/> indicating the outcome and details of the newly created file.</returns>
        Task<UploadResult> UploadFileAsync(int userId, IFormFile file, int? parentId = null, int? teamspaceId = null);

        /// <summary>
        /// Orchestrates the creation of a new, empty folder.
        /// </summary>
        /// <param name="userId">The ID of the user creating the folder.</param>
        /// <param name="request">A <see cref="FolderCreateRequest"/> DTO containing the folder's name and parent ID.</param>
        /// <param name="teamspaceId">Optional teamspace id.</param>
        /// <returns>A <see cref="CreateFolderResult"/> indicating the outcome and details of the new folder.</returns>
        Task<CreateFolderResult> CreateFolderAsync(int userId, FolderCreateRequest request, int? teamspaceId = null);


        #endregion
    }

}