using Amazon.S3;
using CommandLine;

namespace SyncByHash
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(x => SyncByHash.Sync(new AmazonS3Client(), x).Wait());
        }
    }
}