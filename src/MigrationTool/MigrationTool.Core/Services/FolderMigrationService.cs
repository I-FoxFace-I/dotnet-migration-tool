using Microsoft.CodeAnalysis.CSharp;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Rewriters;
using MigrationTool.Core.Utilities;

namespace MigrationTool.Core.Services;

/// <summary>
/// Service for migrating folders (move or copy) with namespace updates.
/// </summary>
public class FolderMigrationService : IFolderMigrationService
{
    /// <inheritdoc />
    public async Task<FolderMigrationResult> MoveFolderAsync(
        string source, 
        string target, 
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        return await MigrateFolderAsync(source, target, dryRun, deleteSource: true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FolderMigrationResult> CopyFolderAsync(
        string source, 
        string target, 
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        return await MigrateFolderAsync(source, target, dryRun, deleteSource: false, cancellationToken);
    }

    private async Task<FolderMigrationResult> MigrateFolderAsync(
        string source, 
        string target, 
        bool dryRun, 
        bool deleteSource,
        CancellationToken cancellationToken)
    {
        var sourceDir = new DirectoryInfo(source);
        var targetDir = new DirectoryInfo(target);

        if (!sourceDir.Exists)
        {
            return CreateErrorResult(source, target, dryRun, !deleteSource, $"Source folder not found: {source}");
        }

        if (targetDir.Exists)
        {
            return CreateErrorResult(source, target, dryRun, !deleteSource, $"Target folder already exists: {target}");
        }

        var migratedFiles = new List<string>();
        var updatedNamespaces = new List<string>();

        var oldNamespace = NamespaceDetector.DetectFromPath(sourceDir.FullName);
        var newNamespace = NamespaceDetector.DetectFromPath(targetDir.FullName);

        if (dryRun)
        {
            foreach (var file in sourceDir.GetFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                migratedFiles.Add(relativePath);
            }
        }
        else
        {
            targetDir.Create();

            foreach (var file in sourceDir.GetFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                var targetPath = Path.Combine(targetDir.FullName, relativePath);
                
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                
                if (file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) && 
                    oldNamespace != null && newNamespace != null)
                {
                    var code = await File.ReadAllTextAsync(file.FullName, cancellationToken);
                    var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
                    var root = await tree.GetRootAsync(cancellationToken);
                    
                    var rewriter = new NamespaceRewriter(oldNamespace, newNamespace);
                    var newRoot = rewriter.Visit(root);
                    
                    await File.WriteAllTextAsync(targetPath, newRoot.ToFullString(), cancellationToken);
                    
                    if (rewriter.ChangesCount > 0)
                    {
                        updatedNamespaces.Add(relativePath);
                    }
                    
                    if (deleteSource)
                    {
                        file.Delete();
                    }
                }
                else
                {
                    if (deleteSource)
                    {
                        File.Move(file.FullName, targetPath);
                    }
                    else
                    {
                        File.Copy(file.FullName, targetPath);
                    }
                }
                
                migratedFiles.Add(relativePath);
            }

            if (deleteSource)
            {
                sourceDir.Delete(true);
            }
        }

        return new FolderMigrationResult(
            Success: true,
            Source: source,
            Target: target,
            DryRun: dryRun,
            FilesCount: migratedFiles.Count,
            Files: migratedFiles,
            OldNamespace: oldNamespace,
            NewNamespace: newNamespace,
            UpdatedNamespaces: updatedNamespaces,
            OriginalPreserved: !deleteSource);
    }

    private static FolderMigrationResult CreateErrorResult(
        string source, string target, bool dryRun, bool originalPreserved, string error)
    {
        return new FolderMigrationResult(
            Success: false,
            Source: source,
            Target: target,
            DryRun: dryRun,
            FilesCount: 0,
            Files: [],
            OldNamespace: null,
            NewNamespace: null,
            UpdatedNamespaces: [],
            OriginalPreserved: originalPreserved,
            ErrorMessage: error);
    }
}
