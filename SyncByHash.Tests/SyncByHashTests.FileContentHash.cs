using System.Security.Cryptography;
using Xunit;

namespace SyncByHash.Tests;

/// <summary>
///     Tests for FileContentHash method.
///     Uses FileSystemTestFixture to ensure proper cleanup and state isolation.
/// </summary>
public partial class SyncByHashTests
{
    #region FileContentHash Tests

    [Fact]
    public void FileContentHash_EmptyFile_ReturnsCorrectHash()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var emptyFile = fixture.CreateFile("empty.txt", string.Empty);

        // Act
        string hash;
        using (var md5 = MD5.Create())
        {
            hash = SyncByHashService.FileContentHash(md5, emptyFile);
        }

        // Assert
        // MD5 hash of an empty file is D41D8CD98F00B204E9800998ECF8427E
        Assert.Equal("D41D8CD98F00B204E9800998ECF8427E", hash);
    }

    [Fact]
    public void FileContentHash_FileWithContent_ReturnsCorrectHash()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var file = fixture.CreateFile("hello.txt", "Hello, World!");

        // Act
        string hash;
        using (var md5 = MD5.Create())
        {
            hash = SyncByHashService.FileContentHash(md5, file);
        }

        // Assert
        // MD5 hash of "Hello, World!" is 65A8E27D8879283831B664BD8B7F0AD4
        Assert.Equal("65A8E27D8879283831B664BD8B7F0AD4", hash);
    }

    [Fact]
    public void FileContentHash_DifferentContent_ReturnsDifferentHashes()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var file1 = fixture.CreateFile("file1.txt", "Content 1");
        var file2 = fixture.CreateFile("file2.txt", "Content 2");

        // Act
        string hash1, hash2;
        using (var md5 = MD5.Create())
        {
            hash1 = SyncByHashService.FileContentHash(md5, file1);
            hash2 = SyncByHashService.FileContentHash(md5, file2);
        }

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void FileContentHash_SameContent_ReturnsSameHash()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        const string content = "Same content in both files";
        var file1 = fixture.CreateFile("file1.txt", content);
        var file2 = fixture.CreateFile("file2.txt", content);

        // Act
        string hash1, hash2;
        using (var md5 = MD5.Create())
        {
            hash1 = SyncByHashService.FileContentHash(md5, file1);
            hash2 = SyncByHashService.FileContentHash(md5, file2);
        }

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void FileContentHash_BinaryFile_ReturnsCorrectHash()
    {
        // Arrange
        using var fixture = new FileSystemTestFixture();
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD };
        var file = fixture.CreateBinaryFile("binary.dat", binaryData);

        // Act
        string hash;
        using (var md5 = MD5.Create())
        {
            hash = SyncByHashService.FileContentHash(md5, file);
        }

        // Assert
        // Verify it returns a valid MD5 hash (32 hex characters)
        Assert.Equal(32, hash.Length);
        Assert.Matches("^[A-F0-9]{32}$", hash);
    }

    #endregion
}