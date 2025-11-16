namespace SyncByHash;

/// <summary>Command-line options.</summary>
public class Options
{
    /// <summary>Root folder to sync.</summary>
    public required string Root { get; set; }

    /// <summary>S3 Bucket to sync.</summary>
    public required string Bucket { get; set; }

    /// <summary>Delete objects not found in the root folder.</summary>
    public bool Delete { get; set; }

    /// <summary>Perform a dry run (only performs a list request) and print what would have been done.</summary>
    public bool DryRun { get; set; }

    /// <summary>Force upload of all files regardless of change status.</summary>
    public bool Force { get; set; }

    /// <summary>Prefix for S3 bucket.</summary>
    public string? Prefix { get; set; }

    /// <summary>Delimiter for S3 bucket.</summary>
    public string? Delimiter { get; set; }
}