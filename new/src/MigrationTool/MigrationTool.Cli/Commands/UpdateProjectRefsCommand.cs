using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// Command to update project references in .csproj files.
/// </summary>
public static class UpdateProjectRefsCommand
{
    /// <summary>
    /// Creates the update-project-refs command.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("update-project-refs", "Update project references in .csproj");
        
        var projectOption = new Option<string>("--project", "Path to .csproj file") { IsRequired = true };
        var oldRefOption = new Option<string>("--old-ref", "Old reference path") { IsRequired = true };
        var newRefOption = new Option<string>("--new-ref", "New reference path") { IsRequired = true };
        
        command.AddOption(projectOption);
        command.AddOption(oldRefOption);
        command.AddOption(newRefOption);
        
        command.SetHandler(ExecuteAsync, projectOption, oldRefOption, newRefOption);
        
        return command;
    }

    private static async Task ExecuteAsync(string projectPath, string oldRef, string newRef)
    {
        try
        {
            var service = new ProjectRefService();
            var result = await service.UpdateReferenceAsync(projectPath, oldRef, newRef);
            
            JsonOutput.WriteSuccess(new
            {
                result.Success,
                result.ProjectPath,
                result.OldRef,
                result.NewRef,
                result.Message
            });
        }
        catch (Exception ex)
        {
            JsonOutput.WriteException(ex);
        }
    }
}
