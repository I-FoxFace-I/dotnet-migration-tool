using Microsoft.CodeAnalysis.CSharp;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Rewriters;
using MigrationTool.Core.Utilities;

namespace MigrationTool.Core.Services;

/// <summary>
/// Service for file-level operations (move, copy, delete, rename).
/// </summary>
public class FileOperationService : IFileOperationService
{
    /// <inheritdoc />
    public async Task<FileOperationResult> MoveFileAsync(
        string source,
        string target,
        bool updateNamespace = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        return await ProcessFileAsync(source, target, FileOperationType.Move, updateNamespace, false, dryRun, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileOperationResult> CopyFileAsync(
        string source,
        string target,
        bool updateNamespace = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        return await ProcessFileAsync(source, target, FileOperationType.Copy, updateNamespace, false, dryRun, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileOperationResult> DeleteFileAsync(
        string path,
        bool checkReferences = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return CreateErrorResult(FileOperationType.Delete, path, null, dryRun, $"File not found: {path}");
        }

        var referencingFiles = new List<string>();
        
        if (checkReferences && Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            // Find files that might reference this file's types
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                var typeName = Path.GetFileNameWithoutExtension(path);
                referencingFiles = await FindReferencingFilesAsync(directory, typeName, cancellationToken);
            }
        }

        if (!dryRun)
        {
            File.Delete(path);
        }

        return new FileOperationResult(
            Success: true,
            Operation: FileOperationType.Delete,
            Path: path,
            TargetPath: null,
            DryRun: dryRun,
            OldNamespace: null,
            NewNamespace: null,
            NamespaceUpdated: false,
            ClassRenamed: false,
            AffectedFiles: [path],
            ReferencingFiles: referencingFiles);
    }

    /// <inheritdoc />
    public async Task<FileOperationResult> RenameFileAsync(
        string path,
        string newName,
        bool renameClass = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return CreateErrorResult(FileOperationType.Rename, path, null, dryRun, $"File not found: {path}");
        }

        var directory = Path.GetDirectoryName(path) ?? ".";
        var targetPath = Path.Combine(directory, newName);

        if (File.Exists(targetPath))
        {
            return CreateErrorResult(FileOperationType.Rename, path, targetPath, dryRun, $"Target file already exists: {targetPath}");
        }

        var oldClassName = Path.GetFileNameWithoutExtension(path);
        var newClassName = Path.GetFileNameWithoutExtension(newName);
        var classRenamed = false;

        if (dryRun)
        {
            return new FileOperationResult(
                Success: true,
                Operation: FileOperationType.Rename,
                Path: path,
                TargetPath: targetPath,
                DryRun: true,
                OldNamespace: null,
                NewNamespace: null,
                NamespaceUpdated: false,
                ClassRenamed: renameClass && Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase),
                AffectedFiles: [path],
                ReferencingFiles: []);
        }

        if (renameClass && Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            var code = await File.ReadAllTextAsync(path, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            var renamer = new ClassRenamer(oldClassName, newClassName);
            var newRoot = renamer.Visit(root);

            if (renamer.TypeRenamed)
            {
                await File.WriteAllTextAsync(targetPath, newRoot.ToFullString(), cancellationToken);
                File.Delete(path);
                classRenamed = true;
            }
            else
            {
                File.Move(path, targetPath);
            }
        }
        else
        {
            File.Move(path, targetPath);
        }

        return new FileOperationResult(
            Success: true,
            Operation: FileOperationType.Rename,
            Path: path,
            TargetPath: targetPath,
            DryRun: false,
            OldNamespace: null,
            NewNamespace: null,
            NamespaceUpdated: false,
            ClassRenamed: classRenamed,
            AffectedFiles: [path, targetPath],
            ReferencingFiles: []);
    }

    /// <inheritdoc />
    public async Task<FileOperationResult> DeleteFolderAsync(
        string path,
        bool checkReferences = true,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
        {
            return CreateErrorResult(FileOperationType.DeleteFolder, path, null, dryRun, $"Folder not found: {path}");
        }

        var affectedFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
        var referencingFiles = new List<string>();

        if (checkReferences)
        {
            var parentDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentDir))
            {
                // Find types defined in this folder
                var csFiles = affectedFiles.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
                foreach (var csFile in csFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var typeName = Path.GetFileNameWithoutExtension(csFile);
                    var refs = await FindReferencingFilesAsync(parentDir, typeName, cancellationToken);
                    referencingFiles.AddRange(refs.Where(r => !r.StartsWith(path)));
                }
            }
        }

        if (!dryRun)
        {
            Directory.Delete(path, recursive: true);
        }

        return new FileOperationResult(
            Success: true,
            Operation: FileOperationType.DeleteFolder,
            Path: path,
            TargetPath: null,
            DryRun: dryRun,
            OldNamespace: null,
            NewNamespace: null,
            NamespaceUpdated: false,
            ClassRenamed: false,
            AffectedFiles: affectedFiles,
            ReferencingFiles: referencingFiles.Distinct().ToList());
    }

    private async Task<FileOperationResult> ProcessFileAsync(
        string source,
        string target,
        FileOperationType operation,
        bool updateNamespace,
        bool renameClass,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(source))
        {
            return CreateErrorResult(operation, source, target, dryRun, $"Source file not found: {source}");
        }

        if (File.Exists(target))
        {
            return CreateErrorResult(operation, source, target, dryRun, $"Target file already exists: {target}");
        }

        var isCsFile = Path.GetExtension(source).Equals(".cs", StringComparison.OrdinalIgnoreCase);
        string? oldNamespace = null;
        string? newNamespace = null;
        var namespaceUpdated = false;

        if (updateNamespace && isCsFile)
        {
            oldNamespace = NamespaceDetector.DetectFromPath(Path.GetDirectoryName(source) ?? source);
            newNamespace = NamespaceDetector.DetectFromPath(Path.GetDirectoryName(target) ?? target);
        }

        if (dryRun)
        {
            return new FileOperationResult(
                Success: true,
                Operation: operation,
                Path: source,
                TargetPath: target,
                DryRun: true,
                OldNamespace: oldNamespace,
                NewNamespace: newNamespace,
                NamespaceUpdated: oldNamespace != newNamespace,
                ClassRenamed: false,
                AffectedFiles: [source],
                ReferencingFiles: []);
        }

        // Ensure target directory exists
        var targetDir = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        if (updateNamespace && isCsFile && oldNamespace != null && newNamespace != null && oldNamespace != newNamespace)
        {
            var code = await File.ReadAllTextAsync(source, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            var rewriter = new NamespaceRewriter(oldNamespace, newNamespace);
            var newRoot = rewriter.Visit(root);

            await File.WriteAllTextAsync(target, newRoot.ToFullString(), cancellationToken);
            namespaceUpdated = rewriter.ChangesCount > 0;

            if (operation == FileOperationType.Move)
            {
                File.Delete(source);
            }
        }
        else
        {
            if (operation == FileOperationType.Move)
            {
                File.Move(source, target);
            }
            else
            {
                File.Copy(source, target);
            }
        }

        return new FileOperationResult(
            Success: true,
            Operation: operation,
            Path: source,
            TargetPath: target,
            DryRun: false,
            OldNamespace: oldNamespace,
            NewNamespace: newNamespace,
            NamespaceUpdated: namespaceUpdated,
            ClassRenamed: false,
            AffectedFiles: operation == FileOperationType.Move ? [source, target] : [target],
            ReferencingFiles: []);
    }

    private static async Task<List<string>> FindReferencingFilesAsync(
        string searchDirectory,
        string typeName,
        CancellationToken cancellationToken)
    {
        var referencingFiles = new List<string>();

        if (!Directory.Exists(searchDirectory))
        {
            return referencingFiles;
        }

        var csFiles = Directory.GetFiles(searchDirectory, "*.cs", SearchOption.AllDirectories);
        
        foreach (var file in csFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                // Simple text search for type references
                if (content.Contains(typeName) && 
                    (content.Contains($": {typeName}") || 
                     content.Contains($"<{typeName}>") || 
                     content.Contains($"new {typeName}") ||
                     content.Contains($"{typeName} ") ||
                     content.Contains($"({typeName}")))
                {
                    referencingFiles.Add(file);
                }
            }
            catch
            {
                // Ignore files that can't be read
            }
        }

        return referencingFiles;
    }

    private static FileOperationResult CreateErrorResult(
        FileOperationType operation,
        string path,
        string? targetPath,
        bool dryRun,
        string error)
    {
        return new FileOperationResult(
            Success: false,
            Operation: operation,
            Path: path,
            TargetPath: targetPath,
            DryRun: dryRun,
            OldNamespace: null,
            NewNamespace: null,
            NamespaceUpdated: false,
            ClassRenamed: false,
            AffectedFiles: [],
            ReferencingFiles: [],
            ErrorMessage: error);
    }
}
