using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// Command to update namespaces in C# files using Roslyn.
/// </summary>
public static class UpdateNamespaceCommand
{
    /// <summary>
    /// Creates the update-namespace command.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("update-namespace", "Update namespace in C# files");
        
        var fileOption = new Option<string>("--file", "Path to C# file") { IsRequired = true };
        var oldOption = new Option<string>("--old", "Old namespace") { IsRequired = true };
        var newOption = new Option<string>("--new", "New namespace") { IsRequired = true };
        
        command.AddOption(fileOption);
        command.AddOption(oldOption);
        command.AddOption(newOption);
        
        command.SetHandler(ExecuteAsync, fileOption, oldOption, newOption);
        
        return command;
    }

    private static async Task ExecuteAsync(string filePath, string oldNamespace, string newNamespace)
    {
        try
        {
            var service = new NamespaceService();
            var result = await service.UpdateNamespaceAsync(filePath, oldNamespace, newNamespace);
            
            JsonOutput.WriteSuccess(new
            {
                result.Success,
                result.FilePath,
                Changes = result.ChangesCount,
                result.Message
            });
        }
        catch (Exception ex)
        {
            JsonOutput.WriteException(ex);
        }
    }
}
