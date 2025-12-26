using System.CommandLine;
using Amazon.RuntimeDependencies;
using Amazon.S3;
using Amazon.SSO;
using Amazon.SSOOIDC;
using SyncByHash;

// Register SSO clients for Native AOT support
GlobalRuntimeDependencyRegistry.Instance.RegisterSSOOIDCClient(() => new AmazonSSOOIDCClient());
GlobalRuntimeDependencyRegistry.Instance.RegisterSSOClient(() => new AmazonSSOClient());

var rootArgument = new Argument<string>("root")
{
    Description = "Root folder to sync"
};

var bucketArgument = new Argument<string>("bucket")
{
    Description = "S3 bucket to sync"
};

var deleteOption = new Option<bool>("--delete")
{
    Description = "Delete objects not found in the root folder",
    Aliases = { "-d" }
};

var dryRunOption = new Option<bool>("--dry-run")
{
    Description = "Perform a dry run and print what would have been done"
};

var forceOption = new Option<bool>("--force")
{
    Description = "Force upload of all files regardless of change status",
    Aliases = { "-f" }
};

var prefixOption = new Option<string?>("--prefix")
{
    Description = "Prefix for S3 bucket"
};

var delimiterOption = new Option<string?>("--delimiter")
{
    Description = "Delimiter for S3 bucket"
};

var rootCommand = new RootCommand("Sync local directories to AWS S3 by comparing file content hashes");
rootCommand.Arguments.Add(rootArgument);
rootCommand.Arguments.Add(bucketArgument);
rootCommand.Options.Add(deleteOption);
rootCommand.Options.Add(dryRunOption);
rootCommand.Options.Add(forceOption);
rootCommand.Options.Add(prefixOption);
rootCommand.Options.Add(delimiterOption);

rootCommand.SetAction(async (parseResult, _) =>
{
    var options = new Options
    {
        Root = parseResult.GetValue(rootArgument)!,
        Bucket = parseResult.GetValue(bucketArgument)!,
        Delete = parseResult.GetValue(deleteOption),
        DryRun = parseResult.GetValue(dryRunOption),
        Force = parseResult.GetValue(forceOption),
        Prefix = parseResult.GetValue(prefixOption),
        Delimiter = parseResult.GetValue(delimiterOption)
    };

    try
    {
        using var client = new AmazonS3Client();
        var service = new SyncByHashService(client);
        await service.Sync(options);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
});

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
