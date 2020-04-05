using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using Xunit;

namespace SyncByHash.Tests
{
    public partial class SyncByHashTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "bucket")]
        [InlineData("root", null)]
        public async Task Sync_Invalid_OptionsRequiredNull(string root, string bucket)
        {
            var client = new Mock<IAmazonS3>();
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await SyncByHash.Sync(client.Object, new Options {Root = root, Bucket = bucket}));
        }

        [Fact]
        public async Task Sync_Invalid_ClientNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await SyncByHash.Sync(null, new Options()));
        }

        [Fact]
        public async Task Sync_Invalid_Null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await SyncByHash.Sync(null, null));
        }

        [Fact]
        public async Task Sync_Invalid_OptionsNull()
        {
            var client = new Mock<IAmazonS3>();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await SyncByHash.Sync(client.Object, null));
        }

        [Fact]
        public async Task Sync_Invalid_OptionsRootAbsolute()
        {
            var client = new Mock<IAmazonS3>();
            var opts = new Options
            {
                Root = Path.Join(Directory.GetCurrentDirectory(),
                    string.Join(Path.DirectorySeparatorChar, "this", "path", "is", "invalid")),
                Bucket = "bucket"
            };
            await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
                await SyncByHash.Sync(client.Object, opts));
        }

        [Fact]
        public async Task Sync_Invalid_OptionsRootRelative()
        {
            var client = new Mock<IAmazonS3>();
            var opts = new Options
            {
                Root = string.Join(Path.DirectorySeparatorChar, ".", "this", "path", "is", "invalid"),
                Bucket = "bucket"
            };
            await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
                await SyncByHash.Sync(client.Object, opts));
        }

        [Fact]
        public async Task Sync_Invalid_S3Error()
        {
            var client = new Mock<IAmazonS3>();
            client.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default(CancellationToken)))
                .Returns(Task.FromResult(new ListObjectsV2Response
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                }));
            var opts = new Options
            {
                Root = Directory.GetCurrentDirectory(),
                Bucket = "bucket"
            };
            await Assert.ThrowsAsync<Exception>(async () => await SyncByHash.Sync(client.Object, opts));
        }

        [Fact]
        public async Task Sync_Valid_OptionsRootAbsolute()
        {
            var client = new Mock<IAmazonS3>().SetupListObjectsV2AsyncWithOkResponse()
                .SetupPutObjectAsyncWithOkResponse();
            var opts = new Options
            {
                Root = Directory.GetCurrentDirectory(),
                Bucket = "bucket"
            };
            await SyncByHash.Sync(client.Object, opts);
        }

        [Fact]
        public async Task Sync_Valid_OptionsRootRelative()
        {
            var client = new Mock<IAmazonS3>().SetupListObjectsV2AsyncWithOkResponse()
                .SetupPutObjectAsyncWithOkResponse();
            var opts = new Options
            {
                Root = Directory.GetCurrentDirectory(),
                Bucket = "bucket"
            };
            await SyncByHash.Sync(client.Object, opts);
        }
    }
}