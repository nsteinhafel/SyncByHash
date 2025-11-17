using System.Net;
using Amazon.S3.Model;
using Moq;
using Xunit;

namespace SyncByHash.Tests;

/// <summary>
///     Tests for DeleteFiles method.
///     Uses TestBase for service creation and follows AAA pattern.
/// </summary>
public partial class SyncByHashTests
{
    #region DeleteFiles Tests

    [Fact]
    public async Task DeleteFiles_NoFilesToDelete_DoesNotCallDeleteObjects()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();

        // S3 has file1.txt, and we're uploading file1.txt (so no deletions needed)
        var keyToHash = new Dictionary<string, string>
        {
            { "file1.txt", "hash1" }
        };
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create("/path/to/file1.txt", "file1.txt")
        };

        // Act
        await service.DeleteFiles("bucket", keyToHash, fileKeyPairs, false);

        // Assert
        mockClient.Verify(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task DeleteFiles_FilesToDelete_CallsDeleteObjects()
    {
        // Arrange
        var mockClient = CreateMockS3Client()
            .SetupDeleteObjectsAsyncWithOkResponse();
        var service = CreateService(mockClient);

        // S3 has file1.txt and file2.txt, but we're only uploading file1.txt
        // So file2.txt should be deleted
        var keyToHash = new Dictionary<string, string>
        {
            { "file1.txt", "hash1" },
            { "file2.txt", "hash2" }
        };
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create("/path/to/file1.txt", "file1.txt")
        };

        // Act
        await service.DeleteFiles("bucket", keyToHash, fileKeyPairs, false);

        // Assert
        mockClient.Verify(x => x.DeleteObjectsAsync(
            It.Is<DeleteObjectsRequest>(r => r.Objects.Count == 1 && r.Objects[0].Key == "file2.txt"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeleteFiles_DryRun_DoesNotCallDeleteObjects()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();

        // S3 has file2.txt, but we're not uploading it
        var keyToHash = new Dictionary<string, string>
        {
            { "file2.txt", "hash2" }
        };

        // Act
        await service.DeleteFiles("bucket", keyToHash, [], true);

        // Assert
        // Should not call DeleteObjectsAsync in dry run mode
        mockClient.Verify(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task DeleteFiles_MultipleFilesToDelete_DeletesAllCorrectly()
    {
        // Arrange
        var mockClient = CreateMockS3Client()
            .SetupDeleteObjectsAsyncWithOkResponse();
        var service = CreateService(mockClient);

        // S3 has file1, file2, file3, but we're only uploading file1
        var keyToHash = new Dictionary<string, string>
        {
            { "file1.txt", "hash1" },
            { "file2.txt", "hash2" },
            { "file3.txt", "hash3" }
        };
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create("/path/to/file1.txt", "file1.txt")
        };

        // Act
        await service.DeleteFiles("bucket", keyToHash, fileKeyPairs, false);

        // Assert
        mockClient.Verify(x => x.DeleteObjectsAsync(
            It.Is<DeleteObjectsRequest>(r =>
                r.Objects.Count == 2 &&
                r.Objects.Any(o => o.Key == "file2.txt") &&
                r.Objects.Any(o => o.Key == "file3.txt")),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeleteFiles_PassesCorrectBucket()
    {
        // Arrange
        var mockClient = CreateMockS3Client()
            .SetupDeleteObjectsAsyncWithOkResponse();
        var service = CreateService(mockClient);

        var keyToHash = new Dictionary<string, string>
        {
            { "file1.txt", "hash1" }
        };

        // Act
        await service.DeleteFiles("my-test-bucket", keyToHash, [], false);

        // Assert
        mockClient.Verify(x => x.DeleteObjectsAsync(
            It.Is<DeleteObjectsRequest>(r => r.BucketName == "my-test-bucket"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeleteFiles_S3ReturnsError_ThrowsException()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();
        mockClient.Setup(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), CancellationToken.None))
            .ReturnsAsync(new DeleteObjectsResponse
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            });

        var keyToHash = new Dictionary<string, string>
        {
            { "file1.txt", "hash1" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.DeleteFiles("bucket", keyToHash, [], false));
    }

    [Fact]
    public async Task DeleteFiles_EmptyS3Bucket_DoesNotCallDeleteObjects()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();

        // S3 has no files
        var keyToHash = new Dictionary<string, string>();
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create("/path/to/file1.txt", "file1.txt")
        };

        // Act
        await service.DeleteFiles("bucket", keyToHash, fileKeyPairs, false);

        // Assert
        mockClient.Verify(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), CancellationToken.None),
            Times.Never);
    }

    #endregion
}