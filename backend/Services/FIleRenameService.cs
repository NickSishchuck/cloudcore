using System.IO;
using System.Threading.Tasks;
using CloudCore.Models;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace CloudCore.Services
{
    public class FIleRenameService : IFileRenameService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        public FIleRenameService(IDbContextFactory<CloudCoreDbContext> dbContextFactory, IFileStorageService fileStorageService)
        {
            _dbContextFactory = dbContextFactory;
            _fileStorageService = fileStorageService;
        }
        public void RenameFile(Item item, string newName, out string newRelativePath)
        {

            var oldFilePath = _fileStorageService.GetFileFullPath(item.UserId, item.FilePath);

            var directory = Path.GetDirectoryName(oldFilePath);
            var extension = Path.GetExtension(oldFilePath);

            var newFilePath = Path.Combine(directory, newName);

            if (File.Exists(newFilePath))
                throw new IOException("File with this name already exists");

            File.Move(oldFilePath, newFilePath);

            newRelativePath = Path.Combine(Path.GetDirectoryName(item.FilePath), newName);

            item.FilePath = newRelativePath;
            item.Name = newName;
        }

        public async Task RenameFolder(CloudCoreDbContext context, Item parent, string newName)
        {
              
            var childItems = await GetAllChildItemsRecursivelyAsync(context, parent.Id, parent.UserId);

            var oldFolderPath = _fileStorageService.GetFolderPath(parent);
            var newFolderPath = _fileStorageService.GetNewFolderPath(oldFolderPath, parent.Name, newName);

            string basePath = _fileStorageService.GetUserStoragePath(parent.UserId);
            foreach (var item in childItems)
            {
                if (item.Type == "file")
                item.FilePath = _fileStorageService.GetNewFilePath(item.FilePath, newFolderPath, basePath);
            }

            parent.Name = newName;
            context.Entry(parent).State = EntityState.Modified;

            Directory.Move(oldFolderPath, newFolderPath);
            await context.SaveChangesAsync();

               
           
        }

        /// <summary>
        /// Recursively retrieves all child items (files and folders) under a specified parent folder.
        /// Traverses the folder hierarchy depth-first to collect all nested items.
        /// </summary>
        /// <param name="context">The database context for querying items</param>
        /// <param name="parentId">The ID of the parent folder to search under</param>
        /// <param name="userId">The user ID to filter items by</param>
        /// <returns>A list of all child items found recursively under the parent folder</returns>
        private static async Task<List<Item>> GetAllChildItemsRecursivelyAsync(CloudCoreDbContext context, int parentId, int userId)
        {
            var result = new List<Item>();
            var directChildren = await context.Items
                .Where(i => i.UserId == userId && i.ParentId == parentId)
                .ToListAsync();


            foreach (var child in directChildren)
            {
                result.Add(child);

                if (child.Type == "folder")
                {
                    var childItems = await GetAllChildItemsRecursivelyAsync(context, child.Id, userId);
                    result.AddRange(childItems);
                }
            }

            return result;
        }

    }
}
