using System.CommandLine;
using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace MigrationTool.Cli;

/// <summary>
/// CLI tool for .NET-specific migration operations using Roslyn.
/// Called from Python MigrationTool for advanced refactoring.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Register MSBuild
        MSBuildLocator.RegisterDefaults();

        var rootCommand = new RootCommand("MigrationTool CLI - .NET helper for Python MigrationTool");

        // analyze-solution command
        var analyzeSolutionCommand = new Command("analyze-solution", "Analyze a .NET solution");
        var solutionPathOption = new Option<string>("--path", "Path to .sln file") { IsRequired = true };
        analyzeSolutionCommand.AddOption(solutionPathOption);
        analyzeSolutionCommand.SetHandler(AnalyzeSolutionAsync, solutionPathOption);
        rootCommand.AddCommand(analyzeSolutionCommand);

        // update-namespace command
        var updateNamespaceCommand = new Command("update-namespace", "Update namespace in C# files");
        var filePathOption = new Option<string>("--file", "Path to C# file") { IsRequired = true };
        var oldNamespaceOption = new Option<string>("--old", "Old namespace") { IsRequired = true };
        var newNamespaceOption = new Option<string>("--new", "New namespace") { IsRequired = true };
        updateNamespaceCommand.AddOption(filePathOption);
        updateNamespaceCommand.AddOption(oldNamespaceOption);
        updateNamespaceCommand.AddOption(newNamespaceOption);
        updateNamespaceCommand.SetHandler(UpdateNamespaceAsync, filePathOption, oldNamespaceOption, newNamespaceOption);
        rootCommand.AddCommand(updateNamespaceCommand);

        // update-project-references command
        var updateRefsCommand = new Command("update-project-refs", "Update project references in .csproj");
        var projectPathOption = new Option<string>("--project", "Path to .csproj file") { IsRequired = true };
        var oldRefOption = new Option<string>("--old-ref", "Old reference path") { IsRequired = true };
        var newRefOption = new Option<string>("--new-ref", "New reference path") { IsRequired = true };
        updateRefsCommand.AddOption(projectPathOption);
        updateRefsCommand.AddOption(oldRefOption);
        updateRefsCommand.AddOption(newRefOption);
        updateRefsCommand.SetHandler(UpdateProjectReferencesAsync, projectPathOption, oldRefOption, newRefOption);
        rootCommand.AddCommand(updateRefsCommand);

        // find-usages command
        var findUsagesCommand = new Command("find-usages", "Find all usages of a symbol");
        var symbolOption = new Option<string>("--symbol", "Symbol name to find") { IsRequired = true };
        findUsagesCommand.AddOption(solutionPathOption);
        findUsagesCommand.AddOption(symbolOption);
        findUsagesCommand.SetHandler(FindUsagesAsync, solutionPathOption, symbolOption);
        rootCommand.AddCommand(findUsagesCommand);

        // move-folder command
        var moveFolderCommand = new Command("move-folder", "Move a folder and update all references");
        var sourceOption = new Option<string>("--source", "Source folder path") { IsRequired = true };
        var targetOption = new Option<string>("--target", "Target folder path") { IsRequired = true };
        var dryRunOption = new Option<bool>("--dry-run", "Preview changes without applying");
        moveFolderCommand.AddOption(sourceOption);
        moveFolderCommand.AddOption(targetOption);
        moveFolderCommand.AddOption(dryRunOption);
        moveFolderCommand.SetHandler(MoveFolderAsync, sourceOption, targetOption, dryRunOption);
        rootCommand.AddCommand(moveFolderCommand);

        // update-solution command
        var updateSolutionCommand = new Command("update-solution", "Update project path in solution file");
        var slnPathOption = new Option<string>("--solution", "Path to .sln file") { IsRequired = true };
        var oldPathOption = new Option<string>("--old-path", "Old project path") { IsRequired = true };
        var newPathOption = new Option<string>("--new-path", "New project path") { IsRequired = true };
        updateSolutionCommand.AddOption(slnPathOption);
        updateSolutionCommand.AddOption(oldPathOption);
        updateSolutionCommand.AddOption(newPathOption);
        updateSolutionCommand.SetHandler(UpdateSolutionAsync, slnPathOption, oldPathOption, newPathOption);
        rootCommand.AddCommand(updateSolutionCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task AnalyzeSolutionAsync(string solutionPath)
    {
        try
        {
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(solutionPath);

            var result = new
            {
                Path = solutionPath,
                Projects = solution.Projects.Select(p => new
                {
                    p.Name,
                    Path = p.FilePath,
                    Documents = p.Documents.Count(),
                    References = p.ProjectReferences.Count()
                }).ToList()
            };

            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = ex.Message }));
            Environment.Exit(1);
        }
    }

    private static async Task UpdateNamespaceAsync(string filePath, string oldNamespace, string newNamespace)
    {
        try
        {
            var code = await File.ReadAllTextAsync(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            var rewriter = new NamespaceRewriter(oldNamespace, newNamespace);
            var newRoot = rewriter.Visit(root);

            if (rewriter.ChangesCount > 0)
            {
                await File.WriteAllTextAsync(filePath, newRoot.ToFullString());
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Success = true,
                    FilePath = filePath,
                    Changes = rewriter.ChangesCount
                }));
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Success = true,
                    FilePath = filePath,
                    Changes = 0,
                    Message = "No changes needed"
                }));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = ex.Message }));
            Environment.Exit(1);
        }
    }

    private static async Task UpdateProjectReferencesAsync(string projectPath, string oldRef, string newRef)
    {
        try
        {
            var content = await File.ReadAllTextAsync(projectPath);
            var updated = content.Replace(oldRef, newRef);

            if (content != updated)
            {
                await File.WriteAllTextAsync(projectPath, updated);
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Success = true,
                    ProjectPath = projectPath,
                    OldRef = oldRef,
                    NewRef = newRef
                }));
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Success = true,
                    ProjectPath = projectPath,
                    Message = "Reference not found"
                }));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = ex.Message }));
            Environment.Exit(1);
        }
    }

    private static async Task FindUsagesAsync(string solutionPath, string symbol)
    {
        try
        {
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(solutionPath);

            var usages = new List<object>();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var root = await document.GetSyntaxRootAsync();
                    if (root == null) continue;

                    var identifiers = root.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(i => i.Identifier.Text == symbol);

                    foreach (var id in identifiers)
                    {
                        var lineSpan = id.GetLocation().GetLineSpan();
                        usages.Add(new
                        {
                            File = document.FilePath,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1
                        });
                    }
                }
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Symbol = symbol,
                Count = usages.Count,
                Usages = usages
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = ex.Message }));
            Environment.Exit(1);
        }
    }

    private static async Task MoveFolderAsync(string source, string target, bool dryRun)
    {
        try
        {
            var sourceDir = new DirectoryInfo(source);
            var targetDir = new DirectoryInfo(target);

            if (!sourceDir.Exists)
            {
                Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = $"Source folder not found: {source}" }));
                Environment.Exit(1);
                return;
            }

            if (targetDir.Exists)
            {
                Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = $"Target folder already exists: {target}" }));
                Environment.Exit(1);
                return;
            }

            var movedFiles = new List<string>();
            var updatedNamespaces = new List<string>();

            // Detect namespace from source folder structure
            var oldNamespace = DetectNamespaceFromPath(sourceDir.FullName);
            var newNamespace = DetectNamespaceFromPath(targetDir.FullName);

            if (dryRun)
            {
                // Just list what would be moved
                foreach (var file in sourceDir.GetFiles("*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                    movedFiles.Add(relativePath);
                }
            }
            else
            {
                // Create target directory
                targetDir.Create();

                // Move all files and update namespaces in C# files
                foreach (var file in sourceDir.GetFiles("*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                    var targetPath = Path.Combine(targetDir.FullName, relativePath);
                    
                    var targetDirectory = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                    
                    // For C# files, update namespaces using Roslyn
                    if (file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) && 
                        oldNamespace != null && newNamespace != null)
                    {
                        var code = await File.ReadAllTextAsync(file.FullName);
                        var tree = CSharpSyntaxTree.ParseText(code);
                        var root = await tree.GetRootAsync();
                        
                        var rewriter = new NamespaceRewriter(oldNamespace, newNamespace);
                        var newRoot = rewriter.Visit(root);
                        
                        // Write to target path (with or without namespace changes)
                        await File.WriteAllTextAsync(targetPath, newRoot.ToFullString());
                        
                        if (rewriter.ChangesCount > 0)
                        {
                            updatedNamespaces.Add(relativePath);
                        }
                        
                        // Delete original file after successful write
                        file.Delete();
                    }
                    else
                    {
                        File.Move(file.FullName, targetPath);
                    }
                    
                    movedFiles.Add(relativePath);
                }

                // Remove empty source directory
                sourceDir.Delete(true);
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Success = true,
                Source = source,
                Target = target,
                DryRun = dryRun,
                FilesCount = movedFiles.Count,
                Files = movedFiles,
                OldNamespace = oldNamespace,
                NewNamespace = newNamespace,
                UpdatedNamespaces = updatedNamespaces
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = ex.Message }));
            Environment.Exit(1);
        }
    }

    private static string? DetectNamespaceFromPath(string path)
    {
        // Try to detect namespace from folder path
        // e.g., C:\repo\src\MyProject\Services -> MyProject.Services
        
        // For non-existent paths, we need to traverse up to find the project root
        // The path might not exist yet (target folder), so we look at parent directories
        var fullPath = Path.GetFullPath(path);
        
        // Find project root (folder containing .csproj) by traversing up
        var current = new DirectoryInfo(fullPath);
        
        // If the directory doesn't exist, go up until we find one that does
        while (current != null && !current.Exists)
        {
            current = current.Parent;
        }
        
        // Now traverse up from the existing directory to find .csproj
        while (current != null)
        {
            if (current.Exists && current.GetFiles("*.csproj").Any())
            {
                var projectName = current.Name;
                var relativePath = Path.GetRelativePath(current.FullName, fullPath);
                
                if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
                    return projectName;
                    
                var subPath = relativePath.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                return $"{projectName}.{subPath}";
            }
            current = current.Parent;
        }
        
        return null;
    }

    private static async Task UpdateSolutionAsync(string solutionPath, string oldPath, string newPath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(solutionPath);
            
            // Normalize paths for comparison
            var normalizedOld = oldPath.Replace("/", "\\");
            var normalizedNew = newPath.Replace("/", "\\");
            
            var updated = content.Replace(normalizedOld, normalizedNew);

            if (content != updated)
            {
                await File.WriteAllTextAsync(solutionPath, updated);
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Success = true,
                    SolutionPath = solutionPath,
                    OldPath = oldPath,
                    NewPath = newPath,
                    Updated = true
                }));
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Success = true,
                    SolutionPath = solutionPath,
                    Message = "Path not found in solution",
                    Updated = false
                }));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = ex.Message }));
            Environment.Exit(1);
        }
    }
}

/// <summary>
/// Roslyn syntax rewriter for namespace updates.
/// </summary>
public class NamespaceRewriter : CSharpSyntaxRewriter
{
    private readonly string _oldNamespace;
    private readonly string _newNamespace;

    public int ChangesCount { get; private set; }

    public NamespaceRewriter(string oldNamespace, string newNamespace)
    {
        _oldNamespace = oldNamespace;
        _newNamespace = newNamespace;
    }

    public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        if (node.Name.ToString() == _oldNamespace)
        {
            ChangesCount++;
            return node.WithName(SyntaxFactory.ParseName(_newNamespace));
        }
        return base.VisitFileScopedNamespaceDeclaration(node);
    }

    public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        if (node.Name.ToString() == _oldNamespace)
        {
            ChangesCount++;
            return node.WithName(SyntaxFactory.ParseName(_newNamespace));
        }
        return base.VisitNamespaceDeclaration(node);
    }

    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (node.Name?.ToString().StartsWith(_oldNamespace) == true)
        {
            var newName = node.Name.ToString().Replace(_oldNamespace, _newNamespace);
            ChangesCount++;
            return node.WithName(SyntaxFactory.ParseName(newName));
        }
        return base.VisitUsingDirective(node);
    }
}
