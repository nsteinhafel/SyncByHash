using System.Net;
using Amazon.S3.Model;
using Moq;
using Xunit;
using static SyncByHash.Tests.TestDataBuilders;

namespace SyncByHash.Tests;

/// <summary>
///     Tests for GetKeysToHashesFromBucket method.
///     Follows AAA pattern: Arrange, Act, Assert.
///     Each test is isolated and creates its own mocks/test data.
/// </summary>
public partial class SyncByHashTests : TestBase
{
    #region GetKeysToHashesFromBucket Tests

    [Fact]
    public async Task GetKeysToHashesFromBucket_EmptyBucket_ReturnsEmptyDictionary()
    {
        // Arrange
        var mockClient = CreateMockS3Client()
            .SetupListObjectsV2AsyncWithOkResponse();
        var service = CreateService(mockClient);

        // Act
        var result = await service.GetKeysToHashesFromBucket("test-bucket");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetKeysToHashesFromBucket_SinglePage_ReturnsCorrectKeyHashPairs()
    {
        // Arrange
        var s3Objects = S3ObjectList()
            .AddObject("file1.txt", "abc123")
            .AddObject("file2.txt", "def456")
            .Build();

        var mockClient = CreateMockS3Client()
            .SetupListObjectsV2AsyncWithOkResponse(s3Objects);
        var service = CreateService(mockClient);

        // Act
        var result = await service.GetKeysToHashesFromBucket("test-bucket");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("abc123", result["file1.txt"]);
        Assert.Equal("def456", result["file2.txt"]);
    }

    [Fact]
    public async Task GetKeysToHashesFromBucket_MultiplePagesWithPagination_ReturnsAllKeyHashPairs()
    {
        // Arrange
        var page1 = S3ObjectList()
            .AddObject("file1.txt", "hash1")
            .AddObject("file2.txt", "hash2")
            .Build();

        var page2 = S3ObjectList()
            .AddObject("file3.txt", "hash3")
            .AddObject("file4.txt", "hash4")
            .Build();

        var mockClient = CreateMockS3Client()
            .SetupListObjectsV2AsyncWithPagination([page1, page2]);
        var service = CreateService(mockClient);

        // Act
        var result = await service.GetKeysToHashesFromBucket("test-bucket");

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("hash1", result["file1.txt"]);
        Assert.Equal("hash2", result["file2.txt"]);
        Assert.Equal("hash3", result["file3.txt"]);
        Assert.Equal("hash4", result["file4.txt"]);
    }

    [Fact]
    public async Task GetKeysToHashesFromBucket_WithPrefix_PassesPrefixToRequest()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();
        mockClient.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), CancellationToken.None))
            .ReturnsAsync(new ListObjectsV2Response
            {
                HttpStatusCode = HttpStatusCode.OK,
                S3Objects = []
            });

        // Act
        await service.GetKeysToHashesFromBucket("test-bucket", "prefix/");

        // Assert
        mockClient.Verify(x => x.ListObjectsV2Async(
            It.Is<ListObjectsV2Request>(r => r.Prefix == "prefix/"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetKeysToHashesFromBucket_WithDelimiter_PassesDelimiterToRequest()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();
        mockClient.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), CancellationToken.None))
            .ReturnsAsync(new ListObjectsV2Response
            {
                HttpStatusCode = HttpStatusCode.OK,
                S3Objects = []
            });

        // Act
        await service.GetKeysToHashesFromBucket("test-bucket", null, "/");

        // Assert
        mockClient.Verify(x => x.ListObjectsV2Async(
            It.Is<ListObjectsV2Request>(r => r.Delimiter == "/"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetKeysToHashesFromBucket_S3ReturnsError_ThrowsException()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();
        mockClient.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), CancellationToken.None))
            .ReturnsAsync(new ListObjectsV2Response
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await service.GetKeysToHashesFromBucket("test-bucket"));
    }

    #endregion
}