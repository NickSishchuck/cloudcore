using CloudCore.Common.Validation;
using CloudCore.Data.Context;

namespace CloudCore.Services.Interfaces
{
    public interface IValidationService
    {

        ValidationResult ValidateFile(IFormFile file);

        /// <summary>
        /// Validates the format and content of a file or folder name.
        /// </summary>
        /// <param name="fileName">The name to validate (file or folder name)</param>
        /// <returns>
        /// A ValidationResult indicating whether the name is valid.
        /// Returns success if valid, or failure with specific error details if invalid.
        /// </returns>
        /// <remarks>
        /// Validates against:
        /// - Null, empty, or whitespace-only names
        /// - Names exceeding maximum length (255 characters)
        /// - Invalid characters (&lt;, &gt;, :, ", |, ?, *, \0, ,)
        /// - Reserved Windows names (CON, PRN, AUX, NUL, COM1-9, LPT1-9)
        /// - Names starting or ending with dots or spaces
        /// </remarks>
        ValidationResult ValidateItemName(string fileName);

        /// <summary>
        /// Validates that the current user has authorization to access resources for the requested user.
        /// </summary>
        /// <param name="currentUserId">The ID of the currently authenticated user from the JWT token</param>
        /// <param name="requestedUserId">The ID of the user whose resources are being accessed</param>
        /// <returns>
        /// A ValidationResult indicating whether access is authorized.
        /// Returns success if the user IDs match, or failure with ACCESS_DENIED error if they don't.
        /// </returns>
        /// <remarks>
        /// Implements the principle that users can only access their own resources.
        /// This prevents unauthorized access to other users' files and folders.
        /// </remarks>
        ValidationResult ValidateUserAuthorization(int currentUserId, int requestedUserId);

        /// <summary>
        /// Validates that a specific item exists in the database and belongs to the specified user.
        /// </summary>
        /// <param name="context">The database context for querying items</param>
        /// <param name="itemId">The ID of the item to validate</param>
        /// <param name="userId">The ID of the user who should own the item</param>
        /// <param name="itemType">Optional filter for item type ("file" or "folder"). If null, any type is accepted.</param>
        /// <returns>
        /// A task that resolves to a ValidationResult indicating whether the item exists and is accessible.
        /// Returns success if found, or failure with ITEM_NOT_FOUND error if not found or inaccessible.
        /// </returns>
        /// <remarks>
        /// Checks that the item:
        /// - Exists in the database
        /// - Belongs to the specified user
        /// - Is not soft-deleted (IsDeleted = false)
        /// - Matches the specified type if itemType parameter is provided
        /// </remarks>
        Task<ValidationResult> ValidateItemExistsAsync(CloudCoreDbContext context, int itemId, int userId, string itemType = null);


        /// <summary>
        /// Validates a list of item IDs to ensure they all exist and belong to the specified user.
        /// </summary>
        /// <param name="context">The database context for querying items</param>
        /// <param name="itemIds">List of item IDs to validate</param>
        /// <param name="userId">The ID of the user who should own all items</param>
        /// <returns>
        /// A task that resolves to a ValidationResult indicating whether all items are valid and accessible.
        /// Returns success if all items are found and belong to the user, or failure with specific error details.
        /// </returns>
        /// <remarks>
        /// Validates that:
        /// - The list is not null or empty
        /// - The list doesn't exceed the maximum allowed count (100 items)
        /// - All specified items exist in the database
        /// - All items belong to the specified user
        /// - All items are not soft-deleted
        /// </remarks>
        Task<ValidationResult> ValidateItemIdsAsync(CloudCoreDbContext context, List<int> itemIds, int userId);


        /// <summary>
        /// Validates that a name is unique within a specific directory for a user.
        /// </summary>
        /// <param name="context">The database context for querying items</param>
        /// <param name="name">The name to check for uniqueness</param>
        /// <param name="userId">The ID of the user who owns the directory</param>
        /// <param name="parentId">The ID of the parent directory (null for root level)</param>
        /// <param name="excludeItemId">Optional ID of an item to exclude from the uniqueness check (used for rename operations)</param>
        /// <returns>
        /// A task that resolves to a ValidationResult indicating whether the name is unique in the specified location.
        /// Returns success if unique, or failure with NAME_ALREADY_EXISTS error if a conflict exists.
        /// </returns>
        /// <remarks>
        /// Used primarily for rename and create operations to prevent naming conflicts.
        /// The excludeItemId parameter allows checking uniqueness while excluding the item being renamed.
        /// Checks are case-sensitive and scoped to the specific parent directory and user.
        /// </remarks>
        Task<ValidationResult> ValidateNameUniquenessAsync(CloudCoreDbContext context, string name, int userId, int? parentId, int? excludeItemId = null);

        /// <summary>
        /// Validates that an archive operation meets size and file count limits.
        /// </summary>
        /// <param name="totalSize">Total size of all files to be included in the archive, in bytes</param>
        /// <param name="fileCount">Total number of files to be included in the archive</param>
        /// <returns>
        /// A ValidationResult indicating whether the archive parameters are within acceptable limits.
        /// Returns success if within limits, or failure with specific error details if limits are exceeded.
        /// </returns>
        /// <remarks>
        /// Validates against system-defined limits:
        /// - Maximum archive size: 2GB (2,147,483,648 bytes)
        /// - Maximum file count: 10,000 files
        /// These limits prevent resource exhaustion and improve system stability.
        /// </remarks>
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

    }
}
