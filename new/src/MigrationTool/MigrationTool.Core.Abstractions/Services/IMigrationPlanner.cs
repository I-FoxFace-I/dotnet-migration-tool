using MigrationTool.Core.Abstractions.Models;

namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Plans migration operations.
/// </summary>
public interface IMigrationPlanner
{
    /// <summary>
    /// Creates a new migration plan.
    /// </summary>
    MigrationPlan CreatePlan(string name);

    /// <summary>
    /// Adds a step to the plan.
    /// </summary>
    MigrationPlan AddStep(MigrationPlan plan, MigrationStep step);

    /// <summary>
    /// Removes a step from the plan.
    /// </summary>
    MigrationPlan RemoveStep(MigrationPlan plan, int stepIndex);

    /// <summary>
    /// Validates the plan.
    /// </summary>
    Task<ValidationResult> ValidatePlanAsync(MigrationPlan plan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports plan to JSON.
    /// </summary>
    string ExportPlan(MigrationPlan plan);

    /// <summary>
    /// Imports plan from JSON.
    /// </summary>
    MigrationPlan ImportPlan(string json);
}
