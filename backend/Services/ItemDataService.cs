using CloudCore.Models;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace CloudCore.Services
{
    public class ItemDataService : IItemDataService
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;

        public ItemDataService(IDbContextFactory<CloudCoreDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<Item>> GetAllChildItemsAsync(int parentId, int userId, int maxDepth = 10000)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var userIdParam = new MySqlParameter("@UserId", userId);
            var parentIdParam = new MySqlParameter("@ParentId", parentId);
            var maxDepthParam = new MySqlParameter("@MaxDepth", maxDepth);

            var sql = @" WITH RECURSIVE ItemsHierarchy AS (SELECT id, name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted,1 as level
                FROM items
                WHERE user_id = @UserId AND parent_id = @ParentId AND is_deleted = FALSE
                UNION ALL

                SELECT i.id, i.name, i.type, i.parent_id, i.user_id, i.file_path, i.file_size, i.mime_type, i.is_deleted, ih.level + 1
                FROM items i
                INNER JOIN ItemsHierarchy ih ON i.parent_id = ih.id
                WHERE i.user_id = @UserId AND ih.type = 'folder' AND i.is_deleted = FALSE AND ih.level < @MaxDepth)

                SELECT id, name, type, parent_id, user_id, file_path, file_size, mime_type, is_deleted
                FROM ItemsHierarchy 
                ORDER BY Level, Type DESC, Name;";

            return await context.Items
                .FromSqlRaw(sql, userIdParam, parentIdParam, maxDepthParam)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsAsync(int userId, int? parentId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var userFiles = await context.Items
                .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == parentId)
                .OrderBy(item => item.Type)
                .ThenBy(item => item.Name)
                .ToListAsync();

            if (userFiles == null)
                return new List<Item>();

            return userFiles;
        }
    }
}
