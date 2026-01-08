using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// CLI command for renaming a file with optional class rename.
/// </summary>
public static class RenameFileCommand
{
    public static Command Create()
    {
        var pathArg = new Argument<string>("path", "File path to rename");
        var newNameArg = new Argument<string>("new-name", "New file name (without path)");
        var noClassOption = new Option<bool>("--no-class", "Skip class rename in C# files");
        var dryRunOption = new Option<bool>("--dry-run", "Preview changes without applying");

        var command = new Command("rename-file", "Rename a file and optionally rename the class inside")
        {
            pathArg,
            newNameArg,
            noClassOption,
            dryRunOption
        };

        command.SetHandler(async (path, newName, noClass, dryRun) =>
        {
            var service = new FileOperationService();
            var result = await service.RenameFileAsync(
                path,
                newName,
                renameClass: !noClass,
                dryRun: dryRun);

            JsonOutput.WriteSuccess(new
            {
                result.Success,
                result.Operation,
                result.Path,
                result.TargetPath,
                result.DryRun,
                result.ClassRenamed,
                result.AffectedFiles,
                result.ErrorMessage
            });
        }, pathArg, newNameArg, noClassOption, dryRunOption);

        return command;
    }
}
