using System.Threading.Tasks;
using CloudCore.Models;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudCore.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        public FileStorageService(IConfiguration configuration, IDbContextFactory<CloudCoreDbContext> dbContextFactory)
        {
            _basePath = configuration["FileStorage:BasePath"];
            _dbContextFactory = dbContextFactory;
        }


        public string GetFileFullPath(int userId, string relativePath)
        {
            // Get full path "/app/storage/users/user/1/documents/test.pdf"
            var fullPath = Path.Combine(GetUserStoragePath(userId), relativePath);

            var resolvedPath = Path.GetFullPath(fullPath);

            if (!resolvedPath.StartsWith(fullPath))
                throw new UnauthorizedAccessException("Invalid file path");

            return resolvedPath;
        }

        public string GetUserStoragePath(int userId)
        {
            // Get user`s root path "/app/storage/users/user/1"
            return Path.Combine(_basePath, "users", $"user{userId}");
        }

        public string GetFolderPath(Item folder)
        {

            var pathParts = new List<string>();
            var current = folder;

            using var context = _dbContextFactory.CreateDbContext();
            while (current != null)
            {
                pathParts.Add(current.Name);

                if (current.ParentId == null)
                    break;

                current = context.Items
                    .Where(i => i.Id == current.ParentId)
                    .FirstOrDefault();
            }
            pathParts.Reverse();
            return Path.Combine(GetUserStoragePath(folder.UserId), Path.Combine(pathParts.ToArray()));
        }


        public string RemoveFromFolderPath(string path, string searchString)
        {
            int lastIndexofSearchString = path.LastIndexOf(searchString);
            string newPath = null;

            if (lastIndexofSearchString != -1)
                newPath = path.Remove(lastIndexofSearchString, searchString.Length);

            return newPath;
        }


        public string GetNewFilePath(string filePath, string folderPath, string userBasePath)
        {
            string newFolderPathWithoutUserPart = folderPath.Remove(folderPath.IndexOf(userBasePath), userBasePath.Length);

            string[] pathParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string[] newFolderParts = newFolderPathWithoutUserPart.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                                                  .Where(part => !string.IsNullOrEmpty(part))
                                                                  .ToArray();

            for (int i = 0; i < newFolderParts.Length && i < pathParts.Length - 1; i++)
                pathParts[i] = newFolderParts[i];


            string result = Path.Combine(pathParts);
            return result;
        }

        public string GetNewFolderPath(string path, string searchString, string newName)
        {
            return Path.Combine(RemoveFromFolderPath(path, searchString), newName);
        }
    }
}
