using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Core.Services;

/// <summary>
/// Local file system implementation for desktop/server environments.
/// </summary>
public class LocalFileSystemService : IFileSystemService
{
    /// <inheritdoc />
    public Task<IEnumerable<string>> GetFilesAsync(string path, string pattern = "*.*", bool recursive = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(path))
        {
            return Task.FromResult<IEnumerable<string>>([]);
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(path, pattern, searchOption);

        return Task.FromResult<IEnumerable<string>>(files);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(path) || Directory.Exists(path));
    }

    /// <inheritdoc />
    public Task<bool> IsDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Directory.Exists(path));
    }

    /// <inheritdoc />
    public Task MoveAsync(string source, string destination, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Ensure parent directory exists for destination
        var destDir = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        if (File.Exists(source))
        {
            File.Move(source, destination);
        }
        else if (Directory.Exists(source))
        {
            Directory.Move(source, destination);
        }
        else
        {
            throw new FileNotFoundException($"Source not found: {source}");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CopyAsync(string source, string destination, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(source))
        {
            var destDir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            File.Copy(source, destination, overwrite);
        }
        else if (Directory.Exists(source))
        {
            CopyDirectory(source, destination, overwrite);
        }
        else
        {
            throw new FileNotFoundException($"Source not found: {source}");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(path))
        {
            File.Delete(path);
        }
        else if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(path))
        {
            return Task.FromResult<IEnumerable<string>>([]);
        }

        var directories = Directory.GetDirectories(path);
        return Task.FromResult<IEnumerable<string>>(directories);
    }

    /// <inheritdoc />
    public Task<FileMetadata> GetFileMetadataAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            return Task.FromResult(new FileMetadata(
                path,
                info.Name,
                info.Length,
                info.CreationTimeUtc,
                info.LastWriteTimeUtc,
                false
            ));
        }

        if (Directory.Exists(path))
        {
            var info = new DirectoryInfo(path);
            return Task.FromResult(new FileMetadata(
                path,
                info.Name,
                0,
                info.CreationTimeUtc,
                info.LastWriteTimeUtc,
                true
            ));
        }

        throw new FileNotFoundException($"Path not found: {path}");
    }

    private static void CopyDirectory(string source, string destination, bool overwrite)
    {
        var dir = new DirectoryInfo(source);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {source}");
        }

        Directory.CreateDirectory(destination);

        foreach (var file in dir.GetFiles())
        {
            var targetPath = Path.Combine(destination, file.Name);
            file.CopyTo(targetPath, overwrite);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            var targetPath = Path.Combine(destination, subDir.Name);
            CopyDirectory(subDir.FullName, targetPath, overwrite);
        }
    }
}
