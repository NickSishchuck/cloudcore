using System.Text.RegularExpressions;
using System.Xml.Linq;
using CloudCore.Common.Errors;
using CloudCore.Common.Validation;
using CloudCore.Data.Context;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudCore.Services.Implementations
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


        private readonly ILogger<ValidationService> _logger;
        private readonly IItemRepository _itemRepository;

        public ValidationService(ILogger<ValidationService> logger, IItemRepository itemRepository)
        {
            _logger = logger;
            _itemRepository = itemRepository;
        }

        public ValidationResult ValidateFile(IFormFile file)
        {
            if (file == null)
            {
                _logger.LogWarning("File validation failed: no file provided");
                return ValidationResult.Failure("File is required", ErrorCodes.FILE_REQUIRED);
            }
            if (file.Length > MAX_SIZE)
            {
                _logger.LogWarning("File validation failed: file {FileName} too large ({FileSize} bytes)", file.FileName, file.Length);
                return ValidationResult.Failure($"File size exceeds maximum allowed size ({FormatFileSize(MAX_SIZE)})", ErrorCodes.FILE_TOO_LARGE);
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("File validation failed: unsupported extension {Extension}", extension);
                return ValidationResult.Failure($"File type not supported. Allowed types: {string.Join(", ", AllowedExtensions)}", ErrorCodes.INVALID_FILE_TYPE);
            }

            var nameValidation = ValidateItemName(file.FileName);
            if (!nameValidation.IsValid)
            {
                _logger.LogWarning("File validation failed: invalid file name {FileName}", file.FileName);
                return nameValidation;
            }

            _logger.LogInformation("File {FileName} passed validation", file.FileName);
            return ValidationResult.Success();

        }
        public ValidationResult ValidateItemName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Item name validation failed: empty name");
                return ValidationResult.Failure("Item name cannot be empty", ErrorCodes.INVALID_NAME);
            }

            if (name.Length > MAX_NAME_LENGTH)
            {
                _logger.LogWarning("Item name validation failed: name too long ({Length} chars)", name.Length);
                return ValidationResult.Failure($"Item name cannot exceed {MAX_NAME_LENGTH} characters", ErrorCodes.NAME_TOO_LONG);
            }

            foreach (var invalidChar in InvalidFileNameChars)
            {
                if (name.Contains(invalidChar))
                {
                    _logger.LogWarning("Item name validation failed: invalid char '{Char}' in {Name}", invalidChar, name);
                    return ValidationResult.Failure($"Item name contains invalid character: {invalidChar}", ErrorCodes.INVALID_CHARECTER);
                }
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(name).ToUpper();
            if (ReservedNames.Contains(nameWithoutExtension))
            {
                _logger.LogWarning("Item name validation failed: reserved name {Name}", nameWithoutExtension);
                return ValidationResult.Failure($"'{name}' is a reserved name", ErrorCodes.RESERVED_NAME);
            }

            if (name.StartsWith('.') || name.StartsWith(' ') || name.EndsWith('.') || name.EndsWith(' '))
            {
                _logger.LogWarning("Item name validation failed: starts/ends with invalid character");
                return ValidationResult.Failure("Item name cannot start or end with a dot or space", ErrorCodes.INVALID_NAME_FORMAT);
            }

            _logger.LogInformation("Item name {Name} passed validation", name);
            return ValidationResult.Success();
        }

        public ValidationResult ValidateUserAuthorization(int currentUserId, int requestedUserId)
        {
            if (currentUserId != requestedUserId)
            {
                _logger.LogWarning("Authorization failed: currentUserId {Current}, requestedUserId {Requested}", currentUserId, requestedUserId);
                return ValidationResult.Failure("You can only access your own files", ErrorCodes.ACCESS_DENIED);
            }

            _logger.LogInformation("Authorization succeeded for user {UserId}", currentUserId);
            return ValidationResult.Success();
        }

        public async Task<ValidationResult> ValidateItemExistsAsync(int itemId, int userId, string? itemType = null)
        {
            _logger.LogInformation("Validating existence of item {ItemId} of type {ItemType} for user {UserId}", itemId, itemType ?? "any", userId);

            var exists = await _itemRepository.ItemExistsAsync(itemId, userId, itemType);

            if (!exists)
            {
                var errorMessage = itemType switch
                {
                    "file" => "File not found",
                    "folder" => "Folder not found",
                    _ => "Item not found"
                };
                _logger.LogWarning("Validation failed: Item {ItemId} not found for user {UserId}", itemId, userId);
                return ValidationResult.Failure(errorMessage, ErrorCodes.ITEM_NOT_FOUND);
            }

            _logger.LogInformation("Validation successful: Item {ItemId} exists.", itemId);
            return ValidationResult.Success();
        }

        public async Task<ValidationResult> ValidateItemIdsAsync(List<int> itemIds, int userId)
        {
            if (itemIds == null || itemIds.Count == 0)
            {
                return ValidationResult.Failure("No items specified", ErrorCodes.NO_ITEMS);
            }
            if (itemIds.Count > 100)
            {
                return ValidationResult.Failure("Too many items selected (max 100)", ErrorCodes.TOO_MANY_FILES);
            }

            var existingItemsCount = await _itemRepository.CountExistingItemsAsync(itemIds, userId);

            if (existingItemsCount != itemIds.Count)
            {
                _logger.LogWarning("ItemIds validation failed: mismatch count ({Found}/{Expected}) for user {UserId}", existingItemsCount, itemIds.Count, userId);
                return ValidationResult.Failure("Some items not found or do not belong to you", ErrorCodes.ITEM_NOT_FOUND);
            }

            _logger.LogInformation("ItemIds validation succeeded for {Count} items for user {UserId}", itemIds.Count, userId);
            return ValidationResult.Success();
        }

        public async Task<ValidationResult> ValidateNameUniquenessAsync(string name, string itemType, int userId, int? parentId, int? excludeItemId = null, bool includeDeleted = false)
        {
            _logger.LogInformation("Validating name uniqueness for '{Name}' of type '{ItemType}' for user {UserId}", name, itemType, userId);

            var isDuplicate = await _itemRepository.DoesItemExistByNameAsync(name, itemType, userId, parentId, excludeItemId, includeDeleted);

            if (isDuplicate)
            {
                _logger.LogWarning("Validation failed: An item of type '{ItemType}' with name '{Name}' already exists.", itemType, name);
                return ValidationResult.Failure(
                    $"A {itemType} with this name already exists in this location",
                    ErrorCodes.NAME_ALREADY_EXISTS
                );
            }

            _logger.LogInformation("Validation successful: Name '{Name}' is unique.", name);
            return ValidationResult.Success();
        }

        public ValidationResult ValidateArchiveSize(long totalSize, int fileCount)
        {
            if (totalSize > MAX_SIZE)
            {
                _logger.LogWarning("Archive validation failed: size {Size} exceeds max {Max}", totalSize, MAX_SIZE);
                return ValidationResult.Failure($"Archive size exceeds maximum allowed size of {FormatFileSize(MAX_SIZE)}", ErrorCodes.ARCHIVE_TOO_LARGE);
            }

            if (fileCount > MAX_FILES_IN_ARCHIVE)
            {
                _logger.LogWarning("Archive validation failed: too many files ({Count})", fileCount);
                return ValidationResult.Failure($"Too many files in archive (max {MAX_FILES_IN_ARCHIVE})", ErrorCodes.TOO_MANY_FILES);
            }

            _logger.LogInformation("Archive validation succeeded: size {Size}, files {Count}", totalSize, fileCount);
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

        public ValidationResult ValidateQuery(string query)
        {
            if(string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("Validation failed: query is null or empty.");
                return ValidationResult.Failure("Querry cannot be null or empty", ErrorCodes.NULL_OR_EMPTY);
            }

            var regex = new Regex("^[a-zA-Z0-9 ]+$");
            if (!regex.IsMatch(query))
            {
                _logger.LogWarning("Validation failed: not allowed symbol.");
                return ValidationResult.Failure("Not allowed symbol", ErrorCodes.NOT_ALLOWED_SYMBOL);
            }

            return ValidationResult.Success();

        }

        public async Task<ValidationResult> ValidateIsFolderSubFolder(int userId, int folderId, int targetFolderId)
        {
            if (folderId == targetFolderId)
            {
                _logger.LogWarning("Validation failed: folder cannot be moved into itself.");
                return ValidationResult.Failure("Folder cannot be moved into itself", ErrorCodes.INVALID_OPERATION);
            }

            var isSubFolder = await _itemRepository.IsFolderSubFolderAsync(userId, folderId, targetFolderId);

            if (isSubFolder)
            {
                _logger.LogWarning("Validation failed: folder {TargetFolderId} is a subfolder of {FolderId}", targetFolderId, folderId);
                return ValidationResult.Failure("Folder cannot be moved into its own subfolder", ErrorCodes.CIRCULAR_REFERENCE);
            }

            _logger.LogInformation("Validation successful: no circular reference detected");
            return ValidationResult.Success();
        }
    }
}
