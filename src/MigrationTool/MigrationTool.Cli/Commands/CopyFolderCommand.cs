using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// Command to copy a folder and update namespaces in the copy.
/// Original files are preserved (copy-based migration).
/// </summary>
public static class CopyFolderCommand
{
    /// <summary>
    /// Creates the copy-folder command.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("copy-folder", "Copy a folder and update namespaces in the copy (original files preserved)");
        
        var sourceOption = new Option<string>("--source", "Source folder path") { IsRequired = true };
        var targetOption = new Option<string>("--target", "Target folder path") { IsRequired = true };
        var dryRunOption = new Option<bool>("--dry-run", "Preview changes without applying");
        
        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(dryRunOption);
        
        command.SetHandler(ExecuteAsync, sourceOption, targetOption, dryRunOption);
        
        return command;
    }

    private static async Task ExecuteAsync(string source, string target, bool dryRun)
    {
        try
        {
            var service = new FolderMigrationService();
            var result = await service.CopyFolderAsync(source, target, dryRun);
            
            if (!result.Success)
            {
                JsonOutput.WriteError(result.ErrorMessage ?? "Unknown error");
                return;
            }
            
            JsonOutput.WriteSuccess(new
            {
                result.Success,
                result.Source,
                result.Target,
                result.DryRun,
                result.FilesCount,
                result.Files,
                result.OldNamespace,
                result.NewNamespace,
                result.UpdatedNamespaces,
                result.OriginalPreserved
            });
        }
        catch (Exception ex)
        {
            JsonOutput.WriteException(ex);
        }
    }
}
