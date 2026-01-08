namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Service for file-level operations (move, copy, delete, rename).
/// </summary>
public interface IFileOperationService
{
    /// <summary>
    /// Moves a file to a new location and optionally updates namespace.
    /// </summary>
    /// <param name="source">Source file path.</param>
    /// <param name="target">Target file path.</param>
    /// <param name="updateNamespace">Whether to update namespace in C# files.</param>
    /// <param name="dryRun">If true, only preview changes without applying.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing operation details.</returns>
    Task<FileOperationResult> MoveFileAsync(
        string source,
        string target,
        bool updateNamespace = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file to a new location and optionally updates namespace.
    /// </summary>
    /// <param name="source">Source file path.</param>
    /// <param name="target">Target file path.</param>
    /// <param name="updateNamespace">Whether to update namespace in C# files.</param>
    /// <param name="dryRun">If true, only preview changes without applying.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing operation details.</returns>
    Task<FileOperationResult> CopyFileAsync(
        string source,
        string target,
        bool updateNamespace = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file with optional reference checking.
    /// </summary>
    /// <param name="path">File path to delete.</param>
    /// <param name="checkReferences">Whether to check for references before deletion.</param>
    /// <param name="dryRun">If true, only preview changes without applying.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing operation details.</returns>
    Task<FileOperationResult> DeleteFileAsync(
        string path,
        bool checkReferences = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a file and optionally renames the class inside.
    /// </summary>
    /// <param name="path">File path to rename.</param>
    /// <param name="newName">New file name (without path).</param>
    /// <param name="renameClass">Whether to rename the main class to match the new file name.</param>
    /// <param name="dryRun">If true, only preview changes without applying.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing operation details.</returns>
    Task<FileOperationResult> RenameFileAsync(
        string path,
        string newName,
        bool renameClass = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a folder with optional reference checking.
    /// </summary>
    /// <param name="path">Folder path to delete.</param>
    /// <param name="checkReferences">Whether to check for references before deletion.</param>
    /// <param name="dryRun">If true, only preview changes without applying.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing operation details.</returns>
    Task<FileOperationResult> DeleteFolderAsync(
        string path,
        bool checkReferences = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a file operation.
/// </summary>
public record FileOperationResult(
    bool Success,
    FileOperationType Operation,
    string Path,
    string? TargetPath,
    bool DryRun,
    string? OldNamespace,
    string? NewNamespace,
    bool NamespaceUpdated,
    bool ClassRenamed,
    IReadOnlyList<string> AffectedFiles,
    IReadOnlyList<string> ReferencingFiles,
    string? ErrorMessage = null);

/// <summary>
/// Type of file operation.
/// </summary>
public enum FileOperationType
{
    Move,
    Copy,
    Delete,
    Rename,
    DeleteFolder
}
