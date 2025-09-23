using CloudCore.Domain.Entities;

namespace CloudCore.Services.Interfaces
{
    public interface IItemStorageService
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

        /// <summary>
        /// Builds the complete folder path by traversing up the folder hierarchy from the given folder to the root.
        /// Combines folder names with the user's base storage path.
        /// </summary>
        /// <param name="folder">The folder item to build the path for</param>
        /// <returns>The complete file system path to the folder</returns>
        string GetFolderPathAsync(Item folder);

        /// <summary>
        /// Removes the last occurrence of a specified string from a folder path.
        /// </summary>
        /// <param name="path">The original path</param>
        /// <param name="searchString">The string to remove from the path</param>
        /// <returns>The modified path with the search string removed, or empty string if not found</returns>
        string RemoveFromFolderPath(string path, string searchString);

        // <summary>
        /// Creates a new file path by replacing the initial path segments with segments from a new folder path.
        /// Removes the user base path portion and maps corresponding segments from the new folder structure.
        /// </summary>
        /// <param name="filePath">The original file path</param>
        /// <param name="folderPath">The new folder path to map segments from</param>
        /// <param name="userBasePath">The user's base path to exclude from mapping</param>
        /// <returns>The modified file path with updated folder segments</returns>
        string GetNewFilePath(string filePath, string folderPath, string userBasePath);

        /// <summary>
        /// Creates a new folder path by replacing the last occurrence of a search string with a new name.
        /// </summary>
        /// <param name="path">The original folder path</param>
        /// <param name="searchString">The string to replace in the path</param>
        /// <param name="newName">The new name to use as replacement</param>
        /// <returns>The new folder path with the replaced name</returns>
        string GetNewFolderPath(string path, string searchString, string newName);


        /// <summary>
        /// Asynchronously saves an uploaded file to the user's storage directory with proper folder structure
        /// </summary>
        /// <param name="userId">The unique identifier of the user who owns the file</param>
        /// <param name="file">The uploaded file from the HTTP request to be saved to disk</param>
        /// <param name="parentId">The optional parent folder ID where the file should be stored. If null, saves to user's root directory</param>
        /// <returns>
        Task<string> SaveFileAsync(int userId, IFormFile file, int? parentId);

        /// <summary>
        /// Determines the MIME type of a file based on its file extension
        /// </summary>
        /// <param name="fileName">The name of the file including its extension (e.g., "document.pdf", "image.jpg")</param>
        /// <returns>
        /// The corresponding MIME type string for the file extension. 
        /// Returns "application/octet-stream" for unknown or unsupported file extensions
        /// </returns>
        string GetMimeType(string fileName);

    }
}
