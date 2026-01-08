using Microsoft.CodeAnalysis.CSharp;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Rewriters;
using MigrationTool.Core.Utilities;

namespace MigrationTool.Core.Services;

/// <summary>
/// Service for migrating code between different solutions.
/// </summary>
public class CrossSolutionMigrationService : ICrossSolutionMigrationService
{
    /// <inheritdoc />
    public async Task<CrossSolutionMigrationResult> MigrateProjectAsync(
        CrossSolutionMigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        var analysis = await AnalyzeMigrationAsync(options, cancellationToken);
        
        if (!analysis.CanMigrate)
        {
            return CreateErrorResult(options, analysis.BlockingError ?? "Migration analysis failed");
        }

        return await ExecuteMigrationAsync(options, analysis, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CrossSolutionMigrationResult> MigrateFolderAsync(
        CrossSolutionMigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        return await MigrateProjectAsync(options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CrossSolutionMigrationResult> MigrateFilesAsync(
        CrossSolutionMigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        return await MigrateProjectAsync(options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CrossSolutionAnalysisResult> AnalyzeMigrationAsync(
        CrossSolutionMigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var files = new List<string>();
        var dependencies = new List<DependencyInfo>();
        var missingDependencies = new List<string>();
        var conflictingFiles = new List<string>();

        // Validate source solution
        if (!File.Exists(options.SourceSolutionPath))
        {
            return CreateAnalysisErrorResult(options, $"Source solution not found: {options.SourceSolutionPath}");
        }

        // Validate target solution
        if (!File.Exists(options.TargetSolutionPath))
        {
            return CreateAnalysisErrorResult(options, $"Target solution not found: {options.TargetSolutionPath}");
        }

        // Validate source path
        var sourcePath = GetAbsolutePath(options.SourceSolutionPath, options.SourcePath);
        if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
        {
            return CreateAnalysisErrorResult(options, $"Source path not found: {sourcePath}");
        }

        // Get target path
        var targetPath = GetAbsolutePath(options.TargetSolutionPath, options.TargetPath);

        // Check for conflicts
        if (Directory.Exists(targetPath))
        {
            var existingFiles = Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories);
            foreach (var file in existingFiles)
            {
                var relativePath = Path.GetRelativePath(targetPath, file);
                var sourceFile = Path.Combine(sourcePath, relativePath);
                if (File.Exists(sourceFile))
                {
                    conflictingFiles.Add(relativePath);
                }
            }

            if (conflictingFiles.Any())
            {
                warnings.Add($"Found {conflictingFiles.Count} conflicting files in target location");
            }
        }

        // Collect files to migrate
        if (Directory.Exists(sourcePath))
        {
            var allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var relativePath = Path.GetRelativePath(sourcePath, file);
                
                // Check exclude patterns
                if (ShouldExclude(relativePath, options.ExcludePatterns))
                {
                    continue;
                }

                files.Add(relativePath);
            }
        }
        else if (File.Exists(sourcePath))
        {
            files.Add(Path.GetFileName(sourcePath));
        }

        // Analyze dependencies from .csproj files
        var csprojFiles = files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        foreach (var csproj in csprojFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var csprojPath = Path.Combine(sourcePath, csproj);
            if (File.Exists(csprojPath))
            {
                var deps = await AnalyzeCsprojDependenciesAsync(csprojPath, cancellationToken);
                dependencies.AddRange(deps);
            }
        }

        // Analyze C# file dependencies (using directives)
        var csFiles = files.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
        foreach (var csFile in csFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var csPath = Path.Combine(sourcePath, csFile);
            if (File.Exists(csPath))
            {
                var usedNamespaces = await AnalyzeCsFileDependenciesAsync(csPath, cancellationToken);
                foreach (var ns in usedNamespaces)
                {
                    if (!dependencies.Any(d => d.Name == ns))
                    {
                        dependencies.Add(new DependencyInfo(ns, DependencyType.Project, null, false, null));
                    }
                }
            }
        }

        // Check for missing dependencies
        var oldNamespace = options.OldNamespacePrefix ?? NamespaceDetector.DetectFromPath(sourcePath);
        foreach (var dep in dependencies.Where(d => d.Type == DependencyType.Project))
        {
            if (!dep.Name.StartsWith(oldNamespace ?? "") && !IsSystemNamespace(dep.Name))
            {
                if (!options.IncludeDependencies)
                {
                    missingDependencies.Add(dep.Name);
                }
            }
        }

        if (missingDependencies.Any())
        {
            warnings.Add($"Found {missingDependencies.Count} external dependencies that won't be migrated");
        }

        return new CrossSolutionAnalysisResult(
            CanMigrate: !conflictingFiles.Any() || options.DryRun,
            SourceSolution: options.SourceSolutionPath,
            TargetSolution: options.TargetSolutionPath,
            SourcePath: sourcePath,
            TargetPath: targetPath,
            FilesToMigrate: files.Count,
            Files: files,
            Dependencies: dependencies,
            MissingDependencies: missingDependencies,
            ConflictingFiles: conflictingFiles,
            Warnings: warnings,
            BlockingError: conflictingFiles.Any() && !options.DryRun 
                ? "Target location contains conflicting files. Use DryRun to preview or resolve conflicts first." 
                : null);
    }

    private async Task<CrossSolutionMigrationResult> ExecuteMigrationAsync(
        CrossSolutionMigrationOptions options,
        CrossSolutionAnalysisResult analysis,
        CancellationToken cancellationToken)
    {
        var migratedFiles = new List<MigratedFileInfo>();
        var updatedNamespaces = new List<string>();
        var updatedUsings = new List<string>();
        var addedToSolution = new List<string>();
        var migratedDependencies = new List<DependencyInfo>();
        var warnings = new List<string>(analysis.Warnings);

        var sourcePath = analysis.SourcePath;
        var targetPath = analysis.TargetPath;

        var oldNamespace = options.OldNamespacePrefix ?? NamespaceDetector.DetectFromPath(sourcePath);
        var newNamespace = options.NewNamespacePrefix ?? NamespaceDetector.DetectFromPath(targetPath);

        if (options.DryRun)
        {
            // Return preview without making changes
            foreach (var file in analysis.Files)
            {
                var isCsFile = file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
                migratedFiles.Add(new MigratedFileInfo(
                    SourcePath: Path.Combine(sourcePath, file),
                    TargetPath: Path.Combine(targetPath, file),
                    NamespaceUpdated: isCsFile && oldNamespace != newNamespace,
                    OldNamespace: isCsFile ? oldNamespace : null,
                    NewNamespace: isCsFile ? newNamespace : null));
            }

            return new CrossSolutionMigrationResult(
                Success: true,
                SourceSolution: options.SourceSolutionPath,
                TargetSolution: options.TargetSolutionPath,
                SourcePath: sourcePath,
                TargetPath: targetPath,
                DryRun: true,
                MigratedFilesCount: migratedFiles.Count,
                MigratedFiles: migratedFiles,
                UpdatedNamespaces: oldNamespace != newNamespace ? [oldNamespace ?? "", newNamespace ?? ""] : [],
                UpdatedUsings: [],
                AddedToSolution: [],
                MigratedDependencies: [],
                Warnings: warnings);
        }

        // Create target directory
        Directory.CreateDirectory(targetPath);

        // Migrate files
        foreach (var file in analysis.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourceFile = Path.Combine(sourcePath, file);
            var targetFile = Path.Combine(targetPath, file);

            // Ensure target directory exists
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var isCsFile = file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
            var namespaceUpdated = false;
            string? fileOldNs = null;
            string? fileNewNs = null;

            if (isCsFile && oldNamespace != null && newNamespace != null && oldNamespace != newNamespace)
            {
                var code = await File.ReadAllTextAsync(sourceFile, cancellationToken);
                var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
                var root = await tree.GetRootAsync(cancellationToken);

                // Update namespace
                var nsRewriter = new NamespaceRewriter(oldNamespace, newNamespace);
                var newRoot = nsRewriter.Visit(root);

                // Update using directives if enabled
                if (options.UpdateUsings)
                {
                    var usingRewriter = new UsingRewriter()
                        .WithReplacement(oldNamespace, newNamespace);
                    newRoot = usingRewriter.Visit(newRoot);

                    if (usingRewriter.ChangesCount > 0)
                    {
                        foreach (var (old, @new) in usingRewriter.ReplacedUsings)
                        {
                            updatedUsings.Add($"{old} -> {@new}");
                        }
                    }
                }

                await File.WriteAllTextAsync(targetFile, newRoot.ToFullString(), cancellationToken);

                if (nsRewriter.ChangesCount > 0)
                {
                    namespaceUpdated = true;
                    fileOldNs = oldNamespace;
                    fileNewNs = newNamespace;
                    updatedNamespaces.Add(file);
                }

                // Delete source if not preserving
                if (!options.PreserveOriginal)
                {
                    File.Delete(sourceFile);
                }
            }
            else
            {
                // Non-C# file or no namespace change needed
                if (options.PreserveOriginal)
                {
                    File.Copy(sourceFile, targetFile, overwrite: true);
                }
                else
                {
                    File.Move(sourceFile, targetFile, overwrite: true);
                }
            }

            migratedFiles.Add(new MigratedFileInfo(
                SourcePath: sourceFile,
                TargetPath: targetFile,
                NamespaceUpdated: namespaceUpdated,
                OldNamespace: fileOldNs,
                NewNamespace: fileNewNs));
        }

        // Add to target solution if requested
        if (options.AddToTargetSolution)
        {
            var csprojFiles = migratedFiles
                .Where(f => f.TargetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.TargetPath);

            foreach (var csproj in csprojFiles)
            {
                // Note: Full solution file manipulation would require more complex logic
                // For now, just track what would be added
                addedToSolution.Add(csproj);
                warnings.Add($"Project {Path.GetFileName(csproj)} should be manually added to the solution");
            }
        }

        // Clean up empty source directory if not preserving
        if (!options.PreserveOriginal && Directory.Exists(sourcePath))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(sourcePath).Any())
                {
                    Directory.Delete(sourcePath, recursive: true);
                }
            }
            catch
            {
                warnings.Add($"Could not clean up empty source directory: {sourcePath}");
            }
        }

        return new CrossSolutionMigrationResult(
            Success: true,
            SourceSolution: options.SourceSolutionPath,
            TargetSolution: options.TargetSolutionPath,
            SourcePath: sourcePath,
            TargetPath: targetPath,
            DryRun: false,
            MigratedFilesCount: migratedFiles.Count,
            MigratedFiles: migratedFiles,
            UpdatedNamespaces: updatedNamespaces,
            UpdatedUsings: updatedUsings,
            AddedToSolution: addedToSolution,
            MigratedDependencies: migratedDependencies,
            Warnings: warnings);
    }

    private static string GetAbsolutePath(string solutionPath, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        var solutionDir = Path.GetDirectoryName(solutionPath) ?? ".";
        return Path.GetFullPath(Path.Combine(solutionDir, relativePath));
    }

    private static bool ShouldExclude(string path, IReadOnlyList<string> excludePatterns)
    {
        foreach (var pattern in excludePatterns)
        {
            if (MatchesPattern(path, pattern))
            {
                return true;
            }
        }
        return false;
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        // Simple wildcard matching
        if (pattern.StartsWith("*"))
        {
            return path.EndsWith(pattern.TrimStart('*'), StringComparison.OrdinalIgnoreCase);
        }
        if (pattern.EndsWith("*"))
        {
            return path.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);
        }
        return path.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<List<DependencyInfo>> AnalyzeCsprojDependenciesAsync(
        string csprojPath,
        CancellationToken cancellationToken)
    {
        var dependencies = new List<DependencyInfo>();

        try
        {
            var content = await File.ReadAllTextAsync(csprojPath, cancellationToken);

            // Simple regex-free parsing for PackageReference
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("<PackageReference"))
                {
                    var includeStart = line.IndexOf("Include=\"", StringComparison.OrdinalIgnoreCase);
                    if (includeStart >= 0)
                    {
                        includeStart += 9;
                        var includeEnd = line.IndexOf('"', includeStart);
                        if (includeEnd > includeStart)
                        {
                            var packageName = line.Substring(includeStart, includeEnd - includeStart);
                            
                            string? version = null;
                            var versionStart = line.IndexOf("Version=\"", StringComparison.OrdinalIgnoreCase);
                            if (versionStart >= 0)
                            {
                                versionStart += 9;
                                var versionEnd = line.IndexOf('"', versionStart);
                                if (versionEnd > versionStart)
                                {
                                    version = line.Substring(versionStart, versionEnd - versionStart);
                                }
                            }

                            dependencies.Add(new DependencyInfo(
                                packageName,
                                DependencyType.NuGetPackage,
                                version,
                                false,
                                null));
                        }
                    }
                }
                else if (line.Contains("<ProjectReference"))
                {
                    var includeStart = line.IndexOf("Include=\"", StringComparison.OrdinalIgnoreCase);
                    if (includeStart >= 0)
                    {
                        includeStart += 9;
                        var includeEnd = line.IndexOf('"', includeStart);
                        if (includeEnd > includeStart)
                        {
                            var projectPath = line.Substring(includeStart, includeEnd - includeStart);
                            dependencies.Add(new DependencyInfo(
                                projectPath,
                                DependencyType.Project,
                                null,
                                false,
                                null));
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return dependencies;
    }

    private static async Task<List<string>> AnalyzeCsFileDependenciesAsync(
        string csPath,
        CancellationToken cancellationToken)
    {
        var namespaces = new List<string>();

        try
        {
            var code = await File.ReadAllTextAsync(csPath, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            var usings = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => !string.IsNullOrEmpty(n))
                .Cast<string>();

            namespaces.AddRange(usings);
        }
        catch
        {
            // Ignore parsing errors
        }

        return namespaces;
    }

    private static bool IsSystemNamespace(string ns)
    {
        return ns.StartsWith("System") ||
               ns.StartsWith("Microsoft") ||
               ns.StartsWith("Newtonsoft") ||
               ns.StartsWith("FluentAssertions") ||
               ns.StartsWith("Xunit") ||
               ns.StartsWith("Moq");
    }

    private static CrossSolutionMigrationResult CreateErrorResult(
        CrossSolutionMigrationOptions options,
        string error)
    {
        return new CrossSolutionMigrationResult(
            Success: false,
            SourceSolution: options.SourceSolutionPath,
            TargetSolution: options.TargetSolutionPath,
            SourcePath: options.SourcePath,
            TargetPath: options.TargetPath,
            DryRun: options.DryRun,
            MigratedFilesCount: 0,
            MigratedFiles: [],
            UpdatedNamespaces: [],
            UpdatedUsings: [],
            AddedToSolution: [],
            MigratedDependencies: [],
            Warnings: [],
            ErrorMessage: error);
    }

    private static CrossSolutionAnalysisResult CreateAnalysisErrorResult(
        CrossSolutionMigrationOptions options,
        string error)
    {
        return new CrossSolutionAnalysisResult(
            CanMigrate: false,
            SourceSolution: options.SourceSolutionPath,
            TargetSolution: options.TargetSolutionPath,
            SourcePath: options.SourcePath,
            TargetPath: options.TargetPath,
            FilesToMigrate: 0,
            Files: [],
            Dependencies: [],
            MissingDependencies: [],
            ConflictingFiles: [],
            Warnings: [],
            BlockingError: error);
    }
}
