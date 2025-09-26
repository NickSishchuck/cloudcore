using System.ComponentModel.DataAnnotations;
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
        /// Retrieves a paginated list of items for a specific user and parent directory.
        /// Supports sorting and filtering for standard folders and trash folders.
        /// </summary>
        /// <param name="userId">User identifier whose items are to be retrieved.</param>
        /// <param name="parentId">Parent directory ID (null for root level).</param>
        /// <param name="page">Page number (1-based index for pagination).</param>
        /// <param name="pageSize">Number of items per page (pagination).</param>
        /// <param name="sortBy">Field to sort the items by (e.g., "name", "size", "modified").</param>
        /// <param name="sortDir">Sort direction: "asc" for ascending, "desc" for descending.</param>
        /// <param name="isTrashFolder">If true, only fetches items from the trash folder; otherwise, gets normal items.</param>
        /// <returns>
        /// A paginated response containing the list of items and pagination metadata.
        /// </returns>
        Task<PaginatedResponse<Item>> GetItemsAsync(int userId, int? parentId, int page, int pageSize, string? sortBy, string? sortDir, bool isTrashFolder = false);

        #endregion

        #region Download

        /// <summary>
        /// Orchestrates the download of a folder and all its contents as a single ZIP archive.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the download.</param>
        /// <param name="folderId">The ID of the folder to be archived and downloaded.</param>
        /// <returns>A tuple containing the archive's data stream and a suggested file name (e.g., "MyFolder.zip").</returns>
        Task<(Stream archiveStream, string fileName)> DownloadFolderAsync(int userId, int folderId);

        /// <summary>
        /// Orchestrates the download of a single file.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the download.</param>
        /// <param name="fileId">The ID of the file to download.</param>
        /// <returns>A <see cref="FileDownloadResult"/> containing the file stream and relevant metadata for the HTTP response.</returns>
        Task<FileDownloadResult> DownloadFileAsync(int userId, int fileId);

        /// <summary>
        /// Orchestrates the download of multiple, specified items (files and/or folders) as a single ZIP archive.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the download.</param>
        /// <param name="itemsId">A list of IDs for the items to be included in the archive.</param>
        /// <returns>A tuple containing the archive's data stream and a suggested file name.</returns>
        Task<(Stream archiveStream, string fileName)> DownloadMultipleItemsAsZipAsync( int userId,List<int> itemsId);

        #endregion

        #region Another
        /// <summary>
        /// Orchestrates the renaming of an existing item.
        /// </summary>
        /// <param name="userId">The ID of the user performing the rename.</param>
        /// <param name="itemId">The ID of the item to rename.</param>
        /// <param name="newName">The desired new name for the item.</param>
        /// <returns>A <see cref="RenameResult"/> indicating the outcome of the operation.</returns>
        Task<RenameResult> RenameItemAsync(int userId, int itemId, string newName);

        /// <summary>
        /// Orchestrates the soft-deletion of an item, moving it to the trash.
        /// If the item is a folder, all its contents will also be soft-deleted.
        /// </summary>
        /// <param name="userId">The ID of the user performing the deletion.</param>
        /// <param name="itemId">The ID of the item to move to trash.</param>
        /// <returns>A <see cref="DeleteResult"/> indicating the outcome of the operation.</returns>
        Task<DeleteResult> SoftDeleteItemAsync(int userId, int itemId);

        /// <summary>
        /// Orchestrates the entire process of uploading a new file.
        /// </summary>
        /// <param name="userId">The ID of the user uploading the file.</param>
        /// <param name="file">The <see cref="IFormFile"/> object from the request.</param>
        /// <param name="parentId">Optional. The ID of the parent folder to upload into. If null, uploads to the root.</param>
        /// <returns>An <see cref="UploadResult"/> indicating the outcome and details of the newly created file.</returns>
        Task<UploadResult> UploadFileAsync(int userId, IFormFile file, int? parentId = null);

        /// <summary>
        /// Orchestrates the creation of a new, empty folder.
        /// </summary>
        /// <param name="userId">The ID of the user creating the folder.</param>
        /// <param name="request">A <see cref="FolderCreateRequest"/> DTO containing the folder's name and parent ID.</param>
        /// <returns>A <see cref="CreateFolderResult"/> indicating the outcome and details of the new folder.</returns>
        Task<CreateFolderResult> CreateFolderAsync(int userId, FolderCreateRequest request);

        /// <summary>
        /// Orchestrates the restoration of a previously soft-deleted item from the trash.
        /// If the item is a folder, all its contents will also be restored.
        /// </summary>
        /// <param name="userId">The ID of the user performing the restoration.</param>
        /// <param name="itemId">The ID of the item to restore.</param>
        /// <returns>A <see cref="RestoreResult"/> indicating the outcome of the operation.</returns>
        Task<RestoreResult> RestoreItemAsync(int userId, int itemId);

        #endregion
    }

}
