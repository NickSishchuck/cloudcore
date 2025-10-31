using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloudCore.Tests
{
    public class DbRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<CloudCoreDbContext> _options;
        private readonly Mock<ILogger<DbRepository>> _mockLogger;
        private readonly TestDbContextFactory _contextFactory;
        private readonly DbRepository _repository;

        public DbRepositoryTests()
        {
            var databaseName = $"TestDb_{Guid.NewGuid()}";
            _options = new DbContextOptionsBuilder<CloudCoreDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _mockLogger = new Mock<ILogger<DbRepository>>();
            _contextFactory = new TestDbContextFactory(_options);
            _repository = new DbRepository(_contextFactory, _mockLogger.Object);
        }

        public void Dispose()
        {
            using var context = new CloudCoreDbContext(_options);
            context.Database.EnsureDeleted();
        }


        #region Helper Classes

        public class TestDbContextFactory : IDbContextFactory<CloudCoreDbContext>
        {
            private readonly DbContextOptions<CloudCoreDbContext> _options;

            public TestDbContextFactory(DbContextOptions<CloudCoreDbContext> options)
            {
                _options = options;
            }

            public CloudCoreDbContext CreateDbContext()
            {
                return new CloudCoreDbContext(_options);
            }

            public Task<CloudCoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CloudCoreDbContext(_options));
            }
        }

        #endregion

        #region Helper Methods

        private async Task SeedDataAsync(params Item[] items)
        {
            await using var context = new CloudCoreDbContext(_options);
            context.Items.AddRange(items);
            await context.SaveChangesAsync();
        }

        private async Task SeedUsersAsync(params User[] users)
        {
            await using var context = new CloudCoreDbContext(_options);
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        private Item CreateTestItem(int id, int userId, string name, string type = "file", int? parentId = null, bool isDeleted = false, long? fileSize = 1000, int? teamspace = null, DateTime? dateTime = null, DateTime? deletedAt = null)
        {
            return new Item
            {
                Id = id,
                UserId = userId,
                Name = name,
                Type = type,
                ParentId = parentId,
                TeamspaceId = teamspace,
                IsDeleted = isDeleted,
                FileSize = fileSize,
                FilePath = type == "file" ? $"/path/to/{name}" : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = dateTime ?? DateTime.UtcNow,
                DeletedAt = deletedAt ?? DateTime.UtcNow
            };
        }

        #endregion

        #region GetItemsAsync Tests
        [Fact]
        public async Task GetItemsAsync_ReturnsAllItemsForUser()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file"),
                CreateTestItem(2, userId, "file2.txt", "file"),
                CreateTestItem(3, 2, "other_user_file.txt", "file") // another user's item
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, null, null, false, null, null);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, items.Count());
            Assert.All(items, item => Assert.Equal(userId, item.UserId));
        }

        [Fact]
        public async Task GetItemsAsync_FiltersByParentId()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "folder1", "folder", null),
                CreateTestItem(2, userId, "file_in_root.txt", "file", null),
                CreateTestItem(3, userId, "file_in_folder1.txt", "file", 1),
                CreateTestItem(4, userId, "file_in_folder1_2.txt", "file", 1)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, 1, 1, 30, null, null, false, null, null);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.All(items, item => Assert.Equal(1, item.ParentId));
        }

        [Fact]
        public async Task GetItemsAsync_ExcludesDeletedItems()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "active_file.txt", "file", isDeleted: false),
                CreateTestItem(2, userId, "deleted_file.txt", "file", isDeleted: true)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, null, null, false, null, null);

            // Assert
            Assert.Single(items);
            Assert.Equal("active_file.txt", items.First().Name);
        }

        [Fact]
        public async Task GetItemsAsync_TrashFolder_ReturnsOnlyDeletedItemsAtTopLevel()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "deleted_folder", "folder", null, isDeleted: true),
                CreateTestItem(2, userId, "file_in_deleted_folder.txt", "file", 1, isDeleted: true),
                CreateTestItem(3, userId, "active_file.txt", "file", null, isDeleted: false),
                CreateTestItem(4, userId, "deleted_file_in_active_folder.txt", "file", 5, isDeleted: true),
                CreateTestItem(5, userId, "active_folder", "folder", null, isDeleted: false)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, null, null, true, null, null);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Contains(items, i => i.Name == "deleted_folder");
            Assert.Contains(items, i => i.Name == "deleted_file_in_active_folder.txt");
        }

        [Fact]
        public async Task GetItemsAsync_SearchQuery_FiltersItemsByName()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "document.pdf", "file"),
                CreateTestItem(2, userId, "image.jpg", "file"),
                CreateTestItem(3, userId, "another_document.docx", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, null, null, false, "document", null);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.All(items, item => Assert.Contains("document", item.Name.ToLower()));
        }


        [Fact]
        public async Task GetItemsAsync_SearchQuery_IsCaseInsensitive()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "UPPERCASE.txt", "file"),
                CreateTestItem(2, userId, "lowercase.txt", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, null, null, false, "CASE", null);

            // Assert
            Assert.Equal(2, totalCount);
        }


        [Fact]
        public async Task GetItemsAsync_TeamspaceId_FiltersCorrectly()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "team1_file.txt", "file", teamspace: 1),
                CreateTestItem(2, userId, "team2_file.txt", "file", teamspace: 2),
                CreateTestItem(3, userId, "personal_file.txt", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, null, null, false, null, 1);

            // Assert
            Assert.Single(items);
            Assert.Equal("team1_file.txt", items.First().Name);
        }

        [Fact]
        public async Task GetItemsAsync_SortByName_Ascending()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "zebra.txt", "file"),
                CreateTestItem(2, userId, "apple.txt", "file"),
                CreateTestItem(3, userId, "banana.txt", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "name", "asc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("apple.txt", itemList[0].Name);
            Assert.Equal("banana.txt", itemList[1].Name);
            Assert.Equal("zebra.txt", itemList[2].Name);
        }

        [Fact]
        public async Task GetItemsAsync_SortByName_Descending()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "apple.txt", "file"),
                CreateTestItem(2, userId, "zebra.txt", "file"),
                CreateTestItem(3, userId, "banana.txt", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "name", "desc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("zebra.txt", itemList[0].Name);
            Assert.Equal("banana.txt", itemList[1].Name);
            Assert.Equal("apple.txt", itemList[2].Name);
        }

        [Fact]
        public async Task GetItemsAsync_SortBySize_Ascending()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "large.txt", "file", fileSize: 5000),
                CreateTestItem(2, userId, "small.txt", "file", fileSize: 100),
                CreateTestItem(3, userId, "medium.txt", "file", fileSize: 1000)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "size", "asc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("small.txt", itemList[0].Name);
            Assert.Equal("medium.txt", itemList[1].Name);
            Assert.Equal("large.txt", itemList[2].Name);
        }

        [Fact]
        public async Task GetItemsAsync_SortBySize_Descending()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "small.txt", "file", fileSize: 100),
                CreateTestItem(2, userId, "large.txt", "file", fileSize: 5000),
                CreateTestItem(3, userId, "medium.txt", "file", fileSize: 1000)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "size", "desc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("large.txt", itemList[0].Name);
            Assert.Equal("medium.txt", itemList[1].Name);
            Assert.Equal("small.txt", itemList[2].Name);
        }

        [Fact]
        public async Task GetItemsAsync_SortByModifiedDate_Ascending()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "first.txt", "file", fileSize: 1000),
                CreateTestItem(2, userId, "second.txt", "file", fileSize: 5000),
                CreateTestItem(3, userId, "third.txt", "file", fileSize: 1000)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "modified", "asc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("third.txt", itemList[2].Name);
            Assert.Equal("second.txt", itemList[1].Name);
            Assert.Equal("first.txt", itemList[0].Name);
        }

        [Fact]
        public async Task GetItemsAsync_SortByModifiedDate_Descending()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "first.txt", "file", fileSize: 1000),
                CreateTestItem(2, userId, "second.txt", "file", fileSize: 5000),
                CreateTestItem(3, userId, "third.txt", "file", fileSize: 1000)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "modified", "desc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("third.txt", itemList[0].Name);
            Assert.Equal("second.txt", itemList[1].Name);
            Assert.Equal("first.txt", itemList[2].Name);
        }

        [Fact]
        public async Task GetItemsAsync_SortByCreatedDate_Ascending()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "first.txt", "file", fileSize: 1000),
                CreateTestItem(2, userId, "second.txt", "file", fileSize: 5000),
                CreateTestItem(3, userId, "third.txt", "file", fileSize: 1000)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "createdat", "asc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("third.txt", itemList[2].Name);
            Assert.Equal("second.txt", itemList[1].Name);
            Assert.Equal("first.txt", itemList[0].Name);
        }

        [Fact]
        public async Task GetItemsAsync_SortByCreatedDate_Descending()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "first.txt", "file", fileSize: 1000),
                CreateTestItem(2, userId, "second.txt", "file", fileSize: 5000),
                CreateTestItem(3, userId, "third.txt", "file", fileSize: 1000)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "createdat", "desc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("third.txt", itemList[0].Name);
            Assert.Equal("second.txt", itemList[1].Name);
            Assert.Equal("first.txt", itemList[2].Name);
        }


        [Fact]
        public async Task GetItemsAsync_FoldersAppearFirst()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file"),
                CreateTestItem(2, userId, "folder1", "folder"),
                CreateTestItem(3, userId, "file2.txt", "file"),
                CreateTestItem(4, userId, "folder2", "folder")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, "name", "asc", false, null, null);

            // Assert
            var itemList = items.ToList();
            Assert.Equal("folder", itemList[0].Type);
            Assert.Equal("folder", itemList[1].Type);
            Assert.Equal("file", itemList[2].Type);
            Assert.Equal("file", itemList[3].Type);
        }

        [Fact]
        public async Task GetItemsAsync_Pagination_FirstPage()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file"),
                CreateTestItem(2, userId, "file2.txt", "file"),
                CreateTestItem(3, userId, "file3.txt", "file"),
                CreateTestItem(4, userId, "file4.txt", "file"),
                CreateTestItem(5, userId, "file5.txt", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 2, "name", "asc", false, null, null);

            // Assert
            Assert.Equal(5, totalCount);
            Assert.Equal(2, items.Count());
        }

        [Fact]
        public async Task GetItemsAsync_Pagination_SecondPage()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file"),
                CreateTestItem(2, userId, "file2.txt", "file"),
                CreateTestItem(3, userId, "file3.txt", "file"),
                CreateTestItem(4, userId, "file4.txt", "file"),
                CreateTestItem(5, userId, "file5.txt", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 2, 2, "name", "asc", false, null, null);

            // Assert
            Assert.Equal(5, totalCount);
            Assert.Equal(2, items.Count());
            var itemList = items.ToList();
            Assert.Equal("file3.txt", itemList[0].Name);
            Assert.Equal("file4.txt", itemList[1].Name);
        }

        [Fact]
        public async Task GetItemsAsync_Pagination_LastPage()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file"),
                CreateTestItem(2, userId, "file2.txt", "file"),
                CreateTestItem(3, userId, "file3.txt", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 2, 2, "name", "asc", false, null, null);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Single(items);
            Assert.Equal("file3.txt", items.First().Name);
        }

        [Fact]
        public async Task GetItemsAsync_EmptyResults()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, 2, "other_user_file.txt", "file")
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, null, null, false, null, null);

            // Assert
            Assert.Equal(0, totalCount);
            Assert.Empty(items);
        }

        [Fact]
        public async Task GetItemsAsync_SearchWithDeletedItemsExcluded()
        {
            // Arrange
            int userId = 1;
            await SeedDataAsync(
                CreateTestItem(1, userId, "test_file.txt", "file", isDeleted: false),
                CreateTestItem(2, userId, "test_deleted.txt", "file", isDeleted: true)
            );

            // Act
            var (items, totalCount) = await _repository.GetItemsAsync(userId, null, 1, 30, null, null, false, "test", null);

            // Assert
            Assert.Single(items);
            Assert.Equal("test_file.txt", items.First().Name);
        }



        #endregion

        #region GetDirectChildrenAsync Tests

        [Fact]
        public async Task GetDirectChildrenAsync_ReturnsOnlyDirectChildren()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;

            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file", parentId),
                CreateTestItem(2, userId, "file2.txt", "file", parentId),
                CreateTestItem(3, userId, "nested.txt", "file", 20), // Another parent
                CreateTestItem(4, userId, "folder1", "folder", parentId)
            );

            // Act
            var results = new List<Item>();
            await foreach (var item in _repository.GetDirectChildrenAsync(userId, parentId))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, item => Assert.Equal(parentId, item.ParentId));
        }

        [Fact]
        public async Task GetDirectChildrenAsync_WithItemType_FiltersCorrectly()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;

            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file", parentId),
                CreateTestItem(2, userId, "folder1", "folder", parentId),
                CreateTestItem(3, userId, "file2.txt", "file", parentId)
            );

            // Act
            var results = new List<Item>();
            await foreach (var item in _repository.GetDirectChildrenAsync(userId, parentId, "file"))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, item => Assert.Equal("file", item.Type));
        }

        [Fact]
        public async Task GetDirectChildrenAsync_ExcludesDeleted_ByDefault()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;

            await SeedDataAsync(
                CreateTestItem(1, userId, "active.txt", "file", parentId, isDeleted: false),
                CreateTestItem(2, userId, "deleted.txt", "file", parentId, isDeleted: true)
            );

            // Act
            var results = new List<Item>();
            await foreach (var item in _repository.GetDirectChildrenAsync(userId, parentId))
            {
                results.Add(item);
            }

            // Assert
            Assert.Single(results);
            Assert.Equal("active.txt", results[0].Name);
        }

        [Fact]
        public async Task GetDirectChildrenAsync_IncludesDeleted_WhenRequested()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;

            await SeedDataAsync(
                CreateTestItem(1, userId, "active.txt", "file", parentId, isDeleted: false),
                CreateTestItem(2, userId, "deleted.txt", "file", parentId, isDeleted: true)
            );

            // Act
            var results = new List<Item>();
            await foreach (var item in _repository.GetDirectChildrenAsync(userId, parentId, includeDeleted: true))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task GetDirectChildrenAsync_RootLevel_ReturnsRootItems()
        {
            // Arrange
            int userId = 1;

            await SeedDataAsync(
                CreateTestItem(1, userId, "root1.txt", "file", null),
                CreateTestItem(2, userId, "root2.txt", "file", null),
                CreateTestItem(3, userId, "nested.txt", "file", 10)
            );

            // Act
            var results = new List<Item>();
            await foreach (var item in _repository.GetDirectChildrenAsync(userId, null))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, item => Assert.Null(item.ParentId));
        }

        #endregion

        #region GetItemAsync Tests

        [Fact]
        public async Task GetItemAsync_ItemExists_ReturnsItem()
        {
            // Arrange
            int userId = 1;
            int itemId = 5;

            await SeedDataAsync(
                CreateTestItem(itemId, userId, "test.txt", "file")
            );

            // Act
            var result = await _repository.GetItemAsync(userId, itemId, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemId, result.Id);
            Assert.Equal("test.txt", result.Name);
        }

        [Fact]
        public async Task GetItemAsync_ItemNotFound_ReturnsNull()
        {
            // Arrange
            int userId = 1;
            int itemId = 999;

            // Act
            var result = await _repository.GetItemAsync(userId, itemId, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetItemAsync_WrongUser_ReturnsNull()
        {
            // Arrange
            int userId = 1;
            int wrongUserId = 2;
            int itemId = 5;

            await SeedDataAsync(
                CreateTestItem(itemId, userId, "test.txt", "file")
            );

            // Act
            var result = await _repository.GetItemAsync(wrongUserId, itemId, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetItemAsync_WithItemType_FiltersCorrectly()
        {
            // Arrange
            int userId = 1;
            int itemId = 5;

            await SeedDataAsync(
                CreateTestItem(itemId, userId, "folder", "folder")
            );

            // Act
            var resultFile = await _repository.GetItemAsync(userId, itemId, "file");
            var resultFolder = await _repository.GetItemAsync(userId, itemId, "folder");

            // Assert
            Assert.Null(resultFile);
            Assert.NotNull(resultFolder);
        }

        #endregion

        #region GetItemByNameAsync Tests

        [Fact]
        public async Task GetItemByNameAsync_ItemExists_ReturnsItem()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;

            await SeedDataAsync(
                CreateTestItem(1, userId, "test.txt", "file", parentId)
            );

            // Act
            var result = await _repository.GetItemByNameAsync(userId, "test.txt", parentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.txt", result.Name);
        }

        [Fact]
        public async Task GetItemByNameAsync_CaseInsensitive_FindsItem()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;

            await SeedDataAsync(
                CreateTestItem(1, userId, "Test.txt", "file", parentId)
            );

            // Act
            var result = await _repository.GetItemByNameAsync(userId, "TEST.TXT", parentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test.txt", result.Name);
        }

        [Fact]
        public async Task GetItemByNameAsync_RootLevel_FindsRootItem()
        {
            // Arrange
            int userId = 1;

            await SeedDataAsync(
                CreateTestItem(1, userId, "root.txt", "file", null)
            );

            // Act
            var result = await _repository.GetItemByNameAsync(userId, "root.txt", null);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.ParentId);
        }

        #endregion

        #region GetDeletedItemAsync Tests

        [Fact]
        public async Task GetDeletedItemAsync_ItemExists_ReturnsItem()
        {
            // Arrange
            int userId = 1;
            int itemId = 5;
            await SeedDataAsync(
                CreateTestItem(itemId, userId, "deleted.txt", "file", isDeleted: true)
            );
            // Act
            var result = await _repository.GetDeletedItemAsync(userId, itemId);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemId, result.Id);
            Assert.True(result.IsDeleted);
        }

        [Fact]
        public async Task GetDeletedItemAsync_ItemNotFound_ReturnsNull()
        {
            // Arrange
            int userId = 1;
            int itemId = 999;
            // Act
            var result = await _repository.GetDeletedItemAsync(userId, itemId);
            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetItemsByIdsForUserAsync Tests

        [Fact]
        public async Task GetItemsByIdsForUserAsync_ReturnsMatchingItems()
        {
            // Arrange
            int userId = 1;
            var itemIds = new List<int> { 1, 2, 3 };

            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file"),
                CreateTestItem(2, userId, "file2.txt", "file"),
                CreateTestItem(3, userId, "file3.txt", "file"),
                CreateTestItem(4, userId, "file4.txt", "file")
            );

            // Act
            var results = new List<Item>();
            await foreach (var item in _repository.GetItemsByIdsForUserAsync(userId, itemIds))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, item => Assert.Contains(item.Id, itemIds));
        }

        [Fact]
        public async Task GetItemsByIdsForUserAsync_ExcludesDeletedItems()
        {
            // Arrange
            int userId = 1;
            var itemIds = new List<int> { 1, 2 };

            await SeedDataAsync(
                CreateTestItem(1, userId, "active.txt", "file", isDeleted: false),
                CreateTestItem(2, userId, "deleted.txt", "file", isDeleted: true)
            );

            // Act
            var results = new List<Item>();
            await foreach (var item in _repository.GetItemsByIdsForUserAsync(userId, itemIds))
            {
                results.Add(item);
            }

            // Assert
            Assert.Single(results);
            Assert.Equal(1, results[0].Id);
        }

        [Fact]
        public async Task GetItemsByIdsForUserAsync_EmptyList_ReturnsEmpty()
        {
            // Arrange
            int userId = 1;
            var itemIds = new List<int>();

            // Act
            var results = new List<Item>();
            await foreach (var item in _repository.GetItemsByIdsForUserAsync(userId, itemIds))
            {
                results.Add(item);
            }

            // Assert
            Assert.Empty(results);
        }

        #endregion

        #region GetDeletedItemsByIdsAsync Tests
        [Fact]
        public async Task GetDeletedItemsByIdsAsync_ReturnsOnlyDeletedItems()
        {
            // Arrange
            int userId = 1;
            var itemIds = new List<int> { 1, 2, 3 };
            await SeedDataAsync(
                CreateTestItem(1, userId, "deleted1.txt", "file", isDeleted: true),
                CreateTestItem(2, userId, "active.txt", "file", isDeleted: false),
                CreateTestItem(3, userId, "deleted2.txt", "file", isDeleted: true)
            );
            // Act
            var results = await _repository.GetDeletedItemsByIdsAsync(itemIds);
            // Assert
            Assert.Equal(2, results.ToList().Count);
            Assert.All(results, item => Assert.True(item.IsDeleted));
        }

        [Fact]
        public async Task GetDeletedItemsByIdsAsync_ReturnsEmptyEnumerableIfPassingValueIsEmpty()
        {
            // Arrange
            var itemIds = new List<int> { };

            // Act
            var results = await _repository.GetDeletedItemsByIdsAsync(itemIds);
            // Assert
            Assert.Equal(Enumerable.Empty<Item>(), results);
        }
        #endregion


        #region IsNameUniqueAsync Tests

        [Fact]
        public async Task IsNameUniqueAsync_NameIsUnique_ReturnsTrue()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;
            string itemName = "unique.txt";
            string itemType = "file";

            await SeedDataAsync(
                CreateTestItem(1, userId, "existing.txt", itemType, parentId)
            );
            // Act
            var result = await _repository.IsNameUniqueAsync(itemName, userId, itemType, parentId);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsNameUniqueAsync_NameIsUnique_ReturnsFalse()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;
            string itemName = "existing.txt";
            string itemType = "file";

            await SeedDataAsync(
                CreateTestItem(1, userId, "existing.txt", itemType, parentId)
            );
            // Act
            var result = await _repository.IsNameUniqueAsync(itemName, userId, itemType, parentId);
            // Assert
            Assert.False(result);
        }

        #endregion

        #region ItemExistsAsync Tests

        [Fact]
        public async Task ItemExistsAsync_ItemExists_ReturnsTrue()
        {
            // Arrange
            int userId = 1;
            int itemId = 5;

            await SeedDataAsync(
                CreateTestItem(itemId, userId, "test.txt", "file")
            );

            // Act
            var result = await _repository.ItemExistsAsync(itemId, userId, null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ItemExistsAsync_ItemExistsWithType_ReturnsTrue()
        {
            // Arrange
            int userId = 1;
            int itemId = 5;

            await SeedDataAsync(
                CreateTestItem(itemId, userId, "test.txt", "file")
            );

            // Act
            var result = await _repository.ItemExistsAsync(itemId, userId, "file");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ItemExistsAsync_ItemNotFound_ReturnsFalse()
        {
            // Arrange
            int userId = 1;
            int itemId = 999;

            // Act
            var result = await _repository.ItemExistsAsync(itemId, userId, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ItemExistsAsync_DeletedItem_ReturnsFalse()
        {
            // Arrange
            int userId = 1;
            int itemId = 5;

            await SeedDataAsync(
                CreateTestItem(itemId, userId, "deleted.txt", "file", isDeleted: true)
            );

            // Act
            var result = await _repository.ItemExistsAsync(itemId, userId, null);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CountExistingItemsAsync Tests

        [Fact]
        public async Task CountExistingItemsAsync_ReturnsCorrectCount()
        {
            // Arrange
            int userId = 1;
            var itemIds = new List<int> { 1, 2, 3, 4 };

            await SeedDataAsync(
                CreateTestItem(1, userId, "file1.txt", "file"),
                CreateTestItem(2, userId, "file2.txt", "file"),
                CreateTestItem(3, userId, "file3.txt", "file")
            );

            // Act
            var count = await _repository.CountExistingItemsAsync(itemIds, userId);

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task CountExistingItemsAsync_ExcludesDeleted()
        {
            // Arrange
            int userId = 1;
            var itemIds = new List<int> { 1, 2 };

            await SeedDataAsync(
                CreateTestItem(1, userId, "active.txt", "file", isDeleted: false),
                CreateTestItem(2, userId, "deleted.txt", "file", isDeleted: true)
            );

            // Act
            var count = await _repository.CountExistingItemsAsync(itemIds, userId);

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CountExistingItemsAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            int userId = 1;
            var itemIds = new List<int>();

            // Act
            var count = await _repository.CountExistingItemsAsync(itemIds, userId);

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region DoesItemExistByNameAsync Tests

        [Fact]
        public async Task DoesItemExistByNameAsync_DuplicateExists_ReturnsTrue()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;

            await SeedDataAsync(
                CreateTestItem(1, userId, "duplicate.txt", "file", parentId)
            );

            // Act
            var result = await _repository.DoesItemExistByNameAsync("duplicate.txt", "file", userId, parentId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DoesItemExistByNameAsync_NoDuplicate_ReturnsFalse()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;

            // Act
            var result = await _repository.DoesItemExistByNameAsync("unique.txt", "file", userId, parentId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DoesItemExistByNameAsync_ExcludesSpecificItem()
        {
            // Arrange
            int userId = 1;
            int parentId = 10;
            int excludeId = 1;

            await SeedDataAsync(
                CreateTestItem(excludeId, userId, "test.txt", "file", parentId)
            );

            // Act
            var result = await _repository.DoesItemExistByNameAsync("test.txt", "file", userId, parentId, excludeId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region AddItemInTransactionAsync Tests

        [Fact]
        public async Task AddItemInTransactionAsync_AddsItemSuccessfully()
        {
            // Arrange
            var item = CreateTestItem(1, 1, "new.txt", "file");

            // Act
            await _repository.AddItemInTranscationAsync(item);

            // Assert
            await using var context = new CloudCoreDbContext(_options);
            var addedItem = await context.Items.FindAsync(1);
            Assert.NotNull(addedItem);
            Assert.Equal("new.txt", addedItem.Name);
        }

        #endregion

        #region GetExpiredItemIdsAsync Tests
        [Fact]
        public async Task GetExpiredItemIdsAsync_ReturnsExpiredItemIds()
        {
            // Arrange
            int userId = 1;
            var thresholdDate = DateTime.UtcNow;
            var oldDate = thresholdDate.AddDays(-5);

            await SeedDataAsync(
                CreateTestItem(1, userId, "ExpiredItem1", "folder", null, isDeleted: true, deletedAt: oldDate),
                CreateTestItem(2, userId, "ExpiredItem2", "folder", null, isDeleted: true, deletedAt: oldDate.AddDays(-1)),
                CreateTestItem(3, userId, "RecentItem", "folder", null, isDeleted: true, deletedAt: thresholdDate.AddDays(1)),
                CreateTestItem(4, userId, "NotDeletedItem", "folder", null, isDeleted: false)
            );

            // Act
            var result = await _repository.GetExpiredItemIdsAsync(thresholdDate);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.DoesNotContain(3, result);
            Assert.DoesNotContain(4, result);
        }

        [Fact]
        public async Task GetExpiredItemIdsAsync_ReturnsEmptyList_WhenNoExpiredItems()
        {
            // Arrange
            int userId = 1;
            var thresholdDate = DateTime.UtcNow;

            await SeedDataAsync(
                CreateTestItem(1, userId, "RecentItem", "folder", null, isDeleted: true, deletedAt: thresholdDate.AddDays(1)),
                CreateTestItem(2, userId, "NotDeletedItem", "folder", null, isDeleted: false)
            );

            // Act
            var result = await _repository.GetExpiredItemIdsAsync(thresholdDate);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetExpiredItemIdsAsync_ReturnsOnlyDeletedItems()
        {
            // Arrange
            int userId = 1;
            var thresholdDate = DateTime.UtcNow;
            var oldDate = thresholdDate.AddDays(-5);

            await SeedDataAsync(
                CreateTestItem(1, userId, "DeletedExpired", "folder", null, isDeleted: true, deletedAt: oldDate),
                CreateTestItem(2, userId, "NotDeletedExpired", "folder", null, isDeleted: false, deletedAt: oldDate)
            );

            // Act
            var result = await _repository.GetExpiredItemIdsAsync(thresholdDate);

            // Assert
            Assert.Single(result);
            Assert.Contains(1, result);
            Assert.DoesNotContain(2, result);
        }

        [Fact]
        public async Task GetExpiredItemIdsAsync_IncludesItemsDeletedExactlyAtThreshold()
        {
            // Arrange
            int userId = 1;
            var thresholdDate = new DateTime(2025, 10, 1, 12, 0, 0, DateTimeKind.Utc);

            await SeedDataAsync(
                CreateTestItem(1, userId, "ExactThreshold", "folder", null, isDeleted: true, deletedAt: thresholdDate),
                CreateTestItem(2, userId, "BeforeThreshold", "folder", null, isDeleted: true, deletedAt: thresholdDate.AddMinutes(-1)),
                CreateTestItem(3, userId, "AfterThreshold", "folder", null, isDeleted: true, deletedAt: thresholdDate.AddMinutes(1))
            );

            // Act
            var result = await _repository.GetExpiredItemIdsAsync(thresholdDate);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.DoesNotContain(3, result);
        }

        [Fact]
        public async Task GetExpiredItemIdsAsync_WorksAcrossMultipleUsers()
        {
            // Arrange
            var thresholdDate = DateTime.UtcNow;
            var oldDate = thresholdDate.AddDays(-5);

            await SeedDataAsync(
                CreateTestItem(1, 1, "User1Expired", "folder", null, isDeleted: true, deletedAt: oldDate),
                CreateTestItem(2, 2, "User2Expired", "folder", null, isDeleted: true, deletedAt: oldDate),
                CreateTestItem(3, 3, "User3Recent", "folder", null, isDeleted: true, deletedAt: thresholdDate.AddDays(1))
            );

            // Act
            var result = await _repository.GetExpiredItemIdsAsync(thresholdDate);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.DoesNotContain(3, result);
        }

        [Fact]
        public async Task GetExpiredItemIdsAsync_HandlesLargeNumberOfItems()
        {
            // Arrange
            int userId = 1;
            var thresholdDate = DateTime.UtcNow;
            var oldDate = thresholdDate.AddDays(-10);

            var items = new List<Item>();
            for (int i = 1; i <= 100; i++)
            {
                items.Add(CreateTestItem(i, userId, $"Item{i}", "folder", null,
                    isDeleted: true,
                    deletedAt: i % 2 == 0 ? oldDate : thresholdDate.AddDays(1)));
            }

            await SeedDataAsync(items.ToArray());

            // Act
            var result = await _repository.GetExpiredItemIdsAsync(thresholdDate);

            // Assert
            Assert.Equal(50, result.Count); // Половина expired
            Assert.All(result, id => Assert.True(id % 2 == 0));
        }

        [Theory]
        [InlineData(-30)] // 30 дней назад
        [InlineData(-7)]  // неделя назад
        [InlineData(-1)]  // вчера
        [InlineData(0)]   // сегодня
        public async Task GetExpiredItemIdsAsync_WorksWithDifferentThresholds(int daysOffset)
        {
            // Arrange
            int userId = 1;
            var baseDate = new DateTime(2025, 10, 15, 12, 0, 0, DateTimeKind.Utc);
            var thresholdDate = baseDate.AddDays(daysOffset);

            await SeedDataAsync(
                CreateTestItem(1, userId, "VeryOld", "folder", null, isDeleted: true, deletedAt: baseDate.AddDays(-31)),
                CreateTestItem(2, userId, "Old", "folder", null, isDeleted: true, deletedAt: baseDate.AddDays(-8)),
                CreateTestItem(3, userId, "Recent", "folder", null, isDeleted: true, deletedAt: baseDate.AddDays(-2)),
                CreateTestItem(4, userId, "Today", "folder", null, isDeleted: true, deletedAt: baseDate)
            );

            // Act
            var result = await _repository.GetExpiredItemIdsAsync(thresholdDate);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, id => Assert.InRange(id, 1, 4));
        }


        #endregion

        #region DeleteItemPermanentlyAsync Tests

        [Fact]
        public async Task DeleteItemPermanentlyAsync_RemovesItem()
        {
            // Arrange
            var item = CreateTestItem(1, 1, "delete-me.txt", "file");
            await SeedDataAsync(item);

            // Act
            await _repository.DeleteItemPermanentlyAsync(item);

            // Assert
            await using var context = new CloudCoreDbContext(_options);
            var deletedItem = await context.Items.FindAsync(1);
            Assert.Null(deletedItem);
        }

        #endregion


        #region GetTeamspaceLimitsAsync Tests

        [Fact]
        public async Task GetTeamspaceLimitsAsync_FreePlan_ReturnsCorrectLimits()
        {
            // Arrange
            int userId = 1;
            string userName = "free_user";
            string userEmail = "free_user_email@gmail.com";
            await SeedUsersAsync(new User { Id = userId, Username = userName, Email = userEmail, SubscriptionPlan = "free" });

            // Act
            var limits = await _repository.GetTeamspaceLimitsAsync(userId);

            // Assert
            Assert.Equal(5120, limits.StorageLimitMb);
            Assert.Equal(5, limits.MemberLimit);
            Assert.Equal(2, limits.MaxTeamspaces);
        }

        [Fact]
        public async Task GetTeamspaceLimitsAsync_PremiumPlan_ReturnsCorrectLimits()
        {
            // Arrange
            int userId = 1;
            string userName = "premium_user";
            string userEmail = "premium_user_email@gmail.com";
            await SeedUsersAsync(new User { Id = userId, Username = userName, Email = userEmail, SubscriptionPlan = "premium" });

            // Act
            var limits = await _repository.GetTeamspaceLimitsAsync(userId);

            // Assert
            Assert.Equal(51200, limits.StorageLimitMb);
            Assert.Equal(25, limits.MemberLimit);
            Assert.Equal(10, limits.MaxTeamspaces);
        }

        [Fact]
        public async Task GetTeamspaceLimitsAsync_EnterprisePlan_ReturnsUnlimitedTeamspaces()
        {
            // Arrange
            int userId = 1;
            string userName = "enterprise_user";
            string userEmail = "enterprise@gmail.com";
            await SeedUsersAsync(new User { Id = userId, Username = userName, Email = userEmail, SubscriptionPlan = "enterprise" });

            // Act
            var limits = await _repository.GetTeamspaceLimitsAsync(userId);

            // Assert
            Assert.Equal(512000, limits.StorageLimitMb);
            Assert.Equal(100, limits.MemberLimit);
            Assert.Equal(-1, limits.MaxTeamspaces);
        }

        [Fact]
        public async Task GetTeamspaceLimitsAsync_UserNotFound_ThrowsException()
        {
            // Arrange
            int userId = 999;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _repository.GetTeamspaceLimitsAsync(userId)
            );
        }

        #endregion
    }
}