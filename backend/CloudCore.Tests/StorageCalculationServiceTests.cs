using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CloudCore.Domain.Entities;
using CloudCore.Services.Implementations;
using CloudCore.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloudCore.Tests
{
    public class StorageCalculationServiceTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILogger<StorageCalculationService>> _loggerMock;
        private readonly StorageCalculationService _service;

        public StorageCalculationServiceTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _loggerMock = new Mock<ILogger<StorageCalculationService>>();
            _service = new StorageCalculationService(_mockItemRepository.Object, _loggerMock.Object);
        }

        #region GetUserTotalStorageAsync Tests

        [Fact]
        public async Task GetUserTotalStorageAsync_NoFiles_ReturnsZero()
        {
            // Arrange
            var userId = 1;
            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, null))
                .ReturnsAsync((0L, 0));

            // Act
            var result = await _service.GetUserTotalStorageAsync(userId);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetUserTotalStorageAsync_WithFiles_ReturnsCorrectSize()
        {
            // Arrange
            var userId = 1;
            var totalSize = 1500L;
            var totalFiles = 3;
            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, null))
                .ReturnsAsync((totalSize, totalFiles));
            // Act
            var result = await _service.GetUserTotalStorageAsync(userId);
            // Assert
            Assert.Equal(totalSize, result);
        }

        [Fact]
        public async Task GetUserTotalStorageAsync_LargeStorage_ReturnsCorrectValue()
        {
            // Arrange
            var userId = 1;
            var totalSize = 10_000_000_000L; // 10 GB
            var fileCount = 1000;

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, null))
                .ReturnsAsync((totalSize, fileCount));

            // Act
            var result = await _service.GetUserTotalStorageAsync(userId);

            // Assert
            Assert.Equal(totalSize, result);
        }


        [Fact]
        public async Task GetUserTotalStorageAsync_CallsRepositoryWithNullParentId()
        {
            // Arrange
            var userId = 1;
            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, null))
                .ReturnsAsync((1000L, 5));

            // Act
            await _service.GetUserTotalStorageAsync(userId);

            // Assert
            _mockItemRepository.Verify(
                x => x.CalculateArchiveSizeAsync(userId, null),
                Times.Once);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(9999)]
        public async Task GetUserTotalStorageAsync_DifferentUserIds_CallsWithCorrectUserId(int userId)
        {
            // Arrange
            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, null))
                .ReturnsAsync((1000L, 5));

            // Act
            await _service.GetUserTotalStorageAsync(userId);

            // Assert
            _mockItemRepository.Verify(
                x => x.CalculateArchiveSizeAsync(userId, null),
                Times.Once);
        }

        #endregion

        #region CalculateFolderSizeAsync Tests

        [Fact]
        public async Task CalculateFolderSizeAsync_EmptyFolder_ReturnsZero()
        {
            // Arrange
            var userId = 1;
            var folderId = 10;

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, folderId))
                .ReturnsAsync((0L, 0));

            // Act
            var (totalSize, fileCount) = await _service.CalculateFolderSizeAsync(userId, folderId);

            // Assert
            Assert.Equal(0, totalSize);
            Assert.Equal(0, fileCount);
        }

        [Fact]
        public async Task CalculateFolderSizeAsync_WithFiles_ReturnsCorrectValues()
        {
            // Arrange
            var userId = 1;
            var folderId = 10;
            var expectedSize = 5000L;
            var expectedCount = 3;

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, folderId))
                .ReturnsAsync((expectedSize, expectedCount));

            // Act
            var (totalSize, fileCount) = await _service.CalculateFolderSizeAsync(userId, folderId);

            // Assert
            Assert.Equal(expectedSize, totalSize);
            Assert.Equal(expectedCount, fileCount);
        }

        [Fact]
        public async Task CalculateFolderSizeAsync_NullFolderId_CalculatesRootLevel()
        {
            // Arrange
            var userId = 1;
            int? folderId = null;

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, null))
                .ReturnsAsync((10000L, 5));

            // Act
            var (totalSize, fileCount) = await _service.CalculateFolderSizeAsync(userId, folderId);

            // Assert
            Assert.Equal(10000L, totalSize);
            Assert.Equal(5, fileCount);

            _mockItemRepository.Verify(
                x => x.CalculateArchiveSizeAsync(userId, null),
                Times.Once);
        }

        [Fact]
        public async Task CalculateFolderSizeAsync_NestedFolder_ReturnsCorrectValues()
        {
            // Arrange
            var userId = 1;
            var folderId = 50;
            var expectedSize = 150000L;
            var expectedCount = 25;

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, folderId))
                .ReturnsAsync((expectedSize, expectedCount));

            // Act
            var (totalSize, fileCount) = await _service.CalculateFolderSizeAsync(userId, folderId);

            // Assert
            Assert.Equal(expectedSize, totalSize);
            Assert.Equal(expectedCount, fileCount);
        }

        [Theory]
        [InlineData(1, 10, 1000L, 5)]
        [InlineData(2, 20, 2000L, 10)]
        [InlineData(3, 30, 3000L, 15)]
        public async Task CalculateFolderSizeAsync_VariousScenarios_ReturnsCorrectValues(
            int userId, int folderId, long expectedSize, int expectedCount)
        {
            // Arrange
            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, folderId))
                .ReturnsAsync((expectedSize, expectedCount));

            // Act
            var (totalSize, fileCount) = await _service.CalculateFolderSizeAsync(userId, folderId);

            // Assert
            Assert.Equal(expectedSize, totalSize);
            Assert.Equal(expectedCount, fileCount);
        }

        #endregion

        #region CalculateMultipleItemsSizeAsync Tests

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            var userId = 1;
            var items = AsyncEnumerable.Empty<Item>();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(0, totalSize);
            Assert.Equal(0, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_OnlyDeletedItems_SkipsAll()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = 1000, IsDeleted = false },
                new Item { Id = 2, Type = "file", FileSize = 2000, IsDeleted = false },
                new Item { Id = 3, Type = "folder", IsDeleted = false }
            }.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(0, totalSize);
            Assert.Equal(0, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_SingleFile_ReturnsFileSize()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = 5000, IsDeleted = true }
            }.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(5000, totalSize);
            Assert.Equal(1, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_MultipleFiles_SumsCorrectly()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = 1000, IsDeleted = true },
                new Item { Id = 2, Type = "file", FileSize = 2000, IsDeleted = true },
                new Item { Id = 3, Type = "file", FileSize = 3000, IsDeleted = true }
            }.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(6000, totalSize);
            Assert.Equal(3, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_FileWithNullSize_TreatsAsZero()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = null, IsDeleted = true },
                new Item { Id = 2, Type = "file", FileSize = 1000, IsDeleted = true }
            }.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(1000, totalSize);
            Assert.Equal(2, fileCount); // Both files counted
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_SingleFolder_CallsRepository()
        {
            // Arrange
            var userId = 1;
            var folderId = 10;
            var items = new List<Item>
            {
                new Item { Id = folderId, Type = "folder", IsDeleted = true }
            }.ToAsyncEnumerable();

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, folderId))
                .ReturnsAsync((5000L, 3));

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(5000, totalSize);
            Assert.Equal(3, fileCount);

            _mockItemRepository.Verify(
                x => x.CalculateArchiveSizeAsync(userId, folderId),
                Times.Once);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_MultipleFolders_SumsCorrectly()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 10, Type = "folder", IsDeleted = true },
                new Item { Id = 20, Type = "folder", IsDeleted = true }
            }.ToAsyncEnumerable();

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, 10))
                .ReturnsAsync((3000L, 2));

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, 20))
                .ReturnsAsync((4000L, 3));

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(7000, totalSize);
            Assert.Equal(5, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_MixedFilesAndFolders_CalculatesCorrectly()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = 1000, IsDeleted = true },
                new Item { Id = 10, Type = "folder", IsDeleted = true },
                new Item { Id = 2, Type = "file", FileSize = 2000, IsDeleted = true }
            }.ToAsyncEnumerable();

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, 10))
                .ReturnsAsync((5000L, 3));

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(8000, totalSize); // 1000 + 5000 + 2000
            Assert.Equal(5, fileCount); // 1 + 3 + 1
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_MixedDeletedAndNotDeleted_OnlyCountsDeleted()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = 1000, IsDeleted = true },
                new Item { Id = 2, Type = "file", FileSize = 2000, IsDeleted = false }, // Skips
                new Item { Id = 3, Type = "file", FileSize = 3000, IsDeleted = true }
            }.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(4000, totalSize); // Only 1000 + 3,000
            Assert.Equal(2, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_FolderNotDeleted_Skips()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 10, Type = "folder", IsDeleted = false }, // Skips
                new Item { Id = 1, Type = "file", FileSize = 1000, IsDeleted = true }
            }.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(1000, totalSize);
            Assert.Equal(1, fileCount);

            // Should not call repository for the deleted folder
            _mockItemRepository.Verify(
                x => x.CalculateArchiveSizeAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_EmptyFolder_IncludesInResult()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 10, Type = "folder", IsDeleted = true }
            }.ToAsyncEnumerable();

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, 10))
                .ReturnsAsync((0L, 0));

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(0, totalSize);
            Assert.Equal(0, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_LargeNumberOfItems_ProcessesAll()
        {
            // Arrange
            var userId = 1;
            var itemsList = Enumerable.Range(1, 100)
                .Select(i => new Item
                {
                    Id = i,
                    Type = "file",
                    FileSize = 1000,
                    IsDeleted = true
                })
                .ToList();

            var items = itemsList.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(100000, totalSize); // 100 * 1000
            Assert.Equal(100, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_NestedFolders_CalculatesRecursively()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 10, Type = "folder", IsDeleted = true },
                new Item { Id = 20, Type = "folder", IsDeleted = true }
            }.ToAsyncEnumerable();

            // First folder contains 3 files
            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, 10))
                .ReturnsAsync((6000L, 3));

            // Second folder contains 2 files
            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, 20))
                .ReturnsAsync((4000L, 2));

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(10000, totalSize);
            Assert.Equal(5, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_ComplexMixture_CalculatesCorrectly()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = 1000, IsDeleted = true },
                new Item { Id = 2, Type = "file", FileSize = 2000, IsDeleted = false }, // Skips
                new Item { Id = 10, Type = "folder", IsDeleted = true },
                new Item { Id = 3, Type = "file", FileSize = null, IsDeleted = true }, // null = 0
                new Item { Id = 20, Type = "folder", IsDeleted = false }, // Skips
                new Item { Id = 4, Type = "file", FileSize = 3000, IsDeleted = true }
            }.ToAsyncEnumerable();

            _mockItemRepository
                .Setup(x => x.CalculateArchiveSizeAsync(userId, 10))
                .ReturnsAsync((5000L, 2));

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            // 1000 (file1) + 0 (file3 null) + 3000 (file4) + 5000 (folder10) = 9000
            // 1 (file1) + 1 (file3) + 1 (file4) + 2 (folder10) = 5
            Assert.Equal(9000, totalSize);
            Assert.Equal(5, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_UnknownType_Ignores()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = 1000, IsDeleted = true },
                new Item { Id = 2, Type = "unknown", IsDeleted = true }, // Uknown type
                new Item { Id = 3, Type = "file", FileSize = 2000, IsDeleted = true }
            }.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(3000, totalSize);
            Assert.Equal(2, fileCount);
        }

        [Fact]
        public async Task CalculateMultipleItemsSizeAsync_ZeroSizeFiles_CountsFiles()
        {
            // Arrange
            var userId = 1;
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", FileSize = 0, IsDeleted = true },
                new Item { Id = 2, Type = "file", FileSize = 0, IsDeleted = true }
            }.ToAsyncEnumerable();

            // Act
            var (totalSize, fileCount) = await _service.CalculateMultipleItemsSizeAsync(userId, items);

            // Assert
            Assert.Equal(0, totalSize);
            Assert.Equal(2, fileCount);
        }

        #endregion
    }
}
