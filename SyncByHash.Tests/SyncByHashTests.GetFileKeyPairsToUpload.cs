using System.Security.Cryptography;
using Xunit;

namespace SyncByHash.Tests;

/// <summary>
///     Tests for GetFileKeyPairsToUpload method.
///     Uses FileSystemTestFixture for file system isolation and guaranteed cleanup.
/// </summary>
public partial class SyncByHashTests
{
    #region GetFileKeyPairsToUpload Tests

    [Fact]
    public void GetFileKeyPairsToUpload_EmptyDirectory_ReturnsEmptyCollection()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var keyToHash = new Dictionary<string, string>();

        // Act
        var result = SyncByHashService.GetFileKeyPairsToUpload(fixture.RootDirectory, keyToHash);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetFileKeyPairsToUpload_NewFiles_ReturnsAllFiles()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        fixture.CreateFile("file1.txt", "Content 1");
        fixture.CreateFile("file2.txt", "Content 2");
        var keyToHash = new Dictionary<string, string>();

        // Act
        var result = SyncByHashService.GetFileKeyPairsToUpload(fixture.RootDirectory, keyToHash);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetFileKeyPairsToUpload_UnchangedFiles_ReturnsEmpty()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var filePath = fixture.CreateFile("file1.txt", "Content");

        // Calculate the hash
        string hash;
        using (var md5 = MD5.Create())
        {
            hash = SyncByHashService.FileContentHash(md5, filePath);
        }

        // Simulate that S3 already has this file with the same hash
        var keyToHash = new Dictionary<string, string>
        {
            { "file1.txt", hash }
        };

        // Act
        var result = SyncByHashService.GetFileKeyPairsToUpload(fixture.RootDirectory, keyToHash);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetFileKeyPairsToUpload_ChangedFiles_ReturnsChangedFiles()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        fixture.CreateFile("file1.txt", "New Content");

        // S3 has the file but with a different hash
        var keyToHash = new Dictionary<string, string>
        {
            { "file1.txt", "oldhash123" }
        };

        // Act
        var result = SyncByHashService.GetFileKeyPairsToUpload(fixture.RootDirectory, keyToHash);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void GetFileKeyPairsToUpload_ForceUpload_ReturnsAllFiles()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var file1 = fixture.CreateFile("file1.txt", "Content 1");
        var file2 = fixture.CreateFile("file2.txt", "Content 2");

        // Calculate hashes
        string hash1, hash2;
        using (var md5 = MD5.Create())
        {
            hash1 = SyncByHashService.FileContentHash(md5, file1);
            hash2 = SyncByHashService.FileContentHash(md5, file2);
        }

        // S3 already has both files with matching hashes
        var keyToHash = new Dictionary<string, string>
        {
            { "file1.txt", hash1 },
            { "file2.txt", hash2 }
        };

        // Act
        // Force upload should return all files even though hashes match
        var result = SyncByHashService.GetFileKeyPairsToUpload(
            fixture.RootDirectory, keyToHash, force: true);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetFileKeyPairsToUpload_WithPrefix_AddsPrefix()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        fixture.CreateFile("file1.txt", "Content");
        var keyToHash = new Dictionary<string, string>();

        // Act
        var result = SyncByHashService.GetFileKeyPairsToUpload(
            fixture.RootDirectory, keyToHash, "prefix/");

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.StartsWith("prefix/", resultList[0].Item2);
    }

    [Fact]
    public void GetFileKeyPairsToUpload_NestedDirectories_ReturnsAllFiles()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        fixture.CreateFile("file1.txt", "Content 1");
        fixture.CreateFile("file2.txt", "Content 2", "subdir");
        var keyToHash = new Dictionary<string, string>();

        // Act
        var result = SyncByHashService.GetFileKeyPairsToUpload(fixture.RootDirectory, keyToHash);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetFileKeyPairsToUpload_WindowsBackslashes_ConvertsToForwardSlashes()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        fixture.CreateFile("file.txt", "Content", "subdir");
        var keyToHash = new Dictionary<string, string>();

        // Act
        var result = SyncByHashService.GetFileKeyPairsToUpload(fixture.RootDirectory, keyToHash);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        // S3 keys should use forward slashes, not backslashes
        Assert.Contains("/", resultList[0].Item2);
        Assert.DoesNotContain("\\", resultList[0].Item2);
    }

    #endregion
}