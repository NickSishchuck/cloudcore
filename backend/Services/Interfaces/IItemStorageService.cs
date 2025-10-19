using CloudCore.Data.Context;
using CloudCore.Domain.Entities;

namespace CloudCore.Services.Interfaces
{
    /// <summary>
    /// Defines a service for direct interaction with the physical file storage system.
    /// This service abstracts away the details of file and directory manipulation on disk.
    /// </summary>
    public interface IItemStorageService
    {
        #region Path Management

        /// <summary>
        /// Builds the file path for the specified user by the user’s storage path.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if the resolved path points outside of the user’s storage directory.
        /// </exception>
        string GetUserStoragePath(int userId);

        /// <summary>
        /// Combines the user's root storage path with a relative path to create a full, absolute path.
        /// Includes validation to prevent path traversal attacks.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="relativePath">The relative path within the user's storage (e.g., "documents/report.pdf").</param>
        /// <returns>The secure, absolute file path on the disk.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the resulting path is outside the user's designated storage area.</exception>
        string GetFileFullPath(int userId, string relativePath);

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

        #endregion

        #region File and Folder Operations

        /// <summary>
        /// Asynchronously saves an uploaded file to the specified target directory within the user's storage.
        /// </summary>
        /// <param name="userId">The ID of the user uploading the file.</param>
        /// <param name="targetDirectory">The relative path of the directory to save the file in.</param>
        /// <param name="file">The uploaded file.</param>
        /// <returns>The relative path where the file was saved.</returns>
        Task<string> SaveFileAsync(int userId, string targetDirectory, IFormFile file);

        /// <summary>
        /// Attempts to create a new directory at the specified relative path within the user's storage.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="relativePath">The relative path of the folder to create.</param>
        /// <returns>True if the folder was created successfully; otherwise, false.</returns>
        bool TryCreateFolder(int userId, string relativePath);

        /// <summary>
        /// Physically deletes an item (file or directory) from the disk.
        /// If the item is a folder, it will be deleted recursively.
        /// </summary>
        /// <param name="item">The item entity to be deleted.</param>
        /// <param name="folderPath">Optional. The absolute path to the item if already known.</param>
        void DeleteItemPhysically(Item item, string? folderPath = null);

        string MoveItemPhysically(Item item, string destinationPath, string? folderPath = null);

        /// <summary>
        /// Physically renames a file or folder on the disk.
        /// </summary>
        /// <param name="item">The item to be renamed.</param>
        /// <param name="newName">The new name for the item.</param>
        /// <param name="folderPath">The current absolute path of the item. Required for renaming.</param>
        /// <returns>The new relative path of the renamed item.</returns>
        string? RenameItemPhysically(Item item, string newName, string? folderPath = null);


        #endregion

        #region Utility Methods

        /// <summary>
        /// Determines the MIME type of a file based on its file extension
        /// </summary>
        /// <param name="fileName">The name of the file including its extension (e.g., "document.pdf", "image.jpg")</param>
        /// <returns>
        /// The corresponding MIME type string for the file extension.
        /// Returns "application/octet-stream" for unknown or unsupported file extensions
        /// </returns>
        string GetMimeType(string fileName);
        #endregion
    }
}
