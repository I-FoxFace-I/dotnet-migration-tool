using MigrationTool.Core.Abstractions.Models;

namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Analyzes .NET project files (.csproj).
/// </summary>
public interface IProjectAnalyzer
{
    /// <summary>
    /// Parses a project file and returns project info.
    /// </summary>
    Task<ProjectInfo> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enriches project info with additional details (files, classes, etc.).
    /// </summary>
    Task<ProjectInfo> EnrichProjectAsync(ProjectInfo project, CancellationToken cancellationToken = default);
}
