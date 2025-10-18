using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudCore.Domain.Entities;
using CloudCore.Services.Implementations;
using CloudCore.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloudCore.Tests
{
    public class TrashCleanupServiceTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<IItemStorageService> _mockItemStorageService;
        private readonly Mock<ILogger<TrashCleanupService>> _mockLogger;
        private readonly TrashCleanupService _service;

        public TrashCleanupServiceTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockItemStorageService = new Mock<IItemStorageService>();
            _mockLogger = new Mock<ILogger<TrashCleanupService>>();

            _service = new TrashCleanupService(_mockItemStorageService.Object, _mockLogger.Object, _mockItemRepository.Object);
        }

        #region CleanupExpiredItemsAsync Tests

        [Fact]
        public async Task CleanupExpiredItemsAsync_NoExpiredItems_ReturnsZero()
        {
            // Arrange
            var emptyList = new List<int>();
            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(0, result);
            _mockItemRepository.Verify(x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()),Times.Never);
        }

        [Fact]
        public async Task CleanupExpiredItemsAsync_SmallBatch_ProcessesAllItems()
        {
            // Arrange
            var expiredIds = new List<int> { 1, 2, 3 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", UserId = 1, FilePath = "file1.txt" },
                new Item { Id = 2, Type = "file", UserId = 1, FilePath = "file2.txt" },
                new Item { Id = 3, Type = "folder", UserId = 1, Name = "folder1" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(3);

            _mockItemRepository
                .Setup(x => x.GetFolderPathAsync(It.IsAny<Item>()))
                .ReturnsAsync("folder1");

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(3, result);
            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(It.IsAny<Item>(), It.IsAny<string>()),
                Times.Exactly(3));
        }


        [Fact]
        public async Task CleanupExpiredItemsAsync_LargeBatch_ProcessesInChunks()
        {
            // Arrange - make 1000 elements
            var expiredIds = Enumerable.Range(1, 1000).ToList();
            var items = expiredIds.Select(id => new Item
            {
                Id = id,
                Type = "file",
                UserId = 1,
                FilePath = $"file{id}.txt"
            }).ToList();

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => items.Where(i => ids.Contains(i.Id)).ToList());

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => ids.Count);

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(1000, result);

            _mockItemRepository.Verify(
                x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task CleanupExpiredItemsAsync_ExactlyOneBatch_ProcessesCorrectly()
        {
            // Arrange
            var expiredIds = Enumerable.Range(1, 500).ToList();
            var items = expiredIds.Select(id => new Item
            {
                Id = id,
                Type = "file",
                UserId = 1,
                FilePath = $"file{id}.txt"
            }).ToList();

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(500);

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(500, result);
            _mockItemRepository.Verify(
                x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()),
                Times.Once);
        }

        [Fact]
        public async Task CleanupExpiredItemsAsync_MultipleBatches_SumsCorrectly()
        {
            // Arrange
            var expiredIds = Enumerable.Range(1, 750).ToList();
            var items = expiredIds.Select(id => new Item
            {
                Id = id,
                Type = "file",
                UserId = 1,
                FilePath = $"file{id}.txt"
            }).ToList();

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => items.Where(i => ids.Contains(i.Id)).ToList());

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => ids.Count);

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(750, result);
        }

        #endregion

        #region ProcessBatchAsync Tests

        [Fact]
        public async Task ProcessBatchAsync_FileItems_DeletesInCorrectOrder()
        {
            // Arrange
            var batchIds = new List<int> { 1, 2 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", UserId = 1, FilePath = "file1.txt" },
                new Item { Id = 2, Type = "file", UserId = 1, FilePath = "file2.txt" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ReturnsAsync(2);

            // Act
            await _service.CleanupExpiredItemsAsync();

            // Assert
            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(It.IsAny<Item>(), null),
                Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessBatchAsync_FolderItems_GetsFolderPath()
        {
            // Arrange
            var batchIds = new List<int> { 1 };
            var folderItem = new Item { Id = 1, Type = "folder", UserId = 1, Name = "folder1" };
            var items = new List<Item> { folderItem };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.GetFolderPathAsync(folderItem))
                .ReturnsAsync("path/to/folder1");

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ReturnsAsync(1);

            // Act
            await _service.CleanupExpiredItemsAsync();

            // Assert
            _mockItemRepository.Verify(
                x => x.GetFolderPathAsync(folderItem),
                Times.Once);

            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(folderItem, "path/to/folder1"),
                Times.Once);
        }

        [Fact]
        public async Task ProcessBatchAsync_MixedItems_ProcessesFilesBeforeFolders()
        {
            // Arrange
            var batchIds = new List<int> { 1, 2, 3, 4 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "folder", UserId = 1, Name = "folder1" },
                new Item { Id = 2, Type = "file", UserId = 1, FilePath = "file1.txt" },
                new Item { Id = 3, Type = "folder", UserId = 1, Name = "folder2" },
                new Item { Id = 4, Type = "file", UserId = 1, FilePath = "file2.txt" }
            };

            var deletionOrder = new List<string>();

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.GetFolderPathAsync(It.IsAny<Item>()))
                .ReturnsAsync("folder/path");

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ReturnsAsync(4);

            _mockItemStorageService
                .Setup(x => x.DeleteItemPhysically(It.IsAny<Item>(), It.IsAny<string>()))
                .Callback<Item, string>((item, path) => deletionOrder.Add(item.Type));

            // Act
            await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(4, deletionOrder.Count);


            Assert.Equal("file", deletionOrder[0]);
            Assert.Equal("file", deletionOrder[1]);
            Assert.Equal("folder", deletionOrder[2]);
            Assert.Equal("folder", deletionOrder[3]);
        }

        [Fact]
        public async Task ProcessBatchAsync_PhysicalDeleteFails_ContinuesProcessing()
        {
            // Arrange
            var batchIds = new List<int> { 1, 2 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", UserId = 1, FilePath = "file1.txt" },
                new Item { Id = 2, Type = "file", UserId = 1, FilePath = "file2.txt" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemStorageService
                .Setup(x => x.DeleteItemPhysically(items[0], null))
                .Throws(new Exception("Physical delete failed"));

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ReturnsAsync(2);

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(2, result);

            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(It.IsAny<Item>(), null),
                Times.Exactly(2));

            _mockItemRepository.Verify(
                x => x.DeleteItemsByIdsAsync(batchIds),
                Times.Once);
        }

        [Fact]
        public async Task ProcessBatchAsync_DatabaseDeleteFails_ReturnsZero()
        {
            // Arrange
            var batchIds = new List<int> { 1, 2 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", UserId = 1, FilePath = "file1.txt" },
                new Item { Id = 2, Type = "file", UserId = 1, FilePath = "file2.txt" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ProcessBatchAsync_DatabaseDeleteFails_PhysicalDeletesStillExecuted()
        {
            // Arrange
            var batchIds = new List<int> { 1 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", UserId = 1, FilePath = "file1.txt" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            await _service.CleanupExpiredItemsAsync();

            // Assert
            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(It.IsAny<Item>(), null),
                Times.Once);
        }

        #endregion

        #region DeletePhysicalItemsAsync Tests

        [Fact]
        public async Task DeletePhysicalItemsAsync_MultipleFiles_DeletesAll()
        {
            // Arrange
            var batchIds = new List<int> { 1, 2, 3 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", UserId = 1, FilePath = "file1.txt" },
                new Item { Id = 2, Type = "file", UserId = 1, FilePath = "file2.txt" },
                new Item { Id = 3, Type = "file", UserId = 1, FilePath = "file3.txt" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ReturnsAsync(3);

            // Act
            await _service.CleanupExpiredItemsAsync();

            // Assert
            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(It.Is<Item>(i => i.Type == "file"), null),
                Times.Exactly(3));
        }

        [Fact]
        public async Task DeletePhysicalItemsAsync_MultipleFolders_DeletesAll()
        {
            // Arrange
            var batchIds = new List<int> { 1, 2 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "folder", UserId = 1, Name = "folder1" },
                new Item { Id = 2, Type = "folder", UserId = 1, Name = "folder2" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.GetFolderPathAsync(It.IsAny<Item>()))
                .ReturnsAsync((Item item) => $"path/to/{item.Name}");

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ReturnsAsync(2);

            // Act
            await _service.CleanupExpiredItemsAsync();

            // Assert
            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(
                    It.Is<Item>(i => i.Type == "folder"),
                    It.IsAny<string>()),
                Times.Exactly(2));

            _mockItemRepository.Verify(
                x => x.GetFolderPathAsync(It.IsAny<Item>()),
                Times.Exactly(2));
        }


        [Fact]
        public async Task DeletePhysicalItemsAsync_GetFolderPathFails_ContinuesWithoutPath()
        {
            // Arrange
            var batchIds = new List<int> { 1 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "folder", UserId = 1, Name = "folder1" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.GetFolderPathAsync(It.IsAny<Item>()))
                .ThrowsAsync(new Exception("GetFolderPath failed"));

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ReturnsAsync(1);

            // Act & Assert
            var result = await _service.CleanupExpiredItemsAsync();
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DeletePhysicalItemsAsync_EmptyList_DoesNothing()
        {
            // Arrange
            var emptyList = new List<int>();

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(emptyList);

            // Act
            await _service.CleanupExpiredItemsAsync();

            // Assert
            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(It.IsAny<Item>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task DeletePhysicalItemsAsync_OneItemFails_ContinuesWithOthers()
        {
            // Arrange
            var batchIds = new List<int> { 1, 2, 3 };
            var items = new List<Item>
            {
                new Item { Id = 1, Type = "file", UserId = 1, FilePath = "file1.txt" },
                new Item { Id = 2, Type = "file", UserId = 1, FilePath = "file2.txt" },
                new Item { Id = 3, Type = "file", UserId = 1, FilePath = "file3.txt" }
            };

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(batchIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(batchIds))
                .ReturnsAsync(items);

            _mockItemStorageService
                .Setup(x => x.DeleteItemPhysically(items[1], null))
                .Throws(new Exception("Delete failed"));

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(batchIds))
                .ReturnsAsync(3);

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(3, result);

            _mockItemStorageService.Verify(
                x => x.DeleteItemPhysically(It.IsAny<Item>(), null),
                Times.Exactly(3));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task CleanupExpiredItemsAsync_ExactlyBatchSizePlus1_ProcessesTwoBatches()
        {
            var expiredIds = Enumerable.Range(1, 501).ToList();
            var items = expiredIds.Select(id => new Item
            {
                Id = id,
                Type = "file",
                UserId = 1,
                FilePath = $"file{id}.txt"
            }).ToList();

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => items.Where(i => ids.Contains(i.Id)).ToList());

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => ids.Count);

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(501, result);

            _mockItemRepository.Verify(
                x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task CleanupExpiredItemsAsync_AllBatchesFail_ReturnsZero()
        {
            // Arrange
            var expiredIds = Enumerable.Range(1, 10).ToList();
            var items = expiredIds.Select(id => new Item
            {
                Id = id,
                Type = "file",
                UserId = 1,
                FilePath = $"file{id}.txt"
            }).ToList();

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(items);

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(It.IsAny<List<int>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task CleanupExpiredItemsAsync_PartialBatchSuccess_ReturnsSumOfSuccessful()
        {

            var expiredIds = Enumerable.Range(1, 750).ToList();
            var items = expiredIds.Select(id => new Item
            {
                Id = id,
                Type = "file",
                UserId = 1,
                FilePath = $"file{id}.txt"
            }).ToList();

            var callCount = 0;

            _mockItemRepository
                .Setup(x => x.GetExpiredItemIdsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expiredIds);

            _mockItemRepository
                .Setup(x => x.GetDeletedItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => items.Where(i => ids.Contains(i.Id)).ToList());

            _mockItemRepository
                .Setup(x => x.DeleteItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) =>
                {
                    callCount++;
                    if (callCount == 1)
                        return 500;
                    throw new Exception("Database error");
                });

            // Act
            var result = await _service.CleanupExpiredItemsAsync();

            // Assert
            Assert.Equal(500, result);
        }

        #endregion
    }



}
