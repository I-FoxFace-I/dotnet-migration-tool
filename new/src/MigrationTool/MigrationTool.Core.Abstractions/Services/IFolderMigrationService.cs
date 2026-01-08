namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Service for migrating folders (move or copy) with namespace updates.
/// </summary>
public interface IFolderMigrationService
{
    /// <summary>
    /// Moves a folder and updates namespaces in C# files.
    /// Original files are deleted after successful move.
    /// </summary>
    /// <param name="source">Source folder path.</param>
    /// <param name="target">Target folder path.</param>
    /// <param name="dryRun">If true, only preview changes without applying.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing migration details.</returns>
    Task<FolderMigrationResult> MoveFolderAsync(
        string source, 
        string target, 
        bool dryRun = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Copies a folder and updates namespaces in C# files.
    /// Original files are preserved.
    /// </summary>
    /// <param name="source">Source folder path.</param>
    /// <param name="target">Target folder path.</param>
    /// <param name="dryRun">If true, only preview changes without applying.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing migration details.</returns>
    Task<FolderMigrationResult> CopyFolderAsync(
        string source, 
        string target, 
        bool dryRun = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a folder migration operation.
/// </summary>
public record FolderMigrationResult(
    bool Success,
    string Source,
    string Target,
    bool DryRun,
    int FilesCount,
    IReadOnlyList<string> Files,
    string? OldNamespace,
    string? NewNamespace,
    IReadOnlyList<string> UpdatedNamespaces,
    bool OriginalPreserved,
    string? ErrorMessage = null);
