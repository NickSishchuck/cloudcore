using System.IO;
using System.Threading.Tasks;
using CloudCore.Models;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace CloudCore.Services
{
    public class FileRenameService : IFileRenameService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly IValidationService _validationService;
        public FileRenameService(IDbContextFactory<CloudCoreDbContext> dbContextFactory, IFileStorageService fileStorageService, IValidationService validationService)
        {
            _dbContextFactory = dbContextFactory;
            _fileStorageService = fileStorageService;
            _validationService = validationService;
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
