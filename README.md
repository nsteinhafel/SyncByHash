# Sync By Hash AWS S3 Utility

The AWS CLI's sync command for an S3 bucket determines if a file has changed by comparing timestamps or comparing by
file sizes which often leads to unnecessary PUT requests to a bucket. This utility instead compares by the hash of the
file contents on the bucket (available via the ETag from S3) to the hash of the file contents on your local file system
to avoid this issue.

This is a work-in-progress for personal use.