using CloudCore.Models;

namespace CloudCore.Services.Interfaces
{
    public interface IFileRenameService
    {
        /// <summary>
        /// Renames a user file in the file system and returns the new relative path
        /// </summary>
        /// <param name="item">The item object containing file information to be renamed</param>
        /// <param name="newName">The new file name without extension</param>
        /// <param name="newRelativePath">Output parameter containing the new relative file path</param>
        /// <exception cref="IOException">Thrown when a file with the same name already exists</exception>
        /// <remarks>
        /// This method preserves the original file extension and only changes the filename.
        /// The operation is performed atomically using File.Move().
        /// </remarks>
        void RenameFile(Item item, string newName, out string newRelativePath);

        /// <summary>
        /// Renames a folder and updates all file paths for child items recursively.
        /// Updates the database records and moves the physical folder on the file system.
        /// </summary>
        /// <param name="context">The database context for operations</param>
        /// <param name="parent">The folder item to rename</param>
        /// <param name="newName">The new name for the folder</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RenameFolder(CloudCoreDbContext context, Item parent, string newName);
    }
}
