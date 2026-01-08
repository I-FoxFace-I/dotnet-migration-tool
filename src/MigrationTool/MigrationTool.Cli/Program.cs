using System.CommandLine;
using Microsoft.Build.Locator;
using MigrationTool.Cli.Commands;

namespace MigrationTool.Cli;

/// <summary>
/// CLI tool for .NET-specific migration operations using Roslyn.
/// Called from Python MigrationTool for advanced refactoring.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Register MSBuild
        MSBuildLocator.RegisterDefaults();

        var rootCommand = new RootCommand("MigrationTool CLI - .NET helper for Python MigrationTool");

        // Register all commands
        rootCommand.AddCommand(AnalyzeSolutionCommand.Create());
        rootCommand.AddCommand(UpdateNamespaceCommand.Create());
        rootCommand.AddCommand(UpdateProjectRefsCommand.Create());
        rootCommand.AddCommand(FindUsagesCommand.Create());
        rootCommand.AddCommand(MoveFolderCommand.Create());
        rootCommand.AddCommand(CopyFolderCommand.Create());
        rootCommand.AddCommand(UpdateSolutionCommand.Create());

        return await rootCommand.InvokeAsync(args);
    }
}
