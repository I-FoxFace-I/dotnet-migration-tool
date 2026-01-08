using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// Command to update project paths in solution files.
/// </summary>
public static class UpdateSolutionCommand
{
    /// <summary>
    /// Creates the update-solution command.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("update-solution", "Update project path in solution file");
        
        var solutionOption = new Option<string>("--solution", "Path to .sln file") { IsRequired = true };
        var oldPathOption = new Option<string>("--old-path", "Old project path") { IsRequired = true };
        var newPathOption = new Option<string>("--new-path", "New project path") { IsRequired = true };
        
        command.AddOption(solutionOption);
        command.AddOption(oldPathOption);
        command.AddOption(newPathOption);
        
        command.SetHandler(ExecuteAsync, solutionOption, oldPathOption, newPathOption);
        
        return command;
    }

    private static async Task ExecuteAsync(string solutionPath, string oldPath, string newPath)
    {
        try
        {
            var service = new SolutionFileService();
            var result = await service.UpdateProjectPathAsync(solutionPath, oldPath, newPath);
            
            JsonOutput.WriteSuccess(new
            {
                result.Success,
                result.SolutionPath,
                OldPath = oldPath,
                NewPath = newPath,
                result.Updated,
                result.Message
            });
        }
        catch (Exception ex)
        {
            JsonOutput.WriteException(ex);
        }
    }
}
