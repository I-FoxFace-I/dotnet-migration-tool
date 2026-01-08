namespace MigrationTool.Core.Abstractions.Graph;

/// <summary>
/// Analyzes the impact of migration operations on the codebase.
/// </summary>
public interface IImpactAnalyzer
{
    /// <summary>
    /// Analyze the impact of moving a file or folder.
    /// </summary>
    Task<ImpactReport> AnalyzeMoveAsync(
        SolutionGraph graph,
        MoveOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze the impact of renaming a namespace.
    /// </summary>
    Task<ImpactReport> AnalyzeRenameNamespaceAsync(
        SolutionGraph graph,
        RenameNamespaceOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze the impact of deleting a file or folder.
    /// </summary>
    Task<ImpactReport> AnalyzeDeleteAsync(
        SolutionGraph graph,
        DeleteOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze the impact of moving a type to a different namespace.
    /// </summary>
    Task<ImpactReport> AnalyzeMoveTypeAsync(
        SolutionGraph graph,
        MoveTypeOperation operation,
        CancellationToken cancellationToken = default);
}

#region Operations

/// <summary>
/// Base class for migration operations.
/// </summary>
public abstract record MigrationOperation
{
    /// <summary>
    /// Human-readable description of the operation.
    /// </summary>
    public abstract string Description { get; }
}

/// <summary>
/// Move a file or folder to a new location.
/// </summary>
public record MoveOperation(
    string SourcePath,
    string TargetPath,
    string? NewNamespace = null
) : MigrationOperation
{
    public override string Description => $"Move {Path.GetFileName(SourcePath)} to {TargetPath}";
    
    /// <summary>
    /// Whether this is a folder move (vs single file).
    /// </summary>
    public bool IsFolder => Directory.Exists(SourcePath);
}

/// <summary>
/// Rename a namespace across the codebase.
/// </summary>
public record RenameNamespaceOperation(
    string OldNamespace,
    string NewNamespace
) : MigrationOperation
{
    public override string Description => $"Rename namespace {OldNamespace} to {NewNamespace}";
}

/// <summary>
/// Delete a file or folder.
/// </summary>
public record DeleteOperation(
    string Path,
    bool Force = false
) : MigrationOperation
{
    public override string Description => $"Delete {System.IO.Path.GetFileName(Path)}";
    
    /// <summary>
    /// Whether this is a folder delete (vs single file).
    /// </summary>
    public bool IsFolder => Directory.Exists(Path);
}

/// <summary>
/// Move a type to a different namespace.
/// </summary>
public record MoveTypeOperation(
    string TypeFullName,
    string NewNamespace,
    string? NewFilePath = null
) : MigrationOperation
{
    public override string Description => $"Move type {TypeFullName} to {NewNamespace}";
}

#endregion

#region Impact Report

/// <summary>
/// Report detailing the impact of a migration operation.
/// </summary>
public record ImpactReport
{
    /// <summary>
    /// The operation being analyzed.
    /// </summary>
    public required MigrationOperation Operation { get; init; }

    /// <summary>
    /// Whether the operation can proceed without errors.
    /// </summary>
    public bool CanProceed => !Errors.Any();

    /// <summary>
    /// Complexity assessment.
    /// </summary>
    public MigrationComplexity Complexity { get; init; } = MigrationComplexity.Simple;

    /// <summary>
    /// Files that will be directly modified or moved.
    /// </summary>
    public IReadOnlyList<AffectedFile> AffectedFiles { get; init; } = [];

    /// <summary>
    /// Types that will be affected.
    /// </summary>
    public IReadOnlyList<AffectedType> AffectedTypes { get; init; } = [];

    /// <summary>
    /// Project references that need to be added.
    /// </summary>
    public IReadOnlyList<RequiredProjectReference> RequiredProjectReferences { get; init; } = [];

    /// <summary>
    /// Package references that need to be added.
    /// </summary>
    public IReadOnlyList<RequiredPackageReference> RequiredPackageReferences { get; init; } = [];

    /// <summary>
    /// Warnings (non-blocking issues).
    /// </summary>
    public IReadOnlyList<MigrationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Errors (blocking issues).
    /// </summary>
    public IReadOnlyList<MigrationError> Errors { get; init; } = [];

    #region Summary Properties

    public int AffectedFilesCount => AffectedFiles.Count;
    public int AffectedTypesCount => AffectedTypes.Count;
    public int AffectedProjectsCount => AffectedFiles.Select(f => f.ProjectPath).Distinct().Count();
    public int RequiredChangesCount => AffectedFiles.Sum(f => f.RequiredChanges.Count);

    #endregion

    /// <summary>
    /// Generate a markdown report.
    /// </summary>
    public string ToMarkdown()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("# Migration Impact Report");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Value |");
        sb.AppendLine($"|--------|-------|");
        sb.AppendLine($"| Operation | {Operation.Description} |");
        sb.AppendLine($"| Complexity | {Complexity} |");
        sb.AppendLine($"| Can Proceed | {(CanProceed ? "✅ Yes" : "❌ No")} |");
        sb.AppendLine($"| Affected Files | {AffectedFilesCount} |");
        sb.AppendLine($"| Affected Types | {AffectedTypesCount} |");
        sb.AppendLine($"| Affected Projects | {AffectedProjectsCount} |");
        sb.AppendLine($"| Required Changes | {RequiredChangesCount} |");
        sb.AppendLine();

        if (Errors.Any())
        {
            sb.AppendLine("## ❌ Errors (Must Fix)");
            sb.AppendLine();
            foreach (var error in Errors)
            {
                sb.AppendLine($"- **{error.Code}**: {error.Message}");
                if (error.FilePath != null)
                    sb.AppendLine($"  - File: `{error.FilePath}`");
            }
            sb.AppendLine();
        }

        if (Warnings.Any())
        {
            sb.AppendLine("## ⚠️ Warnings");
            sb.AppendLine();
            foreach (var warning in Warnings)
            {
                sb.AppendLine($"- **{warning.Code}**: {warning.Message}");
                if (warning.FilePath != null)
                    sb.AppendLine($"  - File: `{warning.FilePath}`");
            }
            sb.AppendLine();
        }

        if (AffectedFiles.Any())
        {
            sb.AppendLine("## Affected Files");
            sb.AppendLine();
            
            var byReason = AffectedFiles.GroupBy(f => f.Reason);
            foreach (var group in byReason)
            {
                sb.AppendLine($"### {group.Key}");
                sb.AppendLine();
                sb.AppendLine("| File | Changes |");
                sb.AppendLine("|------|---------|");
                foreach (var file in group)
                {
                    var changes = string.Join(", ", file.RequiredChanges.Select(c => c.Type.ToString()));
                    sb.AppendLine($"| `{Path.GetFileName(file.FilePath)}` | {changes} |");
                }
                sb.AppendLine();
            }
        }

        if (RequiredProjectReferences.Any())
        {
            sb.AppendLine("## Required Project References");
            sb.AppendLine();
            sb.AppendLine("| Project | Needs Reference To |");
            sb.AppendLine("|---------|-------------------|");
            foreach (var pr in RequiredProjectReferences)
            {
                sb.AppendLine($"| `{Path.GetFileName(pr.ProjectPath)}` | `{Path.GetFileName(pr.ReferencePath)}` |");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

/// <summary>
/// Complexity assessment for migration.
/// </summary>
public enum MigrationComplexity
{
    /// <summary>
    /// Simple operation, few files affected.
    /// </summary>
    Simple,
    
    /// <summary>
    /// Medium complexity, multiple files affected.
    /// </summary>
    Medium,
    
    /// <summary>
    /// Complex operation, many files and cross-project changes.
    /// </summary>
    Complex,
    
    /// <summary>
    /// Very complex, requires manual review.
    /// </summary>
    VeryComplex
}

#endregion

#region Affected Items

/// <summary>
/// A file affected by the migration.
/// </summary>
public record AffectedFile(
    string FilePath,
    string ProjectPath,
    AffectedFileReason Reason,
    IReadOnlyList<RequiredChange> RequiredChanges
);

/// <summary>
/// Why a file is affected.
/// </summary>
public enum AffectedFileReason
{
    DirectlyMoved,
    DirectlyDeleted,
    ContainsUsingDirective,
    ContainsFullyQualifiedReference,
    ContainsInheritance,
    ContainsTypeUsage,
    ContainsXamlReference,
    ProjectFileUpdate
}

/// <summary>
/// A type affected by the migration.
/// </summary>
public record AffectedType(
    string TypeFullName,
    string FilePath,
    AffectedTypeReason Reason
);

/// <summary>
/// Why a type is affected.
/// </summary>
public enum AffectedTypeReason
{
    DirectlyMoved,
    DirectlyDeleted,
    NamespaceChanged,
    ReferencesMovedType,
    InheritsFromMovedType,
    ImplementsMovedInterface
}

/// <summary>
/// A change required in a file.
/// </summary>
public record RequiredChange(
    RequiredChangeType Type,
    int? LineNumber,
    string? CurrentValue,
    string? NewValue,
    string Description
);

/// <summary>
/// Type of required change.
/// </summary>
public enum RequiredChangeType
{
    UpdateUsingDirective,
    AddUsingDirective,
    RemoveUsingDirective,
    UpdateFullyQualifiedName,
    UpdateNamespace,
    UpdateXamlNamespace,
    UpdateProjectReference,
    MoveFile,
    DeleteFile
}

/// <summary>
/// A project reference that needs to be added.
/// </summary>
public record RequiredProjectReference(
    string ProjectPath,
    string ReferencePath,
    string Reason
);

/// <summary>
/// A package reference that needs to be added.
/// </summary>
public record RequiredPackageReference(
    string ProjectPath,
    string PackageId,
    string? Version,
    string Reason
);

#endregion

#region Warnings and Errors

/// <summary>
/// A warning about the migration.
/// </summary>
public record MigrationWarning(
    string Code,
    string Message,
    string? FilePath = null,
    int? LineNumber = null
);

/// <summary>
/// An error that prevents the migration.
/// </summary>
public record MigrationError(
    string Code,
    string Message,
    string? FilePath = null,
    int? LineNumber = null
);

#endregion
