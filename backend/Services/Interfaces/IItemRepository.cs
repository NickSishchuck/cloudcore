using CloudCore.Models;

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
        Task<List<Item>> GetAllChildItemsAsync(int parentId, int userId, int maxDepth = 10000);
    }

}
