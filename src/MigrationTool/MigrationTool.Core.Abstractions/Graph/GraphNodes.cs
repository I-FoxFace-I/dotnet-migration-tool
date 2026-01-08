namespace MigrationTool.Core.Abstractions.Graph;

/// <summary>
/// Base class for all graph nodes.
/// </summary>
public abstract record GraphNode(string Id)
{
    /// <summary>
    /// Display name for UI/logging.
    /// </summary>
    public abstract string DisplayName { get; }
}

/// <summary>
/// Represents a solution in the graph.
/// </summary>
public record SolutionNode(
    string Id,
    string Path,
    string Name
) : GraphNode(Id)
{
    public override string DisplayName => Name;
}

/// <summary>
/// Represents a project in the graph.
/// </summary>
public record ProjectNode(
    string Id,
    string Path,
    string Name,
    string? RootNamespace,
    string? TargetFramework,
    ProjectNodeType ProjectType
) : GraphNode(Id)
{
    public override string DisplayName => Name;
    
    /// <summary>
    /// Directory containing the project.
    /// </summary>
    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
}

/// <summary>
/// Project type for graph nodes.
/// </summary>
public enum ProjectNodeType
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
/// Represents a source file in the graph.
/// </summary>
public record FileNode(
    string Id,
    string Path,
    string? Namespace,
    FileNodeType FileType
) : GraphNode(Id)
{
    public override string DisplayName => System.IO.Path.GetFileName(Path);
    
    /// <summary>
    /// File name without path.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(Path);
    
    /// <summary>
    /// Directory containing the file.
    /// </summary>
    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
}

/// <summary>
/// File type for graph nodes.
/// </summary>
public enum FileNodeType
{
    CSharp,
    Xaml,
    Razor,
    Json,
    Xml,
    Other
}

/// <summary>
/// Represents a type (class, interface, struct, record, enum) in the graph.
/// </summary>
public record TypeNode(
    string Id,
    string FullName,
    string Namespace,
    string Name,
    TypeNodeKind Kind,
    string FileId,
    bool IsPublic = true,
    bool IsPartial = false,
    bool IsStatic = false,
    bool IsAbstract = false
) : GraphNode(Id)
{
    public override string DisplayName => Name;
}

/// <summary>
/// Type kind for graph nodes.
/// </summary>
public enum TypeNodeKind
{
    Class,
    Interface,
    Record,
    Struct,
    Enum,
    Delegate
}

/// <summary>
/// Represents a NuGet package in the graph.
/// </summary>
public record PackageNode(
    string Id,
    string PackageId,
    string? Version
) : GraphNode(Id)
{
    public override string DisplayName => Version != null ? $"{PackageId} ({Version})" : PackageId;
}

/// <summary>
/// Represents a namespace in the graph (virtual node for grouping).
/// </summary>
public record NamespaceNode(
    string Id,
    string Namespace
) : GraphNode(Id)
{
    public override string DisplayName => Namespace;
}
