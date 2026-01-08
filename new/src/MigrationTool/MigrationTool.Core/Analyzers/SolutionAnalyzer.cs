using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Core.Analyzers;

/// <summary>
/// Analyzes .NET solution files.
/// </summary>
public partial class SolutionAnalyzer : ISolutionAnalyzer
{
    private readonly IFileSystemService _fileSystem;
    private readonly IProjectAnalyzer _projectAnalyzer;
    private readonly ILogger<SolutionAnalyzer> _logger;

    // Regex patterns for parsing .sln files
    [GeneratedRegex(@"Project\(""\{([^}]+)\}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""\{([^}]+)\}""", RegexOptions.Compiled)]
    private static partial Regex ProjectLineRegex();

    [GeneratedRegex(@"\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}", RegexOptions.IgnoreCase)]
    private static partial Regex SolutionFolderGuidRegex();

    public SolutionAnalyzer(
        IFileSystemService fileSystem,
        IProjectAnalyzer projectAnalyzer,
        ILogger<SolutionAnalyzer> logger)
    {
        _fileSystem = fileSystem;
        _projectAnalyzer = projectAnalyzer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SolutionInfo> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing solution: {SolutionPath}", solutionPath);

        if (!await _fileSystem.ExistsAsync(solutionPath, cancellationToken))
        {
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");
        }

        var content = await _fileSystem.ReadFileAsync(solutionPath, cancellationToken);
        var solutionDir = Path.GetDirectoryName(solutionPath) ?? string.Empty;
        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);

        var projects = new List<ProjectInfo>();
        var folders = new List<SolutionFolder>();

        // Parse project lines
        var matches = ProjectLineRegex().Matches(content);

        foreach (Match match in matches)
        {
            var typeGuid = match.Groups[1].Value;
            var projectName = match.Groups[2].Value;
            var projectPath = match.Groups[3].Value;
            var projectGuid = match.Groups[4].Value;

            // Skip solution folders
            if (SolutionFolderGuidRegex().IsMatch(typeGuid))
            {
                folders.Add(new SolutionFolder(projectName, projectPath, []));
                continue;
            }

            // Skip non-.csproj files
            if (!projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fullProjectPath = Path.GetFullPath(Path.Combine(solutionDir, projectPath));

            try
            {
                if (await _fileSystem.ExistsAsync(fullProjectPath, cancellationToken))
                {
                    var projectInfo = await _projectAnalyzer.AnalyzeProjectAsync(fullProjectPath, cancellationToken);
                    projects.Add(projectInfo with { ProjectGuid = Guid.TryParse(projectGuid, out var guid) ? guid : null });
                }
                else
                {
                    _logger.LogWarning("Project file not found: {ProjectPath}", fullProjectPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze project: {ProjectPath}", fullProjectPath);
            }
        }

        _logger.LogInformation("Parsed {ProjectCount} projects from {SolutionName}", projects.Count, solutionName);

        return new SolutionInfo
        {
            Name = solutionName,
            Path = solutionPath,
            Projects = projects,
            Folders = folders
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> FindSolutionsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching for solutions in: {DirectoryPath}", directoryPath);

        if (!await _fileSystem.ExistsAsync(directoryPath, cancellationToken))
        {
            return [];
        }

        var solutions = await _fileSystem.GetFilesAsync(directoryPath, "*.sln", recursive: false, cancellationToken);
        var subDirSolutions = await _fileSystem.GetFilesAsync(directoryPath, "*.sln", recursive: true, cancellationToken);

        // Combine and deduplicate, limiting depth
        var allSolutions = solutions
            .Concat(subDirSolutions.Where(s => GetDepth(s, directoryPath) <= 2))
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        _logger.LogInformation("Found {Count} solutions", allSolutions.Count);

        return allSolutions;
    }

    private static int GetDepth(string path, string basePath)
    {
        var relativePath = Path.GetRelativePath(basePath, path);
        return relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length - 1;
    }
}
