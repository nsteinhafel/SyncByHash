using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;

namespace SyncByHash.Tests;

public static class MockClientExtensions
{
    extension(Mock<IAmazonS3> client)
    {
        public Mock<IAmazonS3> SetupListObjectsV2AsyncWithOkResponse(List<S3Object>? s3Objects = null)
        {
            client.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), CancellationToken.None))
                .Returns(Task.FromResult(new ListObjectsV2Response
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    S3Objects = s3Objects ?? []
                }));
            return client;
        }

        public Mock<IAmazonS3> SetupPutObjectAsyncWithOkResponse()
        {
            client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(new PutObjectResponse
                {
                    HttpStatusCode = HttpStatusCode.OK
                }));
            return client;
        }

        public Mock<IAmazonS3> SetupDeleteObjectsAsyncWithOkResponse()
        {
            client.Setup(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(new DeleteObjectsResponse
                {
                    HttpStatusCode = HttpStatusCode.OK
                }));
            return client;
        }

        public Mock<IAmazonS3> SetupListObjectsV2AsyncWithPagination(List<List<S3Object>> pages)
        {
            var callCount = 0;
            client.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), CancellationToken.None))
                .Returns(() =>
                {
                    var currentPage = callCount;
                    callCount++;

                    if (currentPage >= pages.Count)
                        return Task.FromResult(new ListObjectsV2Response
                        {
                            HttpStatusCode = HttpStatusCode.OK,
                            S3Objects = [],
                            ContinuationToken = null
                        });

                    return Task.FromResult(new ListObjectsV2Response
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                        S3Objects = pages[currentPage],
                        ContinuationToken = currentPage < pages.Count - 1 ? $"token-{currentPage + 1}" : null
                    });
                });
            return client;
        }
    }
}
