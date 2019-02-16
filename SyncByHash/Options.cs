using CommandLine;

namespace SyncByHash
{
    /// <summary>Command-line options.</summary>
    public class Options
    {
        /// <summary>Root folder to sync.</summary>
        [Value(0, MetaName = "root", Required = true, HelpText = "Root folder to sync.")]
        public string Root { get; set; }

        /// <summary>S3 Bucket to sync.</summary>
        [Value(1, MetaName = "bucket", Required = true, HelpText = "S3 Bucket to sync.")]
        public string Bucket { get; set; }

        /// <summary>Delete objects not found in the root folder.</summary>
        [Option('d', "delete", Required = false, HelpText = "Delete objects not found in the root folder.")]
        public bool Delete { get; set; }

        /// <summary>Perform a dry run (only performs a list request) and print what would have been done.</summary>
        [Option("dry-run", Required = false,
            HelpText = "Perform a dry run (only performs a list request) and print what would have been done.")]
        public bool DryRun { get; set; }

        /// <summary>Force upload of all files regardless of change status.</summary>
        [Option('f', "force", Required = false, HelpText = "Force upload of all files regardless of change status.")]
        public bool Force { get; set; }
    }
}