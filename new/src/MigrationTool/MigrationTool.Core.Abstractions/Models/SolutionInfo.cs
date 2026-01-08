namespace MigrationTool.Core.Abstractions.Models;

/// <summary>
/// Represents a .NET solution.
/// </summary>
public record SolutionInfo
{
    /// <summary>
    /// Solution name (without extension).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full path to the .sln file.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Directory containing the solution.
    /// </summary>
    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

    /// <summary>
    /// Projects in the solution.
    /// </summary>
    public IReadOnlyList<ProjectInfo> Projects { get; init; } = [];

    /// <summary>
    /// Solution folders (virtual folders).
    /// </summary>
    public IReadOnlyList<SolutionFolder> Folders { get; init; } = [];

    /// <summary>
    /// Total project count.
    /// </summary>
    public int ProjectCount => Projects.Count;

    /// <summary>
    /// Test project count.
    /// </summary>
    public int TestProjectCount => Projects.Count(p => p.IsTestProject);

    /// <summary>
    /// Source (non-test) project count.
    /// </summary>
    public int SourceProjectCount => Projects.Count(p => !p.IsTestProject);
}

/// <summary>
/// Represents a solution folder (virtual folder in .sln).
/// </summary>
public record SolutionFolder(
    string Name,
    string Path,
    IReadOnlyList<string> ProjectPaths
);
