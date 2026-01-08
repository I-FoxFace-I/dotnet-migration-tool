using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// CLI command for copying a file with namespace update.
/// </summary>
public static class CopyFileCommand
{
    public static Command Create()
    {
        var sourceArg = new Argument<string>("source", "Source file path");
        var targetArg = new Argument<string>("target", "Target file path");
        var noNamespaceOption = new Option<bool>("--no-namespace", "Skip namespace update");
        var dryRunOption = new Option<bool>("--dry-run", "Preview changes without applying");

        var command = new Command("copy-file", "Copy a file and update namespace in the copy")
        {
            sourceArg,
            targetArg,
            noNamespaceOption,
            dryRunOption
        };

        command.SetHandler(async (source, target, noNamespace, dryRun) =>
        {
            var service = new FileOperationService();
            var result = await service.CopyFileAsync(
                source,
                target,
                updateNamespace: !noNamespace,
                dryRun: dryRun);

            JsonOutput.WriteSuccess(new
            {
                result.Success,
                result.Operation,
                result.Path,
                result.TargetPath,
                result.DryRun,
                result.OldNamespace,
                result.NewNamespace,
                result.NamespaceUpdated,
                result.AffectedFiles,
                result.ErrorMessage
            });
        }, sourceArg, targetArg, noNamespaceOption, dryRunOption);

        return command;
    }
}
