using Amazon.S3;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace SyncByHash.Tests;

/// <summary>
///     Base class for all tests providing common patterns and helpers.
///     Follows AAA (Arrange-Act-Assert) pattern enforcement.
/// </summary>
public abstract class TestBase
{
    /// <summary>
    ///     Creates a SyncByHashService with a mocked S3 client.
    ///     Use this for consistent service instantiation across tests.
    /// </summary>
    protected SyncByHashService CreateService(Mock<IAmazonS3> mockClient)
    {
        return new SyncByHashService(
            mockClient.Object,
            NullLogger<SyncByHashService>.Instance);
    }

    /// <summary>
    ///     Creates a new mock S3 client.
    ///     Each test should get its own mock to ensure state independence.
    /// </summary>
    protected Mock<IAmazonS3> CreateMockS3Client()
    {
        return new Mock<IAmazonS3>();
    }

    /// <summary>
    ///     Creates a SyncByHashService with a fresh mock S3 client.
    ///     Convenience method for tests that don't need to customize the mock.
    /// </summary>
    protected (SyncByHashService service, Mock<IAmazonS3> mockClient) CreateServiceWithMock()
    {
        var mockClient = CreateMockS3Client();
        var service = CreateService(mockClient);
        return (service, mockClient);
    }
}
