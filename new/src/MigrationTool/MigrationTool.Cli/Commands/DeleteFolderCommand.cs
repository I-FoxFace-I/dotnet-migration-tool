using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// CLI command for deleting a folder with reference checking.
/// </summary>
public static class DeleteFolderCommand
{
    public static Command Create()
    {
        var pathArg = new Argument<string>("path", "Folder path to delete");
        var noCheckOption = new Option<bool>("--no-check", "Skip reference checking");
        var dryRunOption = new Option<bool>("--dry-run", "Preview changes without applying");

        var command = new Command("delete-folder", "Delete a folder with optional reference checking")
        {
            pathArg,
            noCheckOption,
            dryRunOption
        };

        command.SetHandler(async (path, noCheck, dryRun) =>
        {
            var service = new FileOperationService();
            var result = await service.DeleteFolderAsync(
                path,
                checkReferences: !noCheck,
                dryRun: dryRun);

            JsonOutput.WriteSuccess(new
            {
                result.Success,
                result.Operation,
                result.Path,
                result.DryRun,
                FilesCount = result.AffectedFiles.Count,
                result.AffectedFiles,
                result.ReferencingFiles,
                result.ErrorMessage
            });
        }, pathArg, noCheckOption, dryRunOption);

        return command;
    }
}
