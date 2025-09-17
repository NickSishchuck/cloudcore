using System.IO;
using System.Threading.Tasks;
using CloudCore.Models;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using MySqlConnector;

namespace CloudCore.Services
{
    public class FileRenameService : IFileRenameService
    {
        private readonly IFileStorageService _fileStorageService;

        public FileRenameService(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }
        public void RenameFile(Item item, string newName, out string newRelativePath)
        {
            // Get old file path "/app/storage/users/user/1/documents/test.pdf"
            var oldFilePath = _fileStorageService.GetFileFullPath(item.UserId, item.FilePath);

            // Get directory "/documents"
            var directory = Path.GetDirectoryName(oldFilePath);

            var extension = Path.GetExtension(oldFilePath);

            // Make new file path "/app/storage/users/user/1/documents/newFileName" 
            var newFilePath = Path.Combine(directory, newName);

            if (File.Exists(newFilePath))
                throw new IOException("File with this name already exists");

            // Rename File
            File.Move(oldFilePath, newFilePath);

            // Make new path, take old direvtory path from db.
            newRelativePath = Path.Combine(Path.GetDirectoryName(item.FilePath), newName);

            item.FilePath = newRelativePath;
            item.Name = newName;
        }

        public async Task RenameFolder(CloudCoreDbContext context, Item parent, string newName)
        {
            using var transaction = context.Database.BeginTransaction();
            try
            {
                // Get all child items
                var childItems = await GetAllChildItemsRecursivelyAsync(context, parent.Id, parent.UserId);

                // Get old path
                var oldFolderPath = _fileStorageService.GetFolderPath(parent);

                // Get new path
                var newFolderPath = _fileStorageService.GetNewFolderPath(oldFolderPath, parent.Name, newName);

                // Get user`s root folder
                string basePath = _fileStorageService.GetUserStoragePath(parent.UserId);
                foreach (var item in childItems)
                {
                    if (item.Type == "file")
                    {
                        item.FilePath = _fileStorageService.GetNewFilePath(item.FilePath, newFolderPath, basePath);
                    }
                }

                parent.Name = newName;
                context.Entry(parent).State = EntityState.Modified;

                await Task.Run(() => Directory.Move(oldFolderPath, newFolderPath));
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }


           
        }

        /// <summary>
        /// Recursively retrieves all child items (files and folders) under a specified parent folder.
        /// Traverses the folder hierarchy depth-first to collect all nested items.
        /// </summary>
        /// <param name="context">The database context for querying items</param>
        /// <param name="parentId">The ID of the parent folder to search under</param>
        /// <param name="userId">The user ID to filter items by</param>
        /// <returns>A list of all child items found recursively under the parent folder</returns>
        private static async Task<List<Item>> GetAllChildItemsRecursivelyAsync(CloudCoreDbContext context, int parentId, int userId, int maxDepth = 10000)
        {
            var userIdParam = new MySqlParameter("@UserId", userId);
            var parentIdParam = new MySqlParameter("@ParentId", parentId);
            var maxDepthParam = new MySqlParameter("@MaxDepth", maxDepth);

            var sql = @" WITH RECURSIVE ItemsHierarchy AS (SELECT 
                                                            id, 
                                                            name, 
                                                            type, 
                                                            parent_id, 
                                                            user_id, 
                                                            file_path, 
                                                            file_size, 
                                                            mime_type,
                                                            is_deleted,
                                                            1 as level
                                                        FROM items
                                                        WHERE user_id = @UserId
                                                            AND parent_id = @ParentId
                                                            AND is_deleted = FALSE
    
                                                        UNION ALL
    
                                                        SELECT 
                                                            i.id, 
                                                            i.name, 
                                                            i.type, 
                                                            i.parent_id, 
                                                            i.user_id, 
                                                            i.file_path, 
                                                            i.file_size, 
                                                            i.mime_type,
                                                            i.is_deleted,
                                                            ih.level + 1
                                                        FROM items i
                                                        INNER JOIN ItemsHierarchy ih ON i.parent_id = ih.id
                                                        WHERE i.user_id = @UserId
                                                            AND ih.type = 'folder'
                                                            AND i.is_deleted = FALSE
                                                            AND ih.level < @MaxDepth 
                                                    )
                                                    SELECT 
                                                            id, 
                                                            name, 
                                                            type, 
                                                            parent_id, 
                                                            user_id, 
                                                            file_path, 
                                                            file_size, 
                                                            mime_type,
                                                            is_deleted
                                                        FROM ItemsHierarchy 
                                                        ORDER BY Level, Type DESC, Name;";

            return await context.Items
                .FromSqlRaw(sql, userIdParam, parentIdParam, maxDepthParam)
                .ToListAsync();
        }

    }
}
