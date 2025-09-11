using CloudCore.Services.Interfaces;

namespace CloudCore.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        public FileStorageService(IConfiguration configuration)
        {
            _basePath = configuration["FileStorage:BasePath"];
        }

        
        public string GetFileFullPath(int userId, string relativePath)
        {
            var fullPath =  Path.Combine(GetUserStoragePath(userId), relativePath);

            var resolvedPath = Path.GetFullPath(fullPath);

            if (!fullPath.StartsWith(resolvedPath))
                throw new UnauthorizedAccessException("Invalid file path");

            return fullPath;
        }
        
        public string GetUserStoragePath(int userId)
        {
            return Path.Combine(_basePath, "users", $"user{userId}");
        }
    }
}
