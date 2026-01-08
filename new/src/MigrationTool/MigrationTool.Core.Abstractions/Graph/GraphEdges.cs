namespace MigrationTool.Core.Abstractions.Graph;

/// <summary>
/// Base class for all graph edges.
/// </summary>
public abstract record GraphEdge(string SourceId, string TargetId)
{
    /// <summary>
    /// Description of the relationship.
    /// </summary>
    public abstract string Description { get; }
}

/// <summary>
/// Solution contains project.
/// </summary>
public record SolutionContainsProjectEdge(string SourceId, string TargetId) 
    : GraphEdge(SourceId, TargetId)
{
    public override string Description => "contains";
}

/// <summary>
/// Project references another project.
/// </summary>
public record ProjectReferenceEdge(string SourceId, string TargetId) 
    : GraphEdge(SourceId, TargetId)
{
    public override string Description => "references project";
}

/// <summary>
/// Project references a NuGet package.
/// </summary>
public record PackageReferenceEdge(string SourceId, string TargetId) 
    : GraphEdge(SourceId, TargetId)
{
    public override string Description => "references package";
}

/// <summary>
/// Project contains file.
/// </summary>
public record ProjectContainsFileEdge(string SourceId, string TargetId) 
    : GraphEdge(SourceId, TargetId)
{
    public override string Description => "contains";
}

/// <summary>
/// File contains type.
/// </summary>
public record FileContainsTypeEdge(string SourceId, string TargetId) 
    : GraphEdge(SourceId, TargetId)
{
    public override string Description => "defines";
}

/// <summary>
/// Type inherits from another type.
/// </summary>
public record TypeInheritsEdge(string SourceId, string TargetId) 
    : GraphEdge(SourceId, TargetId)
{
    public override string Description => "inherits";
}

/// <summary>
/// Type implements an interface.
/// </summary>
public record TypeImplementsEdge(string SourceId, string TargetId) 
    : GraphEdge(SourceId, TargetId)
{
    public override string Description => "implements";
}

/// <summary>
/// Type uses another type (field, property, method parameter, local variable, etc.).
/// </summary>
public record TypeUsageEdge(
    string SourceId, 
    string TargetId,
    TypeUsageKind UsageKind,
    string? MemberName = null,
    int? LineNumber = null
) : GraphEdge(SourceId, TargetId)
{
    public override string Description => UsageKind switch
    {
        TypeUsageKind.Field => $"has field of type",
        TypeUsageKind.Property => $"has property of type",
        TypeUsageKind.MethodParameter => $"has method parameter of type",
        TypeUsageKind.MethodReturn => $"returns",
        TypeUsageKind.LocalVariable => $"uses locally",
        TypeUsageKind.GenericArgument => $"uses as generic argument",
        TypeUsageKind.Attribute => $"has attribute",
        TypeUsageKind.BaseType => $"inherits from",
        TypeUsageKind.Interface => $"implements",
        _ => "uses"
    };
}

/// <summary>
/// How a type is used.
/// </summary>
public enum TypeUsageKind
{
    Field,
    Property,
    MethodParameter,
    MethodReturn,
    LocalVariable,
    GenericArgument,
    Attribute,
    BaseType,
    Interface,
    Other
}

/// <summary>
/// File has using directive for a namespace.
/// </summary>
public record FileUsesNamespaceEdge(
    string SourceId, 
    string TargetId,
    int LineNumber
) : GraphEdge(SourceId, TargetId)
{
    public override string Description => "uses namespace";
}

/// <summary>
/// Type belongs to namespace.
/// </summary>
public record TypeInNamespaceEdge(string SourceId, string TargetId) 
    : GraphEdge(SourceId, TargetId)
{
    public override string Description => "is in namespace";
}
