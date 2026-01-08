using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// CLI command for deleting a file with reference checking.
/// </summary>
public static class DeleteFileCommand
{
    public static Command Create()
    {
        var pathArg = new Argument<string>("path", "File path to delete");
        var noCheckOption = new Option<bool>("--no-check", "Skip reference checking");
        var dryRunOption = new Option<bool>("--dry-run", "Preview changes without applying");

        var command = new Command("delete-file", "Delete a file with optional reference checking")
        {
            pathArg,
            noCheckOption,
            dryRunOption
        };

        command.SetHandler(async (path, noCheck, dryRun) =>
        {
            var service = new FileOperationService();
            var result = await service.DeleteFileAsync(
                path,
                checkReferences: !noCheck,
                dryRun: dryRun);

            JsonOutput.WriteSuccess(new
            {
                result.Success,
                result.Operation,
                result.Path,
                result.DryRun,
                result.AffectedFiles,
                result.ReferencingFiles,
                result.ErrorMessage
            });
        }, pathArg, noCheckOption, dryRunOption);

        return command;
    }
}
