using System.Net;
using Amazon.S3.Model;
using Moq;
using Xunit;

namespace SyncByHash.Tests;

/// <summary>
///     Tests for UploadFiles method.
///     Uses FileSystemTestFixture for file isolation and TestBase for service creation.
/// </summary>
public partial class SyncByHashTests
{
    #region UploadFiles Tests

    [Fact]
    public async Task UploadFiles_EmptyList_DoesNotCallPutObject()
    {
        // Arrange
        var (service, mockClient) = CreateServiceWithMock();
        var fileKeyPairs = new List<Tuple<string, string>>();

        // Act
        await service.UploadFiles("bucket", fileKeyPairs, false);

        // Assert
        mockClient.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task UploadFiles_DryRun_DoesNotCallPutObject()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var tempFile = fixture.CreateFile("test.txt", "Test content");

        var (service, mockClient) = CreateServiceWithMock();
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create(tempFile, "test-key.txt")
        };

        // Act
        await service.UploadFiles("bucket", fileKeyPairs, true);

        // Assert
        // Should not call PutObjectAsync in dry run mode
        mockClient.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task UploadFiles_ValidFiles_CallsPutObjectForEachFile()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var file1 = fixture.CreateFile("file1.txt", "Content 1");
        var file2 = fixture.CreateFile("file2.txt", "Content 2");

        var mockClient = CreateMockS3Client()
            .SetupPutObjectAsyncWithOkResponse();
        var service = CreateService(mockClient);
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create(file1, "file1.txt"),
            Tuple.Create(file2, "file2.txt")
        };

        // Act
        await service.UploadFiles("test-bucket", fileKeyPairs, false);

        // Assert
        // Should call PutObjectAsync twice
        mockClient.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None),
            Times.Exactly(2));
    }

    [Fact]
    public async Task UploadFiles_ValidFile_PassesCorrectBucketAndKey()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var tempFile = fixture.CreateFile("test.txt", "Test content");

        var mockClient = CreateMockS3Client()
            .SetupPutObjectAsyncWithOkResponse();
        var service = CreateService(mockClient);
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create(tempFile, "test/path/file.txt")
        };

        // Act
        await service.UploadFiles("my-bucket", fileKeyPairs, false);

        // Assert
        mockClient.Verify(x => x.PutObjectAsync(
            It.Is<PutObjectRequest>(r =>
                r.BucketName == "my-bucket" &&
                r.Key == "test/path/file.txt"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UploadFiles_TextFile_SetsCorrectContentType()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var txtFile = fixture.CreateFile("file.txt", "Test content");

        var mockClient = CreateMockS3Client()
            .SetupPutObjectAsyncWithOkResponse();
        var service = CreateService(mockClient);
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create(txtFile, "file.txt")
        };

        // Act
        await service.UploadFiles("bucket", fileKeyPairs, false);

        // Assert
        mockClient.Verify(x => x.PutObjectAsync(
            It.Is<PutObjectRequest>(r => r.ContentType == "text/plain"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UploadFiles_HtmlFile_SetsCorrectContentType()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var htmlFile = fixture.CreateFile("file.html", "<html></html>");

        var mockClient = CreateMockS3Client()
            .SetupPutObjectAsyncWithOkResponse();
        var service = CreateService(mockClient);
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create(htmlFile, "file.html")
        };

        // Act
        await service.UploadFiles("bucket", fileKeyPairs, false);

        // Assert
        mockClient.Verify(x => x.PutObjectAsync(
            It.Is<PutObjectRequest>(r => r.ContentType == "text/html"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UploadFiles_S3ReturnsError_ThrowsException()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var tempFile = fixture.CreateFile("test.txt", "Test content");

        var (service, mockClient) = CreateServiceWithMock();
        mockClient.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None))
            .ReturnsAsync(new PutObjectResponse
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            });
        var fileKeyPairs = new List<Tuple<string, string>>
        {
            Tuple.Create(tempFile, "test.txt")
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await service.UploadFiles("bucket", fileKeyPairs, false));
    }

    #endregion
}