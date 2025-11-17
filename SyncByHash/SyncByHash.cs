using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using MimeTypes.Core;

[assembly: InternalsVisibleTo("SyncByHash.Tests")]

namespace SyncByHash;

/// <summary>Service to sync an S3 bucket by hash rather than by timestamp or file size.</summary>
public class SyncByHashService
{
    private const string MissingOption = "The option '{0}' is required but was not available in the given options.";
    private readonly IAmazonS3 _client;
    private readonly ILogger<SyncByHashService> _logger;

    public SyncByHashService(IAmazonS3 client, ILogger<SyncByHashService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Sync the given folder and bucket.</summary>
    /// <param name="opts">Options.</param>
    /// <returns></returns>
    public async Task Sync(Options opts)
    {
        // 'Root' and 'Bucket' are required.
        if (opts.Root == null)
            throw new InvalidOperationException(string.Format(MissingOption, nameof(opts.Root)));
        if (opts.Bucket == null)
            throw new InvalidOperationException(string.Format(MissingOption, nameof(opts.Bucket)));

        // Get the path from the given root folder.
        var path = Path.IsPathFullyQualified(opts.Root)
            ? opts.Root
            : Path.Join(Directory.GetCurrentDirectory(), opts.Root);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Could not resolve part or all of path '{path}'.");

        try
        {
            // Get map of keys to hashes in the bucket.
            var keyToHash = await GetKeysToHashesFromBucket(opts.Bucket, opts.Prefix, opts.Delimiter);

            // Get file-key pairs.
            var fileKeyPairsToUpload = GetFileKeyPairsToUpload(path, keyToHash, opts.Prefix, opts.Force);

            // Upload files.
            await UploadFiles(opts.Bucket, fileKeyPairsToUpload, opts.DryRun);

            // Delete extra files if desired.
            if (opts.Delete)
                await DeleteFiles(opts.Bucket, keyToHash, fileKeyPairsToUpload, opts.DryRun);
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError(e, "Error encountered when writing an object: {Message}", e.Message);
            throw;
        }
    }

    /// <summary>Delete files that are present in the bucket but not the folder.</summary>
    /// <param name="bucket">Bucket.</param>
    /// <param name="keyToHash">Map of keys to hashes.</param>
    /// <param name="fileKeyPairsToUpload">File-key pairs to upload.</param>
    /// <param name="dryRun">Dry run?</param>
    /// <returns></returns>
    internal async Task DeleteFiles(string bucket, IDictionary<string, string> keyToHash,
        IEnumerable<Tuple<string, string>> fileKeyPairsToUpload, bool dryRun)
    {
        // Find the difference between the bucket and the root.
        var keySet = new HashSet<string>(fileKeyPairsToUpload.Select(x => x.Item2));
        var keysToRemove = keyToHash.Keys.Where(x => !keySet.Contains(x)).ToList();

        if (keysToRemove.Any())
        {
            var deleteRequest = new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = keysToRemove.Select(x => new KeyVersion { Key = x }).ToList()
            };

            _logger.LogInformation("Deleting objects '{Keys}'", string.Join("', '", keysToRemove));
            if (!dryRun)
            {
                var deleteResponse = await _client.DeleteObjectsAsync(deleteRequest);

                if (deleteResponse?.HttpStatusCode != HttpStatusCode.OK)
                    throw new Exception(
                        $"Error deleting objects '{string.Join("', '", keysToRemove)}' from S3!");
            }
        }
        else
        {
            _logger.LogInformation("No objects to remove");
        }
    }

    /// <summary>Upload files to the given bucket.</summary>
    /// <param name="bucket">Bucket.</param>
    /// <param name="fileKeyPairsToUpload">File-key pairs to upload.</param>
    /// <param name="dryRun">Dry run?</param>
    /// <returns></returns>
    internal async Task UploadFiles(string bucket,
        ICollection<Tuple<string, string>> fileKeyPairsToUpload, bool dryRun)
    {
        if (fileKeyPairsToUpload.Any())
            foreach (var (filePath, key) in fileKeyPairsToUpload)
            {
                // Try to get a MIME type for the given extension. Defaults to "application/octet-stream".
                var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(filePath));

                // Build a request to upload this file.
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucket,
                    FilePath = filePath,
                    Key = key,
                    ContentType = mimeType
                };

                // Upload this file to S3.
                _logger.LogInformation("Uploading object '{Key}'", key);

                // If this is a dry run, don't take any further action.
                if (dryRun) continue;

                var putResponse = await _client.PutObjectAsync(putRequest);

                if (putResponse?.HttpStatusCode != HttpStatusCode.OK)
                    throw new Exception($"Error putting object '{key}' to S3!");
            }
        else
            _logger.LogInformation("No objects to upload");
    }

    /// <summary>
    ///     Get a list of file-key pairs for upload. Any files found to be missing from the mapping provided or any that have a
    ///     different hash from the hash provided for that key will be added to the upload set. If force is
    ///     <see langword="true" />
    ///     , this will include all files found in the upload set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="keyToHash">Map of keys to hashes.</param>
    /// <param name="prefix">Prefix (if any)</param>
    /// <param name="force">Force upload?</param>
    /// <returns>File-key pairs to upload.</returns>
    internal static ICollection<Tuple<string, string>> GetFileKeyPairsToUpload(string path,
        IDictionary<string, string> keyToHash, string? prefix = null, bool force = false)
    {
        var fileKeyPairsToUpload = new List<Tuple<string, string>>();

        // Get all files in folder and sub-folders.
        using var md5 = MD5.Create();
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            // Normalize Windows paths to S3 keys.
            var key = $"{prefix}{Path.GetRelativePath(path, file).Replace('\\', '/')}";

            // Force upload?
            if (force ||
                // Is this a new file?
                !keyToHash.ContainsKey(key) ||
                // Has this file changed? Compare the hash that S3 has to the local file's hash.
                !string.Equals(keyToHash[key], FileContentHash(md5, file), StringComparison.OrdinalIgnoreCase))
                fileKeyPairsToUpload.Add(Tuple.Create(file, key));
        }

        return fileKeyPairsToUpload;
    }

    /// <summary>Get a mapping of keys to file hashes from the S3 bucket.</summary>
    /// <param name="bucket">Bucket.</param>
    /// <param name="prefix">Prefix (if any).</param>
    /// <param name="delimiter">Delimiter (if any).</param>
    /// <returns>Map of keys to hashes.</returns>
    internal async Task<IDictionary<string, string>> GetKeysToHashesFromBucket(
        string bucket, string? prefix = null, string? delimiter = null)
    {
        var keyToHash = new Dictionary<string, string>();
        string? continuationToken = null;
        do
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix,
                Delimiter = delimiter,
                ContinuationToken = continuationToken
            };

            // List all objects in the bucket from S3
            var listObjectsResponse = await _client.ListObjectsV2Async(listRequest);

            if (listObjectsResponse?.HttpStatusCode != HttpStatusCode.OK)
                throw new Exception("Error listing objects from S3!");

            foreach (var s3Object in listObjectsResponse.S3Objects)
                // ETags are MD5 file hashes surrounded by double quotes.
                keyToHash[s3Object.Key] = s3Object.ETag.Replace("\"", string.Empty);

            // Do we have another page (S3 will paginate over 1000 items)?
            continuationToken = listObjectsResponse.ContinuationToken;
        } while (continuationToken != null);

        return keyToHash;
    }

    /// <summary>Get the MD5 hash of a file's contents as a string.</summary>
    /// <param name="md5">MD5 implementation.</param>
    /// <param name="filePath">Path to file.</param>
    /// <returns>MD5 hash.</returns>
    internal static string FileContentHash(MD5 md5, string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(md5.ComputeHash(stream));
    }
}