using System.Collections.Concurrent;

namespace MigrationTool.Core.Abstractions.Graph;

/// <summary>
/// Represents the complete dependency graph of one or more .NET solutions.
/// </summary>
public class SolutionGraph
{
    private readonly ConcurrentDictionary<string, SolutionNode> _solutions = new();
    private readonly ConcurrentDictionary<string, ProjectNode> _projects = new();
    private readonly ConcurrentDictionary<string, FileNode> _files = new();
    private readonly ConcurrentDictionary<string, TypeNode> _types = new();
    private readonly ConcurrentDictionary<string, PackageNode> _packages = new();
    private readonly ConcurrentDictionary<string, NamespaceNode> _namespaces = new();
    private readonly ConcurrentBag<GraphEdge> _edges = new();

    /// <summary>
    /// All solution nodes.
    /// </summary>
    public IReadOnlyDictionary<string, SolutionNode> Solutions => _solutions;

    /// <summary>
    /// All project nodes.
    /// </summary>
    public IReadOnlyDictionary<string, ProjectNode> Projects => _projects;

    /// <summary>
    /// All file nodes.
    /// </summary>
    public IReadOnlyDictionary<string, FileNode> Files => _files;

    /// <summary>
    /// All type nodes.
    /// </summary>
    public IReadOnlyDictionary<string, TypeNode> Types => _types;

    /// <summary>
    /// All package nodes.
    /// </summary>
    public IReadOnlyDictionary<string, PackageNode> Packages => _packages;

    /// <summary>
    /// All namespace nodes.
    /// </summary>
    public IReadOnlyDictionary<string, NamespaceNode> Namespaces => _namespaces;

    /// <summary>
    /// All edges in the graph.
    /// </summary>
    public IReadOnlyCollection<GraphEdge> Edges => _edges;

    /// <summary>
    /// Total node count.
    /// </summary>
    public int NodeCount => 
        _solutions.Count + _projects.Count + _files.Count + 
        _types.Count + _packages.Count + _namespaces.Count;

    /// <summary>
    /// Total edge count.
    /// </summary>
    public int EdgeCount => _edges.Count;

    #region Add Nodes

    public void AddSolution(SolutionNode node) => _solutions.TryAdd(node.Id, node);
    public void AddProject(ProjectNode node) => _projects.TryAdd(node.Id, node);
    public void AddFile(FileNode node) => _files.TryAdd(node.Id, node);
    public void AddType(TypeNode node) => _types.TryAdd(node.Id, node);
    public void AddPackage(PackageNode node) => _packages.TryAdd(node.Id, node);
    public void AddNamespace(NamespaceNode node) => _namespaces.TryAdd(node.Id, node);
    public void AddEdge(GraphEdge edge) => _edges.Add(edge);

    #endregion

    #region Query - Get Nodes

    /// <summary>
    /// Get all nodes of a specific type.
    /// </summary>
    public IEnumerable<T> GetNodes<T>() where T : GraphNode
    {
        return typeof(T).Name switch
        {
            nameof(SolutionNode) => _solutions.Values.Cast<T>(),
            nameof(ProjectNode) => _projects.Values.Cast<T>(),
            nameof(FileNode) => _files.Values.Cast<T>(),
            nameof(TypeNode) => _types.Values.Cast<T>(),
            nameof(PackageNode) => _packages.Values.Cast<T>(),
            nameof(NamespaceNode) => _namespaces.Values.Cast<T>(),
            _ => Enumerable.Empty<T>()
        };
    }

    /// <summary>
    /// Get a node by ID.
    /// </summary>
    public GraphNode? GetNode(string id)
    {
        if (_solutions.TryGetValue(id, out var solution)) return solution;
        if (_projects.TryGetValue(id, out var project)) return project;
        if (_files.TryGetValue(id, out var file)) return file;
        if (_types.TryGetValue(id, out var type)) return type;
        if (_packages.TryGetValue(id, out var package)) return package;
        if (_namespaces.TryGetValue(id, out var ns)) return ns;
        return null;
    }

    #endregion

    #region Query - Relationships

    /// <summary>
    /// Get all edges originating from a node.
    /// </summary>
    public IEnumerable<GraphEdge> GetOutgoingEdges(string nodeId)
    {
        return _edges.Where(e => e.SourceId == nodeId);
    }

    /// <summary>
    /// Get all edges pointing to a node.
    /// </summary>
    public IEnumerable<GraphEdge> GetIncomingEdges(string nodeId)
    {
        return _edges.Where(e => e.TargetId == nodeId);
    }

    /// <summary>
    /// Get all nodes that the given node depends on.
    /// </summary>
    public IEnumerable<GraphNode> GetDependencies(string nodeId)
    {
        return GetOutgoingEdges(nodeId)
            .Select(e => GetNode(e.TargetId))
            .Where(n => n != null)!;
    }

    /// <summary>
    /// Get all nodes that depend on the given node.
    /// </summary>
    public IEnumerable<GraphNode> GetDependents(string nodeId)
    {
        return GetIncomingEdges(nodeId)
            .Select(e => GetNode(e.SourceId))
            .Where(n => n != null)!;
    }

    #endregion

    #region Query - Types

    /// <summary>
    /// Get all types in a namespace.
    /// </summary>
    public IEnumerable<TypeNode> GetTypesInNamespace(string @namespace)
    {
        return _types.Values.Where(t => t.Namespace == @namespace);
    }

    /// <summary>
    /// Get all types that reference the given type.
    /// </summary>
    public IEnumerable<TypeNode> GetTypesReferencing(string typeId)
    {
        return GetIncomingEdges(typeId)
            .Where(e => e is TypeUsageEdge or TypeInheritsEdge or TypeImplementsEdge)
            .Select(e => _types.GetValueOrDefault(e.SourceId))
            .Where(t => t != null)!;
    }

    /// <summary>
    /// Get all types that the given type references.
    /// </summary>
    public IEnumerable<TypeNode> GetTypesReferencedBy(string typeId)
    {
        return GetOutgoingEdges(typeId)
            .Where(e => e is TypeUsageEdge or TypeInheritsEdge or TypeImplementsEdge)
            .Select(e => _types.GetValueOrDefault(e.TargetId))
            .Where(t => t != null)!;
    }

    /// <summary>
    /// Find a type by full name.
    /// </summary>
    public TypeNode? FindType(string fullName)
    {
        return _types.Values.FirstOrDefault(t => t.FullName == fullName);
    }

    #endregion

    #region Query - Files

    /// <summary>
    /// Get all files that contain references to a type.
    /// </summary>
    public IEnumerable<FileNode> GetFilesReferencingType(string typeId)
    {
        // Get the file containing the referencing types
        var referencingTypes = GetTypesReferencing(typeId);
        var fileIds = referencingTypes.Select(t => t.FileId).Distinct();
        
        return fileIds
            .Select(fid => _files.GetValueOrDefault(fid))
            .Where(f => f != null)!;
    }

    /// <summary>
    /// Get all files that use a namespace (via using directive).
    /// </summary>
    public IEnumerable<FileNode> GetFilesUsingNamespace(string @namespace)
    {
        var nsNode = _namespaces.Values.FirstOrDefault(n => n.Namespace == @namespace);
        if (nsNode == null) return Enumerable.Empty<FileNode>();

        return GetIncomingEdges(nsNode.Id)
            .OfType<FileUsesNamespaceEdge>()
            .Select(e => _files.GetValueOrDefault(e.SourceId))
            .Where(f => f != null)!;
    }

    /// <summary>
    /// Get the file containing a type.
    /// </summary>
    public FileNode? GetFileContainingType(string typeId)
    {
        var type = _types.GetValueOrDefault(typeId);
        if (type == null) return null;
        return _files.GetValueOrDefault(type.FileId);
    }

    #endregion

    #region Query - Projects

    /// <summary>
    /// Get all projects that depend on the given project.
    /// </summary>
    public IEnumerable<ProjectNode> GetProjectsDependingOn(string projectId)
    {
        return GetIncomingEdges(projectId)
            .OfType<ProjectReferenceEdge>()
            .Select(e => _projects.GetValueOrDefault(e.SourceId))
            .Where(p => p != null)!;
    }

    /// <summary>
    /// Get all projects that the given project depends on.
    /// </summary>
    public IEnumerable<ProjectNode> GetProjectDependencies(string projectId)
    {
        return GetOutgoingEdges(projectId)
            .OfType<ProjectReferenceEdge>()
            .Select(e => _projects.GetValueOrDefault(e.TargetId))
            .Where(p => p != null)!;
    }

    /// <summary>
    /// Get the project containing a file.
    /// </summary>
    public ProjectNode? GetProjectContainingFile(string fileId)
    {
        var edge = _edges
            .OfType<ProjectContainsFileEdge>()
            .FirstOrDefault(e => e.TargetId == fileId);
        
        if (edge == null) return null;
        return _projects.GetValueOrDefault(edge.SourceId);
    }

    /// <summary>
    /// Check if there's a cyclic dependency involving the given project.
    /// </summary>
    public bool HasCyclicDependency(string projectId)
    {
        var visited = new HashSet<string>();
        var stack = new HashSet<string>();
        return HasCycle(projectId, visited, stack);
    }

    private bool HasCycle(string nodeId, HashSet<string> visited, HashSet<string> stack)
    {
        if (stack.Contains(nodeId)) return true;
        if (visited.Contains(nodeId)) return false;

        visited.Add(nodeId);
        stack.Add(nodeId);

        foreach (var dep in GetProjectDependencies(nodeId))
        {
            if (HasCycle(dep.Id, visited, stack))
                return true;
        }

        stack.Remove(nodeId);
        return false;
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get graph statistics.
    /// </summary>
    public GraphStatistics GetStatistics()
    {
        return new GraphStatistics
        {
            SolutionCount = _solutions.Count,
            ProjectCount = _projects.Count,
            FileCount = _files.Count,
            TypeCount = _types.Count,
            PackageCount = _packages.Count,
            NamespaceCount = _namespaces.Count,
            EdgeCount = _edges.Count,
            ProjectReferenceCount = _edges.Count(e => e is ProjectReferenceEdge),
            PackageReferenceCount = _edges.Count(e => e is PackageReferenceEdge),
            TypeUsageCount = _edges.Count(e => e is TypeUsageEdge),
            InheritanceCount = _edges.Count(e => e is TypeInheritsEdge or TypeImplementsEdge)
        };
    }

    #endregion
}

/// <summary>
/// Statistics about the solution graph.
/// </summary>
public record GraphStatistics
{
    public int SolutionCount { get; init; }
    public int ProjectCount { get; init; }
    public int FileCount { get; init; }
    public int TypeCount { get; init; }
    public int PackageCount { get; init; }
    public int NamespaceCount { get; init; }
    public int EdgeCount { get; init; }
    public int ProjectReferenceCount { get; init; }
    public int PackageReferenceCount { get; init; }
    public int TypeUsageCount { get; init; }
    public int InheritanceCount { get; init; }

    public override string ToString() => $"""
        Graph Statistics:
          Solutions: {SolutionCount}
          Projects: {ProjectCount}
          Files: {FileCount}
          Types: {TypeCount}
          Packages: {PackageCount}
          Namespaces: {NamespaceCount}
          Edges: {EdgeCount}
            - Project References: {ProjectReferenceCount}
            - Package References: {PackageReferenceCount}
            - Type Usages: {TypeUsageCount}
            - Inheritance: {InheritanceCount}
        """;
}
