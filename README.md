# SyncByHash

Fast S3 sync utility that uses content hashes (MD5) instead of timestamps to determine which files need uploading.

> **Note**: This is a work-in-progress tool created for personal use. Use at your own risk.

## Why?

AWS CLI's `aws s3 sync` compares timestamps and file sizes, often triggering unnecessary uploads. SyncByHash compares MD5 hashes of local files against S3 ETags to upload only what's actually changed.

## Features

- **Hash-based comparison**: Only uploads files that have actually changed
- **Native AOT compilation**: Single 14MB executable, no .NET runtime required
- **Fast startup**: Native binary with minimal overhead
- **Dry run mode**: Preview changes before executing
- **Prefix/delimiter support**: Sync subsets of your bucket
- **Force mode**: Re-upload everything regardless of hash

## Installation

### Build from source

```bash
dotnet publish -c Release
```

Output: `SyncByHash/bin/Release/net10.0/{runtime}/publish/syncbyhash`

### Requirements

- .NET 10 SDK (for building)
- AWS credentials configured (same as AWS CLI)

## Usage

```bash
syncbyhash <root-folder> <bucket> [options]
```

### Examples

```bash
# Basic sync
syncbyhash ./website my-s3-bucket

# Dry run to preview changes
syncbyhash ./website my-s3-bucket --dry-run

# Sync with deletion of remote files not in local folder
syncbyhash ./website my-s3-bucket --delete

# Force upload all files
syncbyhash ./website my-s3-bucket --force

# Sync to a prefix
syncbyhash ./dist my-bucket --prefix cdn/v2/
```

### Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--delete` | `-d` | Delete S3 objects not found locally |
| `--dry-run` | | Preview actions without executing |
| `--force` | `-f` | Upload all files regardless of hash |
| `--prefix` | | S3 key prefix filter |
| `--delimiter` | | S3 key delimiter filter |

## How it works

1. Lists all objects in the S3 bucket (with optional prefix/delimiter)
2. Extracts MD5 hashes from S3 ETags
3. Computes MD5 hashes of all local files
4. Compares hashes to determine what needs uploading
5. Uploads only changed files with correct MIME types
6. Optionally deletes remote files not present locally

## License

MIT
