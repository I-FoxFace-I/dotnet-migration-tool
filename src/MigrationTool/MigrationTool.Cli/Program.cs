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
        
        // Solution & Project commands
        rootCommand.AddCommand(AnalyzeSolutionCommand.Create());
        rootCommand.AddCommand(UpdateSolutionCommand.Create());
        rootCommand.AddCommand(UpdateProjectRefsCommand.Create());
        rootCommand.AddCommand(FindUsagesCommand.Create());
        
        // Folder commands
        rootCommand.AddCommand(MoveFolderCommand.Create());
        rootCommand.AddCommand(CopyFolderCommand.Create());
        rootCommand.AddCommand(DeleteFolderCommand.Create());
        
        // File commands
        rootCommand.AddCommand(MoveFileCommand.Create());
        rootCommand.AddCommand(CopyFileCommand.Create());
        rootCommand.AddCommand(DeleteFileCommand.Create());
        rootCommand.AddCommand(RenameFileCommand.Create());
        rootCommand.AddCommand(UpdateNamespaceCommand.Create());
        
        // Cross-solution migration
        rootCommand.AddCommand(CrossSolutionMigrateCommand.Create());

        return await rootCommand.InvokeAsync(args);
    }
}
