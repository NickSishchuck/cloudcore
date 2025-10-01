using CloudCore.Common.Validation;
using CloudCore.Data.Context;

namespace CloudCore.Services.Interfaces
{
    public interface IValidationService
    {
        // <summary>
        /// Validates an uploaded file based on predefined rules.
        /// </summary>
        /// <param name="file">The IFormFile object representing the uploaded file.</param>
        /// <returns>A ValidationResult indicating success or failure with an error code.</returns>
        /// <remarks>Checks include null-checks, file size against MAX_SIZE, and allowed file extensions.</remarks>
        ValidationResult ValidateFile(IFormFile file);

        /// <summary>
        /// Validates the format and content of a file or folder name.
        /// </summary>
        /// <param name="fileName">The name of the item to validate.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        /// <remarks>Checks for max length, invalid characters, reserved names, and leading/trailing spaces or dots.</remarks>
        ValidationResult ValidateItemName(string fileName);

        /// <summary>
        /// Validates if a user is authorized to perform an action on a resource.
        /// </summary>
        /// <param name="currentUserId">The ID of the user making the request.</param>
        /// <param name="requestedUserId">The ID of the user who owns the resource.</param>
        /// <returns>A ValidationResult indicating if access is granted or denied.</returns>
        ValidationResult ValidateUserAuthorization(int currentUserId, int requestedUserId);

        /// <summary>
        /// Validates if an archive to be created meets size and file count constraints.
        /// </summary>
        /// <param name="totalSize">The total size in bytes of all files in the archive.</param>
        /// <param name="fileCount">The total number of files in the archive.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        ValidationResult ValidateArchiveSize(long totalSize, int fileCount);

        /// <summary>
        /// Formats a file size in bytes into a human-readable string representation.
        /// </summary>
        /// <param name="size">The file size in bytes</param>
        /// <returns>
        /// A formatted string representing the file size with appropriate units
        /// (Bytes, KB, MB, GB, or TB) rounded to two decimal places.
        /// </returns>
        /// <remarks>
        /// Uses binary units (1024-based) for conversion:
        /// - 1 KB = 1,024 bytes
        /// - 1 MB = 1,048,576 bytes
        /// - 1 GB = 1,073,741,824 bytes
        /// - 1 TB = 1,099,511,627,776 bytes
        /// Returns "0 Bytes" for zero-byte files.
        /// </remarks>
        /// <example>
        /// FormatFileSize(1024) returns "1 KB"
        /// FormatFileSize(1536) returns "1.5 KB"
        /// FormatFileSize(1048576) returns "1 MB"
        /// </example>
        string FormatFileSize(long size);




        /// <summary>
        /// Asynchronously checks if a specific item exists, is active, and belongs to the user.
        /// </summary>
        /// <param name="itemId">The ID of the item to check.</param>
        /// <param name="userId">The ID of the user who should own the item.</param>
        /// <param name="itemType">Optional. The type of the item to check for (e.g., "file" or "folder").</param>
        /// <returns>A Task representing the asynchronous operation, containing a ValidationResult.</returns>
        Task<ValidationResult> ValidateItemExistsAsync(int itemId, int userId, string itemType = null);

        /// <summary>
        /// Asynchronously validates a list of item IDs, ensuring all exist and belong to the user.
        /// </summary>
        /// <param name="itemIds">A list of item IDs to validate.</param>
        /// <param name="userId">The ID of the user who should own all the items.</param>
        /// <returns>A Task representing the asynchronous operation, containing a ValidationResult.</returns>
        Task<ValidationResult> ValidateItemIdsAsync(List<int> itemIds, int userId);

        /// <summary>
        /// Asynchronously checks if a given name is unique for a specific item type within a parent folder.
        /// </summary>
        /// <param name="name">The name to check for uniqueness.</param>
        /// <param name="itemType">The type of the item (e.g., "file" or "folder").</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="parentId">The ID of the parent folder where the item will reside. Null for the root directory.</param>
        /// <param name="excludeItemId">Optional. The ID of an item to exclude from the check, used during rename operations.</param>
        /// <returns>A Task representing the asynchronous operation, containing a ValidationResult.</returns>
        Task<ValidationResult> ValidateNameUniquenessAsync(string name, string itemType, int userId, int? parentId, int? excludeItemId = null, bool includeDeleted = false);


        ValidationResult ValidateQuery(string query);
    }
}
