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
        private readonly IItemRepository _itemRepository;

        public FileRenameService(IFileStorageService fileStorageService, IItemRepository itemRepository)
        {
            _fileStorageService = fileStorageService;
            _itemRepository = itemRepository;
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
                var childItems = await _itemRepository.GetAllChildItemsAsync(parent.Id, parent.UserId);

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

                var filesToUpdate = childItems.Where(i => i.Type == "file").ToList();

                if (filesToUpdate.Count > 0)
                    context.UpdateRange(filesToUpdate);

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

    }
}
