namespace MigrationTool.Core.Abstractions.Models;

/// <summary>
/// Represents a C# source file.
/// </summary>
public record SourceFileInfo
{
    /// <summary>
    /// File name with extension.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full path to the file.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Relative path from project root.
    /// </summary>
    public string? RelativePath { get; init; }

    /// <summary>
    /// Primary namespace declared in the file.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Using directives.
    /// </summary>
    public IReadOnlyList<string> Usings { get; init; } = [];

    /// <summary>
    /// Classes, interfaces, enums, etc. defined in the file.
    /// </summary>
    public IReadOnlyList<TypeInfo> Classes { get; init; } = [];

    /// <summary>
    /// Line count.
    /// </summary>
    public int LineCount { get; init; }

    /// <summary>
    /// Whether this is a test file.
    /// </summary>
    public bool IsTestFile { get; init; }

    /// <summary>
    /// Total test count in this file.
    /// </summary>
    public int TestCount => Classes.Sum(c => c.Tests.Count);
}

/// <summary>
/// Represents a type (class, interface, enum, struct, record).
/// </summary>
public record TypeInfo
{
    /// <summary>
    /// Type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full name including namespace.
    /// </summary>
    public string? FullName { get; init; }

    /// <summary>
    /// Namespace containing this type.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Kind of type.
    /// </summary>
    public TypeKind Kind { get; init; } = TypeKind.Class;

    /// <summary>
    /// Access modifier.
    /// </summary>
    public AccessModifier AccessModifier { get; init; } = AccessModifier.Internal;

    /// <summary>
    /// Base types and interfaces.
    /// </summary>
    public IReadOnlyList<string> BaseTypes { get; init; } = [];

    /// <summary>
    /// Methods in this type.
    /// </summary>
    public IReadOnlyList<MethodInfo> Methods { get; init; } = [];

    /// <summary>
    /// Properties in this type.
    /// </summary>
    public IReadOnlyList<PropertyInfo> Properties { get; init; } = [];

    /// <summary>
    /// Test methods in this type.
    /// </summary>
    public IReadOnlyList<TestInfo> Tests { get; init; } = [];

    /// <summary>
    /// Line number where type is declared.
    /// </summary>
    public int LineNumber { get; init; }
}

/// <summary>
/// Kind of type declaration.
/// </summary>
public enum TypeKind
{
    Class,
    Interface,
    Enum,
    Struct,
    Record,
    RecordStruct,
    Delegate
}

/// <summary>
/// Access modifier.
/// </summary>
public enum AccessModifier
{
    Public,
    Internal,
    Protected,
    Private,
    ProtectedInternal,
    PrivateProtected
}

/// <summary>
/// Represents a method.
/// </summary>
public record MethodInfo(
    string Name,
    string? ReturnType,
    IReadOnlyList<string> Parameters,
    AccessModifier AccessModifier,
    int LineNumber
);

/// <summary>
/// Represents a property.
/// </summary>
public record PropertyInfo(
    string Name,
    string? Type,
    AccessModifier AccessModifier,
    bool HasGetter,
    bool HasSetter,
    int LineNumber
);

/// <summary>
/// Represents a test method.
/// </summary>
public record TestInfo
{
    /// <summary>
    /// Test method name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Class containing the test.
    /// </summary>
    public string? ClassName { get; init; }

    /// <summary>
    /// Full name (ClassName.MethodName).
    /// </summary>
    public string FullName => string.IsNullOrEmpty(ClassName) ? Name : $"{ClassName}.{Name}";

    /// <summary>
    /// Test framework.
    /// </summary>
    public TestFramework Framework { get; init; } = TestFramework.XUnit;

    /// <summary>
    /// Test attributes (Fact, Theory, etc.).
    /// </summary>
    public IReadOnlyList<string> Attributes { get; init; } = [];

    /// <summary>
    /// Line number.
    /// </summary>
    public int LineNumber { get; init; }
}

/// <summary>
/// Test framework enumeration.
/// </summary>
public enum TestFramework
{
    XUnit,
    NUnit,
    MSTest,
    Unknown
}
