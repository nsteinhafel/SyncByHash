using Amazon.S3.Model;

namespace SyncByHash.Tests;

/// <summary>
///     Test data builders for creating consistent test objects.
///     Uses the Builder pattern for fluent, readable test setup.
/// </summary>
public static class TestDataBuilders
{
    public static S3ObjectBuilder S3Object()
    {
        return new S3ObjectBuilder();
    }

    public static S3ObjectListBuilder S3ObjectList()
    {
        return new S3ObjectListBuilder();
    }

    public static OptionsBuilder Options()
    {
        return new OptionsBuilder();
    }

    /// <summary>
    ///     Builder for S3Object test data.
    /// </summary>
    public class S3ObjectBuilder
    {
        private string _etag = "\"defaulthash\"";
        private string _key = "default-key.txt";

        public S3ObjectBuilder WithKey(string key)
        {
            _key = key;
            return this;
        }

        public S3ObjectBuilder WithETag(string etag)
        {
            // Ensure ETag has quotes as S3 returns them
            _etag = etag.StartsWith("\"") ? etag : $"\"{etag}\"";
            return this;
        }

        public S3Object Build()
        {
            return new S3Object
            {
                Key = _key,
                ETag = _etag
            };
        }
    }

    /// <summary>
    ///     Builder for creating lists of S3Objects for pagination testing.
    /// </summary>
    public class S3ObjectListBuilder
    {
        private readonly List<S3Object> _objects = [];

        public S3ObjectListBuilder AddObject(string key, string hash)
        {
            _objects.Add(new S3ObjectBuilder()
                .WithKey(key)
                .WithETag(hash)
                .Build());
            return this;
        }

        public S3ObjectListBuilder AddObject(S3Object obj)
        {
            _objects.Add(obj);
            return this;
        }

        public List<S3Object> Build()
        {
            return [.. _objects];
        }
    }

    /// <summary>
    ///     Builder for Options test data.
    /// </summary>
    public class OptionsBuilder
    {
        private string _bucket = "test-bucket";
        private bool _delete;
        private string? _delimiter;
        private bool _dryRun;
        private bool _force;
        private string? _prefix;
        private string _root = "/tmp/test";

        public OptionsBuilder WithRoot(string root)
        {
            _root = root;
            return this;
        }

        public OptionsBuilder WithBucket(string bucket)
        {
            _bucket = bucket;
            return this;
        }

        public OptionsBuilder WithDelete(bool delete = true)
        {
            _delete = delete;
            return this;
        }

        public OptionsBuilder WithDryRun(bool dryRun = true)
        {
            _dryRun = dryRun;
            return this;
        }

        public OptionsBuilder WithForce(bool force = true)
        {
            _force = force;
            return this;
        }

        public OptionsBuilder WithPrefix(string prefix)
        {
            _prefix = prefix;
            return this;
        }

        public OptionsBuilder WithDelimiter(string delimiter)
        {
            _delimiter = delimiter;
            return this;
        }

        public Options Build()
        {
            return new Options
            {
                Root = _root,
                Bucket = _bucket,
                Delete = _delete,
                DryRun = _dryRun,
                Force = _force,
                Prefix = _prefix,
                Delimiter = _delimiter
            };
        }
    }
}
