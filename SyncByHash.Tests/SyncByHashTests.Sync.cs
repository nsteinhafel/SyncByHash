using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace SyncByHash.Tests;

public partial class SyncByHashTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "bucket")]
    [InlineData("root", null)]
    public async Task Sync_Invalid_OptionsRequiredNull(string? root, string? bucket)
    {
        var client = new Mock<IAmazonS3>();
        var service = new SyncByHashService(client.Object, NullLogger<SyncByHashService>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.Sync(new Options { Root = root!, Bucket = bucket! }));
    }

    [Fact]
    public async Task Sync_Invalid_ClientNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SyncByHashService(null!, NullLogger<SyncByHashService>.Instance));
    }

    [Fact]
    public async Task Sync_Invalid_OptionsNull()
    {
        var client = new Mock<IAmazonS3>();
        var service = new SyncByHashService(client.Object, NullLogger<SyncByHashService>.Instance);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await service.Sync(null!));
    }

    [Fact]
    public async Task Sync_Invalid_OptionsRootAbsolute()
    {
        var client = new Mock<IAmazonS3>();
        var service = new SyncByHashService(client.Object, NullLogger<SyncByHashService>.Instance);
        var opts = new Options
        {
            Root = Path.Join(Directory.GetCurrentDirectory(),
                string.Join(Path.DirectorySeparatorChar, "this", "path", "is", "invalid")),
            Bucket = "bucket"
        };
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await service.Sync(opts));
    }

    [Fact]
    public async Task Sync_Invalid_OptionsRootRelative()
    {
        var client = new Mock<IAmazonS3>();
        var service = new SyncByHashService(client.Object, NullLogger<SyncByHashService>.Instance);
        var opts = new Options
        {
            Root = string.Join(Path.DirectorySeparatorChar, ".", "this", "path", "is", "invalid"),
            Bucket = "bucket"
        };
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await service.Sync(opts));
    }

    [Fact]
    public async Task Sync_Invalid_S3Error()
    {
        var client = new Mock<IAmazonS3>();
        client.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default))
            .Returns(Task.FromResult(new ListObjectsV2Response
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            }));
        var service = new SyncByHashService(client.Object, NullLogger<SyncByHashService>.Instance);
        var opts = new Options
        {
            Root = Directory.GetCurrentDirectory(),
            Bucket = "bucket"
        };
        await Assert.ThrowsAsync<Exception>(async () => await service.Sync(opts));
    }

    [Fact]
    public async Task Sync_Valid_OptionsRootAbsolute()
    {
        var client = new Mock<IAmazonS3>().SetupListObjectsV2AsyncWithOkResponse()
            .SetupPutObjectAsyncWithOkResponse();
        var service = new SyncByHashService(client.Object, NullLogger<SyncByHashService>.Instance);
        var opts = new Options
        {
            Root = Directory.GetCurrentDirectory(),
            Bucket = "bucket"
        };
        await service.Sync(opts);
    }

    [Fact]
    public async Task Sync_Valid_OptionsRootRelative()
    {
        var client = new Mock<IAmazonS3>().SetupListObjectsV2AsyncWithOkResponse()
            .SetupPutObjectAsyncWithOkResponse();
        var service = new SyncByHashService(client.Object, NullLogger<SyncByHashService>.Instance);
        var opts = new Options
        {
            Root = Directory.GetCurrentDirectory(),
            Bucket = "bucket"
        };
        await service.Sync(opts);
    }
}