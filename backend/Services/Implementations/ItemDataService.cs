using CloudCore.Contracts.Responses;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace CloudCore.Services.Implementations
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

        public async Task<PaginatedResponse<Item>> GetItemsAsync(int userId, int? parentId, int page, int pageSize, string? sortBy, string? sortDir)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var query = context.Items
                .Where(i => i.UserId == userId && i.ParentId == parentId && i.IsDeleted == false)
                .OrderBy(i => i.Type == "file" ? 1 : 0);


            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch ((sortBy ?? "name").ToLowerInvariant())
            {
                case "size":
                case "filesize":
                    query = desc
                        ? query.ThenByDescending(i => i.FileSize ?? 0)
                        : query.ThenBy(i => i.FileSize ?? 0);
                    break;

                case "modified":
                case "updatedat":
                    
                    //query = desc
                    //    ? query.ThenByDescending(i => i.UpdatedAt)
                    //    : query.ThenBy(i => i.UpdatedAt);
                    break;

                case "name":
                default:
                    query = desc
                        ? query.ThenByDescending(i => i.Name)
                        : query.ThenBy(i => i.Name);
                    break;
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResponse<Item>
            {
                Data = items,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    HasNext = page < totalPages,
                    HasPrevious = page > 1
                }
            };
        }
    }
}
