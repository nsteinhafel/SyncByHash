using System.Net;
using Amazon.S3.Model;
using Moq;
using Xunit;
using static SyncByHash.Tests.TestDataBuilders;

namespace SyncByHash.Tests;

/// <summary>
///     Tests for Sync method.
///     Uses FileSystemTestFixture for directory isolation and TestDataBuilders for test data.
/// </summary>
public partial class SyncByHashTests
{
    #region Sync Tests

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "bucket")]
    [InlineData("root", null)]
    public async Task Sync_OptionsRequiredFieldsNull_ThrowsInvalidOperationException(string? root, string? bucket)
    {
        // Arrange
        var (service, _) = CreateServiceWithMock();
        var options = Options().WithRoot(root!).WithBucket(bucket!).Build();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.Sync(options));
    }

    [Fact]
    public async Task Sync_OptionsRootAbsoluteNonExistent_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var (service, _) = CreateServiceWithMock();
        var nonExistentPath = Path.Join(Directory.GetCurrentDirectory(), "this", "path", "is", "invalid");
        var options = Options()
            .WithRoot(nonExistentPath)
            .WithBucket("bucket")
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await service.Sync(options));
    }

    [Fact]
    public async Task Sync_OptionsRootRelativeNonExistent_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var (service, _) = CreateServiceWithMock();
        var options = Options()
            .WithRoot(Path.Join(".", "this", "path", "is", "invalid"))
            .WithBucket("bucket")
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await service.Sync(options));
    }

    [Fact]
    public async Task Sync_S3ListReturnsError_ThrowsException()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();
        mockClient.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), CancellationToken.None))
            .ReturnsAsync(new ListObjectsV2Response
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            });
        var options = Options()
            .WithRoot(Directory.GetCurrentDirectory())
            .WithBucket("bucket")
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await service.Sync(options));
    }

    [Fact]
    public async Task Sync_ValidOptionsWithAbsolutePath_Succeeds()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        fixture.CreateFile("test.txt", "test content");

        var mockClient = CreateMockS3Client()
            .SetupListObjectsV2AsyncWithOkResponse()
            .SetupPutObjectAsyncWithOkResponse();

        var service = CreateService(mockClient);
        var options = Options()
            .WithRoot(fixture.RootDirectory)
            .WithBucket("bucket")
            .Build();

        // Act
        await service.Sync(options);

        // Assert - no exceptions thrown
    }

    [Fact]
    public async Task Sync_ValidOptionsWithRelativePath_Succeeds()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        fixture.CreateFile("test.txt", "test content");

        // Create a relative path by going from current directory
        var currentDir = Directory.GetCurrentDirectory();
        var relativePath = Path.GetRelativePath(currentDir, fixture.RootDirectory);

        var mockClient = CreateMockS3Client()
            .SetupListObjectsV2AsyncWithOkResponse()
            .SetupPutObjectAsyncWithOkResponse();

        var service = CreateService(mockClient);
        var options = Options()
            .WithRoot(relativePath)
            .WithBucket("bucket")
            .Build();

        // Act
        await service.Sync(options);

        // Assert - no exceptions thrown
    }

    #endregion
}