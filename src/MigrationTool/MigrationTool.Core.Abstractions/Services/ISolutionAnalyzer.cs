using MigrationTool.Core.Abstractions.Models;

namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Analyzes .NET solution files.
/// </summary>
public interface ISolutionAnalyzer
{
    /// <summary>
    /// Parses a solution file and returns solution info.
    /// </summary>
    Task<SolutionInfo> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all solution files in a directory.
    /// </summary>
    Task<IEnumerable<string>> FindSolutionsAsync(string directoryPath, CancellationToken cancellationToken = default);
}
