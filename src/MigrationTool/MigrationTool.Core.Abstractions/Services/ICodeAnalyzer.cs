using MigrationTool.Core.Abstractions.Models;

namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Analyzes C# source code files using Roslyn.
/// </summary>
public interface ICodeAnalyzer
{
    /// <summary>
    /// Analyzes a single C# file.
    /// </summary>
    Task<SourceFileInfo> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes C# content (without file path).
    /// </summary>
    Task<SourceFileInfo> AnalyzeContentAsync(string content, string fileName = "temp.cs", CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a directory for C# files and analyzes them.
    /// </summary>
    Task<IEnumerable<SourceFileInfo>> ScanDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default);
}
