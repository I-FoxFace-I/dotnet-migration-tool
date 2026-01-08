using System.Text.Json.Serialization;

namespace MigrationTool.Core.Abstractions.Models;

/// <summary>
/// Represents a migration plan containing multiple steps.
/// </summary>
public record MigrationPlan
{
    /// <summary>
    /// Unique identifier for the plan.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Plan name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Plan description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// When the plan was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the plan was last modified.
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Migration steps.
    /// </summary>
    [JsonPropertyName("steps")]
    public IReadOnlyList<MigrationStep> Steps { get; init; } = [];

    /// <summary>
    /// Current status of the plan.
    /// </summary>
    [JsonPropertyName("status")]
    public PlanStatus Status { get; init; } = PlanStatus.Draft;

    /// <summary>
    /// Total step count.
    /// </summary>
    [JsonIgnore]
    public int StepCount => Steps.Count;

    /// <summary>
    /// Completed step count.
    /// </summary>
    [JsonIgnore]
    public int CompletedStepCount => Steps.Count(s => s.Status == StepStatus.Completed);
}

/// <summary>
/// Status of a migration plan.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlanStatus
{
    [JsonPropertyName("draft")] Draft,
    [JsonPropertyName("ready")] Ready,
    [JsonPropertyName("inProgress")] InProgress,
    [JsonPropertyName("completed")] Completed,
    [JsonPropertyName("failed")] Failed,
    [JsonPropertyName("cancelled")] Cancelled
}

/// <summary>
/// Represents a single migration step.
/// </summary>
public record MigrationStep
{
    /// <summary>
    /// Step index (1-based).
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; init; }

    /// <summary>
    /// Action to perform.
    /// </summary>
    [JsonPropertyName("action")]
    public required MigrationAction Action { get; init; }

    /// <summary>
    /// Source path/identifier.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// Target path/identifier.
    /// </summary>
    [JsonPropertyName("target")]
    public required string Target { get; init; }

    /// <summary>
    /// Current status.
    /// </summary>
    [JsonPropertyName("status")]
    public StepStatus Status { get; init; } = StepStatus.Pending;

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// When the step was executed.
    /// </summary>
    [JsonPropertyName("executedAt")]
    public DateTime? ExecutedAt { get; init; }
}

/// <summary>
/// Migration action type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MigrationAction
{
    [JsonPropertyName("moveFile")] MoveFile,
    [JsonPropertyName("moveFolder")] MoveFolder,
    [JsonPropertyName("copyFile")] CopyFile,
    [JsonPropertyName("copyFolder")] CopyFolder,
    [JsonPropertyName("createProject")] CreateProject,
    [JsonPropertyName("deleteProject")] DeleteProject,
    [JsonPropertyName("renameNamespace")] RenameNamespace,
    [JsonPropertyName("addProjectReference")] AddProjectReference,
    [JsonPropertyName("removeProjectReference")] RemoveProjectReference,
    [JsonPropertyName("addPackageReference")] AddPackageReference,
    [JsonPropertyName("removePackageReference")] RemovePackageReference,
    [JsonPropertyName("updateProjectProperty")] UpdateProjectProperty
}

/// <summary>
/// Status of a migration step.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StepStatus
{
    [JsonPropertyName("pending")] Pending,
    [JsonPropertyName("inProgress")] InProgress,
    [JsonPropertyName("completed")] Completed,
    [JsonPropertyName("failed")] Failed,
    [JsonPropertyName("skipped")] Skipped,
    [JsonPropertyName("rolledBack")] RolledBack
}

/// <summary>
/// Result of migration execution.
/// </summary>
public record MigrationResult
{
    /// <summary>
    /// Whether the migration was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The executed plan.
    /// </summary>
    public required MigrationPlan Plan { get; init; }

    /// <summary>
    /// Results for each step.
    /// </summary>
    public IReadOnlyList<StepResult> StepResults { get; init; } = [];

    /// <summary>
    /// Total execution time.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of a single step execution.
/// </summary>
public record StepResult
{
    /// <summary>
    /// The executed step.
    /// </summary>
    public required MigrationStep Step { get; init; }

    /// <summary>
    /// Whether the step was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Execution time.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Result of plan validation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether the plan is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];
}

/// <summary>
/// Validation error.
/// </summary>
public record ValidationError(
    int? StepIndex,
    string Message,
    string? Code
);

/// <summary>
/// Validation warning.
/// </summary>
public record ValidationWarning(
    int? StepIndex,
    string Message,
    string? Code
);
