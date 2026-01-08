namespace MigrationTool.Core.Abstractions.Graph;

/// <summary>
/// Builds a dependency graph from .NET solutions.
/// </summary>
public interface ISolutionGraphBuilder
{
    /// <summary>
    /// Build a graph from a single solution.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file.</param>
    /// <param name="options">Build options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The built solution graph.</returns>
    Task<SolutionGraph> BuildGraphAsync(
        string solutionPath,
        GraphBuildOptions? options = null,
        IProgress<GraphBuildProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Build a graph from multiple solutions (for cross-solution analysis).
    /// </summary>
    /// <param name="solutionPaths">Paths to the .sln files.</param>
    /// <param name="options">Build options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The built solution graph containing all solutions.</returns>
    Task<SolutionGraph> BuildGraphAsync(
        IEnumerable<string> solutionPaths,
        GraphBuildOptions? options = null,
        IProgress<GraphBuildProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for building the solution graph.
/// </summary>
public record GraphBuildOptions
{
    /// <summary>
    /// Whether to analyze type usages (slower but more detailed).
    /// </summary>
    public bool AnalyzeTypeUsages { get; init; } = true;

    /// <summary>
    /// Whether to analyze using directives.
    /// </summary>
    public bool AnalyzeUsingDirectives { get; init; } = true;

    /// <summary>
    /// Whether to include private types.
    /// </summary>
    public bool IncludePrivateTypes { get; init; } = false;

    /// <summary>
    /// Whether to include generated files (*.g.cs, *.generated.cs).
    /// </summary>
    public bool IncludeGeneratedFiles { get; init; } = false;

    /// <summary>
    /// File patterns to exclude.
    /// </summary>
    public IReadOnlyList<string> ExcludePatterns { get; init; } = new[]
    {
        "**/bin/**",
        "**/obj/**",
        "**/.vs/**"
    };

    /// <summary>
    /// Maximum depth for type usage analysis (to prevent infinite loops in large codebases).
    /// </summary>
    public int MaxTypeUsageDepth { get; init; } = 3;

    /// <summary>
    /// Whether to use parallel processing.
    /// </summary>
    public bool UseParallelProcessing { get; init; } = true;

    /// <summary>
    /// Default options.
    /// </summary>
    public static GraphBuildOptions Default => new();

    /// <summary>
    /// Fast options (skip detailed analysis).
    /// </summary>
    public static GraphBuildOptions Fast => new()
    {
        AnalyzeTypeUsages = false,
        AnalyzeUsingDirectives = false,
        IncludePrivateTypes = false,
        IncludeGeneratedFiles = false
    };

    /// <summary>
    /// Full options (include everything).
    /// </summary>
    public static GraphBuildOptions Full => new()
    {
        AnalyzeTypeUsages = true,
        AnalyzeUsingDirectives = true,
        IncludePrivateTypes = true,
        IncludeGeneratedFiles = true,
        MaxTypeUsageDepth = 5
    };
}

/// <summary>
/// Progress information during graph building.
/// </summary>
public record GraphBuildProgress
{
    /// <summary>
    /// Current phase of the build.
    /// </summary>
    public GraphBuildPhase Phase { get; init; }

    /// <summary>
    /// Current item being processed.
    /// </summary>
    public string CurrentItem { get; init; } = string.Empty;

    /// <summary>
    /// Current progress (0-100).
    /// </summary>
    public int ProgressPercent { get; init; }

    /// <summary>
    /// Items processed so far.
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// Total items to process.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string Message => $"[{Phase}] {CurrentItem} ({ProcessedCount}/{TotalCount})";
}

/// <summary>
/// Phases of graph building.
/// </summary>
public enum GraphBuildPhase
{
    LoadingSolution,
    AnalyzingProjects,
    AnalyzingFiles,
    AnalyzingTypes,
    AnalyzingUsages,
    BuildingEdges,
    Completed
}
