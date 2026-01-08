namespace MigrationTool.Tests.Infrastructure.Fixtures;

/// <summary>
/// Creates a temporary directory for tests and cleans it up on dispose.
/// </summary>
public sealed class TempDirectoryFixture : IDisposable
{
    /// <summary>
    /// Path to the temporary directory.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Creates a new temporary directory.
    /// </summary>
    public TempDirectoryFixture()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "MigrationToolTests",
            Guid.NewGuid().ToString("N")[..8]); // Short GUID for readability

        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Creates a subdirectory in the temp directory.
    /// </summary>
    public string CreateSubdirectory(string name)
    {
        var path = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Creates a file in the temp directory.
    /// </summary>
    public string CreateFile(string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    /// <summary>
    /// Gets full path for a relative path.
    /// </summary>
    public string GetPath(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    /// <summary>
    /// Deletes the temporary directory and all its contents.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
