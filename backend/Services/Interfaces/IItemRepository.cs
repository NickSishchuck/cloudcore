using System.Globalization;
using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;

namespace CloudCore.Services.Interfaces
{
    public interface IItemRepository
    {
        /// <summary>
        /// Retrieves all child items (files and folders) under a specified parent folder.
        /// </summary>
        /// <param name="parentId">The ID of the parent folder to search under</param>
        /// <param name="userId">The user ID to filter items by</param>
        /// <param name="maxDepth">The maximal depth to search by</param>
        /// <returns>A list of all child items found recursively under the parent folder</returns>
        Task<List<Item>> GetAllChildItemsAsync(int userId, int parentId, int maxDepth = 10000);

        /// <summary>
        /// Asynchronously retrieves a paginated IEnumerable of items for a specific user, with options for filtering and sorting.
        /// </summary>
        /// <param name="userId">The ID of the user whose items are being requested.</param>
        /// <param name="parentId">The ID of the parent folder to filter by. Use null to retrieve items from the root.</param>
        /// <param name="page">The page number for pagination (starting from 1).</param>
        /// <param name="pageSize">The number of items to include on each page.</param>
        /// <param name="sortBy">The field to sort the items by (e.g., "name", "createdAt"). Defaults to "name".</param>
        /// <param name="sortDir">The sort direction ("asc" for ascending, "desc" for descending). Defaults to "asc".</param>
        /// <param name="IsTrashFolder">A flag indicating whether to fetch items from the trash (where IsDeleted is true).</param>
        /// <returns>A Task that resolves to a PaginatedResponse containing the list of items and pagination metadata.</returns>
        Task<(IEnumerable<Item> Items, int TotalCount)> GetItemsAsync(int userId, int? parentId, int page, int pageSize, string? sortBy = "name", string? sortDir = "asc", bool IsTrashFolder = false, string? searchQuery = null);

        /// <summary>
        /// Asynchronously retrieves a single item by its ID, ensuring it belongs to the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the item.</param>
        /// <param name="itemtId">The ID of the item to retrieve.</param>
        /// <param name="itemtType">The Type of the item to retrieve.</param>
        /// <returns>A Task that resolves to the Item object if found; otherwise, null.</returns>
        Task<Item?> GetItemAsync(int userId, int itemtId, string? itemType);


        /// <summary>
        /// Retrieves a deleted item by its ID for the specified user.
        /// Returns null if not found or not marked as deleted.
        /// </summary>
        /// <param name="userId">The user who owns the item.</param>
        /// <param name="itemId">The ID of the item to retrieve.</param>
        /// <returns>The deleted <see cref="Item"/>, or null if not found.</returns>
        Task<Item?> GetDeletedItemAsync(int userId,int itemtId);

        /// <summary>
        /// Asynchronously retrieves a IEnumerable of items by its ID, ensuring it belongs to the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the item.</param>
        /// <param name="itemsIds">The IDs of the items to retrieve.</param>
        /// <returns>A Task that resolves to the Item object if found; otherwise, null.</returns>
        Task<IEnumerable<Item>> GetItemsByIdsForUserAsync(int userId, List<int> itemsIds);


        /// <summary>
        /// Asynchronously retrieves a IEnumerable of items by its ID.
        /// </summary>
        /// <param name="itemsIds">The IDs of the items to retrieve.</param>
        /// <returns>A Task that resolves to the Item object if found; otherwise, null.</returns>
        Task<IEnumerable<Item>> GetDeletedItemsByIdsAsync(List<int> itemsIds);

        /// <summary>
        /// Asynchronously constructs the full, relative path of a folder by traversing its parent hierarchy.
        /// </summary>
        /// <param name="folder">The folder item for which to build the path.</param>
        /// <returns>A Task that resolves to the relative folder path as a string (e.g., "ParentFolder/SubFolder").</returns>
        Task<string> GetFolderPathAsync(Item folder);

        /// <summary>
        /// Asynchronously checks if an active (not deleted) item exists for a user.
        /// </summary>
        /// <param name="itemId">The ID of the item to check.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="itemType">Optional. A specific type to filter by (e.g., "file" or "folder").</param>
        /// <returns>A Task that resolves to true if the item exists, is active, and matches the criteria; otherwise, false.</returns>
        Task<bool> ItemExistsAsync(int itemId, int userId, string? itemType = null);

        /// <summary>
        /// Asynchronously counts how many of the provided item IDs correspond to existing, active items owned by the user.
        /// </summary>
        /// <param name="itemIds">A list of item IDs to count.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A Task that resolves to an integer representing the count of valid items.</returns>
        Task<int> CountExistingItemsAsync(List<int> itemIds, int userId);

        /// <summary>
        /// Asynchronously checks if an active item with a specific name and type already exists within a given parent folder.
        /// </summary>
        /// <param name="name">The name to check for uniqueness.</param>
        /// <param name="itemType">The type of the item ("file" or "folder").</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="parentId">The ID of the parent folder. Null for the root.</param>
        /// <param name="excludeItemId">Optional. An item ID to exclude from the search, used during rename operations.</param>
        /// <param name="includeDeleted">If true, soft-deleted items are considered duplicates.</param>
        /// <returns>A Task that resolves to true if a duplicate item exists; otherwise, false.</returns>
        Task<bool> DoesItemExistByNameAsync(string name, string itemType, int userId, int? parentId, int? excludeItemId = null, bool includeDeleted = false);


        /// <summary>
        /// Recursively calculates the total size and file count of a folder and all its subfolders
        /// </summary>
        /// <param name="userId">User ID to filter items by ownership</param>
        /// <param name="folderId">Folder ID to calculate size for (null for root level)</param>
        /// <returns>Tuple containing total size in bytes and total file count</returns>
        /// <remarks>
        /// Creates a new database context for each call to ensure thread safety
        /// Recursively processes all subfolders to calculate complete hierarchy size
        /// Only counts files marked as "file" type and not soft-deleted
        /// Returns zero values if no items found in the specified folder
        /// </remarks>
        Task<(long totalSize, int fileCount)> CalculateArchiveSizeAsync(int userId, int? folderId);

        /// <summary>
        /// Atomically updates multiple items in the database, committing all changes in a single transaction.
        /// </summary>
        /// <param name="items">The collection of items to be updated.</param>
        Task UpdateItemsInTransactionAsync(List<Item> items);

        /// <summary>
        /// Adds a single item in the database in a dedicated transaction, rolling back if any error occurs.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        Task AddItemInTranscationAsync(Item item);

        /// <summary>
        /// Permanently deletes the given item from the database (hard delete), using a transaction.
        /// </summary>
        /// <param name="item">The item record to permanently remove.</param>
        Task DeleteItemPermanentlyAsync(Item item);

        /// <summary>
        /// Retrieves the IDs of all items in the trash that have passed the retention period.
        /// </summary>
        /// <param name="thresholdDate">The date before which items are considered expired.</param>
        /// <returns>A list of expired item IDs.</returns>
        Task<List<int>> GetExpiredItemIdsAsync(DateTime thresholdDate);

        /// <summary>
        /// Permanently deletes a list of items from the database by their IDs in a single transaction.
        /// </summary>
        /// <param name="itemIds">The list of item IDs to be deleted.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> DeleteItemsByIdsAsync(List<int> itemIds);
    }
}
