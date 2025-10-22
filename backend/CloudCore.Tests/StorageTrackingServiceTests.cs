using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Implementations;
using CloudCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static CloudCore.Tests.DbRepositoryTests;

namespace CloudCore.Tests
{
    public class StorageTrackingServiceTests
    {
        private readonly DbContextOptions<CloudCoreDbContext> _options;
        private readonly StorageTrackingService _service;
        private readonly CloudCoreDbContext _context;
        private readonly Mock<ISubscriptionService> _mockSubscriptionService = new();
        private readonly Mock<ILogger<StorageTrackingService>> _loggerMock = new();

        public StorageTrackingServiceTests()
        {
            _options = new DbContextOptionsBuilder<CloudCoreDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new CloudCoreDbContext(_options);

            var dbContextFactoryMock = new Mock<IDbContextFactory<CloudCoreDbContext>>();
            dbContextFactoryMock.Setup(f => f.CreateDbContext())
                .Returns(() => new CloudCoreDbContext(_options));

            _service = new StorageTrackingService(
                dbContextFactoryMock.Object,
                _mockSubscriptionService.Object,
                _loggerMock.Object);
        }

        public void Dispose()
        {
            using var context = new CloudCoreDbContext(_options);
            context.Database.EnsureDeleted();
        }

        #region AddToPersonalStorageAsync Tests
        [Fact]
        public async Task AddToPersonalStorageAsync_IncreasesUserStorage()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", Email = "test@gmail.com", PersonalStorageUsedMb = 10, SubscriptionPlan = "free" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _service.AddToPersonalStorageAsync(1, 2 * 1024 * 1024); // 2 MB

            // Assert
            await using var assertContext = new CloudCoreDbContext(_options);
            var updatedUser = await assertContext.Users.FindAsync(1);
            Assert.Equal(12, updatedUser.PersonalStorageUsedMb);
        }

        [Fact]
        public async Task AddToPersonalStorageAsync_IncreasesUserStorage_NullUser_Throws()
        {
            var user = new User { Id = 1, Username = "test", Email = "test@gmail.com", PersonalStorageUsedMb = 10, SubscriptionPlan = "free" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _service.AddToPersonalStorageAsync(999, 2 * 1024 * 1024); // Non-existent user
            });
        }


        #endregion
        [Fact]
        public async Task RemoveFromPersonalStorageAsync_DecreasesUserStorage()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", Email = "test@gmail.com", PersonalStorageUsedMb = 10, SubscriptionPlan = "free" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await _service.RemoveFromPersonalStorageAsync(1, 4 * 1024 * 1024); // 4 MB

            await using var assertContext = new CloudCoreDbContext(_options);
            var updatedUser = await assertContext.Users.FindAsync(1);
            Assert.Equal(6, updatedUser.PersonalStorageUsedMb);
        }

        [Fact]
        public async Task CanAddToPersonalStorageAsync_ReturnsTrueIfWithinLimit()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", Email = "test@gmail.com", PersonalStorageUsedMb = 10, SubscriptionPlan = "free" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            bool canAdd = await _service.CanAddToPersonalStorageAsync(1, 1024 * 1024 * 5); // 5MB
            Assert.True(canAdd);
        }

        [Fact]
        public async Task CanAddToPersonalStorageAsync_ReturnsFalseIfExceedsLimit()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", Email = "test@gmail.com", PersonalStorageUsedMb = 10, SubscriptionPlan = "free" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            bool canAdd = await _service.CanAddToPersonalStorageAsync(1, 1024L * 1024L * 20480L); // 20GB
            Assert.False(canAdd);
        }
    }
}

