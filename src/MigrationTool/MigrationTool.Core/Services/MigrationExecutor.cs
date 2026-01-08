using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Core.Services;

/// <summary>
/// Executes migration operations.
/// Inspired by Python prototype's migration_engine.py.
/// </summary>
public partial class MigrationExecutor : IMigrationExecutor
{
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<MigrationExecutor> _logger;

    /// <summary>
    /// Gets or sets whether to simulate operations without making changes.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets the workspace root directory.
    /// </summary>
    public string? WorkspaceRoot { get; set; }

    public MigrationExecutor(IFileSystemService fileSystem, ILogger<MigrationExecutor> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MigrationResult> ExecuteAsync(
        MigrationPlan plan,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var stepResults = new List<StepResult>();
        var updatedSteps = new List<MigrationStep>();

        _logger.LogInformation("Starting migration plan: {PlanName} with {StepCount} steps (DryRun: {DryRun})",
            plan.Name, plan.StepCount, DryRun);

        for (var i = 0; i < plan.Steps.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = plan.Steps[i];
            var stepNumber = i + 1;

            progress?.Report(new MigrationProgress(
                stepNumber,
                plan.StepCount,
                $"Executing step {stepNumber}: {step.Action}",
                (double)stepNumber / plan.StepCount * 100
            ));

            var stepResult = await ExecuteStepAsync(step, cancellationToken);
            stepResults.Add(stepResult);

            // Update step status
            var updatedStep = step with
            {
                Status = stepResult.Success ? StepStatus.Completed : StepStatus.Failed,
                ErrorMessage = stepResult.ErrorMessage,
                ExecutedAt = DateTime.UtcNow
            };
            updatedSteps.Add(updatedStep);

            if (!stepResult.Success)
            {
                _logger.LogError("Step {StepNumber} failed: {Error}", stepNumber, stepResult.ErrorMessage);

                // Mark remaining steps as skipped
                for (var j = i + 1; j < plan.Steps.Count; j++)
                {
                    updatedSteps.Add(plan.Steps[j] with { Status = StepStatus.Skipped });
                }

                break;
            }
        }

        stopwatch.Stop();

        var success = stepResults.All(r => r.Success);
        var updatedPlan = plan with
        {
            Steps = updatedSteps,
            Status = success ? PlanStatus.Completed : PlanStatus.Failed,
            ModifiedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Migration plan {Status} in {Duration}ms",
            success ? "completed" : "failed", stopwatch.ElapsedMilliseconds);

        return new MigrationResult
        {
            Success = success,
            Plan = updatedPlan,
            StepResults = stepResults,
            Duration = stopwatch.Elapsed,
            ErrorMessage = stepResults.FirstOrDefault(r => !r.Success)?.ErrorMessage
        };
    }

    /// <inheritdoc />
    public async Task<StepResult> ExecuteStepAsync(MigrationStep step, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Executing step {Index}: {Action} - {Source} -> {Target}",
            step.Index, step.Action, step.Source, step.Target);

        try
        {
            var result = step.Action switch
            {
                MigrationAction.MoveFile => await MoveFileAsync(step.Source, step.Target, cancellationToken),
                MigrationAction.MoveFolder => await MoveFolderAsync(step.Source, step.Target, cancellationToken),
                MigrationAction.CopyFile => await CopyFileAsync(step.Source, step.Target, cancellationToken),
                MigrationAction.CopyFolder => await CopyFolderAsync(step.Source, step.Target, cancellationToken),
                MigrationAction.RenameNamespace => await RenameNamespaceAsync(step.Source, step.Target, step.Metadata, cancellationToken),
                MigrationAction.AddProjectReference => await AddProjectReferenceAsync(step.Source, step.Target, cancellationToken),
                MigrationAction.RemoveProjectReference => await RemoveProjectReferenceAsync(step.Source, step.Target, cancellationToken),
                MigrationAction.UpdateProjectProperty => await UpdateProjectPropertyAsync(step.Source, step.Target, step.Metadata, cancellationToken),
                _ => (false, $"Unsupported action: {step.Action}")
            };

            stopwatch.Stop();

            var (success, message) = result;

            return new StepResult
            {
                Step = step with { Status = success ? StepStatus.Completed : StepStatus.Failed },
                Success = success,
                ErrorMessage = success ? null : message,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Step execution failed: {Action}", step.Action);

            return new StepResult
            {
                Step = step with { Status = StepStatus.Failed },
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <inheritdoc />
    public async Task<MigrationResult> RollbackAsync(MigrationPlan plan, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Rollback requested for plan: {PlanName}", plan.Name);

        // Create reverse steps for completed steps
        var completedSteps = plan.Steps
            .Where(s => s.Status == StepStatus.Completed)
            .Reverse()
            .ToList();

        var rollbackSteps = new List<MigrationStep>();
        var index = 1;

        foreach (var step in completedSteps)
        {
            var reverseStep = CreateReverseStep(step, index++);
            if (reverseStep != null)
            {
                rollbackSteps.Add(reverseStep);
            }
        }

        var rollbackPlan = new MigrationPlan
        {
            Name = $"Rollback: {plan.Name}",
            Description = $"Rollback of plan executed at {plan.ModifiedAt}",
            Steps = rollbackSteps,
            Status = PlanStatus.Ready
        };

        return await ExecuteAsync(rollbackPlan, null, cancellationToken);
    }

    #region File Operations

    private async Task<(bool Success, string Message)> MoveFileAsync(string source, string target, CancellationToken cancellationToken)
    {
        var sourcePath = ResolvePath(source);
        var targetPath = ResolvePath(target);

        if (!await _fileSystem.ExistsAsync(sourcePath, cancellationToken))
        {
            return (false, $"Source file does not exist: {sourcePath}");
        }

        if (await _fileSystem.ExistsAsync(targetPath, cancellationToken))
        {
            return (false, $"Target file already exists: {targetPath}");
        }

        if (DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would move file: {Source} -> {Target}", sourcePath, targetPath);
            return (true, $"[DRY RUN] Would move: {source} -> {target}");
        }

        await _fileSystem.MoveAsync(sourcePath, targetPath, cancellationToken);
        _logger.LogInformation("Moved file: {Source} -> {Target}", sourcePath, targetPath);

        return (true, $"Moved: {source} -> {target}");
    }

    private async Task<(bool Success, string Message)> MoveFolderAsync(string source, string target, CancellationToken cancellationToken)
    {
        var sourcePath = ResolvePath(source);
        var targetPath = ResolvePath(target);

        if (!await _fileSystem.ExistsAsync(sourcePath, cancellationToken))
        {
            return (false, $"Source folder does not exist: {sourcePath}");
        }

        if (!await _fileSystem.IsDirectoryAsync(sourcePath, cancellationToken))
        {
            return (false, $"Source is not a directory: {sourcePath}");
        }

        if (await _fileSystem.ExistsAsync(targetPath, cancellationToken))
        {
            return (false, $"Target folder already exists: {targetPath}");
        }

        if (DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would move folder: {Source} -> {Target}", sourcePath, targetPath);
            return (true, $"[DRY RUN] Would move: {source} -> {target}");
        }

        // Check if this is a project folder (contains .csproj)
        var isProjectFolder = await _fileSystem.GetFilesAsync(sourcePath, "*.csproj", false, cancellationToken)
            .ContinueWith(t => t.Result.Any(), cancellationToken);

        await _fileSystem.MoveAsync(sourcePath, targetPath, cancellationToken);
        _logger.LogInformation("Moved folder: {Source} -> {Target}", sourcePath, targetPath);

        // Auto-update references if it's a project folder
        if (isProjectFolder && !string.IsNullOrEmpty(WorkspaceRoot))
        {
            _logger.LogInformation("Updating references after project move...");
            var updateResults = await UpdateReferencesAfterMoveAsync(sourcePath, targetPath, cancellationToken);
            
            var successCount = updateResults.Count(r => r.Success);
            var failCount = updateResults.Count(r => !r.Success);
            
            if (successCount > 0)
            {
                _logger.LogInformation("Updated {Count} project references", successCount);
            }
            if (failCount > 0)
            {
                _logger.LogWarning("Failed to update {Count} project references", failCount);
            }
        }

        return (true, $"Moved: {source} -> {target}");
    }

    private async Task<(bool Success, string Message)> CopyFileAsync(string source, string target, CancellationToken cancellationToken)
    {
        var sourcePath = ResolvePath(source);
        var targetPath = ResolvePath(target);

        if (!await _fileSystem.ExistsAsync(sourcePath, cancellationToken))
        {
            return (false, $"Source file does not exist: {sourcePath}");
        }

        if (await _fileSystem.ExistsAsync(targetPath, cancellationToken))
        {
            return (false, $"Target file already exists: {targetPath}");
        }

        if (DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would copy file: {Source} -> {Target}", sourcePath, targetPath);
            return (true, $"[DRY RUN] Would copy: {source} -> {target}");
        }

        await _fileSystem.CopyAsync(sourcePath, targetPath, false, cancellationToken);
        _logger.LogInformation("Copied file: {Source} -> {Target}", sourcePath, targetPath);

        return (true, $"Copied: {source} -> {target}");
    }

    private async Task<(bool Success, string Message)> CopyFolderAsync(string source, string target, CancellationToken cancellationToken)
    {
        var sourcePath = ResolvePath(source);
        var targetPath = ResolvePath(target);

        if (!await _fileSystem.ExistsAsync(sourcePath, cancellationToken))
        {
            return (false, $"Source folder does not exist: {sourcePath}");
        }

        if (!await _fileSystem.IsDirectoryAsync(sourcePath, cancellationToken))
        {
            return (false, $"Source is not a directory: {sourcePath}");
        }

        if (await _fileSystem.ExistsAsync(targetPath, cancellationToken))
        {
            return (false, $"Target folder already exists: {targetPath}");
        }

        if (DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would copy folder: {Source} -> {Target}", sourcePath, targetPath);
            return (true, $"[DRY RUN] Would copy: {source} -> {target}");
        }

        await _fileSystem.CopyAsync(sourcePath, targetPath, false, cancellationToken);
        _logger.LogInformation("Copied folder: {Source} -> {Target}", sourcePath, targetPath);

        return (true, $"Copied: {source} -> {target}");
    }

    #endregion

    #region Namespace Operations

    [GeneratedRegex(@"namespace\s+[\w.]+", RegexOptions.Compiled)]
    private static partial Regex NamespaceDeclarationRegex();

    [GeneratedRegex(@"using\s+[\w.]+;", RegexOptions.Compiled)]
    private static partial Regex UsingDirectiveRegex();

    private async Task<(bool Success, string Message)> RenameNamespaceAsync(
        string filePath,
        string newNamespace,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        var resolvedPath = ResolvePath(filePath);

        if (!await _fileSystem.ExistsAsync(resolvedPath, cancellationToken))
        {
            return (false, $"File does not exist: {resolvedPath}");
        }

        var oldNamespace = metadata.TryGetValue("OldNamespace", out var ns) ? ns : null;

        if (string.IsNullOrEmpty(oldNamespace))
        {
            return (false, "OldNamespace metadata is required for RenameNamespace action");
        }

        if (DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would rename namespace in {File}: {Old} -> {New}",
                resolvedPath, oldNamespace, newNamespace);
            return (true, $"[DRY RUN] Would rename namespace: {oldNamespace} -> {newNamespace}");
        }

        var content = await _fileSystem.ReadFileAsync(resolvedPath, cancellationToken);
        var updatedContent = ReplaceNamespace(content, oldNamespace, newNamespace);

        if (content == updatedContent)
        {
            _logger.LogInformation("No namespace changes needed in {File}", resolvedPath);
            return (true, "No changes needed");
        }

        await _fileSystem.WriteFileAsync(resolvedPath, updatedContent, cancellationToken);
        _logger.LogInformation("Renamed namespace in {File}: {Old} -> {New}", resolvedPath, oldNamespace, newNamespace);

        return (true, $"Renamed namespace: {oldNamespace} -> {newNamespace}");
    }

    private static string ReplaceNamespace(string content, string oldNamespace, string newNamespace)
    {
        // Replace namespace declaration
        var result = Regex.Replace(
            content,
            $@"namespace\s+{Regex.Escape(oldNamespace)}",
            $"namespace {newNamespace}");

        // Replace using statements
        result = Regex.Replace(
            result,
            $@"using\s+{Regex.Escape(oldNamespace)}",
            $"using {newNamespace}");

        return result;
    }

    #endregion

    #region Project Reference Operations

    private async Task<(bool Success, string Message)> AddProjectReferenceAsync(
        string projectPath,
        string referencePath,
        CancellationToken cancellationToken)
    {
        var resolvedProjectPath = ResolvePath(projectPath);
        var resolvedReferencePath = ResolvePath(referencePath);

        if (!await _fileSystem.ExistsAsync(resolvedProjectPath, cancellationToken))
        {
            return (false, $"Project file does not exist: {resolvedProjectPath}");
        }

        if (DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would add reference {Reference} to {Project}",
                resolvedReferencePath, resolvedProjectPath);
            return (true, $"[DRY RUN] Would add reference: {referencePath}");
        }

        var content = await _fileSystem.ReadFileAsync(resolvedProjectPath, cancellationToken);

        // Calculate relative path from project to reference
        var projectDir = Path.GetDirectoryName(resolvedProjectPath) ?? string.Empty;
        var relativePath = Path.GetRelativePath(projectDir, resolvedReferencePath);

        // Check if reference already exists
        if (content.Contains(relativePath) || content.Contains(referencePath))
        {
            return (true, "Reference already exists");
        }

        // Find ItemGroup with ProjectReference or create new one
        var referenceElement = $"    <ProjectReference Include=\"{relativePath}\" />";

        if (content.Contains("<ProjectReference"))
        {
            // Add to existing ItemGroup
            var insertIndex = content.LastIndexOf("</ItemGroup>", 
                content.LastIndexOf("<ProjectReference", StringComparison.Ordinal), StringComparison.Ordinal);
            
            if (insertIndex > 0)
            {
                var updatedContent = content.Insert(insertIndex, $"{referenceElement}\n  ");
                await _fileSystem.WriteFileAsync(resolvedProjectPath, updatedContent, cancellationToken);
            }
        }
        else
        {
            // Create new ItemGroup
            var itemGroup = $"\n  <ItemGroup>\n{referenceElement}\n  </ItemGroup>\n";
            var insertIndex = content.LastIndexOf("</Project>", StringComparison.Ordinal);
            
            if (insertIndex > 0)
            {
                var updatedContent = content.Insert(insertIndex, itemGroup);
                await _fileSystem.WriteFileAsync(resolvedProjectPath, updatedContent, cancellationToken);
            }
        }

        _logger.LogInformation("Added reference {Reference} to {Project}", referencePath, projectPath);
        return (true, $"Added reference: {referencePath}");
    }

    private async Task<(bool Success, string Message)> RemoveProjectReferenceAsync(
        string projectPath,
        string referencePath,
        CancellationToken cancellationToken)
    {
        var resolvedProjectPath = ResolvePath(projectPath);

        if (!await _fileSystem.ExistsAsync(resolvedProjectPath, cancellationToken))
        {
            return (false, $"Project file does not exist: {resolvedProjectPath}");
        }

        if (DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would remove reference {Reference} from {Project}",
                referencePath, resolvedProjectPath);
            return (true, $"[DRY RUN] Would remove reference: {referencePath}");
        }

        var content = await _fileSystem.ReadFileAsync(resolvedProjectPath, cancellationToken);

        // Remove the ProjectReference line
        var pattern = $@"\s*<ProjectReference Include=""[^""]*{Regex.Escape(Path.GetFileName(referencePath))}[^""]*""\s*/>";
        var updatedContent = Regex.Replace(content, pattern, string.Empty);

        if (content == updatedContent)
        {
            return (true, "Reference not found");
        }

        await _fileSystem.WriteFileAsync(resolvedProjectPath, updatedContent, cancellationToken);
        _logger.LogInformation("Removed reference {Reference} from {Project}", referencePath, projectPath);

        return (true, $"Removed reference: {referencePath}");
    }

    private async Task<(bool Success, string Message)> UpdateProjectPropertyAsync(
        string projectPath,
        string propertyValue,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        var resolvedProjectPath = ResolvePath(projectPath);

        if (!await _fileSystem.ExistsAsync(resolvedProjectPath, cancellationToken))
        {
            return (false, $"Project file does not exist: {resolvedProjectPath}");
        }

        if (!metadata.TryGetValue("PropertyName", out var propertyName))
        {
            return (false, "PropertyName metadata is required for UpdateProjectProperty action");
        }

        if (DryRun)
        {
            _logger.LogInformation("[DRY RUN] Would update property {Property} to {Value} in {Project}",
                propertyName, propertyValue, resolvedProjectPath);
            return (true, $"[DRY RUN] Would update property: {propertyName}={propertyValue}");
        }

        var content = await _fileSystem.ReadFileAsync(resolvedProjectPath, cancellationToken);

        // Update existing property or add new one
        var pattern = $@"<{propertyName}>[^<]*</{propertyName}>";
        var replacement = $"<{propertyName}>{propertyValue}</{propertyName}>";

        string updatedContent;
        if (Regex.IsMatch(content, pattern))
        {
            updatedContent = Regex.Replace(content, pattern, replacement);
        }
        else
        {
            // Add to first PropertyGroup
            var insertIndex = content.IndexOf("</PropertyGroup>", StringComparison.Ordinal);
            if (insertIndex > 0)
            {
                updatedContent = content.Insert(insertIndex, $"    {replacement}\n  ");
            }
            else
            {
                return (false, "Could not find PropertyGroup in project file");
            }
        }

        await _fileSystem.WriteFileAsync(resolvedProjectPath, updatedContent, cancellationToken);
        _logger.LogInformation("Updated property {Property} to {Value} in {Project}",
            propertyName, propertyValue, projectPath);

        return (true, $"Updated property: {propertyName}={propertyValue}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds all projects that reference a given project.
    /// </summary>
    public async Task<IEnumerable<string>> FindAffectedProjectsAsync(
        string movedProjectPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(WorkspaceRoot))
        {
            _logger.LogWarning("WorkspaceRoot is not set, cannot find affected projects");
            return [];
        }

        var projectName = Path.GetFileNameWithoutExtension(movedProjectPath);
        var affectedProjects = new List<string>();

        var allProjects = await _fileSystem.GetFilesAsync(WorkspaceRoot, "*.csproj", true, cancellationToken);

        foreach (var project in allProjects)
        {
            try
            {
                var content = await _fileSystem.ReadFileAsync(project, cancellationToken);
                if (content.Contains(projectName))
                {
                    affectedProjects.Add(project);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read project file: {Project}", project);
            }
        }

        return affectedProjects;
    }

    /// <summary>
    /// Updates project references after a project move.
    /// </summary>
    public async Task<IEnumerable<(string Project, bool Success)>> UpdateReferencesAfterMoveAsync(
        string oldPath,
        string newPath,
        CancellationToken cancellationToken = default)
    {
        var results = new List<(string, bool)>();
        var affectedProjects = await FindAffectedProjectsAsync(oldPath, cancellationToken);

        foreach (var project in affectedProjects)
        {
            try
            {
                var content = await _fileSystem.ReadFileAsync(project, cancellationToken);
                var projectDir = Path.GetDirectoryName(project) ?? string.Empty;

                // Calculate old and new relative paths
                var oldRelativePath = Path.GetRelativePath(projectDir, oldPath);
                var newRelativePath = Path.GetRelativePath(projectDir, newPath);

                // Also try with different path separators
                var updatedContent = content
                    .Replace(oldRelativePath, newRelativePath)
                    .Replace(oldRelativePath.Replace('/', '\\'), newRelativePath.Replace('/', '\\'))
                    .Replace(oldRelativePath.Replace('\\', '/'), newRelativePath.Replace('\\', '/'));

                if (content != updatedContent)
                {
                    if (!DryRun)
                    {
                        await _fileSystem.WriteFileAsync(project, updatedContent, cancellationToken);
                    }
                    _logger.LogInformation("Updated references in {Project}", project);
                    results.Add((project, true));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update references in {Project}", project);
                results.Add((project, false));
            }
        }

        return results;
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        if (!string.IsNullOrEmpty(WorkspaceRoot))
        {
            return Path.Combine(WorkspaceRoot, path);
        }

        return path;
    }

    private static MigrationStep? CreateReverseStep(MigrationStep step, int newIndex)
    {
        return step.Action switch
        {
            MigrationAction.MoveFile => step with
            {
                Index = newIndex,
                Source = step.Target,
                Target = step.Source,
                Status = StepStatus.Pending
            },
            MigrationAction.MoveFolder => step with
            {
                Index = newIndex,
                Source = step.Target,
                Target = step.Source,
                Status = StepStatus.Pending
            },
            MigrationAction.AddProjectReference => step with
            {
                Index = newIndex,
                Action = MigrationAction.RemoveProjectReference,
                Status = StepStatus.Pending
            },
            MigrationAction.RemoveProjectReference => step with
            {
                Index = newIndex,
                Action = MigrationAction.AddProjectReference,
                Status = StepStatus.Pending
            },
            // Copy operations cannot be reversed (would need to delete)
            // Namespace renames would need original namespace stored
            _ => null
        };
    }

    #endregion
}
