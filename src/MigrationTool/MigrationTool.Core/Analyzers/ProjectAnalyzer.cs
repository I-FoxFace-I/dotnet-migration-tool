using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Core.Analyzers;

/// <summary>
/// Analyzes .NET project files (.csproj).
/// </summary>
public class ProjectAnalyzer : IProjectAnalyzer
{
    private readonly IFileSystemService _fileSystem;
    private readonly ICodeAnalyzer _codeAnalyzer;
    private readonly ILogger<ProjectAnalyzer> _logger;

    // Test package names that indicate a test project
    private static readonly HashSet<string> TestPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.NET.Test.Sdk",
        "xunit",
        "xunit.core",
        "NUnit",
        "NUnit3TestAdapter",
        "MSTest.TestFramework",
        "MSTest.TestAdapter"
    };

    public ProjectAnalyzer(
        IFileSystemService fileSystem,
        ICodeAnalyzer codeAnalyzer,
        ILogger<ProjectAnalyzer> logger)
    {
        _fileSystem = fileSystem;
        _codeAnalyzer = codeAnalyzer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProjectInfo> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing project: {ProjectPath}", projectPath);

        if (!await _fileSystem.ExistsAsync(projectPath, cancellationToken))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}");
        }

        var content = await _fileSystem.ReadFileAsync(projectPath, cancellationToken);
        var projectName = Path.GetFileNameWithoutExtension(projectPath);

        try
        {
            var doc = XDocument.Parse(content);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Extract properties
            var propertyGroups = doc.Descendants(ns + "PropertyGroup");
            var targetFramework = GetPropertyValue(propertyGroups, ns, "TargetFramework");
            var targetFrameworks = GetPropertyValue(propertyGroups, ns, "TargetFrameworks")?.Split(';') ?? [];
            var rootNamespace = GetPropertyValue(propertyGroups, ns, "RootNamespace") ?? projectName;
            var assemblyName = GetPropertyValue(propertyGroups, ns, "AssemblyName") ?? projectName;
            var outputType = GetPropertyValue(propertyGroups, ns, "OutputType");
            var useWpf = GetPropertyValue(propertyGroups, ns, "UseWPF");
            var useWinForms = GetPropertyValue(propertyGroups, ns, "UseWindowsForms");
            var useMaui = GetPropertyValue(propertyGroups, ns, "UseMaui");
            var isTestProject = GetPropertyValue(propertyGroups, ns, "IsTestProject");

            // Extract references
            var projectRefs = doc.Descendants(ns + "ProjectReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => new ProjectReference(
                    Path.GetFileNameWithoutExtension(v!),
                    v!
                ))
                .ToList();

            var packageRefs = doc.Descendants(ns + "PackageReference")
                .Select(e => new PackageReference(
                    e.Attribute("Include")?.Value ?? "",
                    e.Attribute("Version")?.Value ?? e.Element(ns + "Version")?.Value
                ))
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .ToList();

            // Determine project type
            var projectType = DetermineProjectType(outputType, useWpf, useWinForms, useMaui, targetFramework);

            // Determine if test project
            var isTest = isTestProject?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
                || packageRefs.Any(p => TestPackages.Contains(p.Name))
                || projectName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
                || projectName.EndsWith(".Test", StringComparison.OrdinalIgnoreCase);

            return new ProjectInfo
            {
                Name = projectName,
                Path = projectPath,
                TargetFramework = targetFramework,
                TargetFrameworks = targetFrameworks,
                RootNamespace = rootNamespace,
                AssemblyName = assemblyName,
                ProjectType = isTest ? ProjectType.Test : projectType,
                IsTestProject = isTest,
                ProjectReferences = projectRefs,
                PackageReferences = packageRefs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse project file: {ProjectPath}", projectPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProjectInfo> EnrichProjectAsync(ProjectInfo project, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enriching project: {ProjectName}", project.Name);

        var projectDir = project.Directory;

        if (!await _fileSystem.ExistsAsync(projectDir, cancellationToken))
        {
            return project;
        }

        try
        {
            var sourceFiles = await _codeAnalyzer.ScanDirectoryAsync(projectDir, recursive: true, cancellationToken);

            return project with
            {
                SourceFiles = sourceFiles.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan source files for project: {ProjectName}", project.Name);
            return project;
        }
    }

    private static string? GetPropertyValue(IEnumerable<XElement> propertyGroups, XNamespace ns, string propertyName)
    {
        return propertyGroups
            .SelectMany(pg => pg.Elements(ns + propertyName))
            .FirstOrDefault()?.Value;
    }

    private static ProjectType DetermineProjectType(string? outputType, string? useWpf, string? useWinForms, string? useMaui, string? targetFramework)
    {
        if (useMaui?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            return ProjectType.Maui;

        if (useWpf?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            return ProjectType.Wpf;

        if (useWinForms?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            return ProjectType.WinForms;

        if (targetFramework?.Contains("aspnetcore", StringComparison.OrdinalIgnoreCase) == true)
            return ProjectType.Web;

        return outputType?.ToLowerInvariant() switch
        {
            "exe" => ProjectType.Console,
            "winexe" => ProjectType.Wpf,
            "library" => ProjectType.ClassLibrary,
            _ => ProjectType.ClassLibrary
        };
    }
}
