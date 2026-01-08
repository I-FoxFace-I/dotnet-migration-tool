using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// CLI command for migrating code between different solutions.
/// </summary>
public static class CrossSolutionMigrateCommand
{
    public static Command Create()
    {
        var sourceSolutionArg = new Argument<string>("source-solution", "Path to source solution file (.sln)");
        var targetSolutionArg = new Argument<string>("target-solution", "Path to target solution file (.sln)");
        var sourcePathArg = new Argument<string>("source-path", "Source path (project, folder, or file) within source solution");
        var targetPathArg = new Argument<string>("target-path", "Target path within target solution");
        
        var oldNamespaceOption = new Option<string?>("--old-namespace", "Old namespace prefix to replace (auto-detected if not specified)");
        var newNamespaceOption = new Option<string?>("--new-namespace", "New namespace prefix to use (auto-detected if not specified)");
        var includeDepsOption = new Option<bool>("--include-deps", "Include dependencies in migration");
        var noUsingsOption = new Option<bool>("--no-usings", "Skip updating using directives");
        var noAddToSlnOption = new Option<bool>("--no-add-to-sln", "Don't add migrated project to target solution");
        var moveOption = new Option<bool>("--move", "Move files instead of copying (deletes originals)");
        var excludeOption = new Option<string[]>("--exclude", "Patterns for files to exclude (e.g., *.Designer.cs)") { AllowMultipleArgumentsPerToken = true };
        var dryRunOption = new Option<bool>("--dry-run", "Preview changes without applying");
        var analyzeOnlyOption = new Option<bool>("--analyze", "Only analyze migration without executing");

        var command = new Command("cross-solution-migrate", "Migrate code between different solutions")
        {
            sourceSolutionArg,
            targetSolutionArg,
            sourcePathArg,
            targetPathArg,
            oldNamespaceOption,
            newNamespaceOption,
            includeDepsOption,
            noUsingsOption,
            noAddToSlnOption,
            moveOption,
            excludeOption,
            dryRunOption,
            analyzeOnlyOption
        };

        command.SetHandler(async (context) =>
        {
            var sourceSolution = context.ParseResult.GetValueForArgument(sourceSolutionArg);
            var targetSolution = context.ParseResult.GetValueForArgument(targetSolutionArg);
            var sourcePath = context.ParseResult.GetValueForArgument(sourcePathArg);
            var targetPath = context.ParseResult.GetValueForArgument(targetPathArg);
            var oldNamespace = context.ParseResult.GetValueForOption(oldNamespaceOption);
            var newNamespace = context.ParseResult.GetValueForOption(newNamespaceOption);
            var includeDeps = context.ParseResult.GetValueForOption(includeDepsOption);
            var noUsings = context.ParseResult.GetValueForOption(noUsingsOption);
            var noAddToSln = context.ParseResult.GetValueForOption(noAddToSlnOption);
            var move = context.ParseResult.GetValueForOption(moveOption);
            var exclude = context.ParseResult.GetValueForOption(excludeOption) ?? [];
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var analyzeOnly = context.ParseResult.GetValueForOption(analyzeOnlyOption);

            var options = new CrossSolutionMigrationOptions
            {
                SourceSolutionPath = sourceSolution,
                TargetSolutionPath = targetSolution,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                OldNamespacePrefix = oldNamespace,
                NewNamespacePrefix = newNamespace,
                IncludeDependencies = includeDeps,
                UpdateUsings = !noUsings,
                AddToTargetSolution = !noAddToSln,
                PreserveOriginal = !move,
                DryRun = dryRun,
                ExcludePatterns = exclude
            };

            var service = new CrossSolutionMigrationService();

            if (analyzeOnly)
            {
                var analysis = await service.AnalyzeMigrationAsync(options);
                JsonOutput.WriteSuccess(new
                {
                    analysis.CanMigrate,
                    analysis.SourceSolution,
                    analysis.TargetSolution,
                    analysis.SourcePath,
                    analysis.TargetPath,
                    analysis.FilesToMigrate,
                    analysis.Files,
                    analysis.Dependencies,
                    analysis.MissingDependencies,
                    analysis.ConflictingFiles,
                    analysis.Warnings,
                    analysis.BlockingError
                });
            }
            else
            {
                var result = await service.MigrateFolderAsync(options);
                JsonOutput.WriteSuccess(new
                {
                    result.Success,
                    result.SourceSolution,
                    result.TargetSolution,
                    result.SourcePath,
                    result.TargetPath,
                    result.DryRun,
                    result.MigratedFilesCount,
                    result.MigratedFiles,
                    result.UpdatedNamespaces,
                    result.UpdatedUsings,
                    result.AddedToSolution,
                    result.MigratedDependencies,
                    result.Warnings,
                    result.ErrorMessage
                });
            }
        });

        return command;
    }
}
