namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Service for updating namespaces in C# files.
/// </summary>
public interface INamespaceService
{
    /// <summary>
    /// Updates namespaces in a C# file.
    /// </summary>
    /// <param name="filePath">Path to the C# file.</param>
    /// <param name="oldNamespace">The namespace to replace.</param>
    /// <param name="newNamespace">The new namespace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing success status and number of changes.</returns>
    Task<NamespaceUpdateResult> UpdateNamespaceAsync(
        string filePath, 
        string oldNamespace, 
        string newNamespace,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a namespace update operation.
/// </summary>
public record NamespaceUpdateResult(
    bool Success,
    string FilePath,
    int ChangesCount,
    string? Message = null);
