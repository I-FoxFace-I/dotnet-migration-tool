using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Graph;

namespace MigrationTool.Core.Graph;

/// <summary>
/// Analyzes the impact of migration operations on the codebase.
/// </summary>
public class ImpactAnalyzer : IImpactAnalyzer
{
    private readonly ILogger<ImpactAnalyzer> _logger;

    public ImpactAnalyzer(ILogger<ImpactAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<ImpactReport> AnalyzeMoveAsync(
        SolutionGraph graph,
        MoveOperation operation,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing move operation: {Source} -> {Target}", 
            operation.SourcePath, operation.TargetPath);

        var affectedFiles = new List<AffectedFile>();
        var affectedTypes = new List<AffectedType>();
        var warnings = new List<MigrationWarning>();
        var errors = new List<MigrationError>();
        var requiredProjectRefs = new List<RequiredProjectReference>();

        // Find the file/folder being moved
        var sourceFileNode = graph.Files.Values
            .FirstOrDefault(f => f.Path.Equals(operation.SourcePath, StringComparison.OrdinalIgnoreCase));

        if (sourceFileNode == null && !operation.IsFolder)
        {
            errors.Add(new MigrationError(
                "FILE_NOT_FOUND",
                $"Source file not found in graph: {operation.SourcePath}",
                operation.SourcePath));
            
            return Task.FromResult(CreateReport(operation, affectedFiles, affectedTypes, 
                requiredProjectRefs, warnings, errors));
        }

        // Check if target already exists
        var targetExists = graph.Files.Values
            .Any(f => f.Path.Equals(operation.TargetPath, StringComparison.OrdinalIgnoreCase));
        
        if (targetExists)
        {
            errors.Add(new MigrationError(
                "TARGET_EXISTS",
                $"Target file already exists: {operation.TargetPath}",
                operation.TargetPath));
        }

        // Determine old and new namespace
        string? oldNamespace = null;
        string? newNamespace = operation.NewNamespace;

        if (sourceFileNode != null)
        {
            oldNamespace = sourceFileNode.Namespace;
            
            // Add the file being moved
            var moveChanges = new List<RequiredChange>
            {
                new(RequiredChangeType.MoveFile, null, operation.SourcePath, operation.TargetPath,
                    $"Move file to {operation.TargetPath}")
            };

            if (oldNamespace != null && newNamespace != null && oldNamespace != newNamespace)
            {
                moveChanges.Add(new RequiredChange(
                    RequiredChangeType.UpdateNamespace,
                    null,
                    oldNamespace,
                    newNamespace,
                    $"Update namespace from {oldNamespace} to {newNamespace}"));
            }

            var sourceProject = graph.GetProjectContainingFile(sourceFileNode.Id);
            affectedFiles.Add(new AffectedFile(
                operation.SourcePath,
                sourceProject?.Path ?? "unknown",
                AffectedFileReason.DirectlyMoved,
                moveChanges));

            // Find types in this file
            var typesInFile = graph.Types.Values
                .Where(t => t.FileId == sourceFileNode.Id)
                .ToList();

            foreach (var type in typesInFile)
            {
                affectedTypes.Add(new AffectedType(
                    type.FullName,
                    sourceFileNode.Path,
                    AffectedTypeReason.DirectlyMoved));

                // Find files that reference this type
                var referencingFiles = graph.GetFilesReferencingType(type.Id).ToList();
                foreach (var refFile in referencingFiles)
                {
                    if (refFile.Path == sourceFileNode.Path) continue; // Skip self

                    var refProject = graph.GetProjectContainingFile(refFile.Id);
                    var changes = new List<RequiredChange>();

                    // Check if using directive needs update
                    if (oldNamespace != null && newNamespace != null && oldNamespace != newNamespace)
                    {
                        var usesOldNamespace = graph.Edges
                            .OfType<FileUsesNamespaceEdge>()
                            .Any(e => e.SourceId == refFile.Id && 
                                     graph.Namespaces.GetValueOrDefault(e.TargetId)?.Namespace == oldNamespace);

                        if (usesOldNamespace)
                        {
                            changes.Add(new RequiredChange(
                                RequiredChangeType.UpdateUsingDirective,
                                null,
                                $"using {oldNamespace};",
                                $"using {newNamespace};",
                                $"Update using directive for {type.Name}"));
                        }
                        else
                        {
                            changes.Add(new RequiredChange(
                                RequiredChangeType.AddUsingDirective,
                                null,
                                null,
                                $"using {newNamespace};",
                                $"Add using directive for {type.Name}"));
                        }
                    }

                    if (changes.Any())
                    {
                        affectedFiles.Add(new AffectedFile(
                            refFile.Path,
                            refProject?.Path ?? "unknown",
                            AffectedFileReason.ContainsUsingDirective,
                            changes));

                        affectedTypes.Add(new AffectedType(
                            type.FullName,
                            refFile.Path,
                            AffectedTypeReason.ReferencesMovedType));
                    }
                }

                // Check for inheritance relationships
                var inheritingTypes = graph.Edges
                    .OfType<TypeInheritsEdge>()
                    .Where(e => e.TargetId == type.Id)
                    .Select(e => graph.Types.GetValueOrDefault(e.SourceId))
                    .Where(t => t != null)
                    .ToList();

                foreach (var inheritingType in inheritingTypes)
                {
                    if (inheritingType == null) continue;
                    
                    var inheritingFile = graph.GetFileContainingType(inheritingType.Id);
                    if (inheritingFile != null && inheritingFile.Path != sourceFileNode.Path)
                    {
                        affectedTypes.Add(new AffectedType(
                            inheritingType.FullName,
                            inheritingFile.Path,
                            AffectedTypeReason.InheritsFromMovedType));
                    }
                }

                // Check for interface implementations
                var implementingTypes = graph.Edges
                    .OfType<TypeImplementsEdge>()
                    .Where(e => e.TargetId == type.Id)
                    .Select(e => graph.Types.GetValueOrDefault(e.SourceId))
                    .Where(t => t != null)
                    .ToList();

                foreach (var implementingType in implementingTypes)
                {
                    if (implementingType == null) continue;
                    
                    var implementingFile = graph.GetFileContainingType(implementingType.Id);
                    if (implementingFile != null && implementingFile.Path != sourceFileNode.Path)
                    {
                        affectedTypes.Add(new AffectedType(
                            implementingType.FullName,
                            implementingFile.Path,
                            AffectedTypeReason.ImplementsMovedInterface));
                    }
                }
            }

            // Check for partial classes
            var partialTypes = typesInFile.Where(t => t.IsPartial).ToList();
            foreach (var partialType in partialTypes)
            {
                var otherParts = graph.Types.Values
                    .Where(t => t.FullName == partialType.FullName && t.Id != partialType.Id)
                    .ToList();

                if (otherParts.Any())
                {
                    warnings.Add(new MigrationWarning(
                        "PARTIAL_CLASS",
                        $"Type {partialType.Name} is a partial class with {otherParts.Count} other part(s). " +
                        "Consider moving all parts together.",
                        operation.SourcePath));
                }
            }

            // Check cross-project dependencies
            var sourceProject2 = graph.GetProjectContainingFile(sourceFileNode.Id);
            if (sourceProject2 != null)
            {
                // Find target project from target path
                var targetDir = Path.GetDirectoryName(operation.TargetPath);
                var targetProject = graph.Projects.Values
                    .FirstOrDefault(p => targetDir?.StartsWith(p.Directory, StringComparison.OrdinalIgnoreCase) == true);

                if (targetProject != null && targetProject.Id != sourceProject2.Id)
                {
                    // Check if projects that reference the moved type need new project references
                    var referencingProjects = affectedFiles
                        .Where(f => f.Reason == AffectedFileReason.ContainsUsingDirective)
                        .Select(f => f.ProjectPath)
                        .Distinct()
                        .ToList();

                    foreach (var refProjectPath in referencingProjects)
                    {
                        var refProject = graph.Projects.Values
                            .FirstOrDefault(p => p.Path.Equals(refProjectPath, StringComparison.OrdinalIgnoreCase));
                        
                        if (refProject != null)
                        {
                            // Check if ref project already references target project
                            var hasRef = graph.Edges
                                .OfType<ProjectReferenceEdge>()
                                .Any(e => e.SourceId == refProject.Id && e.TargetId == targetProject.Id);

                            if (!hasRef)
                            {
                                requiredProjectRefs.Add(new RequiredProjectReference(
                                    refProjectPath,
                                    targetProject.Path,
                                    $"Required for access to moved type(s)"));
                            }
                        }
                    }
                }
            }
        }

        // Handle folder move
        if (operation.IsFolder)
        {
            var filesInFolder = graph.Files.Values
                .Where(f => f.Path.StartsWith(operation.SourcePath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in filesInFolder)
            {
                var relativePath = file.Path.Substring(operation.SourcePath.Length);
                var newPath = Path.Combine(operation.TargetPath, relativePath.TrimStart('\\', '/'));
                
                var project = graph.GetProjectContainingFile(file.Id);
                affectedFiles.Add(new AffectedFile(
                    file.Path,
                    project?.Path ?? "unknown",
                    AffectedFileReason.DirectlyMoved,
                    new List<RequiredChange>
                    {
                        new(RequiredChangeType.MoveFile, null, file.Path, newPath,
                            $"Move file to {newPath}")
                    }));
            }

            if (filesInFolder.Count > 10)
            {
                warnings.Add(new MigrationWarning(
                    "LARGE_FOLDER_MOVE",
                    $"Moving {filesInFolder.Count} files. Consider reviewing the impact carefully.",
                    operation.SourcePath));
            }
        }

        // Remove duplicates from affected files
        affectedFiles = affectedFiles
            .GroupBy(f => f.FilePath)
            .Select(g => new AffectedFile(
                g.Key,
                g.First().ProjectPath,
                g.First().Reason,
                g.SelectMany(f => f.RequiredChanges).Distinct().ToList()))
            .ToList();

        // Remove duplicates from affected types
        affectedTypes = affectedTypes
            .GroupBy(t => new { t.TypeFullName, t.FilePath })
            .Select(g => g.First())
            .ToList();

        return Task.FromResult(CreateReport(operation, affectedFiles, affectedTypes, 
            requiredProjectRefs, warnings, errors));
    }

    /// <inheritdoc />
    public Task<ImpactReport> AnalyzeRenameNamespaceAsync(
        SolutionGraph graph,
        RenameNamespaceOperation operation,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing namespace rename: {Old} -> {New}", 
            operation.OldNamespace, operation.NewNamespace);

        var affectedFiles = new List<AffectedFile>();
        var affectedTypes = new List<AffectedType>();
        var warnings = new List<MigrationWarning>();
        var errors = new List<MigrationError>();

        // Find all types in the old namespace
        var typesInNamespace = graph.GetTypesInNamespace(operation.OldNamespace).ToList();

        if (!typesInNamespace.Any())
        {
            warnings.Add(new MigrationWarning(
                "NAMESPACE_EMPTY",
                $"No types found in namespace {operation.OldNamespace}"));
        }

        // Find all files that define types in this namespace
        var definingFiles = typesInNamespace
            .Select(t => graph.GetFileContainingType(t.Id))
            .Where(f => f != null)
            .Distinct()
            .ToList();

        foreach (var file in definingFiles)
        {
            if (file == null) continue;
            
            var project = graph.GetProjectContainingFile(file.Id);
            affectedFiles.Add(new AffectedFile(
                file.Path,
                project?.Path ?? "unknown",
                AffectedFileReason.ContainsUsingDirective,
                new List<RequiredChange>
                {
                    new(RequiredChangeType.UpdateNamespace, null, 
                        operation.OldNamespace, operation.NewNamespace,
                        $"Update namespace declaration")
                }));
        }

        // Find all files that use this namespace
        var usingFiles = graph.GetFilesUsingNamespace(operation.OldNamespace).ToList();

        foreach (var file in usingFiles)
        {
            if (definingFiles.Contains(file)) continue; // Already added
            
            var project = graph.GetProjectContainingFile(file.Id);
            
            // Find the line number of the using directive
            var usingEdge = graph.Edges
                .OfType<FileUsesNamespaceEdge>()
                .FirstOrDefault(e => e.SourceId == file.Id && 
                    graph.Namespaces.GetValueOrDefault(e.TargetId)?.Namespace == operation.OldNamespace);

            affectedFiles.Add(new AffectedFile(
                file.Path,
                project?.Path ?? "unknown",
                AffectedFileReason.ContainsUsingDirective,
                new List<RequiredChange>
                {
                    new(RequiredChangeType.UpdateUsingDirective, 
                        usingEdge?.LineNumber, 
                        $"using {operation.OldNamespace};",
                        $"using {operation.NewNamespace};",
                        $"Update using directive")
                }));
        }

        // Add affected types
        foreach (var type in typesInNamespace)
        {
            var file = graph.GetFileContainingType(type.Id);
            affectedTypes.Add(new AffectedType(
                type.FullName,
                file?.Path ?? "unknown",
                AffectedTypeReason.NamespaceChanged));
        }

        return Task.FromResult(CreateReport(operation, affectedFiles, affectedTypes, 
            new List<RequiredProjectReference>(), warnings, errors));
    }

    /// <inheritdoc />
    public Task<ImpactReport> AnalyzeDeleteAsync(
        SolutionGraph graph,
        DeleteOperation operation,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing delete operation: {Path}", operation.Path);

        var affectedFiles = new List<AffectedFile>();
        var affectedTypes = new List<AffectedType>();
        var warnings = new List<MigrationWarning>();
        var errors = new List<MigrationError>();

        var fileNode = graph.Files.Values
            .FirstOrDefault(f => f.Path.Equals(operation.Path, StringComparison.OrdinalIgnoreCase));

        if (fileNode == null && !operation.IsFolder)
        {
            errors.Add(new MigrationError(
                "FILE_NOT_FOUND",
                $"File not found in graph: {operation.Path}",
                operation.Path));
            
            return Task.FromResult(CreateReport(operation, affectedFiles, affectedTypes, 
                new List<RequiredProjectReference>(), warnings, errors));
        }

        if (fileNode != null)
        {
            var project = graph.GetProjectContainingFile(fileNode.Id);
            affectedFiles.Add(new AffectedFile(
                operation.Path,
                project?.Path ?? "unknown",
                AffectedFileReason.DirectlyDeleted,
                new List<RequiredChange>
                {
                    new(RequiredChangeType.DeleteFile, null, operation.Path, null,
                        "Delete file")
                }));

            // Find types in this file
            var typesInFile = graph.Types.Values
                .Where(t => t.FileId == fileNode.Id)
                .ToList();

            foreach (var type in typesInFile)
            {
                affectedTypes.Add(new AffectedType(
                    type.FullName,
                    fileNode.Path,
                    AffectedTypeReason.DirectlyDeleted));

                // Find files that reference this type - these will have broken references!
                var referencingFiles = graph.GetFilesReferencingType(type.Id).ToList();
                foreach (var refFile in referencingFiles)
                {
                    if (refFile.Path == fileNode.Path) continue;

                    if (!operation.Force)
                    {
                        errors.Add(new MigrationError(
                            "TYPE_IN_USE",
                            $"Type {type.Name} is referenced in {refFile.Path}. " +
                            "Use --force to delete anyway.",
                            refFile.Path));
                    }
                    else
                    {
                        warnings.Add(new MigrationWarning(
                            "BROKEN_REFERENCE",
                            $"Deleting {type.Name} will break references in {refFile.Path}",
                            refFile.Path));
                    }
                }
            }
        }

        // Handle folder delete
        if (operation.IsFolder)
        {
            var filesInFolder = graph.Files.Values
                .Where(f => f.Path.StartsWith(operation.Path, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in filesInFolder)
            {
                var project = graph.GetProjectContainingFile(file.Id);
                affectedFiles.Add(new AffectedFile(
                    file.Path,
                    project?.Path ?? "unknown",
                    AffectedFileReason.DirectlyDeleted,
                    new List<RequiredChange>
                    {
                        new(RequiredChangeType.DeleteFile, null, file.Path, null,
                            "Delete file")
                    }));
            }
        }

        return Task.FromResult(CreateReport(operation, affectedFiles, affectedTypes, 
            new List<RequiredProjectReference>(), warnings, errors));
    }

    /// <inheritdoc />
    public Task<ImpactReport> AnalyzeMoveTypeAsync(
        SolutionGraph graph,
        MoveTypeOperation operation,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing move type: {Type} -> {Namespace}", 
            operation.TypeFullName, operation.NewNamespace);

        var affectedFiles = new List<AffectedFile>();
        var affectedTypes = new List<AffectedType>();
        var warnings = new List<MigrationWarning>();
        var errors = new List<MigrationError>();

        var typeNode = graph.FindType(operation.TypeFullName);

        if (typeNode == null)
        {
            errors.Add(new MigrationError(
                "TYPE_NOT_FOUND",
                $"Type not found: {operation.TypeFullName}"));
            
            return Task.FromResult(CreateReport(operation, affectedFiles, affectedTypes, 
                new List<RequiredProjectReference>(), warnings, errors));
        }

        var sourceFile = graph.GetFileContainingType(typeNode.Id);
        if (sourceFile == null)
        {
            errors.Add(new MigrationError(
                "FILE_NOT_FOUND",
                $"Source file not found for type: {operation.TypeFullName}"));
            
            return Task.FromResult(CreateReport(operation, affectedFiles, affectedTypes, 
                new List<RequiredProjectReference>(), warnings, errors));
        }

        var oldNamespace = typeNode.Namespace;
        var newNamespace = operation.NewNamespace;

        // Add the source file
        var sourceProject = graph.GetProjectContainingFile(sourceFile.Id);
        var changes = new List<RequiredChange>
        {
            new(RequiredChangeType.UpdateNamespace, null, oldNamespace, newNamespace,
                $"Update namespace for {typeNode.Name}")
        };

        if (operation.NewFilePath != null)
        {
            changes.Add(new RequiredChange(
                RequiredChangeType.MoveFile, null, sourceFile.Path, operation.NewFilePath,
                $"Move file to {operation.NewFilePath}"));
        }

        affectedFiles.Add(new AffectedFile(
            sourceFile.Path,
            sourceProject?.Path ?? "unknown",
            AffectedFileReason.DirectlyMoved,
            changes));

        affectedTypes.Add(new AffectedType(
            typeNode.FullName,
            sourceFile.Path,
            AffectedTypeReason.DirectlyMoved));

        // Find all files that reference this type
        var referencingFiles = graph.GetFilesReferencingType(typeNode.Id).ToList();
        foreach (var refFile in referencingFiles)
        {
            if (refFile.Path == sourceFile.Path) continue;

            var refProject = graph.GetProjectContainingFile(refFile.Id);
            var refChanges = new List<RequiredChange>();

            // Check if using directive needs update
            var usesOldNamespace = graph.Edges
                .OfType<FileUsesNamespaceEdge>()
                .Any(e => e.SourceId == refFile.Id && 
                         graph.Namespaces.GetValueOrDefault(e.TargetId)?.Namespace == oldNamespace);

            if (usesOldNamespace)
            {
                refChanges.Add(new RequiredChange(
                    RequiredChangeType.UpdateUsingDirective,
                    null,
                    $"using {oldNamespace};",
                    $"using {newNamespace};",
                    $"Update using directive for {typeNode.Name}"));
            }
            else
            {
                refChanges.Add(new RequiredChange(
                    RequiredChangeType.AddUsingDirective,
                    null,
                    null,
                    $"using {newNamespace};",
                    $"Add using directive for {typeNode.Name}"));
            }

            affectedFiles.Add(new AffectedFile(
                refFile.Path,
                refProject?.Path ?? "unknown",
                AffectedFileReason.ContainsUsingDirective,
                refChanges));
        }

        return Task.FromResult(CreateReport(operation, affectedFiles, affectedTypes, 
            new List<RequiredProjectReference>(), warnings, errors));
    }

    private static ImpactReport CreateReport(
        MigrationOperation operation,
        List<AffectedFile> affectedFiles,
        List<AffectedType> affectedTypes,
        List<RequiredProjectReference> requiredProjectRefs,
        List<MigrationWarning> warnings,
        List<MigrationError> errors)
    {
        // Calculate complexity
        var complexity = CalculateComplexity(affectedFiles, affectedTypes, requiredProjectRefs, errors);

        return new ImpactReport
        {
            Operation = operation,
            Complexity = complexity,
            AffectedFiles = affectedFiles,
            AffectedTypes = affectedTypes,
            RequiredProjectReferences = requiredProjectRefs,
            RequiredPackageReferences = new List<RequiredPackageReference>(),
            Warnings = warnings,
            Errors = errors
        };
    }

    private static MigrationComplexity CalculateComplexity(
        List<AffectedFile> affectedFiles,
        List<AffectedType> affectedTypes,
        List<RequiredProjectReference> requiredProjectRefs,
        List<MigrationError> errors)
    {
        if (errors.Any())
            return MigrationComplexity.VeryComplex;

        var score = 0;
        
        // File count
        score += affectedFiles.Count switch
        {
            <= 1 => 0,
            <= 5 => 1,
            <= 20 => 2,
            _ => 3
        };

        // Type count
        score += affectedTypes.Count switch
        {
            <= 1 => 0,
            <= 5 => 1,
            <= 20 => 2,
            _ => 3
        };

        // Cross-project references
        score += requiredProjectRefs.Count switch
        {
            0 => 0,
            <= 2 => 1,
            _ => 2
        };

        // Number of distinct projects affected
        var projectCount = affectedFiles.Select(f => f.ProjectPath).Distinct().Count();
        score += projectCount switch
        {
            <= 1 => 0,
            <= 3 => 1,
            _ => 2
        };

        return score switch
        {
            <= 1 => MigrationComplexity.Simple,
            <= 4 => MigrationComplexity.Medium,
            <= 7 => MigrationComplexity.Complex,
            _ => MigrationComplexity.VeryComplex
        };
    }
}
