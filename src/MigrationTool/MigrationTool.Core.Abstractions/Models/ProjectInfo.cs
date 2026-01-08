namespace MigrationTool.Core.Abstractions.Models;

/// <summary>
/// Represents a .NET project.
/// </summary>
public record ProjectInfo
{
    /// <summary>
    /// Project name (without extension).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full path to the .csproj file.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Project GUID from solution.
    /// </summary>
    public Guid? ProjectGuid { get; init; }

    /// <summary>
    /// Directory containing the project.
    /// </summary>
    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

    /// <summary>
    /// Target framework (e.g., "net9.0", "net9.0-windows").
    /// </summary>
    public string? TargetFramework { get; init; }

    /// <summary>
    /// Multiple target frameworks if multi-targeting.
    /// </summary>
    public IReadOnlyList<string> TargetFrameworks { get; init; } = [];

    /// <summary>
    /// Project type.
    /// </summary>
    public ProjectType ProjectType { get; init; } = ProjectType.ClassLibrary;

    /// <summary>
    /// Root namespace.
    /// </summary>
    public string? RootNamespace { get; init; }

    /// <summary>
    /// Assembly name.
    /// </summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// Whether this is a test project.
    /// </summary>
    public bool IsTestProject { get; init; }

    /// <summary>
    /// Project references.
    /// </summary>
    public IReadOnlyList<ProjectReference> ProjectReferences { get; init; } = [];

    /// <summary>
    /// Package references.
    /// </summary>
    public IReadOnlyList<PackageReference> PackageReferences { get; init; } = [];

    /// <summary>
    /// Source files in the project.
    /// </summary>
    public IReadOnlyList<SourceFileInfo> SourceFiles { get; init; } = [];

    /// <summary>
    /// Total file count.
    /// </summary>
    public int FileCount => SourceFiles.Count;

    /// <summary>
    /// Total class count.
    /// </summary>
    public int ClassCount => SourceFiles.Sum(f => f.Classes.Count);

    /// <summary>
    /// Total test count.
    /// </summary>
    public int TestCount => SourceFiles.Sum(f => f.TestCount);
}

/// <summary>
/// Project type enumeration.
/// </summary>
public enum ProjectType
{
    ClassLibrary,
    Console,
    Wpf,
    WinForms,
    Web,
    WebApi,
    Blazor,
    Maui,
    Test,
    Other
}

/// <summary>
/// Reference to another project.
/// </summary>
public record ProjectReference(
    string Name,
    string Path
);

/// <summary>
/// Reference to a NuGet package.
/// </summary>
public record PackageReference(
    string Name,
    string? Version
);
