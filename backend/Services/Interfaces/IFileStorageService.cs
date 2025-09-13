namespace CloudCore.Services.Interfaces
{
    public interface IFileStorageService
    {
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
        string GetUserStoragePath(int userId);

        /// <summary>
        /// Builds the path for the specified user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The user directory path.</returns>
        string GetFileFullPath(int userId, string relativePath);
    }
}
