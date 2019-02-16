using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using CommandLine;
using HeyRed.Mime;

namespace SyncByHash
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(x => Sync(x).Wait());
        }

        /// <summary>Sync the given folder and bucket.</summary>
        /// <param name="opts">Options.</param>
        /// <returns></returns>
        private static async Task Sync(Options opts)
        {
            var path = Path.IsPathFullyQualified(opts.Root)
                ? opts.Root
                : Path.Join(Directory.GetCurrentDirectory(), opts.Root);

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Could not resolve part or all of path '{path}'.");

            var client = new AmazonS3Client();

            try
            {
                // Get map of keys to hashes in the bucket.
                var keyToHash = await GetKeysToHashesFromBucket(client, opts.Bucket);

                // Get file-key pairs.
                var fileKeyPairsToUpload = GetFileKeyPairsToUpload(path, keyToHash, opts.Force);

                // Upload files.
                await UploadFiles(client, opts.Bucket, fileKeyPairsToUpload, opts.DryRun);

                // Delete extra files if desired.
                if (opts.Delete)
                    await DeleteFiles(client, opts.Bucket, keyToHash, fileKeyPairsToUpload, opts.DryRun);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
            }
        }

        /// <summary>Delete files that are present in the bucket but not the folder.</summary>
        /// <param name="client">S3 client.</param>
        /// <param name="bucket">Bucket.</param>
        /// <param name="keyToHash">Map of keys to hashes.</param>
        /// <param name="fileKeyPairsToUpload">File-key pairs to upload.</param>
        /// <param name="dryRun">Dry run?</param>
        /// <returns></returns>
        private static async Task DeleteFiles(IAmazonS3 client, string bucket, IDictionary<string, string> keyToHash,
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
                    Objects = keysToRemove.Select(x => new KeyVersion {Key = x}).ToList()
                };

                Console.WriteLine($"Deleting objects '{string.Join("', '", keysToRemove)}'.");
                if (!dryRun)
                {
                    var deleteResponse = await client.DeleteObjectsAsync(deleteRequest);

                    if (deleteResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(
                            $"Error deleting objects '{string.Join("', '", keysToRemove)}' from S3!");
                    }
                }
            }
            else
                Console.WriteLine("No objects to remove.");
        }

        /// <summary>Upload files to the given bucket.</summary>
        /// <param name="client">S3 client.</param>
        /// <param name="bucket">Bucket.</param>
        /// <param name="fileKeyPairsToUpload">File-key pairs to upload.</param>
        /// <param name="dryRun">Dry run?</param>
        /// <returns></returns>
        private static async Task UploadFiles(IAmazonS3 client, string bucket,
            ICollection<Tuple<string, string>> fileKeyPairsToUpload, bool dryRun)
        {
            if (fileKeyPairsToUpload.Any())
            {
                foreach (var (filePath, key) in fileKeyPairsToUpload)
                {
                    // Use a MIME guesser to guess our file type.
                    var mimeType = MimeGuesser.GuessMimeType(filePath);

                    if (string.IsNullOrEmpty(mimeType))
                        Console.WriteLine($"Could not determine MIME type for file '{filePath}'.");

                    // Build a request to upload this file.
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = bucket,
                        FilePath = filePath,
                        Key = key,
                        ContentType = mimeType
                    };

                    // Upload this file to S3.
                    Console.WriteLine($"Uploading object '{key}'.");

                    // If this is a dry run, don't take any further action.
                    if (dryRun) continue;

                    var putResponse = await client.PutObjectAsync(putRequest);

                    if (putResponse.HttpStatusCode != HttpStatusCode.OK)
                        throw new Exception($"Error putting object '{key}' to S3!");
                }
            }
            else
                Console.WriteLine("No objects to upload.");
        }

        /// <summary>
        /// Get a list of file-key pairs for upload. Any files found to be missing from the mapping provided or any that have a
        /// different hash from the hash provided for that key will be added to the upload set. If force is <see langword="true" />
        /// , this will include all files found in the upload set.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="keyToHash">Map of keys to hashes.</param>
        /// <param name="force">Force upload?</param>
        /// <returns>File-key pairs to upload.</returns>
        private static ICollection<Tuple<string, string>> GetFileKeyPairsToUpload(string path,
            IDictionary<string, string> keyToHash, bool force)
        {
            var fileKeyPairsToUpload = new List<Tuple<string, string>>();

            // Get all files in folder and sub-folders.
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                // Normalize Windows paths to S3 keys.
                var key = Path.GetRelativePath(path, file).Replace('\\', '/');

                // Force upload?
                if (force ||
                    // Is this a new file?
                    !keyToHash.ContainsKey(key) ||
                    // Has this file changed? Compare the hash that S3 has to the local file's hash.
                    !string.Equals(keyToHash[key], FileContentHash(file), StringComparison.OrdinalIgnoreCase))
                    fileKeyPairsToUpload.Add(Tuple.Create(file, key));
            }

            return fileKeyPairsToUpload;
        }

        /// <summary>Get a mapping of keys to file hashes from the S3 bucket.</summary>
        /// <param name="client">S3 client.</param>
        /// <param name="bucket">Bucket.</param>
        /// <returns>Map of keys to hashes.</returns>
        private static async Task<IDictionary<string, string>> GetKeysToHashesFromBucket(IAmazonS3 client,
            string bucket)
        {
            var keyToHash = new Dictionary<string, string>();
            string continuationToken = null;
            do
            {
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = bucket,
                    ContinuationToken = continuationToken
                };

                // List all objects in the bucket from S3
                var listObjectsResponse = await client.ListObjectsV2Async(listRequest);

                if (listObjectsResponse.HttpStatusCode != HttpStatusCode.OK)
                    throw new Exception("Error listing objects from S3!");

                foreach (var s3Object in listObjectsResponse.S3Objects)
                {
                    // ETags are MD5 file hashes surrounded by double quotes.
                    keyToHash[s3Object.Key] = s3Object.ETag.Replace("\"", string.Empty);
                }

                // Do we have another page (S3 will paginate over 1000 items)?
                continuationToken = listObjectsResponse.ContinuationToken;
            } while (continuationToken != null);

            return keyToHash;
        }

        /// <summary>Get the MD5 hash of a file's contents as a string.</summary>
        /// <param name="filePath">Path to file.</param>
        /// <returns>MD5 hash.</returns>
        private static string FileContentHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
        }
    }
}