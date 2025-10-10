using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudCore.Services.Implementations
{
    public class ItemStorageService : IItemStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<ItemStorageService> _logger;
        public ItemStorageService(IConfiguration configuration, ILogger<ItemStorageService> logger)
        {
            _basePath = configuration["FileStorage:BasePath"];
            _logger = logger;
        }


        public string GetFileFullPath(int userId, string relativePath)
        {
            // Get full path "/app/storage/users/user/1/documents/test.pdf"
            var fullPath = Path.Combine(GetUserStoragePath(userId), relativePath);

            var resolvedPath = Path.GetFullPath(fullPath);

            if (!resolvedPath.StartsWith(GetUserStoragePath(userId), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Path traversal attempt detected: UserId={UserId}, Path={Path}", userId, relativePath);
                throw new UnauthorizedAccessException("Access denied: Invalid file path");
            }

            return resolvedPath;
        }

        public string GetUserStoragePath(int userId)
        {
            // Get user`s root path "/app/storage/users/user/1"
            return Path.Combine(_basePath, "users", $"user{userId}");
        }

        public string RemoveFromFolderPath(string path, string searchString)
        {
            int lastIndexofSearchString = path.LastIndexOf(searchString);
            string newPath = null;

            if (lastIndexofSearchString != -1)
                newPath = path.Remove(lastIndexofSearchString, searchString.Length);

            return newPath;
        }


        public string GetNewFilePath(string filePath, string folderPath, string userBasePath)
        {
            string newFolderPathWithoutUserPart = folderPath.Remove(folderPath.IndexOf(userBasePath), userBasePath.Length);

            string[] pathParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string[] newFolderParts = newFolderPathWithoutUserPart.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                                                  .Where(part => !string.IsNullOrEmpty(part))
                                                                  .ToArray();

            for (int i = 0; i < newFolderParts.Length && i < pathParts.Length - 1; i++)
                pathParts[i] = newFolderParts[i];


            string result = Path.Combine(pathParts);
            return result;
        }

        public string GetNewFolderPath(string path, string searchString, string newName)
        {
            return Path.Combine(RemoveFromFolderPath(path, searchString), newName);
        }

        public async Task<string> SaveFileAsync(int userId, string targetDirectory, IFormFile file)
        {
            var userStorageRoot = GetUserStoragePath(userId);

            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(userStorageRoot, targetDirectory, fileName);

            var resolvedPath = Path.GetFullPath(filePath);
            if (!resolvedPath.StartsWith(userStorageRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Path traversal attempt in SaveFileAsync: UserId={UserId}, Target={Target}",
                    userId, targetDirectory);
                throw new UnauthorizedAccessException("Invalid target directory");
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var relativePath = Path.GetRelativePath(userStorageRoot, filePath);

            return relativePath;
        }

        public bool TryCreateFolder(int userId, string relativePath)
        {
            try
            {
                string userStoragePath = GetUserStoragePath(userId);
                string folderFullPath = Path.Combine(userStoragePath, relativePath);

                if (Directory.Exists(folderFullPath))
                {
                    return false;
                }

                Directory.CreateDirectory(folderFullPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            return MimeTypeMappings.TryGetValue(extension ?? "", out var mimeType) ? mimeType : "application/octet-stream";
        }

        public string? RenameItemPhysically(Item item, string newName, IEnumerable<Item>? childItems = null, string? folderPath = null)
        {
            if (item.Type == "file")
            {
                // Get old file path "/app/storage/users/user/1/documents/test.pdf"
                var oldFilePath = GetFileFullPath(item.UserId, item.FilePath);

                // Get directory "/documents"
                var directory = Path.GetDirectoryName(oldFilePath);


                //var extension = Path.GetExtension(oldFilePath);


                // Make new file path "/app/storage/users/user/1/documents/newFileName" 
                var newFilePath = Path.Combine(directory, newName);

                if (File.Exists(newFilePath))
                    throw new IOException("File with this name already exists");

                File.Move(oldFilePath, newFilePath);

                var newRelativePath = Path.Combine(Path.GetDirectoryName(item.FilePath), newName);
                return newRelativePath;
            }
            else if (item.Type == "folder")
            {
                var oldFolderPath = Path.Combine(GetUserStoragePath(item.UserId), folderPath);
                var newFolderPath = Path.Combine(Path.GetDirectoryName(oldFolderPath), newName);

                if (Directory.Exists(newFolderPath))
                    throw new IOException($"Folder with name '{newName}' already exists in this directory.");

                Directory.Move(oldFolderPath, newFolderPath);

                return null;
            }

            throw new NotSupportedException($"Item type '{item.Type}' is not supported for physical renaming.");
        }


        public void DeleteItemPhysically(Item item, string? folderPath = null)
        {
            var basePath = GetUserStoragePath(item.UserId);

            if (item.Type == "file")
            {
                var fullPath = Path.Combine(basePath, item.FilePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted: {Path}", fullPath);
                }
                else
                    _logger.LogWarning("File not found for deletion: {Path}", fullPath);
            }
            else if (item.Type == "folder")
            {
                var fullPath = Path.Combine(basePath, folderPath);

                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    _logger.LogInformation("Folder deleted: {Path}", fullPath);
                }
                else
                    _logger.LogWarning("Folder not found for deletion: {Path}", fullPath);
            }
            else
                _logger.LogWarning("Unknown item type: {Type}", item.Type);
        }

        public string MoveItemPhysically(Item item, string destinationPath, string? folderPath = null)
        {
            var basePath = GetUserStoragePath(item.UserId);

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                _logger.LogError("Destination path is null or empty");
                throw new ArgumentException("Destination path cannot be null or empty", nameof(destinationPath));
            }

            string relativePath;
            if (item.Type == "file")
            {
                relativePath = MoveFile(item, basePath, destinationPath);
            }
            else if (item.Type == "folder")
            {
                relativePath = MoveFolder(item, basePath, destinationPath, folderPath);
            }
            else
            {
                _logger.LogError("Unknown item type: {Type}", item.Type);
                throw new NotSupportedException($"Item type '{item.Type}' is not supported for moving");
            }
            return relativePath;
        }

        private string MoveFile(Item item, string basePath, string destinationPath)
        {
            var sourceFilePath = Path.Combine(basePath, item.FilePath);

            if (!File.Exists(sourceFilePath))
            {
                _logger.LogError("Source file not found: {Path}", sourceFilePath);
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}", sourceFilePath);
            }

            var fileName = Path.GetFileName(sourceFilePath);
            var destinationFilePath = Path.Combine(destinationPath, fileName);

            if (File.Exists(destinationFilePath))
            {
                _logger.LogError("Destination file already exists: {Path}", destinationFilePath);
                throw new IOException($"Destination file already exists: {destinationFilePath}");
            }
            File.Move(sourceFilePath, destinationFilePath);

            var relativePath = Path.GetRelativePath(basePath, destinationFilePath);

            return relativePath;
            
        }

        private string MoveFolder(Item item, string basePath, string destinationPath, string folderPath)
        {
            var fullFolderPath = Path.Combine(basePath, folderPath);

            if (!Directory.Exists(fullFolderPath))
            {
                _logger.LogError("Source folder not found: {Path}", fullFolderPath);
                throw new DirectoryNotFoundException($"Source folder not found: {fullFolderPath}");
            }

            var fullDestinationPath = Path.Combine(destinationPath, item.Name);

            if (Directory.Exists(fullDestinationPath))
            {
                _logger.LogError("Destination folder already exists: {Path}", fullDestinationPath);
                throw new IOException($"Destination folder already exists: {fullDestinationPath}");
            }

            Directory.Move(fullFolderPath, fullDestinationPath);

            _logger.LogInformation("Folder physically moved: {Source} -> {Destination}",
                fullFolderPath, fullDestinationPath);

            var relativePath = Path.GetRelativePath(basePath, fullDestinationPath);

            return relativePath;
        }

        private static readonly Dictionary<string, string> MimeTypeMappings = new Dictionary<string, string>
        {
            
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".rtf"] = "application/rtf",
            [".txt"] = "text/plain",
            [".odt"] = "application/vnd.oasis.opendocument.text",
            [".pages"] = "application/vnd.apple.pages",
            [".dotx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.template",
            [".dotm"] = "application/vnd.ms-word.template.macroEnabled.12",
            [".docm"] = "application/vnd.ms-word.document.macroEnabled.12",
            [".xml"] = "application/xml",
            [".html"] = "text/html",
            [".htm"] = "text/html",
            [".mht"] = "message/rfc822",

            
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".xlsm"] = "application/vnd.ms-excel.sheet.macroEnabled.12",
            [".xlsb"] = "application/vnd.ms-excel.sheet.binary.macroEnabled.12",
            [".xltx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.template",
            [".csv"] = "text/csv",
            [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
            [".numbers"] = "application/vnd.apple.numbers",
            [".tsv"] = "text/tab-separated-values",

            
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".pptm"] = "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
            [".potx"] = "application/vnd.openxmlformats-officedocument.presentationml.template",
            [".ppsx"] = "application/vnd.openxmlformats-officedocument.presentationml.slideshow",
            [".ppsm"] = "application/vnd.ms-powerpoint.slideshow.macroEnabled.12",
            [".odp"] = "application/vnd.oasis.opendocument.presentation",
            [".key"] = "application/vnd.apple.keynote",

            
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".tiff"] = "image/tiff",
            [".tif"] = "image/tiff",
            [".svg"] = "image/svg+xml",
            [".webp"] = "image/webp",
            [".ico"] = "image/x-icon",
            [".heic"] = "image/heic",
            [".heif"] = "image/heif",
            [".raw"] = "image/x-canon-cr2",
            [".psd"] = "image/vnd.adobe.photoshop",

            
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".flac"] = "audio/flac",
            [".aac"] = "audio/aac",
            [".ogg"] = "audio/ogg",
            [".wma"] = "audio/x-ms-wma",
            [".m4a"] = "audio/mp4",
            [".opus"] = "audio/opus",
            [".aiff"] = "audio/aiff",

            
            [".mp4"] = "video/mp4",
            [".avi"] = "video/x-msvideo",
            [".mkv"] = "video/x-matroska",
            [".mov"] = "video/quicktime",
            [".wmv"] = "video/x-ms-wmv",
            [".flv"] = "video/x-flv",
            [".webm"] = "video/webm",
            [".m4v"] = "video/x-m4v",
            [".3gp"] = "video/3gpp",
            [".ogv"] = "video/ogg",

            
            [".zip"] = "application/zip",
            [".rar"] = "application/vnd.rar",
            [".7z"] = "application/x-7z-compressed",
            [".tar"] = "application/x-tar",
            [".gz"] = "application/gzip",
            [".bz2"] = "application/x-bzip2",
            [".xz"] = "application/x-xz",
            [".cab"] = "application/vnd.ms-cab-compressed",
            [".dmg"] = "application/x-apple-diskimage",
            [".iso"] = "application/x-iso9660-image",

            
            [".js"] = "application/javascript",
            [".css"] = "text/css",
            [".json"] = "application/json",
            [".sql"] = "application/sql",
            [".py"] = "text/x-python",
            [".java"] = "text/x-java-source",
            [".cpp"] = "text/x-c++src",
            [".c"] = "text/x-csrc",
            [".cs"] = "text/x-csharp",
            [".php"] = "application/x-httpd-php",
            [".rb"] = "application/x-ruby",
            [".go"] = "text/x-go",
            [".rs"] = "text/rust",
            [".swift"] = "text/x-swift",
            [".kt"] = "text/x-kotlin",
            [".ts"] = "application/typescript",
            [".scss"] = "text/x-scss",
            [".less"] = "text/x-less",
            [".yaml"] = "application/x-yaml",
            [".yml"] = "application/x-yaml",
            [".md"] = "text/markdown",

            
            [".ttf"] = "font/ttf",
            [".otf"] = "font/otf",
            [".woff"] = "font/woff",
            [".woff2"] = "font/woff2",
            [".eot"] = "application/vnd.ms-fontobject",

            
            [".epub"] = "application/epub+zip",
            [".mobi"] = "application/x-mobipocket-ebook",
            [".fb2"] = "application/x-fictionbook+xml",
            [".azw"] = "application/vnd.amazon.ebook",
            [".azw3"] = "application/vnd.amazon.ebook",

            
            [".dwg"] = "image/vnd.dwg",
            [".dxf"] = "image/vnd.dxf",
            [".ai"] = "application/illustrator",
            [".eps"] = "application/postscript",
            [".indd"] = "application/x-indesign",
            [".sketch"] = "application/sketch",

            
            [".log"] = "text/plain",
            [".cfg"] = "text/plain",
            [".conf"] = "text/plain",
            [".ini"] = "text/plain",
            [".properties"] = "text/plain",
            [".ics"] = "text/calendar",
            [".vcf"] = "text/vcard",
            [".gpx"] = "application/gpx+xml",
            [".kml"] = "application/vnd.google-earth.kml+xml"
        };

    }
}
