using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudCore.Common.Validation;
using CloudCore.Domain.Entities;
using CloudCore.Services.Implementations;
using CloudCore.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloudCore.Tests
{
    public class ZipArchiveServiceTests : IDisposable
    {
        private readonly Mock<IItemStorageService> _mockFileStorageService;
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILogger<ZipArchiveService>> _mockLogger;
        private readonly Mock<IStorageCalculationService> _mockStorageCalculationService;
        private readonly ZipArchiveService _service;
        private readonly List<string> _tempFiles;


        public ZipArchiveServiceTests()
        {
            _mockFileStorageService = new Mock<IItemStorageService>();
            _mockValidationService = new Mock<IValidationService>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockLogger = new Mock<ILogger<ZipArchiveService>>();
            _mockStorageCalculationService = new Mock<IStorageCalculationService>();
            _tempFiles = new List<string>();
            _service = new ZipArchiveService(
                _mockFileStorageService.Object,
                _mockValidationService.Object,
                _mockItemRepository.Object,
                _mockLogger.Object,
                _mockStorageCalculationService.Object);
        }


        public void Dispose()
        {
            // Clean up temporary files created during tests
            foreach (var tempFile in _tempFiles)
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        #region Test Helper Classes

        // Helper class to create testable IAsyncEnumerable
        internal class TestAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IEnumerable<T> _items;

            public TestAsyncEnumerable(IEnumerable<T> items)
            {
                _items = items;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new TestAsyncEnumerator<T>(_items.GetEnumerator());
            }
        }

        internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator;

            public TestAsyncEnumerator(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public T Current => _enumerator.Current;

            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(_enumerator.MoveNext());
            }

            public ValueTask DisposeAsync()
            {
                _enumerator.Dispose();
                return new ValueTask();
            }
        }
        private void SetupValidation(bool isValid, string errorMessage = "")
        {
            _mockStorageCalculationService
                .Setup(x => x.CalculateFolderSizeAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync((0L, 0));

            _mockValidationService
                .Setup(x => x.ValidateArchiveSize(It.IsAny<long>(), It.IsAny<int>()))
                .Returns(ValidationResult.Success());

            if (!isValid)
            {
                _mockValidationService
                    .Setup(x => x.ValidateArchiveSize(It.IsAny<long>(), It.IsAny<int>()))
                    .Returns(ValidationResult.Failure(errorMessage, "ERROR_CODE"));
            }
        }

        private void SetupEmptyFolder(int userId, int folderId)
        {
            _mockItemRepository
                .Setup(x => x.GetDirectChildrenAsync(userId, folderId, null, false))
                .Returns(CreateAsyncEnumerable(Array.Empty<Item>()));
        }

        private void SetupFolderStructure(int userId, int folderId, string testFilePath)
        {
            var subFolder = new Item
            {
                Id = 11,
                UserId = userId,
                Name = "SubFolder",
                Type = "folder",
                IsDeleted = false
            };

            var file = new Item
            {
                Id = 12,
                UserId = userId,
                Name = "test.txt",
                Type = "file",
                FilePath = testFilePath,
                IsDeleted = false
            };

            _mockItemRepository
                .Setup(x => x.GetDirectChildrenAsync(userId, folderId, null, false))
                .Returns(CreateAsyncEnumerable(new[] { subFolder, file }));

            _mockItemRepository
                .Setup(x => x.GetDirectChildrenAsync(userId, subFolder.Id, null, false))
                .Returns(CreateAsyncEnumerable(Array.Empty<Item>()));

            _mockFileStorageService
                .Setup(x => x.GetFileFullPath(userId, testFilePath))
                .Returns(testFilePath);
        }

        private string CreateTestFile(string content)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);
            return tempFile;
        }

        private IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
        {
            return new TestAsyncEnumerable<T>(items);
        }

        #endregion

        #region CreateFolderArchiveAsync Tests

        [Fact]
        public async Task CreateFolderArchiveAsync_ValidFolder_ReturnsFileStream()
        {
            // Arrange
            int userId = 1;
            int folderId = 10;
            string folderName = "TestFolder";

            SetupValidation(isValid: true);
            SetupEmptyFolder(userId, folderId);

            // Act
            var result = await _service.CreateFolderArchiveAsync(userId, folderId, folderName);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CanRead);

            // Clean up
            var path = result.Name;
            result.Dispose();
            _tempFiles.Add(path);
        }

        [Fact]
        public async Task CreateFolderArchiveAsync_InvalidSize_ThrowsException()
        {
            // Arrange
            int userId = 1;
            int folderId = 10;
            string folderName = "LargeFolder";

            SetupValidation(isValid: false, errorMessage: "Archive exceeds size limit");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateFolderArchiveAsync(userId, folderId, folderName)
            );

            Assert.Equal("Archive exceeds size limit", exception.Message);
        }

        [Fact]
        public async Task CreateFolderArchiveAsync_WithFilesAndFolders_CreatesArchive()
        {
            // Arrange
            int userId = 1;
            int folderId = 10;
            string folderName = "RootFolder";

            var testFile = CreateTestFile("test content");
            _tempFiles.Add(testFile);

            SetupValidation(isValid: true);
            SetupFolderStructure(userId, folderId, testFile);

            // Act
            var result = await _service.CreateFolderArchiveAsync(userId, folderId, folderName);

            // Assert
            Assert.NotNull(result);
            using (var zipArchive = new ZipArchive(result, ZipArchiveMode.Read, false))
            {
                Assert.True(zipArchive.Entries.Count > 0);
            }

            _tempFiles.Add(result.Name);
        }

        [Fact]
        public async Task CreateFolderArchiveAsync_EmptyFolder_CreatesEmptyArchive()
        {
            // Arrange
            int userId = 1;
            int folderId = 10;

            SetupValidation(isValid: true);
            SetupEmptyFolder(userId, folderId);

            // Act
            var result = await _service.CreateFolderArchiveAsync(userId, folderId, "EmptyFolder");

            // Assert
            Assert.NotNull(result);
            using (var zipArchive = new ZipArchive(result, ZipArchiveMode.Read, false))
            {
                Assert.Empty(zipArchive.Entries);
            }

            _tempFiles.Add(result.Name);
        }

        [Fact]
        public async Task CreateFolderArchiveAsync_FileNotFound_SkipsFileAndLogsWarning()
        {
            // Arrange
            int userId = 1;
            int folderId = 10;

            SetupValidation(isValid: true);

            var fileItem = new Item
            {
                Id = 1,
                UserId = userId,
                Name = "missing.txt",
                Type = "file",
                FilePath = "nonexistent.txt",
                IsDeleted = false
            };

            _mockItemRepository
                .Setup(x => x.GetDirectChildrenAsync(userId, folderId, null, false))
                .Returns(CreateAsyncEnumerable(new[] { fileItem }));

            _mockFileStorageService
                .Setup(x => x.GetFileFullPath(userId, "nonexistent.txt"))
                .Returns("C:\\nonexistent\\path\\file.txt");

            // Act
            var result = await _service.CreateFolderArchiveAsync(userId, folderId, "Folder");

            // Assert
            Assert.NotNull(result);

            // Verify that warning was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            result.Dispose();
            _tempFiles.Add(result.Name);
        }

        [Fact]
        public async Task CreateFolderArchiveAsync_EmptyFilePath_SkipsFileAndLogsWarning()
        {
            // Arrange
            int userId = 1;
            int folderId = 10;

            SetupValidation(isValid: true);

            var fileItem = new Item
            {
                Id = 1,
                UserId = userId,
                Name = "emptypath.txt",
                Type = "file",
                FilePath = "",
                IsDeleted = false
            };

            _mockItemRepository
                .Setup(x => x.GetDirectChildrenAsync(userId, folderId, null, false))
                .Returns(CreateAsyncEnumerable(new[] { fileItem }));

            // Act
            var result = await _service.CreateFolderArchiveAsync(userId, folderId, "Folder");

            // Assert
            Assert.NotNull(result);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("FilePath is empty")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            result.Dispose();
            _tempFiles.Add(result.Name);
        }

        #endregion

        #region CreateMultipleItemArchiveAsync Tests

        [Fact]
        public async Task CreateMultipleItemArchiveAsync_MultipleFiles_CreatesArchive()
        {
            // Arrange
            int userId = 1;
            var testFile1 = CreateTestFile("content 1");
            var testFile2 = CreateTestFile("content 2");
            _tempFiles.Add(testFile1);
            _tempFiles.Add(testFile2);

            var items = CreateAsyncEnumerable(new[]
            {
                new Item { Id = 1, UserId = userId, Name = "file1.txt", Type = "file", FilePath = testFile1, FileSize = 100, IsDeleted = false },
                new Item { Id = 2, UserId = userId, Name = "file2.txt", Type = "file", FilePath = testFile2, FileSize = 100, IsDeleted = false }
            });

            _mockValidationService
                .Setup(x => x.ValidateArchiveSize(It.IsAny<long>(), It.IsAny<int>()))
                .Returns(ValidationResult.Success());

            _mockFileStorageService
                .Setup(x => x.GetFileFullPath(userId, It.IsAny<string>()))
                .Returns<int, string>((uid, path) => path);

            // Act
            var result = await _service.CreateMultipleItemArchiveAsync(userId, items);

            // Assert
            Assert.NotNull(result);
            using (var zipArchive = new ZipArchive(result, ZipArchiveMode.Read, false))
            {
                Assert.Equal(2, zipArchive.Entries.Count);
            }

            _tempFiles.Add(result.Name);
        }

        [Fact]
        public async Task CreateMultipleItemArchiveAsync_ExceedsLimits_ThrowsException()
        {
            // Arrange
            int userId = 1;
            var items = CreateAsyncEnumerable(new[]
            {
                new Item { Id = 1, UserId = userId, Type = "file", FileSize = 3000 * 1024 * 1024L, IsDeleted = false }
            });

            _mockValidationService
                .Setup(x => x.ValidateArchiveSize(It.IsAny<long>(), It.IsAny<int>()))
                .Returns(ValidationResult.Failure("Archive size exceeds maximum allowed size", "ARCHIVE_TOO_LARGE"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateMultipleItemArchiveAsync(userId, items)
            );

            Assert.Equal("Archive size exceeds maximum allowed size", exception.Message);
        }

        [Fact]
        public async Task CreateMultipleItemArchiveAsync_WithFolders_CreatesNestedStructure()
        {
            // Arrange
            int userId = 1;
            var testFile = CreateTestFile("nested content");
            _tempFiles.Add(testFile);

            var folderItem = new Item
            {
                Id = 1,
                UserId = userId,
                Name = "Folder1",
                Type = "folder",
                IsDeleted = false
            };

            var items = CreateAsyncEnumerable(new[] { folderItem });

            var nestedFile = new Item
            {
                Id = 10,
                UserId = userId,
                Name = "nested.txt",
                Type = "file",
                FilePath = testFile,
                IsDeleted = false
            };

            _mockItemRepository
                .Setup(x => x.GetDirectChildrenAsync(userId, folderItem.Id, null, false))
                .Returns(CreateAsyncEnumerable(new[] { nestedFile }));

            _mockFileStorageService
                .Setup(x => x.GetFileFullPath(userId, testFile))
                .Returns(testFile);

            _mockValidationService
                .Setup(x => x.ValidateArchiveSize(It.IsAny<long>(), It.IsAny<int>()))
                .Returns(ValidationResult.Success());

            // Act
            var result = await _service.CreateMultipleItemArchiveAsync(userId, items);

            // Assert
            Assert.NotNull(result);
            using (var zipArchive = new ZipArchive(result, ZipArchiveMode.Read, false))
            {
                Assert.True(zipArchive.Entries.Count >= 1);
            }

            _tempFiles.Add(result.Name);
        }

        [Fact]
        public async Task CreateMultipleItemArchiveAsync_SkipsDeletedItems()
        {
            // Arrange
            int userId = 1;
            var testFile = CreateTestFile("content");
            _tempFiles.Add(testFile);

            var items = CreateAsyncEnumerable(new[]
            {
                new Item { Id = 1, UserId = userId, Name = "deleted.txt", Type = "file", FilePath = testFile, FileSize = 1000, IsDeleted = true },
                new Item { Id = 2, UserId = userId, Name = "active.txt", Type = "file", FilePath = testFile, FileSize = 2000, IsDeleted = false }
            });

            _mockFileStorageService
                .Setup(x => x.GetFileFullPath(userId, testFile))
                .Returns(testFile);

            _mockValidationService
                .Setup(x => x.ValidateArchiveSize(It.IsAny<long>(), It.IsAny<int>()))
                .Returns(ValidationResult.Success());

            // Act
            var result = await _service.CreateMultipleItemArchiveAsync(userId, items);

            // Assert
            Assert.NotNull(result);
            using (var zipArchive = new ZipArchive(result, ZipArchiveMode.Read, false))
            {
                Assert.Single(zipArchive.Entries);
                Assert.Equal("active.txt", zipArchive.Entries[0].Name);
            }

            _tempFiles.Add(result.Name);
        }

        #endregion

        #region CalculateMultipleItemsSizeAsync Tests

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_ReturnsCorrectValues()
        {
            // Arrange
            int userId = 1;
            var items = CreateAsyncEnumerable(new[]
            {
                new Item { Id = 1, UserId = userId, Type = "file", FileSize = 1000, IsDeleted = false },
                new Item { Id = 2, UserId = userId, Type = "file", FileSize = 2000, IsDeleted = false }
            });

            _mockStorageCalculationService
                .Setup(x => x.CalculateMultipleItemsSizeAsync(userId, It.IsAny<IAsyncEnumerable<Item>>()))
                .ReturnsAsync((3000L, 2));

            // Act
            var result = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(3000L, result.totalSize);
            Assert.Equal(2, result.fileCount);

            _mockStorageCalculationService.Verify(
                x => x.CalculateMultipleItemsSizeAsync(userId, It.IsAny<IAsyncEnumerable<Item>>()),
                Times.Once);
        }

        #endregion

    }
}
