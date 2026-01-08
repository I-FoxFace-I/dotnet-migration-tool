namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Service for migrating code between different solutions.
/// Useful for reusing features implemented in one solution in another.
/// </summary>
public interface ICrossSolutionMigrationService
{
    /// <summary>
    /// Migrates a project from source solution to target solution.
    /// </summary>
    /// <param name="options">Migration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing migration details.</returns>
    Task<CrossSolutionMigrationResult> MigrateProjectAsync(
        CrossSolutionMigrationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates a folder from source solution to target solution.
    /// </summary>
    /// <param name="options">Migration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing migration details.</returns>
    Task<CrossSolutionMigrationResult> MigrateFolderAsync(
        CrossSolutionMigrationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates specific files from source solution to target solution.
    /// </summary>
    /// <param name="options">Migration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing migration details.</returns>
    Task<CrossSolutionMigrationResult> MigrateFilesAsync(
        CrossSolutionMigrationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes what would be migrated without performing the migration.
    /// </summary>
    /// <param name="options">Migration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis result with dependencies and potential issues.</returns>
    Task<CrossSolutionAnalysisResult> AnalyzeMigrationAsync(
        CrossSolutionMigrationOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for cross-solution migration.
/// </summary>
public record CrossSolutionMigrationOptions
{
    /// <summary>
    /// Path to the source solution file (.sln).
    /// </summary>
    public required string SourceSolutionPath { get; init; }

    /// <summary>
    /// Path to the target solution file (.sln).
    /// </summary>
    public required string TargetSolutionPath { get; init; }

    /// <summary>
    /// Source path (project, folder, or file) within the source solution.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Target path within the target solution.
    /// </summary>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Old namespace prefix to replace.
    /// If null, will be auto-detected from source path.
    /// </summary>
    public string? OldNamespacePrefix { get; init; }

    /// <summary>
    /// New namespace prefix to use.
    /// If null, will be auto-detected from target path.
    /// </summary>
    public string? NewNamespacePrefix { get; init; }

    /// <summary>
    /// Whether to include dependencies in the migration.
    /// </summary>
    public bool IncludeDependencies { get; init; } = false;

    /// <summary>
    /// Whether to update using directives in the target solution.
    /// </summary>
    public bool UpdateUsings { get; init; } = true;

    /// <summary>
    /// Whether to add the migrated project to the target solution.
    /// </summary>
    public bool AddToTargetSolution { get; init; } = true;

    /// <summary>
    /// Whether to preserve original files (copy mode) or delete them (move mode).
    /// </summary>
    public bool PreserveOriginal { get; init; } = true;

    /// <summary>
    /// If true, only preview changes without applying.
    /// </summary>
    public bool DryRun { get; init; } = false;

    /// <summary>
    /// Patterns for files to exclude from migration (e.g., "*.Designer.cs").
    /// </summary>
    public IReadOnlyList<string> ExcludePatterns { get; init; } = [];

    /// <summary>
    /// NuGet packages to add to the target project.
    /// </summary>
    public IReadOnlyList<AdditionalPackageReference> AdditionalPackages { get; init; } = [];
}

/// <summary>
/// Result of cross-solution migration.
/// </summary>
public record CrossSolutionMigrationResult(
    bool Success,
    string SourceSolution,
    string TargetSolution,
    string SourcePath,
    string TargetPath,
    bool DryRun,
    int MigratedFilesCount,
    IReadOnlyList<MigratedFileInfo> MigratedFiles,
    IReadOnlyList<string> UpdatedNamespaces,
    IReadOnlyList<string> UpdatedUsings,
    IReadOnlyList<string> AddedToSolution,
    IReadOnlyList<DependencyInfo> MigratedDependencies,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage = null);

/// <summary>
/// Information about a migrated file.
/// </summary>
public record MigratedFileInfo(
    string SourcePath,
    string TargetPath,
    bool NamespaceUpdated,
    string? OldNamespace,
    string? NewNamespace);

/// <summary>
/// Information about a dependency.
/// </summary>
public record DependencyInfo(
    string Name,
    DependencyType Type,
    string? Version,
    bool Migrated,
    string? MigrationPath);

/// <summary>
/// Type of dependency.
/// </summary>
public enum DependencyType
{
    Project,
    NuGetPackage,
    Assembly
}

/// <summary>
/// NuGet package reference for cross-solution migration.
/// </summary>
public record AdditionalPackageReference(
    string PackageId,
    string Version);

/// <summary>
/// Result of cross-solution migration analysis.
/// </summary>
public record CrossSolutionAnalysisResult(
    bool CanMigrate,
    string SourceSolution,
    string TargetSolution,
    string SourcePath,
    string TargetPath,
    int FilesToMigrate,
    IReadOnlyList<string> Files,
    IReadOnlyList<DependencyInfo> Dependencies,
    IReadOnlyList<string> MissingDependencies,
    IReadOnlyList<string> ConflictingFiles,
    IReadOnlyList<string> Warnings,
    string? BlockingError = null);
