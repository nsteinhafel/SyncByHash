namespace SyncByHash.Tests;

/// <summary>
///     Fixture for tests that need file system isolation.
///     Implements IDisposable to ensure cleanup even if tests fail.
///     Each fixture creates an isolated temporary directory.
/// </summary>
public sealed class FileSystemTestFixture : IDisposable
{
    private readonly List<string> _tempDirectories = new();
    private readonly List<string> _tempFiles = new();
    private bool _disposed;

    public FileSystemTestFixture()
    {
        // Create a unique temporary directory for this test
        RootDirectory = Path.Combine(Path.GetTempPath(), $"SyncByHashTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(RootDirectory);
        _tempDirectories.Add(RootDirectory);
    }

    /// <summary>
    ///     Root temporary directory for this test fixture.
    ///     All files and directories created by this fixture are under this root.
    /// </summary>
    public string RootDirectory { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Clean up all created resources
        foreach (var file in _tempFiles)
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // Best effort cleanup
            }

        // Delete directories in reverse order (deepest first)
        _tempDirectories.Reverse();
        foreach (var directory in _tempDirectories)
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch
            {
                // Best effort cleanup
            }

        _disposed = true;
    }

    /// <summary>
    ///     Creates a temporary file with optional content.
    ///     File will be automatically cleaned up when fixture is disposed.
    /// </summary>
    /// <param name="fileName">Name of the file (not full path)</param>
    /// <param name="content">Optional file content</param>
    /// <param name="subdirectory">Optional subdirectory under root</param>
    /// <returns>Full path to the created file</returns>
    public string CreateFile(string fileName, string? content = null, string? subdirectory = null)
    {
        var directory = subdirectory != null
            ? Path.Combine(RootDirectory, subdirectory)
            : RootDirectory;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _tempDirectories.Add(directory);
        }

        var filePath = Path.Combine(directory, fileName);

        if (content != null)
            File.WriteAllText(filePath, content);
        else
            File.Create(filePath).Dispose();

        _tempFiles.Add(filePath);
        return filePath;
    }

    /// <summary>
    ///     Creates a temporary file with binary content.
    ///     File will be automatically cleaned up when fixture is disposed.
    /// </summary>
    public string CreateBinaryFile(string fileName, byte[] content, string? subdirectory = null)
    {
        var directory = subdirectory != null
            ? Path.Combine(RootDirectory, subdirectory)
            : RootDirectory;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _tempDirectories.Add(directory);
        }

        var filePath = Path.Combine(directory, fileName);
        File.WriteAllBytes(filePath, content);

        _tempFiles.Add(filePath);
        return filePath;
    }

    /// <summary>
    ///     Creates a subdirectory under the root directory.
    ///     Directory will be automatically cleaned up when fixture is disposed.
    /// </summary>
    public string CreateSubdirectory(string name)
    {
        var path = Path.Combine(RootDirectory, name);
        Directory.CreateDirectory(path);
        _tempDirectories.Add(path);
        return path;
    }

    /// <summary>
    ///     Gets the full path for a file/directory relative to the root.
    ///     Does not create the file/directory.
    /// </summary>
    public string GetPath(params string[] pathParts)
    {
        return Path.Combine(RootDirectory, Path.Combine(pathParts));
    }
}