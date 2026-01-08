using System.CommandLine;
using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Graph;
using MigrationTool.Core.Graph;
using MigrationTool.Cli.Utilities;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// CLI command to analyze a solution and build a dependency graph.
/// </summary>
public static class AnalyzeGraphCommand
{
    public static Command Create()
    {
        var command = new Command("analyze-graph", "Build and analyze a dependency graph for a solution");

        var solutionOption = new Option<string>(
            aliases: new[] { "--solution", "-s" },
            description: "Path to the solution file")
        {
            IsRequired = true
        };

        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path for the graph (JSON format)");

        var fastOption = new Option<bool>(
            aliases: new[] { "--fast", "-f" },
            description: "Use fast mode (skip detailed type usage analysis)");

        var fullOption = new Option<bool>(
            aliases: new[] { "--full" },
            description: "Use full mode (include private types and generated files)");

        var statsOnlyOption = new Option<bool>(
            aliases: new[] { "--stats-only" },
            description: "Only output statistics, not the full graph");

        command.AddOption(solutionOption);
        command.AddOption(outputOption);
        command.AddOption(fastOption);
        command.AddOption(fullOption);
        command.AddOption(statsOnlyOption);

        command.SetHandler(async (solution, output, fast, full, statsOnly) =>
        {
            await ExecuteAsync(solution, output, fast, full, statsOnly);
        }, solutionOption, outputOption, fastOption, fullOption, statsOnlyOption);

        return command;
    }

    private static async Task ExecuteAsync(
        string solutionPath,
        string? outputPath,
        bool fast,
        bool full,
        bool statsOnly)
    {
        try
        {
            if (!File.Exists(solutionPath))
            {
                JsonOutput.WriteError($"Solution file not found: {solutionPath}");
                return;
            }

            // Create simple logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            var logger = loggerFactory.CreateLogger<SolutionGraphBuilder>();

            // Determine options
            var options = fast ? GraphBuildOptions.Fast 
                        : full ? GraphBuildOptions.Full 
                        : GraphBuildOptions.Default;

            // Build graph
            Console.Error.WriteLine($"Building graph for: {solutionPath}");
            Console.Error.WriteLine($"Options: {(fast ? "Fast" : full ? "Full" : "Default")}");

            await using var builder = new SolutionGraphBuilder(logger);
            
            var progress = new Progress<GraphBuildProgress>(p =>
            {
                Console.Error.WriteLine($"  [{p.Phase}] {p.CurrentItem} ({p.ProgressPercent}%)");
            });

            var graph = await builder.BuildGraphAsync(solutionPath, options, progress);

            // Get statistics
            var stats = graph.GetStatistics();

            if (statsOnly)
            {
                // Output just statistics
                var statsResult = new
                {
                    success = true,
                    solutionPath,
                    statistics = new
                    {
                        stats.SolutionCount,
                        stats.ProjectCount,
                        stats.FileCount,
                        stats.TypeCount,
                        stats.PackageCount,
                        stats.NamespaceCount,
                        stats.EdgeCount,
                        stats.ProjectReferenceCount,
                        stats.PackageReferenceCount,
                        stats.TypeUsageCount,
                        stats.InheritanceCount
                    }
                };
                
                Console.WriteLine(JsonSerializer.Serialize(statsResult, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                // Output full graph
                var graphResult = new
                {
                    success = true,
                    solutionPath,
                    statistics = new
                    {
                        stats.SolutionCount,
                        stats.ProjectCount,
                        stats.FileCount,
                        stats.TypeCount,
                        stats.PackageCount,
                        stats.NamespaceCount,
                        stats.EdgeCount
                    },
                    solutions = graph.Solutions.Values.Select(s => new { s.Id, s.Name, s.Path }),
                    projects = graph.Projects.Values.Select(p => new 
                    { 
                        p.Id, 
                        p.Name, 
                        p.Path, 
                        p.RootNamespace,
                        ProjectType = p.ProjectType.ToString()
                    }),
                    files = graph.Files.Values.Select(f => new 
                    { 
                        f.Id, 
                        f.Path, 
                        f.Namespace,
                        FileType = f.FileType.ToString()
                    }),
                    types = graph.Types.Values.Select(t => new 
                    { 
                        t.Id, 
                        t.FullName, 
                        t.Namespace, 
                        t.Name,
                        Kind = t.Kind.ToString(),
                        t.IsPublic,
                        t.IsPartial,
                        t.IsStatic,
                        t.IsAbstract
                    }),
                    namespaces = graph.Namespaces.Values.Select(n => new { n.Id, n.Namespace }),
                    packages = graph.Packages.Values.Select(p => new { p.Id, p.PackageId, p.Version }),
                    edges = graph.Edges.Select(e => new 
                    { 
                        Type = e.GetType().Name, 
                        e.SourceId, 
                        e.TargetId,
                        e.Description
                    })
                };

                var json = JsonSerializer.Serialize(graphResult, new JsonSerializerOptions { WriteIndented = true });
                
                if (outputPath != null)
                {
                    await File.WriteAllTextAsync(outputPath, json);
                    Console.Error.WriteLine($"Graph saved to: {outputPath}");
                    
                    // Also output summary to stdout
                    JsonOutput.WriteSuccess(new
                    {
                        savedTo = outputPath,
                        statistics = new
                        {
                            stats.SolutionCount,
                            stats.ProjectCount,
                            stats.FileCount,
                            stats.TypeCount,
                            stats.NamespaceCount,
                            stats.EdgeCount
                        }
                    });
                }
                else
                {
                    Console.WriteLine(json);
                }
            }
        }
        catch (Exception ex)
        {
            JsonOutput.WriteError(ex.Message);
        }
    }
}
