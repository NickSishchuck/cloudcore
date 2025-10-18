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

        private Item CreateTestItem(int id, int userId, string name, string type = "file", int? parentId = null, bool isDeleted = false, long? fileSize = 1000)
        {
            return new Item
            {
                Id = id,
                UserId = userId,
                Name = name,
                Type = type,
                ParentId = parentId,
                IsDeleted = isDeleted,
                FileSize = fileSize,
                FilePath = type == "file" ? $"/path/to/{name}" : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
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
                CreateTestItem(3, userId, "nested.txt", "file", 20), // Другой parent
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
            await SeedUsersAsync(new User {Id = userId, Username = userName, Email = userEmail, SubscriptionPlan = "premium" });

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

