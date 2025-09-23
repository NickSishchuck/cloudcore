using System.IO;
using System.Threading.Tasks;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using MySqlConnector;

namespace CloudCore.Services.Implementations
{
    public class ItemRenameService : IItemRenameService
    {
        private readonly IItemStorageService _fileStorageService;
        private readonly IItemDataService _itemDataService;

        public ItemRenameService(IItemStorageService fileStorageService, IItemDataService itemDataService)
        {
            _fileStorageService = fileStorageService;
            _itemDataService = itemDataService;
        }

        public Task RenameFileAsync(Item item, string newName)
        {
            return Task.Run(() =>
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

                File.Move(oldFilePath, newFilePath);

                var newRelativePath = Path.Combine(Path.GetDirectoryName(item.FilePath), newName);

                item.FilePath = newRelativePath;
                item.Name = newName;
            });
        }



        public async Task RenameFolderAsync(CloudCoreDbContext context, Item parent, string newName)
        {

            // Get all child items
            var childItems = await _itemDataService.GetAllChildItemsAsync(parent.Id, parent.UserId);

            // Get old path
            var oldFolderPath = await _fileStorageService.GetFolderPathAsync(parent);

            // Get new path
            var newFolderPath = _fileStorageService.GetNewFolderPath(oldFolderPath, parent.Name, newName);

            // Get user`s root folder
            string basePath = _fileStorageService.GetUserStoragePath(parent.UserId);
            foreach (var item in childItems)
            {
                if (item.Type == "file")
                    item.FilePath = _fileStorageService.GetNewFilePath(item.FilePath, newFolderPath, basePath);
            }

            var filesToUpdate = childItems.Where(i => i.Type == "file").ToList();

            if (filesToUpdate.Count > 0)
                context.UpdateRange(filesToUpdate);

            parent.Name = newName;
            context.Entry(parent).State = EntityState.Modified;

            Directory.Move(oldFolderPath, newFolderPath);
            await context.SaveChangesAsync();

        }

    }
}
