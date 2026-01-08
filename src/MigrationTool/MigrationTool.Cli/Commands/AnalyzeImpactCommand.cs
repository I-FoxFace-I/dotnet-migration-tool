using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Graph;
using MigrationTool.Core.Graph;
using MigrationTool.Cli.Utilities;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// CLI command to analyze the impact of a migration operation.
/// </summary>
public static class AnalyzeImpactCommand
{
    public static Command Create()
    {
        var command = new Command("analyze-impact", "Analyze the impact of a migration operation");

        var solutionOption = new Option<string>(
            aliases: new[] { "--solution", "-s" },
            description: "Path to the solution file")
        {
            IsRequired = true
        };

        var operationOption = new Option<string>(
            aliases: new[] { "--operation", "-op" },
            description: "Operation type: move, rename-namespace, delete, move-type")
        {
            IsRequired = true
        };

        var sourceOption = new Option<string>(
            aliases: new[] { "--source", "-src" },
            description: "Source path or identifier (file path, namespace, or type name)")
        {
            IsRequired = true
        };

        var targetOption = new Option<string?>(
            aliases: new[] { "--target", "-tgt" },
            description: "Target path or identifier (for move/rename operations)");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Force operation (for delete)");

        var markdownOption = new Option<bool>(
            aliases: new[] { "--markdown", "-md" },
            description: "Output as markdown instead of JSON");

        command.AddOption(solutionOption);
        command.AddOption(operationOption);
        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(forceOption);
        command.AddOption(markdownOption);

        command.SetHandler(async (solution, operation, source, target, force, markdown) =>
        {
            await ExecuteAsync(solution, operation, source, target, force, markdown);
        }, solutionOption, operationOption, sourceOption, targetOption, forceOption, markdownOption);

        return command;
    }

    private static async Task ExecuteAsync(
        string solutionPath,
        string operation,
        string source,
        string? target,
        bool force,
        bool markdown)
    {
        try
        {
            if (!File.Exists(solutionPath))
            {
                JsonOutput.WriteError($"Solution file not found: {solutionPath}");
                return;
            }

            // Create simple loggers
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            var graphLogger = loggerFactory.CreateLogger<SolutionGraphBuilder>();
            var analyzerLogger = loggerFactory.CreateLogger<ImpactAnalyzer>();

            // Build graph first
            Console.Error.WriteLine($"Building graph for: {solutionPath}");
            await using var builder = new SolutionGraphBuilder(graphLogger);
            var graph = await builder.BuildGraphAsync(solutionPath, GraphBuildOptions.Default);

            Console.Error.WriteLine($"Graph built: {graph.GetStatistics().TypeCount} types, {graph.GetStatistics().FileCount} files");

            // Create analyzer
            var analyzer = new ImpactAnalyzer(analyzerLogger);

            // Parse operation and analyze
            ImpactReport report;
            
            switch (operation.ToLowerInvariant())
            {
                case "move":
                    if (target == null)
                    {
                        JsonOutput.WriteError("Target path is required for move operation");
                        return;
                    }
                    report = await analyzer.AnalyzeMoveAsync(graph, new MoveOperation(source, target));
                    break;

                case "rename-namespace":
                case "rename-ns":
                    if (target == null)
                    {
                        JsonOutput.WriteError("Target namespace is required for rename-namespace operation");
                        return;
                    }
                    report = await analyzer.AnalyzeRenameNamespaceAsync(graph, new RenameNamespaceOperation(source, target));
                    break;

                case "delete":
                    report = await analyzer.AnalyzeDeleteAsync(graph, new DeleteOperation(source, force));
                    break;

                case "move-type":
                    if (target == null)
                    {
                        JsonOutput.WriteError("Target namespace is required for move-type operation");
                        return;
                    }
                    report = await analyzer.AnalyzeMoveTypeAsync(graph, new MoveTypeOperation(source, target));
                    break;

                default:
                    JsonOutput.WriteError($"Unknown operation: {operation}. Valid operations: move, rename-namespace, delete, move-type");
                    return;
            }

            // Output report
            if (markdown)
            {
                Console.WriteLine(report.ToMarkdown());
            }
            else
            {
                var result = new
                {
                    success = true,
                    operation = report.Operation.Description,
                    canProceed = report.CanProceed,
                    complexity = report.Complexity.ToString(),
                    summary = new
                    {
                        affectedFilesCount = report.AffectedFilesCount,
                        affectedTypesCount = report.AffectedTypesCount,
                        affectedProjectsCount = report.AffectedProjectsCount,
                        requiredChangesCount = report.RequiredChangesCount,
                        warningsCount = report.Warnings.Count,
                        errorsCount = report.Errors.Count
                    },
                    affectedFiles = report.AffectedFiles.Select(f => new
                    {
                        f.FilePath,
                        f.ProjectPath,
                        reason = f.Reason.ToString(),
                        changes = f.RequiredChanges.Select(c => new
                        {
                            type = c.Type.ToString(),
                            c.LineNumber,
                            c.CurrentValue,
                            c.NewValue,
                            c.Description
                        })
                    }),
                    affectedTypes = report.AffectedTypes.Select(t => new
                    {
                        t.TypeFullName,
                        t.FilePath,
                        reason = t.Reason.ToString()
                    }),
                    requiredProjectReferences = report.RequiredProjectReferences.Select(r => new
                    {
                        r.ProjectPath,
                        r.ReferencePath,
                        r.Reason
                    }),
                    warnings = report.Warnings.Select(w => new
                    {
                        w.Code,
                        w.Message,
                        w.FilePath,
                        w.LineNumber
                    }),
                    errors = report.Errors.Select(e => new
                    {
                        e.Code,
                        e.Message,
                        e.FilePath,
                        e.LineNumber
                    })
                };

                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch (Exception ex)
        {
            JsonOutput.WriteError(ex.Message);
        }
    }
}
