using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;

namespace SyncByHash.Tests
{
    public static class MockClientExtensions
    {
        public static Mock<IAmazonS3> SetupListObjectsV2AsyncWithOkResponse(this Mock<IAmazonS3> client,
            List<S3Object> s3Objects = null)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            client.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default(CancellationToken)))
                .Returns(Task.FromResult(new ListObjectsV2Response
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    S3Objects = s3Objects ?? new List<S3Object>()
                }));
            return client;
        }

        public static Mock<IAmazonS3> SetupPutObjectAsyncWithOkResponse(this Mock<IAmazonS3> client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default(CancellationToken)))
                .Returns(Task.FromResult(new PutObjectResponse
                {
                    HttpStatusCode = HttpStatusCode.OK
                }));
            return client;
        }
    }
}