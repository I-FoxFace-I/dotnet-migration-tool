using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Graph;

namespace MigrationTool.Core.Graph;

/// <summary>
/// Builds a dependency graph from .NET solutions using Roslyn.
/// </summary>
public class SolutionGraphBuilder : ISolutionGraphBuilder, IAsyncDisposable
{
    private readonly ILogger<SolutionGraphBuilder> _logger;
    private MSBuildWorkspace? _workspace;
    private static bool _msbuildRegistered;
    private static readonly object _msbuildLock = new();

    public SolutionGraphBuilder(ILogger<SolutionGraphBuilder> logger)
    {
        _logger = logger;
        EnsureMSBuildRegistered();
    }

    private static void EnsureMSBuildRegistered()
    {
        lock (_msbuildLock)
        {
            if (!_msbuildRegistered)
            {
                try
                {
                    MSBuildLocator.RegisterDefaults();
                    _msbuildRegistered = true;
                }
                catch (InvalidOperationException)
                {
                    // Already registered
                    _msbuildRegistered = true;
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<SolutionGraph> BuildGraphAsync(
        string solutionPath,
        GraphBuildOptions? options = null,
        IProgress<GraphBuildProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await BuildGraphAsync(
            new[] { solutionPath },
            options,
            progress,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SolutionGraph> BuildGraphAsync(
        IEnumerable<string> solutionPaths,
        GraphBuildOptions? options = null,
        IProgress<GraphBuildProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= GraphBuildOptions.Default;
        var graph = new SolutionGraph();
        var solutionPathsList = solutionPaths.ToList();

        _logger.LogInformation("Building graph for {Count} solution(s)", solutionPathsList.Count);

        _workspace = MSBuildWorkspace.Create();
        _workspace.WorkspaceFailed += (sender, args) =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                _logger.LogWarning("Workspace warning: {Message}", args.Diagnostic.Message);
            }
        };

        try
        {
            var totalProjects = 0;
            var processedProjects = 0;

            // First pass: count total projects and load solutions/projects
            var solutionsAndProjects = new List<(SolutionNode? solutionNode, Solution solution)>();
            
            foreach (var path in solutionPathsList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                ReportProgress(progress, GraphBuildPhase.LoadingSolution, path, 0, 0, 0);
                
                if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    // Load single project
                    var project = await _workspace.OpenProjectAsync(path, cancellationToken: cancellationToken);
                    totalProjects++;
                    
                    // Create a virtual solution node for the project
                    var virtualSolutionNode = new SolutionNode(
                        Id: $"proj:{path}",
                        Path: path,
                        Name: Path.GetFileNameWithoutExtension(path));
                    graph.AddSolution(virtualSolutionNode);
                    
                    solutionsAndProjects.Add((virtualSolutionNode, project.Solution));
                }
                else
                {
                    // Load solution
                    var solution = await _workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken);
                    totalProjects += solution.Projects.Count();

                    // Add solution node
                    var solutionNode = new SolutionNode(
                        Id: $"sln:{path}",
                        Path: path,
                        Name: Path.GetFileNameWithoutExtension(path));
                    graph.AddSolution(solutionNode);
                    
                    solutionsAndProjects.Add((solutionNode, solution));
                }
            }

            // Second pass: process projects
            foreach (var (solutionNode, solution) in solutionsAndProjects)
            {
                foreach (var project in solution.Projects)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    processedProjects++;
                    
                    var progressPercent = (int)((double)processedProjects / totalProjects * 100);
                    ReportProgress(progress, GraphBuildPhase.AnalyzingProjects, project.Name, progressPercent, processedProjects, totalProjects);

                    await ProcessProjectAsync(graph, solutionNode!, project, options, progress, cancellationToken);
                }
            }

            // Build type usage edges if requested
            if (options.AnalyzeTypeUsages)
            {
                await BuildTypeUsageEdgesAsync(graph, options, progress, cancellationToken);
            }

            ReportProgress(progress, GraphBuildPhase.Completed, "Done", 100, totalProjects, totalProjects);
            
            var stats = graph.GetStatistics();
            _logger.LogInformation("Graph built: {Stats}", stats);

            return graph;
        }
        finally
        {
            _workspace?.Dispose();
            _workspace = null;
        }
    }

    private async Task ProcessProjectAsync(
        SolutionGraph graph,
        SolutionNode solutionNode,
        Project project,
        GraphBuildOptions options,
        IProgress<GraphBuildProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (project.FilePath == null) return;

        // Determine project type
        var projectType = DetermineProjectType(project);

        // Add project node
        var projectNode = new ProjectNode(
            Id: $"proj:{project.FilePath}",
            Path: project.FilePath,
            Name: project.Name,
            RootNamespace: project.DefaultNamespace,
            TargetFramework: null, // Would need to parse .csproj for this
            ProjectType: projectType);
        
        graph.AddProject(projectNode);
        graph.AddEdge(new SolutionContainsProjectEdge(solutionNode.Id, projectNode.Id));

        // Add project references
        foreach (var projectRef in project.ProjectReferences)
        {
            var refProject = project.Solution.GetProject(projectRef.ProjectId);
            if (refProject?.FilePath != null)
            {
                var refProjectId = $"proj:{refProject.FilePath}";
                graph.AddEdge(new ProjectReferenceEdge(projectNode.Id, refProjectId));
            }
        }

        // Add package references
        foreach (var metadataRef in project.MetadataReferences)
        {
            if (metadataRef.Display != null && metadataRef.Display.Contains("nuget", StringComparison.OrdinalIgnoreCase))
            {
                var packageName = ExtractPackageName(metadataRef.Display);
                if (packageName != null)
                {
                    var packageId = $"pkg:{packageName}";
                    if (!graph.Packages.ContainsKey(packageId))
                    {
                        graph.AddPackage(new PackageNode(packageId, packageName, null));
                    }
                    graph.AddEdge(new PackageReferenceEdge(projectNode.Id, packageId));
                }
            }
        }

        // Process documents
        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null) return;

        foreach (var document in project.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (document.FilePath == null) continue;
            if (ShouldExcludeFile(document.FilePath, options)) continue;

            await ProcessDocumentAsync(graph, projectNode, document, compilation, options, cancellationToken);
        }
    }

    private async Task ProcessDocumentAsync(
        SolutionGraph graph,
        ProjectNode projectNode,
        Document document,
        Compilation compilation,
        GraphBuildOptions options,
        CancellationToken cancellationToken)
    {
        if (document.FilePath == null) return;

        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
        if (syntaxTree == null) return;

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync(cancellationToken);

        // Determine file type
        var fileType = DetermineFileType(document.FilePath);

        // Detect namespace from file
        var namespaceDecl = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var fileNamespace = namespaceDecl?.Name.ToString();

        // Add file node
        var fileNode = new FileNode(
            Id: $"file:{document.FilePath}",
            Path: document.FilePath,
            Namespace: fileNamespace,
            FileType: fileType);
        
        graph.AddFile(fileNode);
        graph.AddEdge(new ProjectContainsFileEdge(projectNode.Id, fileNode.Id));

        // Process using directives
        if (options.AnalyzeUsingDirectives)
        {
            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            foreach (var usingDirective in usingDirectives)
            {
                var nsName = usingDirective.Name?.ToString();
                if (nsName == null) continue;

                var nsId = $"ns:{nsName}";
                if (!graph.Namespaces.ContainsKey(nsId))
                {
                    graph.AddNamespace(new NamespaceNode(nsId, nsName));
                }

                var lineNumber = usingDirective.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                graph.AddEdge(new FileUsesNamespaceEdge(fileNode.Id, nsId, lineNumber));
            }
        }

        // Process type declarations (classes, interfaces, records, structs)
        var typeDeclarations = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .ToList();

        foreach (var typeDecl in typeDeclarations)
        {
            var symbol = semanticModel.GetDeclaredSymbol(typeDecl);
            if (symbol == null) continue;

            // Skip private types if not requested
            if (!options.IncludePrivateTypes && symbol.DeclaredAccessibility == Accessibility.Private)
                continue;

            var typeKind = typeDecl switch
            {
                ClassDeclarationSyntax => TypeNodeKind.Class,
                InterfaceDeclarationSyntax => TypeNodeKind.Interface,
                RecordDeclarationSyntax => TypeNodeKind.Record,
                StructDeclarationSyntax => TypeNodeKind.Struct,
                _ => TypeNodeKind.Class
            };

            var typeNode = new TypeNode(
                Id: $"type:{symbol.ToDisplayString()}",
                FullName: symbol.ToDisplayString(),
                Namespace: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                Name: symbol.Name,
                Kind: typeKind,
                FileId: fileNode.Id,
                IsPublic: symbol.DeclaredAccessibility == Accessibility.Public,
                IsPartial: typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                IsStatic: typeDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
                IsAbstract: typeDecl.Modifiers.Any(SyntaxKind.AbstractKeyword));

            graph.AddType(typeNode);
            graph.AddEdge(new FileContainsTypeEdge(fileNode.Id, typeNode.Id));

            // Add namespace edge
            if (!string.IsNullOrEmpty(typeNode.Namespace))
            {
                var nsId = $"ns:{typeNode.Namespace}";
                if (!graph.Namespaces.ContainsKey(nsId))
                {
                    graph.AddNamespace(new NamespaceNode(nsId, typeNode.Namespace));
                }
                graph.AddEdge(new TypeInNamespaceEdge(typeNode.Id, nsId));
            }

            // Process base types and interfaces
            if (symbol.BaseType != null && symbol.BaseType.SpecialType != SpecialType.System_Object)
            {
                var baseTypeId = $"type:{symbol.BaseType.ToDisplayString()}";
                graph.AddEdge(new TypeInheritsEdge(typeNode.Id, baseTypeId));
            }

            foreach (var iface in symbol.Interfaces)
            {
                var ifaceId = $"type:{iface.ToDisplayString()}";
                graph.AddEdge(new TypeImplementsEdge(typeNode.Id, ifaceId));
            }
        }

        // Process enum declarations separately (EnumDeclarationSyntax is not TypeDeclarationSyntax)
        var enumDeclarations = root.DescendantNodes()
            .OfType<EnumDeclarationSyntax>()
            .ToList();

        foreach (var enumDecl in enumDeclarations)
        {
            var symbol = semanticModel.GetDeclaredSymbol(enumDecl);
            if (symbol == null) continue;

            // Skip private types if not requested
            if (!options.IncludePrivateTypes && symbol.DeclaredAccessibility == Accessibility.Private)
                continue;

            var typeNode = new TypeNode(
                Id: $"type:{symbol.ToDisplayString()}",
                FullName: symbol.ToDisplayString(),
                Namespace: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                Name: symbol.Name,
                Kind: TypeNodeKind.Enum,
                FileId: fileNode.Id,
                IsPublic: symbol.DeclaredAccessibility == Accessibility.Public,
                IsPartial: false,
                IsStatic: false,
                IsAbstract: false);

            graph.AddType(typeNode);
            graph.AddEdge(new FileContainsTypeEdge(fileNode.Id, typeNode.Id));

            // Add namespace edge
            if (!string.IsNullOrEmpty(typeNode.Namespace))
            {
                var nsId = $"ns:{typeNode.Namespace}";
                if (!graph.Namespaces.ContainsKey(nsId))
                {
                    graph.AddNamespace(new NamespaceNode(nsId, typeNode.Namespace));
                }
                graph.AddEdge(new TypeInNamespaceEdge(typeNode.Id, nsId));
            }
        }
    }

    private async Task BuildTypeUsageEdgesAsync(
        SolutionGraph graph,
        GraphBuildOptions options,
        IProgress<GraphBuildProgress>? progress,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Building type usage edges...");
        
        var types = graph.Types.Values.ToList();
        var totalTypes = types.Count;
        var processedTypes = 0;

        // For each type, find usages in the codebase
        // This is a simplified version - full implementation would use SymbolFinder
        foreach (var typeNode in types)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processedTypes++;

            if (processedTypes % 100 == 0)
            {
                var progressPercent = (int)((double)processedTypes / totalTypes * 100);
                ReportProgress(progress, GraphBuildPhase.AnalyzingUsages, typeNode.Name, progressPercent, processedTypes, totalTypes);
            }

            // Note: For full type usage analysis, we would need to:
            // 1. Get the INamedTypeSymbol for this type
            // 2. Use SymbolFinder.FindReferencesAsync to find all usages
            // 3. Create TypeUsageEdge for each usage
            // This is expensive for large codebases, so we skip it for now
            // and rely on the inheritance/implementation edges which we already have
        }
    }

    private static ProjectNodeType DetermineProjectType(Project project)
    {
        var name = project.Name.ToLowerInvariant();
        
        if (name.Contains("test") || name.Contains("spec"))
            return ProjectNodeType.Test;
        if (name.Contains("wpf") || name.Contains("desktop"))
            return ProjectNodeType.Wpf;
        if (name.Contains("web") || name.Contains("api"))
            return ProjectNodeType.WebApi;
        if (name.Contains("blazor"))
            return ProjectNodeType.Blazor;
        if (name.Contains("maui"))
            return ProjectNodeType.Maui;
        if (name.Contains("console"))
            return ProjectNodeType.Console;
        
        return ProjectNodeType.ClassLibrary;
    }

    private static FileNodeType DetermineFileType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".cs" => FileNodeType.CSharp,
            ".xaml" => FileNodeType.Xaml,
            ".razor" => FileNodeType.Razor,
            ".json" => FileNodeType.Json,
            ".xml" or ".csproj" or ".props" or ".targets" => FileNodeType.Xml,
            _ => FileNodeType.Other
        };
    }

    private static bool ShouldExcludeFile(string filePath, GraphBuildOptions options)
    {
        var fileName = Path.GetFileName(filePath);
        
        // Exclude generated files
        if (!options.IncludeGeneratedFiles)
        {
            if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains(".Designer.", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check exclude patterns
        foreach (var pattern in options.ExcludePatterns)
        {
            if (MatchesGlobPattern(filePath, pattern))
                return true;
        }

        return false;
    }

    private static bool MatchesGlobPattern(string path, string pattern)
    {
        // Simple glob matching (supports ** and *)
        var normalizedPath = path.Replace('\\', '/');
        var normalizedPattern = pattern.Replace('\\', '/');

        if (normalizedPattern.StartsWith("**/"))
        {
            var suffix = normalizedPattern[3..];
            return normalizedPath.Contains(suffix, StringComparison.OrdinalIgnoreCase);
        }

        return normalizedPath.Contains(normalizedPattern.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractPackageName(string metadataDisplay)
    {
        // Extract package name from NuGet path like:
        // C:\Users\...\.nuget\packages\newtonsoft.json\13.0.3\lib\net6.0\Newtonsoft.Json.dll
        var parts = metadataDisplay.Split(new[] { "packages", "Packages" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;

        var packagePath = parts[1].TrimStart('\\', '/');
        var packageParts = packagePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        
        return packageParts.Length > 0 ? packageParts[0] : null;
    }

    private static void ReportProgress(
        IProgress<GraphBuildProgress>? progress,
        GraphBuildPhase phase,
        string currentItem,
        int progressPercent,
        int processedCount,
        int totalCount)
    {
        progress?.Report(new GraphBuildProgress
        {
            Phase = phase,
            CurrentItem = currentItem,
            ProgressPercent = progressPercent,
            ProcessedCount = processedCount,
            TotalCount = totalCount
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_workspace != null)
        {
            _workspace.Dispose();
            _workspace = null;
        }
        await Task.CompletedTask;
    }
}
