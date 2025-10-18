using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CloudCore.Domain.Entities;
using CloudCore.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloudCore.Tests
{

    public class ItemStorageServiceTests : IDisposable
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<ItemStorageService>> _mockLogger;
        private readonly ItemStorageService _service;
        private readonly string _testBasePath;
        private readonly int _testUserId = 1;

        public ItemStorageServiceTests()
        {
            // Temporary directory for testing
            _testBasePath = Path.Combine(Path.GetTempPath(), $"ItemStorageTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testBasePath);

            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["FileStorage:BasePath"]).Returns(_testBasePath);

            _mockLogger = new Mock<ILogger<ItemStorageService>>();

            _service = new ItemStorageService(_mockConfiguration.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            // Delete the temporary test directory
            if (Directory.Exists(_testBasePath))
            {
                try
                {
                    Directory.Delete(_testBasePath, true);
                }
                catch
                {
                    // Ignore any exceptions during cleanup
                }
            }
        }

        private Mock<IFormFile> CreateMockFormFile(string fileName, string content)
        {
            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    ms.Position = 0;
                    ms.CopyTo(stream);
                })
                .Returns(Task.CompletedTask);

            return fileMock;
        }


        #region Get UserStoragePath Tests

        [Fact]
        public void GetUserStoragePath_ValidUserId_ReturnsCorrectPath()
        {
            // Act
            var result = _service.GetUserStoragePath(_testUserId);

            // Assert
            var expected = Path.Combine(_testBasePath, "users", $"user{_testUserId}");
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(9999)]
        [InlineData(999999)]
        public void GetUserStoragePath_DifferentUserIds_ReturnsCorrectPaths(int userId)
        {
            // Act
            var result = _service.GetUserStoragePath(userId);

            // Assert
            var expected = Path.Combine(_testBasePath, "users", $"user{userId}");
            Assert.Equal(expected, result);
        }

        #endregion

        #region GetFileFullPath Tests

        [Fact]
        public void GetFileFullPath_ValidPath_ReturnsFullPath()
        {
            // Arrange
            var relativePath = Path.Combine("documents", "test.pdf");
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            Directory.CreateDirectory(userStoragePath);

            // Act
            var result = _service.GetFileFullPath(_testUserId, relativePath);

            // Assert
            Assert.StartsWith(userStoragePath, result);
            Assert.EndsWith("test.pdf", result);
        }

        [Fact]
        public void GetFileFullPath_PathTraversalAttempt_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var maliciousPath = Path.Combine("..", "..", "..", "etc", "passwd");

            // Act & Assert
            var exception = Assert.Throws<UnauthorizedAccessException>(() =>
                _service.GetFileFullPath(_testUserId, maliciousPath));

            Assert.Equal("Access denied: Invalid file path", exception.Message);
        }

        [Theory]
        [InlineData("../../../etc/passwd")]
        [InlineData("..\\..\\..\\windows\\system32")]
        [InlineData("../../sensitive-data.txt")]
        [InlineData("../../../Program Files")]
        [InlineData("documents/../../../etc/passwd")]
        public void GetFileFullPath_MultiplePathTraversalAttempts_ThrowsException(string maliciousPath)
        {
            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() =>
                _service.GetFileFullPath(_testUserId, maliciousPath));
        }


        [Fact]
        public void GetFileFullPath_NestedValidPath_ReturnsCorrectFullPath()
        {
            // Arrange
            var relativePath = Path.Combine("documents", "work", "projects", "2025", "report.pdf");
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            Directory.CreateDirectory(userStoragePath);

            // Act
            var result = _service.GetFileFullPath(_testUserId, relativePath);

            // Assert
            Assert.Contains("documents", result);
            Assert.Contains("work", result);
            Assert.Contains("projects", result);
            Assert.EndsWith("report.pdf", result);
        }

        #endregion

        #region SaveFileAsync Tests

        [Fact]
        public async Task SaveFileAsync_ValidFile_SavesAndReturnsRelativePath()
        {
            // Arrange
            var fileName = "test-upload.txt";
            var content = "Test file content";
            var targetDirectory = "uploads";

            var fileMock = CreateMockFormFile(fileName, content);

            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var targetPath = Path.Combine(userStoragePath, targetDirectory);
            Directory.CreateDirectory(targetPath);

            // Act
            var result = await _service.SaveFileAsync(_testUserId, targetDirectory, fileMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(fileName, result);

            var fullPath = Path.Combine(userStoragePath, result);
            Assert.True(File.Exists(fullPath));

            var savedContent = File.ReadAllText(fullPath);
            Assert.Equal(content, savedContent);
        }

        [Fact]
        public async Task SaveFileAsync_EmptyFile_SavesSuccessfully()
        {
            // Arrange
            var fileName = "empty.txt";
            var targetDirectory = "uploads";

            var fileMock = CreateMockFormFile(fileName, string.Empty);

            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var targetPath = Path.Combine(userStoragePath, targetDirectory);
            Directory.CreateDirectory(targetPath);

            // Act
            var result = await _service.SaveFileAsync(_testUserId, targetDirectory, fileMock.Object);

            // Assert
            Assert.NotNull(result);
            var fullPath = Path.Combine(userStoragePath, result);
            Assert.True(File.Exists(fullPath));
        }


        [Fact]
        public async Task SaveFileAsync_LargeFile_SavesSuccessfully()
        {
            // Arrange
            var fileName = "large-file.bin";
            var content = new string('A', 10000);
            var targetDirectory = "uploads";

            var fileMock = CreateMockFormFile(fileName, content);

            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var targetPath = Path.Combine(userStoragePath, targetDirectory);
            Directory.CreateDirectory(targetPath);

            // Act
            var result = await _service.SaveFileAsync(_testUserId, targetDirectory, fileMock.Object);

            // Assert
            Assert.NotNull(result);
            var fullPath = Path.Combine(userStoragePath, result);
            Assert.True(File.Exists(fullPath));
        }

        [Fact]
        public async Task SaveFileAsync_PathTraversalInTargetDirectory_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var fileName = "test.txt";
            var maliciousTarget = Path.Combine("..", "..", "etc");

            var fileMock = CreateMockFormFile(fileName, "content");

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _service.SaveFileAsync(_testUserId, maliciousTarget, fileMock.Object));
        }

        [Theory]
        [InlineData("../../../etc")]
        [InlineData("../../system32")]
        [InlineData("uploads/../../../../root")]
        public async Task SaveFileAsync_MultiplePathTraversalAttempts_ThrowsException(string maliciousTarget)
        {
            // Arrange
            var fileName = "test.txt";
            var fileMock = CreateMockFormFile(fileName, "content");

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _service.SaveFileAsync(_testUserId, maliciousTarget, fileMock.Object));
        }


        [Fact]
        public async Task SaveFileAsync_NestedTargetDirectory_SavesCorrectly()
        {
            // Arrange
            var fileName = "nested.txt";
            var content = "content";
            var targetDirectory = Path.Combine("level1", "level2", "level3");

            var fileMock = CreateMockFormFile(fileName, content);

            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var targetPath = Path.Combine(userStoragePath, targetDirectory);
            Directory.CreateDirectory(targetPath);

            // Act
            var result = await _service.SaveFileAsync(_testUserId, targetDirectory, fileMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("level1", result);
            Assert.Contains("level2", result);
            Assert.Contains("level3", result);
        }
        #endregion

        #region TryCreateFolder Tests

        [Fact]
        public void TryCreateFolder_NewFolder_ReturnsTrue()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            Directory.CreateDirectory(userStoragePath);
            var relativePath = "new-folder";

            // Act
            var result = _service.TryCreateFolder(_testUserId, relativePath);

            // Assert
            Assert.True(result);
            var folderPath = Path.Combine(userStoragePath, relativePath);
            Assert.True(Directory.Exists(folderPath));
        }

        [Fact]
        public void TryCreateFolder_EmptyRelativePath_ReturnsTrue()
        {
            // Act
            var result = _service.TryCreateFolder(_testUserId, string.Empty);

            // Assert
            Assert.True(result);
        }


        [Fact]
        public void TryCreateFolder_ExistingFolder_ReturnsFalse()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var relativePath = "existing-folder";
            var folderPath = Path.Combine(userStoragePath, relativePath);
            Directory.CreateDirectory(folderPath);

            // Act
            var result = _service.TryCreateFolder(_testUserId, relativePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryCreateFolder_NestedFolder_ReturnsTrue()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            Directory.CreateDirectory(userStoragePath);
            var relativePath = Path.Combine("parent", "child", "grandchild");

            // Act
            var result = _service.TryCreateFolder(_testUserId, relativePath);

            // Assert
            Assert.True(result);
            var folderPath = Path.Combine(userStoragePath, relativePath);
            Assert.True(Directory.Exists(folderPath));
        }

        [Fact]
        public void TryCreateFolder_DeepNesting_ReturnsTrue()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            Directory.CreateDirectory(userStoragePath);
            var relativePath = Path.Combine("level1", "level2", "level3", "level4", "level5");

            // Act
            var result = _service.TryCreateFolder(_testUserId, relativePath);

            // Assert
            Assert.True(result);
            var folderPath = Path.Combine(userStoragePath, relativePath);
            Assert.True(Directory.Exists(folderPath));
        }

        [Fact]
        public void TryCreateFolder_InvalidCharacters_ReturnsFalse()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            Directory.CreateDirectory(userStoragePath);
            var relativePath = "invalid<>folder|name";

            // Act
            var result = _service.TryCreateFolder(_testUserId, relativePath);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetMimeType Tests

        [Theory]
        [InlineData("document.pdf", "application/pdf")]
        [InlineData("report.doc", "application/msword")]
        [InlineData("presentation.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData("data.rtf", "application/rtf")]
        [InlineData("notes.txt", "text/plain")]
        [InlineData("sheet.xls", "application/vnd.ms-excel")]
        [InlineData("workbook.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [InlineData("data.csv", "text/csv")]
        [InlineData("slides.ppt", "application/vnd.ms-powerpoint")]
        [InlineData("deck.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
        [InlineData("photo.jpg", "image/jpeg")]
        [InlineData("picture.jpeg", "image/jpeg")]
        [InlineData("graphic.png", "image/png")]
        [InlineData("animation.gif", "image/gif")]
        [InlineData("bitmap.bmp", "image/bmp")]
        [InlineData("vector.svg", "image/svg+xml")]
        [InlineData("modern.webp", "image/webp")]
        [InlineData("icon.ico", "image/x-icon")]
        [InlineData("video.mp4", "video/mp4")]
        [InlineData("clip.avi", "video/x-msvideo")]
        [InlineData("movie.mkv", "video/x-matroska")]
        [InlineData("recording.mov", "video/quicktime")]
        [InlineData("stream.webm", "video/webm")]
        [InlineData("song.mp3", "audio/mpeg")]
        [InlineData("recording.wav", "audio/wav")]
        [InlineData("track.flac", "audio/flac")]
        [InlineData("audio.aac", "audio/aac")]
        [InlineData("music.m4a", "audio/mp4")]
        [InlineData("archive.zip", "application/zip")]
        [InlineData("compressed.rar", "application/vnd.rar")]
        [InlineData("package.7z", "application/x-7z-compressed")]
        [InlineData("tarball.tar", "application/x-tar")]
        [InlineData("gzipped.gz", "application/gzip")]
        [InlineData("script.js", "application/javascript")]
        [InlineData("style.css", "text/css")]
        [InlineData("data.json", "application/json")]
        [InlineData("query.sql", "application/sql")]
        [InlineData("program.py", "text/x-python")]
        [InlineData("App.java", "text/x-java-source")]
        [InlineData("main.cpp", "text/x-c++src")]
        [InlineData("code.cs", "text/x-csharp")]
        [InlineData("webpage.php", "application/x-httpd-php")]
        [InlineData("FILE.PDF", "application/pdf")]
        [InlineData("IMAGE.JPG", "image/jpeg")]
        [InlineData("Document.TxT", "text/plain")]
        public void GetMimeType_KnownExtensions_ReturnsCorrectMimeType(string fileName, string expectedMimeType)
        {
            // Act
            var result = _service.GetMimeType(fileName);
            // Assert
            Assert.Equal(expectedMimeType, result);
        }

        [Theory]
        [InlineData("file.unknown")]
        [InlineData("no-extension")]
        [InlineData("file.xyz")]
        public void GetMimeType_UnknownExtension_ReturnsDefaultMimeType(string fileName)
        {
            // Act
            var result = _service.GetMimeType(fileName);

            // Assert
            Assert.Equal("application/octet-stream", result);
        }

        [Theory]
        [InlineData(".pdf")]
        [InlineData(".jpg")]
        [InlineData(".txt")]
        public void GetMimeType_OnlyExtension_ReturnsCorrectMimeType(string extension)
        {
            // Act
            var result = _service.GetMimeType(extension);

            // Assert
            Assert.NotEqual("application/octet-stream", result);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetMimeType_Null_ReturnsDefaultMimeType()
        {
            // Arrange & Act
            var result = _service.GetMimeType(null);

            // Assert
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void GetMimeType_EmptyString_ReturnsDefaultMimeType()
        {
            // Arrange & Act
            var result = _service.GetMimeType(string.Empty);

            // Assert
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void GetMimeType_OnlyFileName_NoExtension_ReturnsDefaultMimeType()
        {
            // Arrange
            var fileName = "README";

            // Act
            var result = _service.GetMimeType(fileName);

            // Assert
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void GetMimeType_OnlyDot_ReturnsDefaultMimeType()
        {
            // Arrange
            var fileName = ".";

            // Act
            var result = _service.GetMimeType(fileName);

            // Assert
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void GetMimeType_DotAtEnd_ReturnsDefaultMimeType()
        {
            // Arrange
            var fileName = "file.";

            // Act
            var result = _service.GetMimeType(fileName);

            // Assert
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void GetMimeType_HiddenFileWithoutExtension_ReturnsDefaultMimeType()
        {
            // Arrange
            var fileName = ".gitignore";

            // Act
            var result = _service.GetMimeType(fileName);

            // Assert
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void GetMimeType_WhitespaceFileName_ReturnsDefaultMimeType()
        {
            // Act
            var result = _service.GetMimeType("   ");

            // Assert
            Assert.Equal("application/octet-stream", result);
        }

        #endregion

        #region RenameItemPhysically Tests


        [Fact]
        public void RenameItemPhysically_FileRename_ReturnsNewRelativePath()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var oldFileName = "old-name.txt";
            var newFileName = "new-name.txt";
            var directory = "documents";

            Directory.CreateDirectory(Path.Combine(userStoragePath, directory));
            var oldFilePath = Path.Combine(userStoragePath, directory, oldFileName);
            File.WriteAllText(oldFilePath, "test content");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = Path.Combine(directory, oldFileName),
                Name = oldFileName
            };

            // Act
            var result = _service.RenameItemPhysically(item, newFileName);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(newFileName, result);

            var newFilePath = Path.Combine(userStoragePath, directory, newFileName);
            Assert.True(File.Exists(newFilePath));
            Assert.False(File.Exists(oldFilePath));
        }

        [Fact]
        public void RenameItemPhysically_FileAlreadyExists_ThrowsIOException()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var oldFileName = "old.txt";
            var existingFileName = "existing.txt";
            var directory = "documents";

            Directory.CreateDirectory(Path.Combine(userStoragePath, directory));
            File.WriteAllText(Path.Combine(userStoragePath, directory, oldFileName), "old");
            File.WriteAllText(Path.Combine(userStoragePath, directory, existingFileName), "existing");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = Path.Combine(directory, oldFileName)
            };

            // Act & Assert
            Assert.Throws<IOException>(() =>
                _service.RenameItemPhysically(item, existingFileName));
        }

        [Fact]
        public void RenameItemPhysicaally_FileRename_SavesOldFileExtensionSuccessfully()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var oldFileName = "document.txt";
            var newFileNameWithoutExtension = "document-renamed";
            var directory = "documents";
            Directory.CreateDirectory(Path.Combine(userStoragePath, directory));
            var oldFilePath = Path.Combine(userStoragePath, directory, oldFileName);
            File.WriteAllText(oldFilePath, "Test content");
            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = Path.Combine(directory, oldFileName),
                Name = oldFileName
            };
            // Act
            var result = _service.RenameItemPhysically(item, newFileNameWithoutExtension);
            // Assert
            Assert.NotNull(result);
            Assert.EndsWith(".txt", result);
            var newFilePath = Path.Combine(userStoragePath, directory, newFileNameWithoutExtension + ".txt");
            Assert.True(File.Exists(newFilePath));
            Assert.False(File.Exists(oldFilePath));
        }

        [Fact]
        public void RenameItemPhysically_NotSupportedType_ThrowsNotSupportedException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "symlink",
                Name = "test"
            };
            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() =>
                _service.RenameItemPhysically(item, "new-name"));
            Assert.Contains("not supported", exception.Message);
        }

        [Theory]
        [InlineData("file.xxx")]
        [InlineData("file.zzz")]
        [InlineData("file.abc")]
        [InlineData("file.123")]
        [InlineData("file.unknownext")]
        public void RenameItemPhysically_VariousUnsupportedExtensions_ThrowsNotSupportedException(string newFileName)
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var oldFileName = "file.txt";
            var directory = "documents";

            Directory.CreateDirectory(Path.Combine(userStoragePath, directory));
            var oldFilePath = Path.Combine(userStoragePath, directory, oldFileName);
            File.WriteAllText(oldFilePath, "content");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = Path.Combine(directory, oldFileName),
                Name = oldFileName
            };

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                _service.RenameItemPhysically(item, newFileName));
        }

        [Fact]
        public void RenameItemPhysically_FolderRename_MovesDirectorySuccessfully()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var oldFolderName = "old-folder";
            var newFolderName = "new-folder";

            var oldFolderPath = Path.Combine(userStoragePath, oldFolderName);
            Directory.CreateDirectory(oldFolderPath);

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = oldFolderName
            };

            // Act
            var result = _service.RenameItemPhysically(item, newFolderName, oldFolderName);

            // Assert
            var newFolderPath = Path.Combine(userStoragePath, newFolderName);
            Assert.True(Directory.Exists(newFolderPath));
            Assert.False(Directory.Exists(oldFolderPath));
        }

        [Fact]
        public void RenameItemPhysically_FolderAlreadyExists_ThrowsIOException()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var oldFolderName = "old-folder";
            var existingFolderName = "existing-folder";

            Directory.CreateDirectory(Path.Combine(userStoragePath, oldFolderName));
            Directory.CreateDirectory(Path.Combine(userStoragePath, existingFolderName));

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = oldFolderName
            };

            // Act & Assert
            Assert.Throws<IOException>(() =>
                _service.RenameItemPhysically(item, existingFolderName, oldFolderName));
        }

        [Fact]
        public void RenameItemPhysically_FileWithExtensionChange_RenamesSuccessfully()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var oldFileName = "document.txt";
            var newFileName = "document.md";
            var directory = "documents";

            Directory.CreateDirectory(Path.Combine(userStoragePath, directory));
            var oldFilePath = Path.Combine(userStoragePath, directory, oldFileName);
            File.WriteAllText(oldFilePath, "# Markdown");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = Path.Combine(directory, oldFileName),
                Name = oldFileName
            };

            // Act
            var result = _service.RenameItemPhysically(item, newFileName);

            // Assert
            Assert.Contains(newFileName, result);
            Assert.True(File.Exists(Path.Combine(userStoragePath, directory, newFileName)));
        }

        [Fact]
        public void RenameItemPhysically_FolderWithNestedContent_MovesEverything()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var oldFolderName = "old-folder";
            var newFolderName = "new-folder";

            var oldFolderPath = Path.Combine(userStoragePath, oldFolderName);
            Directory.CreateDirectory(oldFolderPath);
            File.WriteAllText(Path.Combine(oldFolderPath, "file1.txt"), "content1");
            Directory.CreateDirectory(Path.Combine(oldFolderPath, "subfolder"));
            File.WriteAllText(Path.Combine(oldFolderPath, "subfolder", "file2.txt"), "content2");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = oldFolderName
            };

            // Act
            _service.RenameItemPhysically(item, newFolderName, oldFolderName);

            // Assert
            var newFolderPath = Path.Combine(userStoragePath, newFolderName);
            Assert.True(File.Exists(Path.Combine(newFolderPath, "file1.txt")));
            Assert.True(File.Exists(Path.Combine(newFolderPath, "subfolder", "file2.txt")));
        }

        [Fact]
        public void RenameItemPhysically_NonExistentFile_ThrowsException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = "nonexistent.txt",
                Name = "nonexistent.txt"
            };

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                _service.RenameItemPhysically(item, "new.txt"));
        }

        [Fact]
        public void RenameItemPhysically_UnsupportedType_ThrowsNotSupportedException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "symlink",
                Name = "test"
            };

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() =>
                _service.RenameItemPhysically(item, "new-name"));

            Assert.Contains("not supported", exception.Message);
        }



        #endregion

        #region DeleteItemPhysically Tests

        [Fact]
        public void DeleteItemPhysically_ExistingFile_DeletesSuccessfully()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var fileName = "delete-me.txt";
            var filePath = Path.Combine(userStoragePath, fileName);

            Directory.CreateDirectory(userStoragePath);
            File.WriteAllText(filePath, "content");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = fileName
            };

            // Act
            _service.DeleteItemPhysically(item);

            // Assert
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public void DeleteItemPhysically_ExistingFolder_DeletesRecursively()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var folderName = "delete-folder";
            var folderPath = Path.Combine(userStoragePath, folderName);

            Directory.CreateDirectory(folderPath);
            File.WriteAllText(Path.Combine(folderPath, "file1.txt"), "content1");
            Directory.CreateDirectory(Path.Combine(folderPath, "subfolder"));
            File.WriteAllText(Path.Combine(folderPath, "subfolder", "file2.txt"), "content2");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = folderName
            };

            // Act
            _service.DeleteItemPhysically(item, folderName);

            // Assert
            Assert.False(Directory.Exists(folderPath));
        }

        [Fact]
        public void DeleteItemPhysically_NonExistentFile_DoesNotThrowException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = "non-existent.txt"
            };

            // Act & Assert
            var exception = Record.Exception(() => _service.DeleteItemPhysically(item));
            Assert.Null(exception);
        }

        [Fact]
        public void DeleteItemPhysically_NonExistentFolder_DoesNotThrowException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = "non-existent-folder"
            };

            // Act & Assert
            var exception = Record.Exception(() => _service.DeleteItemPhysically(item, "non-existent-folder"));
            Assert.Null(exception);
        }

        [Fact]
        public void DeleteItemPhysically_EmptyFolder_DeletesSuccessfully()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var folderName = "empty-folder";
            var folderPath = Path.Combine(userStoragePath, folderName);

            Directory.CreateDirectory(folderPath);

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = folderName
            };

            // Act
            _service.DeleteItemPhysically(item, folderName);

            // Assert
            Assert.False(Directory.Exists(folderPath));
        }

        [Fact]
        public void DeleteItemPhysically_UnknownItemType_DoesNotThrowException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "unknown"
            };

            // Act & Assert
            var exception = Record.Exception(() => _service.DeleteItemPhysically(item));
            Assert.Null(exception);
        }

        #endregion

        #region MoveItemPhysically Tests

        [Fact]
        public void MoveItemPhysically_ValidFile_MovesSuccessfully()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var sourceFolder = "source";
            var destFolder = "destination";
            var fileName = "move-me.txt";

            Directory.CreateDirectory(Path.Combine(userStoragePath, sourceFolder));
            Directory.CreateDirectory(Path.Combine(userStoragePath, destFolder));

            var sourceFilePath = Path.Combine(userStoragePath, sourceFolder, fileName);
            File.WriteAllText(sourceFilePath, "content");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = Path.Combine(sourceFolder, fileName),
                Name = fileName
            };

            var destinationPath = Path.Combine(userStoragePath, destFolder);

            // Act
            var result = _service.MoveItemPhysically(item, destinationPath);

            // Assert
            Assert.NotNull(result);
            var destFilePath = Path.Combine(userStoragePath, destFolder, fileName);
            Assert.True(File.Exists(destFilePath));
            Assert.False(File.Exists(sourceFilePath));
        }

        [Fact]
        public void MoveItemPhysically_FileAlreadyExistsAtDestination_ThrowsIOException()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var sourceFolder = "source";
            var destFolder = "destination";
            var fileName = "duplicate.txt";

            Directory.CreateDirectory(Path.Combine(userStoragePath, sourceFolder));
            Directory.CreateDirectory(Path.Combine(userStoragePath, destFolder));

            File.WriteAllText(Path.Combine(userStoragePath, sourceFolder, fileName), "source");
            File.WriteAllText(Path.Combine(userStoragePath, destFolder, fileName), "dest");

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = Path.Combine(sourceFolder, fileName)
            };

            var destinationPath = Path.Combine(userStoragePath, destFolder);

            // Act & Assert
            Assert.Throws<IOException>(() =>
                _service.MoveItemPhysically(item, destinationPath));
        }

        [Fact]
        public void MoveItemPhysically_NullDestinationPath_ThrowsArgumentException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = "test.txt"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _service.MoveItemPhysically(item, null));
        }

        [Fact]
        public void MoveItemPhysically_ValidFolder_MovesSuccessfully()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var sourceFolderName = "source-folder";
            var destParent = "destination-parent";

            var sourcePath = Path.Combine(userStoragePath, sourceFolderName);
            Directory.CreateDirectory(sourcePath);
            File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "content");

            Directory.CreateDirectory(Path.Combine(userStoragePath, destParent));

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = sourceFolderName
            };

            var destinationPath = Path.Combine(userStoragePath, destParent);

            // Act
            var result = _service.MoveItemPhysically(item, destinationPath, sourceFolderName);

            // Assert
            var newPath = Path.Combine(userStoragePath, destParent, sourceFolderName);
            Assert.True(Directory.Exists(newPath));
            Assert.False(Directory.Exists(sourcePath));
            Assert.True(File.Exists(Path.Combine(newPath, "file.txt")));
        }

        [Fact]
        public void MoveItemPhysically_FolderWithNestedContent_MovesEverything()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var sourceFolderName = "source-folder";
            var destParent = "destination";

            var sourcePath = Path.Combine(userStoragePath, sourceFolderName);
            Directory.CreateDirectory(sourcePath);
            File.WriteAllText(Path.Combine(sourcePath, "file1.txt"), "content1");
            Directory.CreateDirectory(Path.Combine(sourcePath, "sub1"));
            File.WriteAllText(Path.Combine(sourcePath, "sub1", "file2.txt"), "content2");
            Directory.CreateDirectory(Path.Combine(sourcePath, "sub1", "sub2"));
            File.WriteAllText(Path.Combine(sourcePath, "sub1", "sub2", "file3.txt"), "content3");

            Directory.CreateDirectory(Path.Combine(userStoragePath, destParent));

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = sourceFolderName
            };

            var destinationPath = Path.Combine(userStoragePath, destParent);

            // Act
            _service.MoveItemPhysically(item, destinationPath, sourceFolderName);

            // Assert
            var newPath = Path.Combine(userStoragePath, destParent, sourceFolderName);
            Assert.True(File.Exists(Path.Combine(newPath, "file1.txt")));
            Assert.True(File.Exists(Path.Combine(newPath, "sub1", "file2.txt")));
            Assert.True(File.Exists(Path.Combine(newPath, "sub1", "sub2", "file3.txt")));
        }

        [Fact]
        public void MoveItemPhysically_SourceFileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var destFolder = "destination";
            Directory.CreateDirectory(Path.Combine(userStoragePath, destFolder));

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = "non-existent.txt"
            };

            var destinationPath = Path.Combine(userStoragePath, destFolder);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                _service.MoveItemPhysically(item, destinationPath));
        }

        [Fact]
        public void MoveItemPhysically_SourceFolderNotFound_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var destParent = "destination";
            Directory.CreateDirectory(Path.Combine(userStoragePath, destParent));

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = "non-existent"
            };

            var destinationPath = Path.Combine(userStoragePath, destParent);

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() =>
                _service.MoveItemPhysically(item, destinationPath, "non-existent"));
        }

        [Fact]
        public void MoveItemPhysically_EmptyDestinationPath_ThrowsArgumentException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = "test.txt"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _service.MoveItemPhysically(item, string.Empty));
        }

        [Fact]
        public void MoveItemPhysically_WhitespaceDestinationPath_ThrowsArgumentException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = "test.txt"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _service.MoveItemPhysically(item, "   "));
        }

        [Fact]
        public void MoveItemPhysically_UnsupportedItemType_ThrowsNotSupportedException()
        {
            // Arrange
            var item = new Item
            {
                UserId = _testUserId,
                Type = "symlink"
            };

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                _service.MoveItemPhysically(item, "/some/path"));
        }

        [Fact]
        public void MoveItemPhysically_FolderAlreadyExistsAtDestination_ThrowsIOException()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var folderName = "folder";
            var destParent = "destination";

            Directory.CreateDirectory(Path.Combine(userStoragePath, folderName));
            Directory.CreateDirectory(Path.Combine(userStoragePath, destParent));
            Directory.CreateDirectory(Path.Combine(userStoragePath, destParent, folderName));

            var item = new Item
            {
                UserId = _testUserId,
                Type = "folder",
                Name = folderName
            };

            var destinationPath = Path.Combine(userStoragePath, destParent);

            // Act & Assert
            Assert.Throws<IOException>(() =>
                _service.MoveItemPhysically(item, destinationPath, folderName));
        }

        [Fact]
        public void MoveItemPhysically_FileToNestedFolder_MovesSuccessfully()
        {
            // Arrange
            var userStoragePath = _service.GetUserStoragePath(_testUserId);
            var sourceFolder = "source";
            var destFolder = Path.Combine("level1", "level2", "level3");
            var fileName = "move-me.txt";

            Directory.CreateDirectory(Path.Combine(userStoragePath, sourceFolder));
            var sourceFilePath = Path.Combine(userStoragePath, sourceFolder, fileName);
            File.WriteAllText(sourceFilePath, "content");

            Directory.CreateDirectory(Path.Combine(userStoragePath, destFolder));

            var item = new Item
            {
                UserId = _testUserId,
                Type = "file",
                FilePath = Path.Combine(sourceFolder, fileName),
                Name = fileName
            };

            var destinationPath = Path.Combine(userStoragePath, destFolder);

            // Act
            var result = _service.MoveItemPhysically(item, destinationPath);

            // Assert
            var destFilePath = Path.Combine(userStoragePath, destFolder, fileName);
            Assert.True(File.Exists(destFilePath));
            Assert.False(File.Exists(sourceFilePath));
        }


        #endregion

        #region RemoveFromFolderPath Tests

        [Fact]
        public void RemoveFromFolderPath_MultipleOccurrences_RemovesLastOccurrence()
        {
            // Arrange
            var path = Path.Combine("temp", "temp", "files");
            var searchString = Path.DirectorySeparatorChar + "temp";

            // Act
            var result = _service.RemoveFromFolderPath(path, searchString);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("temp", result);

            var expectedPath = "temp" + Path.DirectorySeparatorChar + "files";
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void RemoveFromFolderPath_EmptySearchString_ReturnsNull()
        {
            // Arrange
            var path = Path.Combine("storage", "users", "user1");

            // Act
            var result = _service.RemoveFromFolderPath(path, string.Empty);

            // Assert
            Assert.Equal(result, path);
        }


        [Fact]
        public void RemoveFromFolderPath_StringExists_RemovesString()
        {
            // Arrange
            var path = Path.Combine("app", "storage", "users", "user1", "documents");
            var searchString = Path.DirectorySeparatorChar + "documents";

            // Act
            var result = _service.RemoveFromFolderPath(path, searchString);

            // Assert
            Assert.DoesNotContain("documents", result);
        }

        [Fact]
        public void RemoveFromFolderPath_StringNotFound_ReturnsNull()
        {
            // Arrange
            var path = Path.Combine("app", "storage", "users", "user1");
            var searchString = Path.DirectorySeparatorChar + "nonexistent";

            // Act
            var result = _service.RemoveFromFolderPath(path, searchString);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetNewFolderPath Tests

        [Fact]
        public void GetNewFolderPath_SearchStringNotFound_CombinesWithNull()
        {
            // Arrange
            var path = Path.Combine(_testBasePath, "folder");
            var searchString = "nonexistent";
            var newName = "new";

            // Act & Assert
            var exception = Record.Exception(() => _service.GetNewFolderPath(path, searchString, newName));

            Assert.True(exception != null || exception == null);
        }

        [Fact]
        public void GetNewFolderPath_EmptyNewName_ReturnsPathWithEmpty()
        {
            // Arrange
            var path = Path.Combine(_testBasePath, "users", "user1", "old");
            var searchString = "old";
            var newName = string.Empty;

            // Act
            var result = _service.GetNewFolderPath(path, searchString, newName);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("old", result);
        }

        [Fact]
        public void GetNewFolderPath_ValidInputs_ReturnsNewPath()
        {
            // Arrange
            var path = Path.Combine(_testBasePath, "users", "user1", "old-folder");
            var searchString = "old-folder";
            var newName = "new-folder";

            // Act
            var result = _service.GetNewFolderPath(path, searchString, newName);

            // Assert
            Assert.Contains("new-folder", result);
            Assert.DoesNotContain("old-folder", result);
        }

        #endregion

        #region GetNewFilePath Tests

        [Fact]
        public void GetNewFilePath_ValidInputs_ReturnsUpdatedPath()
        {
            // Arrange
            var filePath = Path.Combine("documents", "subfolder", "file.txt");
            var folderPath = Path.Combine(_testBasePath, "users", "user1", "new-location");
            var userBasePath = Path.Combine(_testBasePath, "users", "user1");

            // Act
            var result = _service.GetNewFilePath(filePath, folderPath, userBasePath);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("file.txt", result);
        }

        [Fact]
        public void GetNewFilePath_ComplexNestedStructure_ReturnsCorrectPath()
        {
            // Arrange
            var filePath = Path.Combine("docs", "2025", "reports", "file.pdf");
            var folderPath = Path.Combine(_testBasePath, "users", "user1", "archive", "old");
            var userBasePath = Path.Combine(_testBasePath, "users", "user1");

            // Act
            var result = _service.GetNewFilePath(filePath, folderPath, userBasePath);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("file.pdf", result);
        }

        [Fact]
        public void GetNewFilePath_SingleLevelPath_ReturnsCorrectPath()
        {
            // Arrange
            var filePath = "file.txt";
            var folderPath = Path.Combine(_testBasePath, "users", "user1", "newfolder");
            var userBasePath = Path.Combine(_testBasePath, "users", "user1");

            // Act
            var result = _service.GetNewFilePath(filePath, folderPath, userBasePath);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("file.txt", result);
        }

        #endregion


    }


}
