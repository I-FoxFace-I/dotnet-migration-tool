using System.CommandLine;
using Microsoft.CodeAnalysis.MSBuild;
using MigrationTool.Cli.Utilities;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// Command to analyze a .NET solution and return project information.
/// Uses Roslyn MSBuildWorkspace for accurate analysis.
/// </summary>
public static class AnalyzeSolutionCommand
{
    /// <summary>
    /// Creates the analyze-solution command.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("analyze-solution", "Analyze a .NET solution");
        
        var pathOption = new Option<string>("--path", "Path to .sln file") { IsRequired = true };
        command.AddOption(pathOption);
        
        command.SetHandler(ExecuteAsync, pathOption);
        
        return command;
    }

    private static async Task ExecuteAsync(string solutionPath)
    {
        try
        {
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(solutionPath);

            var result = new
            {
                Path = solutionPath,
                Projects = solution.Projects.Select(p => new
                {
                    p.Name,
                    Path = p.FilePath,
                    Documents = p.Documents.Count(),
                    References = p.ProjectReferences.Count()
                }).ToList()
            };

            JsonOutput.WriteSuccess(result);
        }
        catch (Exception ex)
        {
            JsonOutput.WriteException(ex);
        }
    }
}
