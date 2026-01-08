using System.Text.Json;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Core.Services;

/// <summary>
/// Plans migration operations.
/// Inspired by Python prototype's migration_planner.py.
/// </summary>
public class MigrationPlanner : IMigrationPlanner
{
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<MigrationPlanner> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public MigrationPlanner(IFileSystemService fileSystem, ILogger<MigrationPlanner> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public MigrationPlan CreatePlan(string name)
    {
        _logger.LogInformation("Creating new migration plan: {Name}", name);

        return new MigrationPlan
        {
            Name = name,
            Status = PlanStatus.Draft,
            Steps = []
        };
    }

    /// <inheritdoc />
    public MigrationPlan AddStep(MigrationPlan plan, MigrationStep step)
    {
        var newStep = step with { Index = plan.Steps.Count + 1 };
        var updatedSteps = plan.Steps.Append(newStep).ToList();

        _logger.LogInformation("Added step {Index}: {Action} to plan {PlanName}",
            newStep.Index, newStep.Action, plan.Name);

        return plan with
        {
            Steps = updatedSteps,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public MigrationPlan RemoveStep(MigrationPlan plan, int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= plan.Steps.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(stepIndex),
                $"Step index {stepIndex} is out of range. Plan has {plan.Steps.Count} steps.");
        }

        var updatedSteps = plan.Steps
            .Where((_, i) => i != stepIndex)
            .Select((s, i) => s with { Index = i + 1 })
            .ToList();

        _logger.LogInformation("Removed step at index {Index} from plan {PlanName}",
            stepIndex, plan.Name);

        return plan with
        {
            Steps = updatedSteps,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidatePlanAsync(MigrationPlan plan, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating plan: {PlanName} with {StepCount} steps", plan.Name, plan.StepCount);

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Check if plan has steps
        if (plan.Steps.Count == 0)
        {
            errors.Add(new ValidationError(null, "Plan has no steps", "EMPTY_PLAN"));
        }

        // Validate each step
        for (var i = 0; i < plan.Steps.Count; i++)
        {
            var step = plan.Steps[i];
            await ValidateStepAsync(step, i, errors, warnings, cancellationToken);
        }

        // Check for circular dependencies
        CheckCircularDependencies(plan, errors);

        // Check for conflicting operations
        CheckConflictingOperations(plan, errors, warnings);

        var result = new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };

        _logger.LogInformation("Validation {Result}: {ErrorCount} errors, {WarningCount} warnings",
            result.IsValid ? "passed" : "failed", errors.Count, warnings.Count);

        return result;
    }

    /// <inheritdoc />
    public string ExportPlan(MigrationPlan plan)
    {
        _logger.LogInformation("Exporting plan: {PlanName}", plan.Name);

        return JsonSerializer.Serialize(plan, JsonOptions);
    }

    /// <inheritdoc />
    public MigrationPlan ImportPlan(string json)
    {
        _logger.LogInformation("Importing plan from JSON");

        var plan = JsonSerializer.Deserialize<MigrationPlan>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize migration plan");

        // Reset status to Draft and update ModifiedAt
        var importedPlan = plan with
        {
            Status = PlanStatus.Draft,
            ModifiedAt = DateTime.UtcNow,
            Steps = plan.Steps.Select(s => s with { Status = StepStatus.Pending }).ToList()
        };

        _logger.LogInformation("Imported plan: {PlanName} with {StepCount} steps", importedPlan.Name, importedPlan.StepCount);

        return importedPlan;
    }

    #region Validation Helpers

    private async Task ValidateStepAsync(
        MigrationStep step,
        int index,
        List<ValidationError> errors,
        List<ValidationWarning> warnings,
        CancellationToken cancellationToken)
    {
        // Check required fields
        if (string.IsNullOrWhiteSpace(step.Source) && RequiresSource(step.Action))
        {
            errors.Add(new ValidationError(index, "Source is required", "MISSING_SOURCE"));
        }

        if (string.IsNullOrWhiteSpace(step.Target) && RequiresTarget(step.Action))
        {
            errors.Add(new ValidationError(index, "Target is required", "MISSING_TARGET"));
        }

        // Check if source exists (for move/copy operations)
        if (IsFileOperation(step.Action) && !string.IsNullOrWhiteSpace(step.Source))
        {
            if (!await _fileSystem.ExistsAsync(step.Source, cancellationToken))
            {
                warnings.Add(new ValidationWarning(index, $"Source does not exist: {step.Source}", "SOURCE_NOT_FOUND"));
            }
        }

        // Check if target already exists
        if (IsFileOperation(step.Action) && !string.IsNullOrWhiteSpace(step.Target))
        {
            if (await _fileSystem.ExistsAsync(step.Target, cancellationToken))
            {
                warnings.Add(new ValidationWarning(index, $"Target already exists: {step.Target}", "TARGET_EXISTS"));
            }
        }

        // Check action-specific requirements
        switch (step.Action)
        {
            case MigrationAction.RenameNamespace:
                if (!step.Metadata.ContainsKey("OldNamespace"))
                {
                    errors.Add(new ValidationError(index, "OldNamespace metadata is required", "MISSING_OLD_NAMESPACE"));
                }
                break;

            case MigrationAction.UpdateProjectProperty:
                if (!step.Metadata.ContainsKey("PropertyName"))
                {
                    errors.Add(new ValidationError(index, "PropertyName metadata is required", "MISSING_PROPERTY_NAME"));
                }
                break;
        }
    }

    private static void CheckCircularDependencies(MigrationPlan plan, List<ValidationError> errors)
    {
        // Check if any step moves a file that another step depends on
        var movedPaths = new HashSet<string>();
        var targetPaths = new HashSet<string>();

        foreach (var step in plan.Steps)
        {
            if (step.Action is MigrationAction.MoveFile or MigrationAction.MoveFolder)
            {
                if (movedPaths.Contains(step.Target))
                {
                    errors.Add(new ValidationError(step.Index,
                        $"Circular dependency: {step.Target} is both source and target",
                        "CIRCULAR_DEPENDENCY"));
                }

                movedPaths.Add(step.Source);
                targetPaths.Add(step.Target);
            }
        }
    }

    private static void CheckConflictingOperations(
        MigrationPlan plan,
        List<ValidationError> errors,
        List<ValidationWarning> warnings)
    {
        var targetPaths = new Dictionary<string, int>();

        foreach (var step in plan.Steps)
        {
            if (!string.IsNullOrWhiteSpace(step.Target))
            {
                if (targetPaths.TryGetValue(step.Target, out var existingIndex))
                {
                    errors.Add(new ValidationError(step.Index,
                        $"Conflict: Target '{step.Target}' is used by step {existingIndex + 1}",
                        "DUPLICATE_TARGET"));
                }
                else
                {
                    targetPaths[step.Target] = step.Index - 1;
                }
            }
        }
    }

    private static bool RequiresSource(MigrationAction action)
    {
        return action is not MigrationAction.CreateProject;
    }

    private static bool RequiresTarget(MigrationAction action)
    {
        return action is not MigrationAction.DeleteProject;
    }

    private static bool IsFileOperation(MigrationAction action)
    {
        return action is MigrationAction.MoveFile
            or MigrationAction.MoveFolder
            or MigrationAction.CopyFile
            or MigrationAction.CopyFolder;
    }

    #endregion
}
