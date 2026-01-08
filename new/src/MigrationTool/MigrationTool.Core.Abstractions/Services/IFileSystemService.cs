namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Abstraction for file system operations.
/// Different implementations for Server (full access), WASM (upload-based), MAUI (platform-specific).
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Gets all files matching a pattern in a directory.
    /// </summary>
    Task<IEnumerable<string>> GetFilesAsync(string path, string pattern = "*.*", bool recursive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads file content as string.
    /// </summary>
    Task<string> ReadFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes content to a file.
    /// </summary>
    Task WriteFileAsync(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file or directory exists.
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if path is a directory.
    /// </summary>
    Task<bool> IsDirectoryAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file or directory.
    /// </summary>
    Task MoveAsync(string source, string destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file or directory.
    /// </summary>
    Task CopyAsync(string source, string destination, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file or directory.
    /// </summary>
    Task DeleteAsync(string path, bool recursive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a directory.
    /// </summary>
    Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subdirectories.
    /// </summary>
    Task<IEnumerable<string>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file info (size, dates, etc.).
    /// </summary>
    Task<FileMetadata> GetFileMetadataAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Basic file metadata.
/// </summary>
public record FileMetadata(
    string Path,
    string Name,
    long Size,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    bool IsDirectory
);
