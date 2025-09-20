using System.Xml.Linq;
using CloudCore.Models;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudCore.Services
{
    public class ValidationService : IValidationService
    {
        private const long MAX_SIZE = 2L * 1024 * 1024 * 1024; // 2 GB
        private const int MAX_FILES_IN_ARCHIVE = 10000;
        private const int MAX_NAME_LENGTH = 250;

        private static readonly char[] InvalidFileNameChars = { '<', '>', ':', '\"', '|', '?', '*', '\0', ',' };
        private static readonly string[] ReservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>{
            // Documents
            ".pdf", ".doc", ".docx", ".rtf", ".txt", ".odt", ".pages",
            ".dotx", ".dotm", ".docm", ".xml", ".html", ".htm", ".mht",
    
            // Tables
            ".xls", ".xlsx", ".xlsm", ".xlsb", ".xltx", ".csv", ".ods",
            ".numbers", ".tsv",
    
            // Presentations
            ".ppt", ".pptx", ".pptm", ".potx", ".ppsx", ".ppsm", ".odp",
            ".key",
    
            // Images
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif",
            ".svg", ".webp", ".ico", ".heic", ".heif", ".raw", ".psd",
    
            // Audio
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a",
            ".opus", ".aiff",
    
            // Video
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm",
            ".m4v", ".3gp", ".ogv",
    
            // Archives
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz",
            ".cab", ".dmg", ".iso",
    
            // Code
            ".js", ".css", ".json", ".xml", ".sql", ".py", ".java",
            ".cpp", ".c", ".cs", ".php", ".rb", ".go", ".rs", ".swift",
            ".kt", ".ts", ".scss", ".less", ".yaml", ".yml", ".md",
    
            // Fonts
            ".ttf", ".otf", ".woff", ".woff2", ".eot",
    
            // Books
            ".epub", ".mobi", ".fb2", ".azw", ".azw3",
    
            // Design
            ".dwg", ".dxf", ".ai", ".eps", ".indd", ".sketch",
    
            // Another
            ".log", ".cfg", ".conf", ".ini", ".properties",
            ".ics", ".vcf", ".gpx", ".kml"
        };


        public ValidationResult ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ValidationResult.Failure("File is required", "FILE_REQUIRED");
            if (file.Length > MAX_SIZE)
                return ValidationResult.Failure($"File size exceeds maximum allowed size ({FormatFileSize(MAX_SIZE)})", "FILE_TOO_LARGE");

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return ValidationResult.Failure($"File type not supported. Allowed types: {string.Join(", ", AllowedExtensions)}", "INVALID_FILE_TYPE");

            var nameValidation = ValidateItemName(file.FileName);
            if (!nameValidation.IsValid)
                return nameValidation;

            return ValidationResult.Success();
        }
        public ValidationResult ValidateItemName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return ValidationResult.Failure("Item name cannot be empty", "INVALID_NAME");

            if (name.Length > MAX_NAME_LENGTH)
                return ValidationResult.Failure($"Item name cannot exceed {MAX_NAME_LENGTH} characters", "NAME_TOO_LONG");

            foreach (var invalidChar in InvalidFileNameChars)
            {
                if (name.Contains(invalidChar))
                    return ValidationResult.Failure($"Item name contains invalid character: {invalidChar}", "INVALID_CHARACTER");
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(name).ToUpper();
            if (ReservedNames.Contains(nameWithoutExtension))
                return ValidationResult.Failure($"'{name}' is a reserved name", "RESERVED_NAME");

            if (name.StartsWith('.') || name.StartsWith(' ') || name.EndsWith('.') || name.EndsWith(' '))
                return ValidationResult.Failure("Item name cannot start or end with a dot or space", "INVALID_NAME_FORMAT");

            return ValidationResult.Success();
        }

        public ValidationResult ValidateUserAuthorization(int currentUserId, int requestedUserId)
        {
            if (currentUserId != requestedUserId)
                return ValidationResult.Failure("You can only access your own files", "ACCESS_DENIED");

            return ValidationResult.Success();
        }

        public async Task<ValidationResult> ValidateItemExistsAsync(CloudCoreDbContext context, int itemId, int userId, string itemType = null)
        {
            var query = context.Items
                .Where(i => i.Id == itemId && i.UserId == userId && i.IsDeleted == false);

            if (!string.IsNullOrEmpty(itemType))
                query = query.Where(i => i.Type == itemType);

            var item = await query.FirstOrDefaultAsync();

            if (item == null)
            {
                var errorMessage = itemType switch
                {
                    "file" => "File not found",
                    "folder" => "Folder not found",
                    _ => "Item not found"
                };
                return ValidationResult.Failure(errorMessage, "ITEM_NOT_FOUND");
            }

            return ValidationResult.Success();
        }

        public async Task<ValidationResult> ValidateItemIdsAsync(CloudCoreDbContext context, List<int> itemIds, int userId)
        {
            if (itemIds == null || !itemIds.Any())
                return ValidationResult.Failure("No items specified", "NO_ITEMS");

            if (itemIds.Count > 100)
                return ValidationResult.Failure("Too many items selected (max 100)", "TOO_MANY_ITEMS");

            var existingItems = await context.Items
                .Where(i => itemIds.Contains(i.Id) && i.UserId == userId && i.IsDeleted == false)
                .CountAsync();

            if (existingItems != itemIds.Count)
                return ValidationResult.Failure("Some items not found or don't belong to you", "ITEMS_NOT_FOUND");

            return ValidationResult.Success();
        }

        public async Task<ValidationResult> ValidateNameUniquenessAsync(CloudCoreDbContext context, string name, int userId, int? parentId, int? excludeItemId = null)
        {
            var query = context.Items
                .Where(i => i.Name == name && i.UserId == userId && i.ParentId == parentId && i.IsDeleted == false);

            if (excludeItemId.HasValue)
                query = query.Where(i => i.Id != excludeItemId.Value);

            var existingItem = await query.FirstOrDefaultAsync();

            if (existingItem != null)
                return ValidationResult.Failure("An item with this name already exists in this location", "NAME_ALREADY_EXISTS");

            return ValidationResult.Success();
        }

        public ValidationResult ValidateArchiveSize(long totalSize, int fileCount)
        {
            if (totalSize > MAX_SIZE)
                return ValidationResult.Failure($"Archive size exceeds maximum allowed size of {FormatFileSize(MAX_SIZE)}", "ARCHIVE_TOO_LARGE");

            if (fileCount > MAX_FILES_IN_ARCHIVE)
                return ValidationResult.Failure($"Too many files in archive (max {MAX_FILES_IN_ARCHIVE})", "TOO_MANY_ITEMS");

            return ValidationResult.Success();
        }

        public string FormatFileSize(long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            string format = order >= 2 ? "0.##" : "0.#";
            return $"{len.ToString(format)} {sizes[order]}";
        }

    }
}
