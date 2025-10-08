using CloudCore.Domain.Entities;
using static CloudCore.Contracts.Responses.ItemResultResponses;

namespace CloudCore.Services.Interfaces
{
    /// <summary>
    /// Defines a domain service responsible for orchestrating complex business logic involving Item entities.
    /// This service does not directly handle data persistence but prepares entities for operations like creation, renaming, and deletion.
    /// </summary>
    public interface IItemManagerService
    {
        /// <summary>
        /// Prepares an item and its children (if it's a folder) for a rename operation.
        /// This includes updating the name of the primary item and recalculating file paths for all descendants.
        /// </summary>
        /// <param name="item">The item (file or folder) to be renamed.</param>
        /// <param name="newName">The new name for the item.</param>
        /// <param name="childItems">Optional list of all descendant items if the primary item is a folder.</param>
        /// <param name="folderPath">The current absolute path of the folder on disk. Required only if renaming a folder.</param>
        /// <returns>A list of all item entities (primary and children) that have been modified and need to be updated in the database.</returns>
        List<Item> PrepareItemsForRenaming(Item item, string newName, List<Item>? childItems = null, string? folderPath = null);

        /// <summary>
        /// Prepares an item and its children (if it's a folder) for a soft-delete operation.
        /// This involves setting the IsDeleted flag to true and recording the deletion timestamp.
        /// </summary>
        /// <param name="item">The primary item to be soft-deleted.</param>
        /// <param name="childItems">Optional list of all descendant items if the primary item is a folder.</param>
        /// <returns>A list of all item entities that have been marked as deleted and need to be updated in the database.</returns>
        List<Item> PrepareItemsForSoftDelete(Item item, List<Item>? childItems = null);

        /// <summary>
        /// Prepares one or more items for restoration from a soft-deleted state.
        /// This involves setting the IsDeleted flag to false and clearing the deletion timestamp.
        /// </summary>
        /// <param name="items">A list of items to be restored.</param>
        void PrepareItemsForRestore(List<Item> items);

        /// <summary>
        /// Handles the business logic for permanently deleting an item, including any cleanup of associated resources.
        /// Note: The actual data persistence is handled by the data service layer.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the deletion.</param>
        /// <param name="id">The ID of the item to be permanently deleted.</param>
        /// <returns>A result object indicating the outcome of the deletion process.</returns>
        Task<DeleteResult> DeleteItemPermanentlyAsync(int userId, int id);

        /// <summary>
        /// Processes a file upload by saving the file to physical storage and creating a corresponding Item entity.
        /// </summary>
        /// <param name="userId">The ID of the user uploading the file.</param>
        /// <param name="parentId">Optional. The ID of the parent folder where the file will be located.</param>
        /// <param name="file">The uploaded file from the HTTP request.</param>
        /// <param name="taregetDirectory">The relative directory path where the file should be saved.</param>
        /// <returns>A new Item entity, populated with metadata from the uploaded file, ready to be saved to the database.</returns>
        Task<Item> ProcessUploadAsync(int userId, int? parentId, IFormFile file, string taregetDirectory);

        
    }
}

