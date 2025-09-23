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
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        public ItemStorageService(IConfiguration configuration, IDbContextFactory<CloudCoreDbContext> dbContextFactory)
        {
            _basePath = configuration["FileStorage:BasePath"];
            _dbContextFactory = dbContextFactory;
        }


        public string GetFileFullPath(int userId, string relativePath)
        {
            // Get full path "/app/storage/users/user/1/documents/test.pdf"
            var fullPath = Path.Combine(GetUserStoragePath(userId), relativePath);

            var resolvedPath = Path.GetFullPath(fullPath);

            if (!resolvedPath.StartsWith(fullPath))
                throw new UnauthorizedAccessException("Invalid file path");

            return resolvedPath;
        }

        public string GetUserStoragePath(int userId)
        {
            // Get user`s root path "/app/storage/users/user/1"
            return Path.Combine(_basePath, "users", $"user{userId}");
        }

        public string GetFolderPathAsync(Item folder)
        {

            var pathParts = new List<string>();
            var current = folder;

            using var context = _dbContextFactory.CreateDbContext();
            while (current != null)
            {
                pathParts.Add(current.Name);

                if (current.ParentId == null)
                    break;

                current = context.Items
                    .AsNoTracking()
                    .Where(i => i.Id == current.ParentId)
                    .FirstOrDefault();
            }
            pathParts.Reverse();
            return Path.Combine(GetUserStoragePath(folder.UserId), Path.Combine(pathParts.ToArray()));
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

        public async Task<string> SaveFileAsync(int userId, IFormFile file, int? parentId)
        {
            var userDirectory = GetUserStoragePath(userId);

            string targetDirectory = userDirectory;

            if (parentId.HasValue)
            {
                using var context = _dbContextFactory.CreateDbContext();
                var parentFolder = await context.Items
                    .AsNoTracking()
                    .Where(i => i.Id == parentId && i.UserId == userId && i.IsDeleted == false && i.Type == "folder")
                    .FirstOrDefaultAsync();

                if (parentFolder != null)
                    targetDirectory = GetFolderPathAsync(parentFolder);
                else
                    throw new DirectoryNotFoundException();
            }

            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(targetDirectory, fileName);

            if (File.Exists(filePath))
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                var counter = 1;

                do
                {
                    fileName = $"{nameWithoutExtension}({counter}){extension}";
                    filePath = Path.Combine(targetDirectory, fileName);
                    counter++;
                }
                while (File.Exists(filePath));
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.GetRelativePath(userDirectory, filePath);
        }

        public string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            return MimeTypeMappings.TryGetValue(extension ?? "", out var mimeType) ? mimeType : "application/octet-stream";
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
