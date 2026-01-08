namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Service for updating solution (.sln) files.
/// </summary>
public interface ISolutionFileService
{
    /// <summary>
    /// Updates a project path in a solution file.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file.</param>
    /// <param name="oldPath">The old project path to replace.</param>
    /// <param name="newPath">The new project path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing success status.</returns>
    Task<SolutionUpdateResult> UpdateProjectPathAsync(
        string solutionPath, 
        string oldPath, 
        string newPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a solution file update operation.
/// </summary>
public record SolutionUpdateResult(
    bool Success,
    string SolutionPath,
    bool Updated,
    string? Message = null);
