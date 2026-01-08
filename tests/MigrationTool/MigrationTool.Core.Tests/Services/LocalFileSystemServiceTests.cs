using FluentAssertions;
using MigrationTool.Core.Services;
using Xunit;

namespace MigrationTool.Core.Tests.Services;

public class LocalFileSystemServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly LocalFileSystemService _service;

    public LocalFileSystemServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "MigrationToolTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _service = new LocalFileSystemService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public async Task ExistsAsync_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "content");

        // Act
        var result = await _service.ExistsAsync(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "nonexistent.txt");

        // Act
        var result = await _service.ExistsAsync(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ExistingDirectory_ReturnsTrue()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "subdir");
        Directory.CreateDirectory(dirPath);

        // Act
        var result = await _service.ExistsAsync(dirPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ReadFileAsync_ExistingFile_ReturnsContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "test.txt");
        var content = "Hello, World!";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await _service.ReadFileAsync(filePath);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public async Task WriteFileAsync_NewFile_CreatesFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "new.txt");
        var content = "New content";

        // Act
        await _service.WriteFileAsync(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        (await File.ReadAllTextAsync(filePath)).Should().Be(content);
    }

    [Fact]
    public async Task WriteFileAsync_NestedPath_CreatesDirectories()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "a", "b", "c", "file.txt");
        var content = "Nested content";

        // Act
        await _service.WriteFileAsync(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task GetFilesAsync_ReturnsMatchingFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDir, "file1.cs"), "");
        await File.WriteAllTextAsync(Path.Combine(_testDir, "file2.cs"), "");
        await File.WriteAllTextAsync(Path.Combine(_testDir, "file3.txt"), "");

        // Act
        var result = (await _service.GetFilesAsync(_testDir, "*.cs", false)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.All(f => f.EndsWith(".cs")).Should().BeTrue();
    }

    [Fact]
    public async Task GetFilesAsync_Recursive_FindsNestedFiles()
    {
        // Arrange
        var subDir = Path.Combine(_testDir, "sub");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(_testDir, "root.cs"), "");
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.cs"), "");

        // Act
        var result = (await _service.GetFilesAsync(_testDir, "*.cs", true)).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDirectoriesAsync_ReturnsSubdirectories()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testDir, "dir1"));
        Directory.CreateDirectory(Path.Combine(_testDir, "dir2"));
        await File.WriteAllTextAsync(Path.Combine(_testDir, "file.txt"), "");

        // Act
        var result = (await _service.GetDirectoriesAsync(_testDir)).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task IsDirectoryAsync_Directory_ReturnsTrue()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "subdir");
        Directory.CreateDirectory(dirPath);

        // Act
        var result = await _service.IsDirectoryAsync(dirPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsDirectoryAsync_File_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "file.txt");
        await File.WriteAllTextAsync(filePath, "");

        // Act
        var result = await _service.IsDirectoryAsync(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CopyAsync_File_CopiesFile()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDir, "source.txt");
        var destPath = Path.Combine(_testDir, "dest.txt");
        await File.WriteAllTextAsync(sourcePath, "content");

        // Act
        await _service.CopyAsync(sourcePath, destPath);

        // Assert
        File.Exists(sourcePath).Should().BeTrue();
        File.Exists(destPath).Should().BeTrue();
        (await File.ReadAllTextAsync(destPath)).Should().Be("content");
    }

    [Fact]
    public async Task MoveAsync_File_MovesFile()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDir, "source.txt");
        var destPath = Path.Combine(_testDir, "dest.txt");
        await File.WriteAllTextAsync(sourcePath, "content");

        // Act
        await _service.MoveAsync(sourcePath, destPath);

        // Assert
        File.Exists(sourcePath).Should().BeFalse();
        File.Exists(destPath).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_File_DeletesFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "todelete.txt");
        await File.WriteAllTextAsync(filePath, "content");

        // Act
        await _service.DeleteAsync(filePath);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Directory_DeletesDirectory()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "todelete");
        Directory.CreateDirectory(dirPath);
        await File.WriteAllTextAsync(Path.Combine(dirPath, "file.txt"), "content");

        // Act
        await _service.DeleteAsync(dirPath, recursive: true);

        // Assert
        Directory.Exists(dirPath).Should().BeFalse();
    }

    [Fact]
    public async Task CreateDirectoryAsync_CreatesDirectory()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "newdir");

        // Act
        await _service.CreateDirectoryAsync(dirPath);

        // Assert
        Directory.Exists(dirPath).Should().BeTrue();
    }

    [Fact]
    public async Task GetFileMetadataAsync_File_ReturnsMetadata()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "meta.txt");
        await File.WriteAllTextAsync(filePath, "content");

        // Act
        var result = await _service.GetFileMetadataAsync(filePath);

        // Assert
        result.Path.Should().Be(filePath);
        result.Name.Should().Be("meta.txt");
        result.Size.Should().Be(7); // "content" = 7 bytes
        result.IsDirectory.Should().BeFalse();
    }

    [Fact]
    public async Task GetFileMetadataAsync_Directory_ReturnsMetadata()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "metadir");
        Directory.CreateDirectory(dirPath);

        // Act
        var result = await _service.GetFileMetadataAsync(dirPath);

        // Assert
        result.Name.Should().Be("metadir");
        result.IsDirectory.Should().BeTrue();
    }
}
