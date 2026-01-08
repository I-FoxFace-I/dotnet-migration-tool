namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Service for updating project references in .csproj files.
/// </summary>
public interface IProjectRefService
{
    /// <summary>
    /// Updates a project reference path in a .csproj file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="oldRef">The old reference path to replace.</param>
    /// <param name="newRef">The new reference path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing success status.</returns>
    Task<ProjectRefUpdateResult> UpdateReferenceAsync(
        string projectPath, 
        string oldRef, 
        string newRef,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a project reference update operation.
/// </summary>
public record ProjectRefUpdateResult(
    bool Success,
    string ProjectPath,
    string? OldRef = null,
    string? NewRef = null,
    string? Message = null);
