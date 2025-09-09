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

        /// <summary>
        /// Builds the absolute file path for the specified user by combining the user’s storage path 
        /// with the provided relative path. Validates the path to prevent directory traversal outside 
        /// of the user’s storage.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="relativePath">The relative file path within the user’s storage.</param>
        /// <returns>The absolute file path.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if the resolved path points outside of the user’s storage directory.
        /// </exception>
        public string GetFileFullPath(int userId, string relativePath)
        {
            var fullPath =  Path.Combine(GetUserStoragePath(userId), relativePath);

            var resolvedPath = Path.GetFullPath(fullPath);

            if (!fullPath.StartsWith(resolvedPath))
                throw new UnauthorizedAccessException("Invalid file path");

            return fullPath;
        }
        /// <summary>
        /// Builds the path for the specified user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The user directory path.</returns>
        public string GetUserStoragePath(int userId)
        {
            return Path.Combine(_basePath, "users", $"user{userId}");
        }
    }
}
