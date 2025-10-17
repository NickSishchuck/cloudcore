using CloudCore.Common.Errors;
using CloudCore.Services.Implementations;
using CloudCore.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloudCore.Tests;

public class ValidationServiceTests
{
    private readonly Mock<ILogger<ValidationService>> _mockLogger;
    private readonly Mock<IItemRepository> _mockItemRepository;
    private readonly ValidationService _service; // system under test

    public ValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ValidationService>>();
        _mockItemRepository = new Mock<IItemRepository>();
        _service = new ValidationService(_mockLogger.Object, _mockItemRepository.Object);
    }

    #region Helper Methods

    private Mock<IFormFile> CreateMockFile(string fileName, long length)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(length);
        return mockFile;
    }

    #endregion

    #region ValidateFile Tests
    [Fact]
    public void ValidateFile_ShouldReturnFailure_WhenFileIsNull()
    {
        // Act
        var result = _service.ValidateFile(null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.FILE_REQUIRED);
        result.ErrorMessage.Should().Be("File is required");
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("image.png")]
    [InlineData("video.mp4")]
    [InlineData("archive.zip")]
    public void ValidateFile_ShouldSucceed_WhenFileIsValid(string fileName)
    {
        // Arrange
        var file = CreateMockFile(fileName, 1024);

        // Act
        var result = _service.ValidateFile(file.Object);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFile_ShouldReturnFailure_WhenFileTooLarge()
    {
        // Arrange
        var file = CreateMockFile("large.pdf", 3L * 1024 * 1024 * 1024); // 3 GB

        // Act
        var result = _service.ValidateFile(file.Object);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.FILE_TOO_LARGE);
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("script.bat")]
    [InlineData("file.unknown")]
    public void ValidateFile_ShouldReturnFailure_WhenExtensionNotAllowed(string fileName)
    {
        // Arrange
        var file = CreateMockFile(fileName, 1024);

        // Act
        var result = _service.ValidateFile(file.Object);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.INVALID_FILE_TYPE);
    }

    [Fact]
    public void ValidateFile_ShouldReturnFailure_WhenFileExtensionIsMissed()
    {
        // Arrange
        var file = CreateMockFile("invalid", 1024);
        // Act
        var result = _service.ValidateFile(file.Object);
        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.INVALID_FILE_TYPE);
    }

    [Fact]
    public void ValidateFile_ShouldReturnFailure_WhenFileNameIsInvalid()
    {
        // Arrange
        var file = CreateMockFile("inva|id.txt", 1024);
        // Act
        var result = _service.ValidateFile(file.Object);
        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.INVALID_CHARECTER);
    }
    #endregion

    #region ValidateItemName Tests

    [Theory]
    [InlineData("ValidFileName.txt")]
    [InlineData("Document 2024.pdf")]
    [InlineData("my-file_v2.docx")]
    public void ValidateItemName_ShouldReturnSuccess_WhenNameIsValid(string name)
    {
        // Act
        var result = _service.ValidateItemName(name);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateItemName_ShouldReturnFailure_WhenNameIsEmpty(string name)
    {
        // Act
        var result = _service.ValidateItemName(name);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.INVALID_NAME);
    }


    [Fact]
    public void ValidateItemName_ShouldReturnFailure_WhenNameTooLong()
    {
        // Arrange
        var longName = new string('a', 251);

        // Act
        var result = _service.ValidateItemName(longName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NAME_TOO_LONG);
    }


    [Theory]
    [InlineData("file<test>.txt")]
    [InlineData("file>test.txt")]
    [InlineData("file:test.txt")]
    [InlineData("file\"test.txt")]
    [InlineData("file|test.txt")]
    [InlineData("file?test.txt")]
    [InlineData("file*test.txt")]
    public void ValidateItemName_ShouldReturnFailure_WhenContainsInvalidChars(string name)
    {
        // Act
        var result = _service.ValidateItemName(name);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.INVALID_CHARECTER);
    }


    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    public void ValidateItemName_ShouldReturnFailure_WhenNameIsReserved(string name)
    {
        // Act
        var result = _service.ValidateItemName(name);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.RESERVED_NAME);
    }


    [Theory]
    [InlineData(".hidden")]
    [InlineData("file.")]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    public void ValidateItemName_ShouldReturnFailure_WhenStartsOrEndsInvalid(string name)
    {
        // Act
        var result = _service.ValidateItemName(name);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.INVALID_NAME_FORMAT);
    }

    #endregion

    #region ValidateUserAuthorization Tests

    [Fact]
    public void ValidateUserAuthorization_ShouldReturnSuccess_WhenUserIdsMatch()
    {
        // Act
        var result = _service.ValidateUserAuthorization(123, 123);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateUserAuthorization_ShouldReturnFailure_WhenUserIdsDontMatch()
    {
        // Act
        var result = _service.ValidateUserAuthorization(123, 456);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ACCESS_DENIED);
    }

    #endregion

    #region ValidateItemExistsAsync Tests
    [Fact]
    public async Task ValidateItemExistsAsync_ShouldReturnSuccess_WhenItemExists()
    {
        // Arrange
        _mockItemRepository
            .Setup(r => r.ItemExistsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ValidateItemExistsAsync(1, 123, "file");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("file", "File not found")]
    [InlineData("folder", "Folder not found")]
    [InlineData(null, "Item not found")]
    public async Task ValidateItemExistsAsync_ShouldReturnFailure_WhenItemNotExists(string itemType, string expectedMessage)
    {
        // Arrange
        _mockItemRepository
            .Setup(r => r.ItemExistsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateItemExistsAsync(999, 123, itemType);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ITEM_NOT_FOUND);
        result.ErrorMessage.Should().Be(expectedMessage);
    }

    #endregion

    #region ValidateItemIdsAsync Tests

    [Fact]
    public async Task ValidateItemIdsAsync_ShouldReturnFailure_WhenListIsNull()
    {
        // Act
        var result = await _service.ValidateItemIdsAsync(null, 123);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NO_ITEMS);
    }

    [Fact]
    public async Task ValidateItemIdsAsync_ShouldReturnFailure_WhenListIsEmpty()
    {
        // Act
        var result = await _service.ValidateItemIdsAsync(new List<int>(), 123);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NO_ITEMS);
    }

    [Fact]
    public async Task ValidateItemIdsAsync_ShouldReturnFailure_WhenTooManyItems()
    {
        // Arrange
        var items = Enumerable.Range(1, 101).ToList();

        // Act
        var result = await _service.ValidateItemIdsAsync(items, 123);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.TOO_MANY_FILES);
    }

    [Fact]
    public async Task ValidateItemIdsAsync_ShouldReturnSuccess_WhenAllItemsExist()
    {
        // Arrange
        var itemIds = new List<int> { 1, 2, 3 };
        _mockItemRepository
            .Setup(r => r.CountExistingItemsAsync(itemIds, 123))
            .ReturnsAsync(3);

        // Act
        var result = await _service.ValidateItemIdsAsync(itemIds, 123);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateItemIdsAsync_ShouldReturnFailure_WhenSomeItemsMissing()
    {
        // Arrange
        var itemIds = new List<int> { 1, 2, 3 };
        _mockItemRepository
            .Setup(r => r.CountExistingItemsAsync(itemIds, 123))
            .ReturnsAsync(2); // Only 2 out of 3 exist

        // Act
        var result = await _service.ValidateItemIdsAsync(itemIds, 123);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ITEM_NOT_FOUND);
    }

    #endregion

    #region ValidateNameUniquenessAsync Tests

    [Fact]
    public async Task ValidateNameUniquenessAsync_ShouldReturnSuccess_WhenNameIsUnique()
    {
        // Arrange
        _mockItemRepository
            .Setup(r => r.DoesItemExistByNameAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateNameUniquenessAsync("NewFile.txt", "file", 123, null);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateNameUniquenessAsync_ShouldReturnFailure_WhenNameExists()
    {
        // Arrange
        _mockItemRepository
            .Setup(r => r.DoesItemExistByNameAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ValidateNameUniquenessAsync("ExistingFile.txt", "file", 123, null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NAME_ALREADY_EXISTS);
    }

    #endregion

    #region ValidateArchiveSize Tests

    [Fact]
    public void ValidateArchiveSize_ShouldReturnSuccess_WhenSizeAndCountValid()
    {
        // Act
        var result = _service.ValidateArchiveSize(1024 * 1024, 100);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateArchiveSize_ShouldReturnFailure_WhenSizeTooLarge()
    {
        // Arrange
        var tooLargeSize = 3L * 1024 * 1024 * 1024; // 3 GB

        // Act
        var result = _service.ValidateArchiveSize(tooLargeSize, 100);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.ARCHIVE_TOO_LARGE);
    }

    [Fact]
    public void ValidateArchiveSize_ShouldReturnFailure_WhenTooManyFiles()
    {
        // Act
        var result = _service.ValidateArchiveSize(1024, 10001);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.TOO_MANY_FILES);
    }

    #endregion

    #region ValidateQuery Tests

    [Theory]
    [InlineData("simple query")]
    [InlineData("query123")]
    [InlineData("UPPERCASE")]
    public void ValidateQuery_ShouldReturnSuccess_WhenQueryValid(string query)
    {
        // Act
        var result = _service.ValidateQuery(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateQuery_ShouldReturnFailure_WhenQueryNullOrEmpty(string query)
    {
        // Act
        var result = _service.ValidateQuery(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NULL_OR_EMPTY);
    }

    [Theory]
    [InlineData("query@special")]
    [InlineData("query#hash")]
    [InlineData("query!exclamation")]
    public void ValidateQuery_ShouldReturnFailure_WhenContainsSpecialChars(string query)
    {
        // Act
        var result = _service.ValidateQuery(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.NOT_ALLOWED_SYMBOL);
    }

    #endregion

    #region ValidateIsFolderSubFolder Tests

    [Fact]
    public async Task ValidateIsFolderSubFolder_ShouldReturnFailure_WhenFolderIsSame()
    {
        // Act
        var result = await _service.ValidateIsFolderSubFolder(123, 5, 5);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.INVALID_OPERATION);
    }

    [Fact]
    public async Task ValidateIsFolderSubFolder_ShouldReturnFailure_WhenTargetIsSubfolder()
    {
        // Arrange
        _mockItemRepository
            .Setup(r => r.IsFolderSubFolderAsync(123, 1, 2))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ValidateIsFolderSubFolder(123, 1, 2);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.CIRCULAR_REFERENCE);
    }

    [Fact]
    public async Task ValidateIsFolderSubFolder_ShouldReturnSuccess_WhenNoCircularReference()
    {
        // Arrange
        _mockItemRepository
            .Setup(r => r.IsFolderSubFolderAsync(123, 1, 2))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ValidateIsFolderSubFolder(123, 1, 2);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region FormatFileSize Tests

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1,5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1610612736, "1,5 GB")]
    public void FormatFileSize_ShouldFormatCorrectly(long size, string expected)
    {
        // Act
        var result = _service.FormatFileSize(size);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
