using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CloudCore.Domain.Entities;
using CloudCore.Services.Implementations;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloudCore.Tests
{
    public class ItemManager_serviceTests
    {
        private readonly Mock<IItemStorageService> _mockStorage;
        private readonly Mock<ILogger<ItemManagerService>> _mockLogger;
        private readonly ItemManagerService _service;

        public ItemManager_serviceTests()
        {
            _mockStorage = new Mock<IItemStorageService>();
            _mockLogger = new Mock<ILogger<ItemManagerService>>();
            _service = new ItemManagerService(_mockStorage.Object, _mockLogger.Object);
        }

        #region Helpers
        private async IAsyncEnumerable<Item> GetAsyncEnumerable(params Item[] items)
        {
            foreach (var item in items)
            {
                yield return item;
                await Task.Yield();
            }
        }
        #endregion

        #region PrepareItemsForRenaming Tests

        [Fact]
        public async Task PrepareItemsForRenaming_FileItem_UpdatesNameAndFilePath()
        {
            // Arrange
            var fileItem = new Item { Id = 5, Type = "file", Name = "old.txt", FilePath = "old.txt" };
            _mockStorage.Setup(s => s.RenameItemPhysically(fileItem, "new.txt", null)).Returns("new.txt");

            // Act
            var results = new List<Item>();
            await foreach (var result in _service.PrepareItemsForRenaming(fileItem, "new.txt"))
            {
                results.Add(result);
            }

            // Assert
            Assert.Single(results);
            Assert.Equal("new", results[0].Name);
            Assert.Equal("new.txt", results[0].FilePath);
        }

        [Fact]
        public async Task PrepareItemsForRenaming_FolderItem_UpdatesName()
        {
            // Arrange
            var folderItem = new Item { Id = 1, Type = "folder", Name = "OldFolder", UserId = 100 };
            var childFile = new Item { Id = 2, Type = "file", FilePath = "oldfolder/file.txt", UserId = 100 };
            var childItems = GetAsyncEnumerable(childFile);

            _mockStorage.Setup(s => s.GetNewFolderPath(It.IsAny<string>(), "OldFolder", "NewFolder")).Returns("newfolder");
            _mockStorage.Setup(s => s.GetUserStoragePath(100)).Returns("/base/path/");
            _mockStorage.Setup(s => s.GetNewFilePath(childFile.FilePath, "newfolder", "/base/path/")).Returns("newfolder/file.txt");
            _mockStorage.Setup(s => s.RenameItemPhysically(folderItem, "NewFolder", It.IsAny<string>()));

            // Act
            var results = new List<Item>();
            await foreach (var result in _service.PrepareItemsForRenaming(folderItem, "NewFolder", childItems, "/base/path/oldfolder"))
            {
                results.Add(result);
            }

            // Assert
            Assert.Equal(2, results.Count); // 1 child, 1 folder
            Assert.Equal("NewFolder", results[1].Name); // Folder
            Assert.Equal("newfolder/file.txt", results[0].FilePath); // Child file updated
        }


        [Fact]
        public async Task PrepareItemsForRenaming_UnsupportedType_Throws()
        {
            // Arrange
            var unknownItem = new Item { Id = 1, Type = "link", Name = "Link" };

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
            {
                await foreach (var _ in _service.PrepareItemsForRenaming(unknownItem, "Something")) { }
            });
        }

        #endregion

        #region PrepareItemsForMoving Tests
        [Fact]
        public async Task PrepareItemsForMoving_FileItem_UpdatesParentAndPath()
        {
            // Arrange
            var fileItem = new Item { Id = 2, UserId = 42, Type = "file", Name = "file.txt", FilePath = "file.txt" };
            _mockStorage.Setup(s => s.MoveItemPhysically(fileItem, "/dest", null)).Returns("dest/file.txt");

            // Act
            var results = new List<Item>();
            await foreach (var result in _service.PrepareItemsForMoving(fileItem, 8, "/dest", "/src"))
            {
                results.Add(result);
            }

            // Assert
            Assert.Single(results);
            Assert.Equal(8, results[0].ParentId);
            Assert.Equal("dest/file.txt", results[0].FilePath);
        }

        [Fact]
        public async Task PrepareItemsForMoving_FolderItem_UpdatesChildrenFilePath()
        {
            // Arrange
            var folderItem = new Item { Id = 1, Type = "folder", Name = "folder", UserId = 1 };
            var childFile = new Item { Id = 2, Type = "file", Name = "a.txt", FilePath = "folder/a.txt", UserId = 1 };

            var childItems = GetAsyncEnumerable(childFile);

            _mockStorage.Setup(s => s.GetUserStoragePath(1)).Returns("/storage");
            _mockStorage.Setup(s => s.MoveItemPhysically(folderItem, "/dest", "/src"));

            // Act
            var results = new List<Item>();
            await foreach (var result in _service.PrepareItemsForMoving(
                folderItem, 100, "/dest", "/src", childItems))
            {
                results.Add(result);
            }

            // Assert
            Assert.Contains(results, i => i.Id == 1 && i.ParentId == 100);
            Assert.Contains(results, i => i.Id == 2);
        }


        [Fact]
        public async Task PrepareItemsForMoving_UnsupportedType_Throws()
        {
            // Arrange
            var unknownItem = new Item { Id = 1, Type = "link", Name = "Link" };

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
            {
                await foreach (var _ in _service.PrepareItemsForMoving(unknownItem, 42, "/dest", "/src")) { }
            });
        }

        [Fact]
        public async Task PrepareItemsForMoving_NullItem_Throws()
        {
            // Arrange
            Item? nullItem = null;
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in _service.PrepareItemsForMoving(nullItem!, 42, "/dest", "/src")) { }
            });
        }

        [Fact]
        public async Task PrepareItemsForMoving_NullDestinationFolderPath_Throws()
        {
            // Arrange
            var unknownItem = new Item { Id = 1, Type = "file", Name = "file" };
            string? destinationFolder = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in _service.PrepareItemsForMoving(unknownItem, 42, null!, destinationFolder!)) { }
            });
        }

        [Fact]
        public async Task PrepareItemsForMoving_NullSourseFolderPath_Throws()
        {
            // Arrange
            var folderItem = new Item { Id = 1, Type = "folder", Name = "folder" };
            string? sourceFolder = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in _service.PrepareItemsForMoving(folderItem, 42, "/dest", sourceFolder!)) { }
            });
        }

        #endregion

        #region PrepareItemsForSoftDeleteAsync & PrepareItemsForRestoreAsync Tests

        [Fact]
        public async Task PrepareItemsForSoftDeleteAsync_MarksItemsAsDeleted()
        {
            // Arrange
            var item = new Item { Id = 1, IsDeleted = false };
            var items = GetAsyncEnumerable(item);

            // Act
            var resultList = new List<Item>();
            await foreach (var result in _service.PrepareItemsForSoftDeleteAsync(items))
            {
                resultList.Add(result);
            }

            // Assert
            Assert.Single(resultList);
            Assert.True(resultList[0].IsDeleted);
            Assert.NotNull(resultList[0].DeletedAt);
        }

        [Fact]
        public async Task PrepareItemsForRestoreAsync_MarksItemsAsRestored()
        {
            var item = new Item { Id = 1, IsDeleted = true, DeletedAt = DateTime.UtcNow };
            var items = GetAsyncEnumerable(item);

            var resultList = new List<Item>();
            await foreach (var result in _service.PrepareItemsForRestoreAsync(items))
            {
                resultList.Add(result);
            }

            Assert.Single(resultList);
            Assert.False(resultList[0].IsDeleted);
            Assert.Null(resultList[0].DeletedAt);
        }

        #endregion

        #region ProcessUploadAsync

        [Fact]
        public async Task ProcessUploadAsync_CreatesCorrectItem()
        {
            // Arrange
            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.FileName).Returns("photo.jpg");
            formFileMock.Setup(f => f.Length).Returns(456L);
            formFileMock.Setup(f => f.ContentType).Returns("image/jpeg");
            _mockStorage.Setup(s => s.SaveFileAsync(18, "/target", formFileMock.Object)).ReturnsAsync("uploads/photo.jpg");

            // Act
            var item = await _service.ProcessUploadAsync(18, null, formFileMock.Object, "/target");

            // Assert
            Assert.Equal("photo.jpg", item.Name);
            Assert.Equal("file", item.Type);
            Assert.Equal(18, item.UserId);
            Assert.Equal("uploads/photo.jpg", item.FilePath);
            Assert.Equal(456, item.FileSize);
            Assert.Equal("image/jpeg", item.MimeType);
            Assert.False(item.IsDeleted);
        }

        #endregion
    }
}