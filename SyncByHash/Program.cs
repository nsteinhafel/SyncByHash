using System.CommandLine;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncByHash;

var rootArgument = new Argument<string>("root")
{
    Description = "Root folder to sync"
};

var bucketArgument = new Argument<string>("bucket")
{
    Description = "S3 bucket to sync"
};

var deleteOption = new Option<bool>("--delete", "-d")
{
    Description = "Delete objects not found in the root folder"
};

var dryRunOption = new Option<bool>("--dry-run")
{
    Description = "Perform a dry run and print what would have been done"
};

var forceOption = new Option<bool>("--force", "-f")
{
    Description = "Force upload of all files regardless of change status"
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

rootCommand.SetAction(async (parseResult, token) =>
{
    var root = parseResult.GetValue(rootArgument)!;
    var bucket = parseResult.GetValue(bucketArgument)!;
    var delete = parseResult.GetValue(deleteOption);
    var dryRun = parseResult.GetValue(dryRunOption);
    var force = parseResult.GetValue(forceOption);
    var prefix = parseResult.GetValue(prefixOption);
    var delimiter = parseResult.GetValue(delimiterOption);

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<IAmazonS3, AmazonS3Client>();
            services.AddSingleton<SyncByHashService>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        })
        .Build();

    var service = host.Services.GetRequiredService<SyncByHashService>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var options = new Options
    {
        Root = root,
        Bucket = bucket,
        Delete = delete,
        DryRun = dryRun,
        Force = force,
        Prefix = prefix,
        Delimiter = delimiter
    };

    try
    {
        await service.Sync(options);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Sync failed: {Message}", ex.Message);
        throw;
    }
});

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
