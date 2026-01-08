using MigrationTool.Core.Abstractions.Models;

namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Executes migration operations.
/// </summary>
public interface IMigrationExecutor
{
    /// <summary>
    /// Executes a migration plan.
    /// </summary>
    Task<MigrationResult> ExecuteAsync(MigrationPlan plan, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a single migration step.
    /// </summary>
    Task<StepResult> ExecuteStepAsync(MigrationStep step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a completed plan (if possible).
    /// </summary>
    Task<MigrationResult> RollbackAsync(MigrationPlan plan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information during migration execution.
/// </summary>
public record MigrationProgress(
    int CurrentStep,
    int TotalSteps,
    string CurrentAction,
    double PercentComplete
);
